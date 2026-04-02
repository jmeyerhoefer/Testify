namespace GdP23

module TaskPipeline =
    open CsvParsing
    open System
    open System.Collections.Generic
    open System.Diagnostics
    open System.Globalization
    open System.IO
    open System.Security.Cryptography
    open System.Threading
    open System.Text
    open System.Text.Json
    open System.Text.RegularExpressions

    type SnapshotDataset =
        {
            SnapshotRecordsByKey: IDictionary<string * string * string, SnapshotRecord list>
            RemovedFilesByKey: IDictionary<string * string * string, RemovedRecord list>
        }

    type ProcessResult =
        {
            ExitCode: int
            StandardOutput: string
            StandardError: string
        }

    [<Literal>]
    let private DockerCliTimeoutMs = 30000

    [<Literal>]
    let private DockerInspectTimeoutMs = 10000

    [<Literal>]
    let private DockerContainerTimeoutMinutes = 3.0

    let private jsonOptions = JsonSerializerOptions(WriteIndented = true)
    let private logGate = obj ()

    let private rewrittenTaskSpecs =
        [
            "02", "4", "Zahlen.fsproj"
            "03", "2", "Peano.fsproj"
            "03", "3", "Leibniz.fsproj"
            "04", "2", "Dates.fsproj"
        ]

    let log (message: string) : unit =
        let timestamp = DateTimeOffset.Now.ToString("u", CultureInfo.InvariantCulture)
        lock logGate (fun () -> printfn "[%s] %s" timestamp message)

    let ensureDirectory (path: string) : unit =
        Directory.CreateDirectory(path) |> ignore

    let writeJson (path: string) (value: 'T) : unit =
        let directory = Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace(directory)) then
            ensureDirectory directory

        File.WriteAllText(path, JsonSerializer.Serialize(value, jsonOptions), Encoding.UTF8)

    let getTimestamp (fileNameWithTimestamp: string) : string =
        match fileNameWithTimestamp.Split("-", 2, StringSplitOptions.None) |> Array.tryHead with
        | Some timestamp when not (String.IsNullOrWhiteSpace timestamp) -> timestamp
        | _ -> failwith $"Could not extract timestamp from '%s{fileNameWithTimestamp}'."

    let getLogicalName (fileNameWithTimestamp: string) : string =
        let separatorIndex = fileNameWithTimestamp.IndexOf("-")

        if separatorIndex < 0 || separatorIndex = fileNameWithTimestamp.Length - 1 then
            failwith $"Could not extract logical name from '%s{fileNameWithTimestamp}'."

        fileNameWithTimestamp.Substring(separatorIndex + 1)

    let private methodNamePattern =
        Regex(@"member\s+.+?\.\`\`(?<name>.+?)\`\`\s*\(", RegexOptions.Compiled)

    let private dockerRunGate = obj ()

    let private ensureWorkspaceDirectory (path: string) : unit =
        if path.StartsWith(WorkspacesRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path) then
            Directory.Delete(path, true)

        ensureDirectory path

    let private deleteFileIfExists (path: string) : unit =
        if File.Exists path then
            File.Delete path

    let private deleteDirectoryIfExists (path: string) : unit =
        if Directory.Exists path then
            Directory.Delete(path, true)

    let private pruneEmptyDirectoriesUpTo (stopDirectory: string) (startDirectory: string) : unit =
        let rec loop (currentDirectory: string) =
            if String.IsNullOrWhiteSpace currentDirectory then
                ()
            elif String.Equals(currentDirectory, stopDirectory, StringComparison.OrdinalIgnoreCase) then
                if Directory.Exists currentDirectory && not (Directory.EnumerateFileSystemEntries(currentDirectory) |> Seq.isEmpty) then
                    ()
                elif Directory.Exists currentDirectory then
                    Directory.Delete currentDirectory
            else
                if Directory.Exists currentDirectory && Directory.EnumerateFileSystemEntries(currentDirectory) |> Seq.isEmpty then
                    Directory.Delete currentDirectory
                    let parent = Directory.GetParent(currentDirectory)

                    if not (isNull parent) then
                        loop parent.FullName

        loop startDirectory

    let private loadMethodNames (path: string) : string list =
        File.ReadLines(path)
        |> Seq.choose (fun line ->
            let matchResult = methodNamePattern.Match(line)

            if matchResult.Success then
                Some matchResult.Groups["name"].Value
            else
                None)
        |> Seq.distinct
        |> Seq.toList

    let private loadTemplateNamespace (path: string) : string =
        File.ReadLines(path)
        |> Seq.tryPick (fun line ->
            let trimmed = line.Trim()

            if trimmed.StartsWith("namespace ", StringComparison.Ordinal) then
                Some(trimmed.Substring("namespace ".Length).Trim())
            else
                None)
        |> Option.defaultWith (fun () -> failwith $"Could not find namespace declaration in '%s{path}'.")

    let private loadPropertyMethodNames (path: string) : string list =
        let propertyMarkers =
            [
                "Check.One"
                "|=>"
                "||=>"
                "shouldBeTrueUsing"
                "shouldBeFalseUsing"
            ]

        let flushBlock
            (currentName: string option)
            (currentLines: ResizeArray<string>)
            (propertyMethods: ResizeArray<string>)
            =
            match currentName with
            | Some methodName ->
                let body = String.concat Environment.NewLine currentLines

                if propertyMarkers |> List.exists body.Contains then
                    propertyMethods.Add methodName
            | None -> ()

        let propertyMethods = ResizeArray<string>()
        let currentLines = ResizeArray<string>()
        let mutable currentName: string option = None

        for line in File.ReadLines(path) do
            let matchResult = methodNamePattern.Match(line)

            if matchResult.Success then
                flushBlock currentName currentLines propertyMethods
                currentName <- Some matchResult.Groups["name"].Value
                currentLines.Clear()
                currentLines.Add line
            elif currentName.IsSome then
                currentLines.Add line

        flushBlock currentName currentLines propertyMethods

        propertyMethods
        |> Seq.distinct
        |> Seq.toList

    let private getPrimarySourceFileName (solutionDirectory: string) : string =
        let sourceFiles =
            Directory.GetFiles(solutionDirectory, "*.fs", SearchOption.TopDirectoryOnly)
            |> Array.map Path.GetFileName

        match sourceFiles with
        | [| onlyFile |] -> onlyFile
        | [||] -> failwith $"No source file found in _solution directory '%s{solutionDirectory}'."
        | _ -> failwith $"Expected exactly one source file in _solution directory '%s{solutionDirectory}', but found %d{sourceFiles.Length}."

    let private createTaskInfo (sheetId: string, assignmentId: string, projectFileName: string) : TaskInfo =
        let templateDirectory = Path.Combine(TemplatesRoot, sheetId, assignmentId, "template")
        let solutionDirectory = Path.Combine(TemplatesRoot, sheetId, assignmentId, "_solution")
        let testsPath = Path.Combine(templateDirectory, "Tests.fs")
        let testifyTestsPath = Path.Combine(templateDirectory, "TestifyTests.fs")

        {
            SheetId = sheetId
            AssignmentId = assignmentId
            ProjectFileName = projectFileName
            TemplateNamespace = loadTemplateNamespace testsPath
            TemplateDirectory = templateDirectory
            SolutionDirectory = solutionDirectory
            UploadsSheetDirectory = Path.Combine(UploadsRoot, sheetId)
            ResultsRootDirectory = DockerResultsRoot
            PrimarySourceFileName = getPrimarySourceFileName solutionDirectory
            ExpectedOriginalMethods = loadMethodNames testsPath
            ExpectedTestifyMethods = loadMethodNames testifyTestsPath
            ExpectedOriginalPropertyMethods = loadPropertyMethodNames testsPath
            ExpectedTestifyPropertyMethods = loadPropertyMethodNames testifyTestsPath
        }

    let loadTaskInfos () : TaskInfo list =
        rewrittenTaskSpecs |> List.map createTaskInfo

    let private keyOf (sheetId: string) (assignmentId: string) (groupIdTeamId: string) =
        sheetId, assignmentId, groupIdTeamId

    let private groupIdTeamId (groupId: string) (teamId: string) : string =
        $"{groupId}_{teamId}"

    let loadDataset () : SnapshotDataset =
        let snapshotRows = loadSnapshotRecords(Path.Combine(RootPath, "gdp23-tests.csv"))
        let removedRows = loadRemovedRecords(Path.Combine(RootPath, "gdp23-removed.csv"))

        let snapshotRecordsByKey =
            snapshotRows
            |> Seq.groupBy (fun row -> keyOf row.SheetId row.AssignmentId (groupIdTeamId row.GroupId row.TeamId))
            |> Seq.map (fun (key, rows) ->
                key,
                (rows
                 |> Seq.sortBy (fun row -> row.SnapshotTimestamp)
                 |> Seq.toList))
            |> dict

        let removedFilesByKey =
            removedRows
            |> Seq.groupBy (fun row -> keyOf row.SheetId row.AssignmentId (groupIdTeamId row.GroupId row.TeamId))
            |> Seq.map (fun (key, rows) ->
                key,
                (rows |> Seq.sortBy (fun row -> row.DeleteTimestamp) |> Seq.toList))
            |> dict

        {
            SnapshotRecordsByKey = snapshotRecordsByKey
            RemovedFilesByKey = removedFilesByKey
        }

    let private tryGetHistoricalTestsFailed (snapshot: SnapshotInfo) : int option =
        snapshot.HistoricalRecord
        |> Option.bind (fun record ->
            match record.TestsPassed, record.TestsTotal with
            | Some passed, Some total -> Some(total - passed)
            | _ -> None)

    let private isInterestingExampleSnapshot (snapshot: SnapshotInfo) : bool =
        match snapshot.HistoricalRecord with
        | Some record ->
            record.Compiled
            && not record.InternalError
            && snapshot.SourceFilePresent
            && (
                match record.TestsPassed, record.TestsTotal with
                | Some passed, Some total -> passed < total
                | _ -> false
            )
        | None -> false

    let private selectRepresentativeSnapshotsForTask (options: CliOptions) (snapshots: SnapshotInfo list) : SnapshotInfo list =
        if options.AllSnapshots then
            snapshots |> List.sortBy (fun snapshot -> snapshot.GroupIdTeamId, snapshot.Timestamp)
        else
            snapshots
            |> List.filter isInterestingExampleSnapshot
            |> List.groupBy (fun snapshot -> snapshot.GroupIdTeamId)
            |> List.map (fun (_, groupSnapshots) ->
                groupSnapshots
                |> List.sortByDescending (fun snapshot ->
                    let failingTests = tryGetHistoricalTestsFailed snapshot |> Option.defaultValue -1
                    failingTests, snapshot.Timestamp)
                |> List.head)
            |> List.sortByDescending (fun snapshot ->
                let failingTests = tryGetHistoricalTestsFailed snapshot |> Option.defaultValue -1
                failingTests, snapshot.Timestamp)
            |> fun values ->
                if options.MaxExamplesPerTask > 0 then
                    values |> List.truncate options.MaxExamplesPerTask
                else
                    values

    let getSnapshots (dataset: SnapshotDataset) (taskInfo: TaskInfo) (options: CliOptions) : SnapshotInfo list =
        let groupTeamDirectories =
            if Directory.Exists(taskInfo.UploadsSheetDirectory) then
                Directory.GetDirectories(taskInfo.UploadsSheetDirectory)
                |> Array.map Path.GetFileName
                |> Array.toList
            else
                []

        groupTeamDirectories
        |> List.filter (fun groupTeam ->
            match options.GroupIdTeamId with
            | Some expected -> String.Equals(groupTeam, expected, StringComparison.OrdinalIgnoreCase)
            | None -> true)
        |> List.collect (fun groupTeam ->
            let key = keyOf taskInfo.SheetId taskInfo.AssignmentId groupTeam

            let snapshotRecords =
                match dataset.SnapshotRecordsByKey.TryGetValue key with
                | true, values -> values
                | false, _ -> []

            let removedRows =
                match dataset.RemovedFilesByKey.TryGetValue key with
                | true, values -> values
                | false, _ -> []

            snapshotRecords
            |> List.filter (fun record ->
                match options.Timestamp with
                | Some expected -> record.SnapshotTimestamp = expected
                | None -> true)
            |> List.map (fun record ->
                let timestamp = record.SnapshotTimestamp
                let submissionsDirectory = taskInfo.SubmissionsDirectory(groupTeam)

                let submissionFiles =
                    if Directory.Exists submissionsDirectory then
                        Directory.GetFiles submissionsDirectory
                    else
                        [||]

                let deletedAtSnapshot =
                    removedRows
                    |> List.takeWhile (fun row -> row.DeleteTimestamp <= timestamp)
                    |> List.map (fun row -> row.PhysicalFileName)
                    |> fun values -> HashSet<string>(values)

                let files =
                    submissionFiles
                    |> Array.filter (fun path ->
                        let fileName = Path.GetFileName path
                        getTimestamp fileName <= timestamp
                        && not (deletedAtSnapshot.Contains fileName))
                    |> Array.groupBy (fun path -> getLogicalName(Path.GetFileName path))
                    |> Array.map (fun (logicalName, group) ->
                        let latest = group |> Array.maxBy (fun path -> getTimestamp(Path.GetFileName path))
                        {
                            LogicalName = logicalName
                            PhysicalPath = latest
                            UploadedAt = latest |> Path.GetFileName |> getTimestamp
                        })
                    |> Array.sortBy (fun file -> file.LogicalName)
                    |> Array.toList

                {
                    Task = taskInfo
                    GroupIdTeamId = groupTeam
                    Timestamp = timestamp
                    Files = files
                    SourceFilePresent = files |> List.exists (fun file -> file.LogicalName = taskInfo.PrimarySourceFileName)
                    HistoricalRecord = Some record
                }))
        |> selectRepresentativeSnapshotsForTask options

    let private ensureCleanDirectory (path: string) : unit =
        if path.StartsWith(DockerResultsRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path) then
            Directory.Delete(path, true)

        ensureDirectory path

    let private copyDirectory (sourceDirectory: string) (targetDirectory: string) (shouldCopy: string -> bool) : unit =
        ensureDirectory targetDirectory

        for filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories) do
            let relativePath = Path.GetRelativePath(sourceDirectory, filePath)

            if shouldCopy relativePath then
                let targetPath = Path.Combine(targetDirectory, relativePath)
                let targetParent = Path.GetDirectoryName(targetPath)

                if not (String.IsNullOrWhiteSpace targetParent) then
                    ensureDirectory targetParent

                File.Copy(filePath, targetPath, true)

    let private copyTestifySource (workspaceDirectory: string) : unit =
        let targetDirectory = Path.Combine(workspaceDirectory, TestifySourceDirectoryName)

        copyDirectory RepoRoot targetDirectory (fun relativePath ->
            relativePath = ".gitignore"
            || relativePath = "Mini.fs"
            || relativePath = "Testify.fsproj"
            || relativePath.StartsWith("Testify\\", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("Testify/", StringComparison.OrdinalIgnoreCase))

    let private isProtectedHarnessName (taskInfo: TaskInfo) (logicalName: string) : bool =
        logicalName = "Tests.fs"
        || logicalName = "TestifyTests.fs"
        || logicalName = taskInfo.ProjectFileName

    let private ensureNonZero (value: uint64) : uint64 =
        if value = 0UL then 1UL else value

    let private ensureOddNonZero (value: uint64) : uint64 =
        let nonZero = ensureNonZero value
        nonZero ||| 1UL

    let private readUInt64LittleEndian (bytes: byte[]) (offset: int) : uint64 =
        [ 0 .. 7 ]
        |> List.fold (fun state index -> state ||| ((uint64 bytes[offset + index]) <<< (8 * index))) 0UL

    let private createReplayEntry (snapshot: SnapshotInfo) (methodName: string) : ReplayEntry =
        let identity =
            String.concat
                "|"
                [
                    snapshot.Task.SheetId
                    snapshot.Task.AssignmentId
                    snapshot.GroupIdTeamId
                    snapshot.Timestamp
                    methodName
                ]

        let hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes identity)
        let seed = readUInt64LittleEndian hashBytes 0 |> ensureNonZero
        let gamma = readUInt64LittleEndian hashBytes 8 |> ensureOddNonZero

        {
            MethodName = methodName
            ReplayText = $"Rnd={seed},{gamma}; Size=None"
        }

    let private generateReplayCatalogContents (snapshot: SnapshotInfo) (entries: ReplayEntry list) : string =
        let matchCases =
            match entries with
            | [] -> "        | _ -> None"
            | values ->
                values
                |> List.map (fun entry ->
                    let methodLiteral = sprintf "%A" entry.MethodName
                    let replayLiteral = sprintf "%A" entry.ReplayText

                    $"""        | {methodLiteral} ->
            Testify.CheckConfig.tryParseReplay {replayLiteral}""")
                |> String.concat Environment.NewLine

        $"""namespace {snapshot.Task.TemplateNamespace}

module ReplayCatalog =
    let tryGetReplay (methodName: string) : FsCheck.Replay option =
        match methodName with
{matchCases}
        | _ -> None

    let applyReplay (methodName: string) (config: FsCheck.Config) : FsCheck.Config =
        match tryGetReplay methodName with
        | Some replay -> config.WithReplay(Some replay)
        | None -> config
"""

    let private generateProjectFileContents (taskInfo: TaskInfo) (workspaceDirectory: string) : string =
        let sourceFiles =
            Directory.GetFiles(workspaceDirectory, "*", SearchOption.TopDirectoryOnly)
            |> Array.map Path.GetFileName
            |> Array.filter (fun fileName ->
                let extension = Path.GetExtension(fileName).ToLowerInvariant()
                (extension = ".fs" || extension = ".fsi")
                && fileName <> "Tests.fs"
                && fileName <> "TestifyTests.fs")
            |> Array.sortBy (fun fileName ->
                let extensionRank =
                    match Path.GetExtension(fileName).ToLowerInvariant() with
                    | ".fsi" -> 0
                    | _ -> 1

                extensionRank, fileName.ToLowerInvariant())
            |> Array.toList

        let compileItems =
            sourceFiles @ [ "Tests.fs"; "TestifyTests.fs" ]
            |> List.map (fun fileName -> $"\t\t<Compile Include=\"{fileName}\" />")
            |> String.concat Environment.NewLine

        $"""<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>false</ImplicitUsings>
		<Nullable>disable</Nullable>
		<GenerateProgramFile>false</GenerateProgramFile>
		<IsTestProject>true</IsTestProject>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="{TestifySourceDirectoryName}\Testify.fsproj" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.7.2" />
		<PackageReference Include="MSTest.TestAdapter" Version="3.10.4" />
		<PackageReference Include="MSTest.TestFramework" Version="3.10.4" />
		<PackageReference Update="FSharp.Core" Version="10.0.103" />
		<PackageReference Include="FsCheck" Version="3.3.2" />
		<PackageReference Include="Unquote" Version="7.0.1" />
	</ItemGroup>

	<ItemGroup>
{compileItems}
	</ItemGroup>
</Project>
"""

    let materializeWorkspace (snapshot: SnapshotInfo) (force: bool) : WorkspaceManifest =
        let resultDirectory = snapshot.ResultDirectory
        let workspaceDirectory =
            Path.Combine(WorkspacesRoot, snapshot.Task.SheetId, snapshot.GroupIdTeamId, snapshot.Task.AssignmentId, snapshot.Timestamp)

        let projectFilePath = Path.Combine(workspaceDirectory, snapshot.Task.ProjectFileName)
        let replayCatalogPath = Path.Combine(workspaceDirectory, "ReplayCatalog.fs")

        if force then
            ensureCleanDirectory resultDirectory
            ensureWorkspaceDirectory workspaceDirectory
        else
            ensureDirectory resultDirectory

        if force || not (Directory.Exists workspaceDirectory) then
            ensureWorkspaceDirectory workspaceDirectory
            copyDirectory snapshot.Task.TemplateDirectory workspaceDirectory (fun relativePath ->
                not (relativePath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)))
            copyTestifySource workspaceDirectory
            File.WriteAllText(projectFilePath, generateProjectFileContents snapshot.Task workspaceDirectory, Encoding.UTF8)

        let includedUploads = ResizeArray<MaterializedUpload>()
        let ignoredHarnessFiles = ResizeArray<HarnessConflict>()

        for file in snapshot.Files do
            if isProtectedHarnessName snapshot.Task file.LogicalName then
                ignoredHarnessFiles.Add({ LogicalName = file.LogicalName; SourcePath = file.PhysicalPath })
            else
                let destinationPath = Path.Combine(workspaceDirectory, file.LogicalName)
                let destinationParent = Path.GetDirectoryName destinationPath

                if not (String.IsNullOrWhiteSpace destinationParent) then
                    ensureDirectory destinationParent

                File.Copy(file.PhysicalPath, destinationPath, true)
                includedUploads.Add({ LogicalName = file.LogicalName; SourcePath = file.PhysicalPath; RelativePath = file.LogicalName })

        let replayEntries =
            snapshot.Task.PairedPropertyMethodNames
            |> List.map (createReplayEntry snapshot)

        File.WriteAllText(replayCatalogPath, generateReplayCatalogContents snapshot replayEntries, Encoding.UTF8)
        File.WriteAllText(projectFilePath, generateProjectFileContents snapshot.Task workspaceDirectory, Encoding.UTF8)

        writeJson
            (Path.Combine(resultDirectory, "replays.json"))
            {| entries =
                   replayEntries
                   |> List.map (fun entry ->
                       {| methodName = entry.MethodName
                          replay = entry.ReplayText |}) |}

        let workspaceFiles =
            Directory.GetFiles(workspaceDirectory, "*", SearchOption.AllDirectories)
            |> Array.map (fun filePath -> Path.GetRelativePath(workspaceDirectory, filePath))
            |> Array.sort
            |> Array.toList

        let manifest =
            {
                Snapshot = snapshot
                WorkspaceDirectory = workspaceDirectory
                ProjectFilePath = projectFilePath
                ReplayCatalogPath = replayCatalogPath
                ReplayEntries = replayEntries
                IncludedUploads = includedUploads |> Seq.toList
                IgnoredHarnessFiles = ignoredHarnessFiles |> Seq.toList
                WorkspaceFiles = workspaceFiles
            }

        writeJson
            (Path.Combine(resultDirectory, "workspace-manifest.json"))
            {| sheetId = snapshot.Task.SheetId
               assignmentId = snapshot.Task.AssignmentId
               groupIdTeamId = snapshot.GroupIdTeamId
               timestamp = snapshot.Timestamp
               primarySourceFileName = snapshot.Task.PrimarySourceFileName
               sourceFilePresent = snapshot.SourceFilePresent
               projectFileName = snapshot.Task.ProjectFileName
               workspaceDirectory = workspaceDirectory
               projectFilePath = projectFilePath
               replayCatalogPath = replayCatalogPath
               replayEntries =
                   manifest.ReplayEntries
                   |> List.map (fun entry ->
                       {| methodName = entry.MethodName
                          replay = entry.ReplayText |})
               includedUploads =
                   manifest.IncludedUploads
                   |> List.map (fun file ->
                       {| logicalName = file.LogicalName
                          sourcePath = file.SourcePath
                          relativePath = file.RelativePath |})
               ignoredHarnessFiles =
                   manifest.IgnoredHarnessFiles
                   |> List.map (fun file ->
                       {| logicalName = file.LogicalName
                          sourcePath = file.SourcePath |}) |}

        writeJson
            (Path.Combine(resultDirectory, "workspace-files.json"))
            {| files =
                   workspaceFiles
                   |> List.map (fun relativePath ->
                       let absolutePath = Path.Combine(workspaceDirectory, relativePath)
                       let fileInfo = FileInfo absolutePath
                       {| relativePath = relativePath; length = fileInfo.Length |}) |}

        manifest

    let runProcessWithTimeout (workingDirectory: string) (fileName: string) (arguments: string list) (timeoutMs: int option) : ProcessResult =
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- fileName
        startInfo.WorkingDirectory <- workingDirectory
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.CreateNoWindow <- true

        for argument in arguments do
            startInfo.ArgumentList.Add(argument)

        use proc = new Process()
        proc.StartInfo <- startInfo

        if not (proc.Start()) then
            failwith $"Failed to start process '%s{fileName}'."

        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()
        let exited =
            match timeoutMs with
            | Some value when value > 0 -> proc.WaitForExit value
            | _ ->
                proc.WaitForExit()
                true

        if not exited then
            try
                proc.Kill(true)
            with
            | _ -> ()

            try
                proc.WaitForExit()
            with
            | _ -> ()

        System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask)

        if not exited then
            let commandText = String.concat " " (fileName :: arguments)
            failwith $"Process timed out after {timeoutMs.Value} ms: {commandText}{Environment.NewLine}{stderrTask.Result}"

        {
            ExitCode = proc.ExitCode
            StandardOutput = stdoutTask.Result
            StandardError = stderrTask.Result
        }

    let runProcess (workingDirectory: string) (fileName: string) (arguments: string list) : ProcessResult =
        runProcessWithTimeout workingDirectory fileName arguments None

    let ensureDockerImage () : unit =
        let inspectResult = runProcess RootPath "docker" [ "image"; "inspect"; DockerImageTag ]

        if inspectResult.ExitCode <> 0 then
            log $"Building Docker image '%s{DockerImageTag}'."

            let buildResult =
                runProcess RootPath "docker" [ "build"; "--tag"; DockerImageTag; "--file"; Path.Combine(RootPath, "Dockerfile"); RootPath ]

            if buildResult.ExitCode <> 0 then
                failwith $"Failed to build Docker image '%s{DockerImageTag}'.\n%s{buildResult.StandardError}"

    let private tryReadIntProperty (documentPath: string) (propertyName: string) : int option =
        if not (File.Exists documentPath) then
            None
        else
            use document = JsonDocument.Parse(File.ReadAllText(documentPath, Encoding.UTF8))
            let mutable value = Unchecked.defaultof<JsonElement>

            if document.RootElement.TryGetProperty(propertyName, &value) then
                match value.ValueKind with
                | JsonValueKind.Number -> Some(value.GetInt32())
                | JsonValueKind.Null -> None
                | _ -> None
            else
                None

    let private sanitizeContainerNameSegment (value: string) : string =
        Regex("[^a-zA-Z0-9_.-]", RegexOptions.Compiled).Replace(value, "-").ToLowerInvariant()

    let private tryGetContainerStateValue (containerName: string) (format: string) : string option =
        let inspectResult = runProcessWithTimeout RootPath "docker" [ "inspect"; "--format"; format; containerName ] (Some DockerInspectTimeoutMs)

        if inspectResult.ExitCode = 0 then
            inspectResult.StandardOutput.Trim()
            |> fun value -> if String.IsNullOrWhiteSpace value then None else Some value
        else
            None

    let private removeContainerIfPresent (containerName: string) : unit =
        let _ = runProcessWithTimeout RootPath "docker" [ "rm"; "-f"; containerName ] (Some DockerCliTimeoutMs)
        ()

    let cleanupSnapshotArtifacts (snapshot: SnapshotInfo) : unit =
        let resultDirectory = snapshot.ResultDirectory
        let workspaceDirectory =
            Path.Combine(WorkspacesRoot, snapshot.Task.SheetId, snapshot.GroupIdTeamId, snapshot.Task.AssignmentId, snapshot.Timestamp)

        deleteDirectoryIfExists workspaceDirectory

        for filePath in
            [
                Path.Combine(resultDirectory, "build.log")
                Path.Combine(resultDirectory, "test-results.xml")
                Path.Combine(resultDirectory, "docker-run.log")
                Path.Combine(resultDirectory, "container-metadata.json")
                Path.Combine(resultDirectory, "workspace-manifest.json")
                Path.Combine(resultDirectory, "workspace-files.json")
            ] do
            deleteFileIfExists filePath

        for directoryPath in Directory.GetDirectories(resultDirectory) do
            deleteDirectoryIfExists directoryPath

        let workspaceAssignmentDirectory =
            Path.Combine(WorkspacesRoot, snapshot.Task.SheetId, snapshot.GroupIdTeamId, snapshot.Task.AssignmentId)

        pruneEmptyDirectoriesUpTo workspaceAssignmentDirectory workspaceDirectory

    let runSnapshotInDocker (manifest: WorkspaceManifest) : RawRunArtifacts =
        log $"Waiting for Docker slot for {manifest.Snapshot.Task.DisplayName} / {manifest.Snapshot.GroupIdTeamId} / {manifest.Snapshot.Timestamp}"

        lock dockerRunGate (fun () ->
            log $"Acquired Docker slot for {manifest.Snapshot.Task.DisplayName} / {manifest.Snapshot.GroupIdTeamId} / {manifest.Snapshot.Timestamp}"
            let resultDirectory = manifest.Snapshot.ResultDirectory
            let buildLogPath = Path.Combine(resultDirectory, "build.log")
            let testResultsPath = Path.Combine(resultDirectory, "test-results.xml")
            let testifyResultsDirectory = Path.Combine(resultDirectory, "testify-results")
            let runMetadataPath = Path.Combine(resultDirectory, "run-metadata.json")
            let containerMetadataPath = Path.Combine(resultDirectory, "container-metadata.json")
            let dockerRunLogPath = Path.Combine(resultDirectory, "docker-run.log")

            ensureDirectory DockerCacheRoot
            ensureDirectory DockerNuGetPackagesHostPath
            ensureDirectory DockerNuGetHttpCacheHostPath
            ensureDirectory DockerCliHomeHostPath

            if Directory.Exists testifyResultsDirectory then
                Directory.Delete(testifyResultsDirectory, true)

            for path in [ buildLogPath; testResultsPath; containerMetadataPath; dockerRunLogPath ] do
                if File.Exists path then
                    File.Delete path

            let projectFileName = Path.GetFileName manifest.ProjectFilePath

            let shellScript =
                String.concat
                    "\n"
                    [
                        "set +e"
                        "mkdir -p /results/testify-results"
                        $"dotnet build \"{projectFileName}\" \"-flp:Summary;Verbosity=normal;LogFile={DockerResultsPath}/build.log\""
                        "build_exit=$?"
                        "test_exit=null"
                        "if [ \"$build_exit\" -eq 0 ]; then"
                        $"  dotnet test \"{projectFileName}\" --no-build --results-directory \"{DockerResultsPath}\" --logger \"trx;LogFileName=test-results.xml\""
                        "  test_exit=$?"
                        "fi"
                        "printf '{\"buildExitCode\":%s,\"testExitCode\":%s}\\n' \"$build_exit\" \"$test_exit\" > /results/container-metadata.json"
                        "exit 0"
                    ]

            let startedAtUtc = DateTimeOffset.UtcNow
            let containerName =
                [
                    "gdp23"
                    manifest.Snapshot.Task.SheetId
                    manifest.Snapshot.Task.AssignmentId
                    manifest.Snapshot.GroupIdTeamId
                    manifest.Snapshot.Timestamp
                    Guid.NewGuid().ToString("N").Substring(0, 8)
                ]
                |> List.map sanitizeContainerNameSegment
                |> String.concat "-"

            removeContainerIfPresent containerName

            log $"Starting container {containerName} for {manifest.Snapshot.Task.DisplayName} / {manifest.Snapshot.GroupIdTeamId} / {manifest.Snapshot.Timestamp}"

            let dockerStartResult =
                runProcessWithTimeout
                    resultDirectory
                    "docker"
                    [
                        "run"
                        "--detach"
                        "--name"
                        containerName
                        "--mount"
                        $"type=bind,source={manifest.WorkspaceDirectory},target={DockerWorkspacePath}"
                        "--mount"
                        $"type=bind,source={resultDirectory},target={DockerResultsPath}"
                        "--mount"
                        $"type=bind,source={DockerNuGetPackagesHostPath},target={DockerNuGetPackagesPath}"
                        "--mount"
                        $"type=bind,source={DockerNuGetHttpCacheHostPath},target={DockerNuGetHttpCachePath}"
                        "--mount"
                        $"type=bind,source={DockerCliHomeHostPath},target={DockerCliHomePath}"
                        "--workdir"
                        DockerWorkspacePath
                        "--env"
                        $"TESTIFY_RESULT_ROOT={DockerResultsPath}/testify-results"
                        "--env"
                        $"NUGET_PACKAGES={DockerNuGetPackagesPath}"
                        "--env"
                        $"NUGET_HTTP_CACHE_PATH={DockerNuGetHttpCachePath}"
                        "--env"
                        $"DOTNET_CLI_HOME={DockerCliHomePath}"
                        "--env"
                        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
                        "--env"
                        "DOTNET_CLI_TELEMETRY_OPTOUT=1"
                        "--env"
                        "DOTNET_NOLOGO=1"
                        DockerImageTag
                        "sh"
                        "-lc"
                        shellScript
                    ]
                    (Some DockerCliTimeoutMs)

            if dockerStartResult.ExitCode <> 0 then
                failwith $"Failed to start Docker container for {manifest.Snapshot.Task.DisplayName} / {manifest.Snapshot.GroupIdTeamId} / {manifest.Snapshot.Timestamp}.{Environment.NewLine}{dockerStartResult.StandardError}"

            let timeoutAtUtc = startedAtUtc.AddMinutes(DockerContainerTimeoutMinutes)
            let mutable lastProgressLogAtUtc = DateTimeOffset.MinValue

            let rec waitForContainerCompletion () : string option =
                let metadataExists = File.Exists containerMetadataPath
                let status = tryGetContainerStateValue containerName "{{.State.Status}}"

                if metadataExists && status <> Some "running" then
                    status
                elif DateTimeOffset.UtcNow >= timeoutAtUtc then
                    failwith $"Timed out while waiting for Docker container '{containerName}' to finish."
                else
                    let now = DateTimeOffset.UtcNow

                    if now - lastProgressLogAtUtc >= TimeSpan.FromSeconds(10.0) then
                        lastProgressLogAtUtc <- now
                        let stateText = status |> Option.defaultValue "unknown"
                        log $"Container {containerName} still running: state={stateText}, metadataReady={metadataExists}"

                    Thread.Sleep(1000)
                    waitForContainerCompletion ()

            let finalContainerStatus =
                try
                    waitForContainerCompletion ()
                finally
                    ()

            let finalStatusText = finalContainerStatus |> Option.defaultValue "unknown"
            log $"Container {containerName} finished with status {finalStatusText}"

            let dockerLogsResult = runProcessWithTimeout resultDirectory "docker" [ "logs"; containerName ] (Some DockerCliTimeoutMs)
            let dockerExitCode =
                tryGetContainerStateValue containerName "{{.State.ExitCode}}"
                |> Option.bind (fun value ->
                    match Int32.TryParse value with
                    | true, parsed -> Some parsed
                    | _ -> None)
                |> Option.defaultValue dockerLogsResult.ExitCode

            removeContainerIfPresent containerName

            let finishedAtUtc = DateTimeOffset.UtcNow

            File.WriteAllText(
                dockerRunLogPath,
                $"START:{Environment.NewLine}{dockerStartResult.StandardOutput}{Environment.NewLine}{Environment.NewLine}LOGS:{Environment.NewLine}{dockerLogsResult.StandardOutput}{Environment.NewLine}{Environment.NewLine}LOGS_STDERR:{Environment.NewLine}{dockerLogsResult.StandardError}",
                Encoding.UTF8)

            writeJson
                runMetadataPath
                {| buildExitCode = tryReadIntProperty containerMetadataPath "buildExitCode"
                   testExitCode = tryReadIntProperty containerMetadataPath "testExitCode"
                   dockerExitCode = dockerExitCode
                   startedAtUtc = startedAtUtc.ToString("O", CultureInfo.InvariantCulture)
                   finishedAtUtc = finishedAtUtc.ToString("O", CultureInfo.InvariantCulture)
                   durationSeconds = (finishedAtUtc - startedAtUtc).TotalSeconds
                   imageTag = DockerImageTag
                   containerName = containerName
                   containerStatus = finalContainerStatus
                   workspaceDirectory = manifest.WorkspaceDirectory
                   resultDirectory = resultDirectory
                   containerMetadataPath = containerMetadataPath
                   buildLogPath = buildLogPath
                   buildLogExists = File.Exists buildLogPath
                   testResultsPath = testResultsPath
                   testResultsExists = File.Exists testResultsPath
                   testifyResultsDirectory = testifyResultsDirectory
                   testifyResultsExists = Directory.Exists testifyResultsDirectory
                   dockerRunLogPath = dockerRunLogPath
                   standardOutput = dockerLogsResult.StandardOutput
                   standardError = dockerLogsResult.StandardError |}

            {
                ResultDirectory = resultDirectory
                BuildLogPath = buildLogPath
                TestResultsPath = testResultsPath
                TestifyResultsDirectory = testifyResultsDirectory
                RunMetadataPath = runMetadataPath
            })

    let hasCompletedSnapshot (snapshot: SnapshotInfo) : bool =
        File.Exists(Path.Combine(snapshot.ResultDirectory, "comparison.json"))
