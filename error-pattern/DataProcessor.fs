module DataProcessor


open Docker.DotNet
open DockerController
open FSharp.Data
open System.IO
open Types


/// <summary>
/// TODO
/// </summary>
/// <param name="fileNameWithTimestamp">TODO</param>
let getTimestamp (fileNameWithTimestamp: string): string =
    let timestampOption: string option =
        fileNameWithTimestamp.Split "-"
        |> Array.tryHead
    match timestampOption with
    | Some timestamp -> timestamp
    | None -> failwith $"Failed to retrieve timestamp from: %s{fileNameWithTimestamp}"


/// <summary>
/// TODO
/// </summary>
/// <param name="fileNameWithTimestamp">TODO</param>
let getFileNameWithoutTimestamp (fileNameWithTimestamp: string): string =
    let indexOfSeparator: int = fileNameWithTimestamp.IndexOf "-"
    if indexOfSeparator = -1 then
        failwith $"Failed to retrieve file name from: %s{fileNameWithTimestamp}"
    fileNameWithTimestamp.Substring (indexOfSeparator + 1)


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getSnapshotTimestamps (taskInfo: TaskInfo) (groupAndTeamId: string) : string seq =
    let path: string = Path.Combine (RootPath, "data", "Tests", $"%s{taskInfo.ExerciseId}.csv")
    let csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()

    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"

    csvFile.Filter (fun (row: CsvRow) ->
        row.GetColumn "SHEET" = taskInfo.SheetId
        && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
        && row.GetColumn "GROUPID" = groupId
        && row.GetColumn "TEAMID" = teamId
    )
    |> fun (filtered: Runtime.CsvFile<CsvRow>) -> filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "SNAPSHOT_TIMESTAMP")


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
/// <param name="snapshotTimestamp">TODO</param>
let getDeletedFiles (taskInfo: TaskInfo) (groupAndTeamId: string) (snapshotTimestamp: string): string seq =
    let path: string = Path.Combine (RootPath, "data", "Removed", $"{taskInfo.ExerciseId}.csv")
    let csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()

    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"

    csvFile.Filter (fun (row: CsvRow) ->
           row.GetColumn "SHEET" = taskInfo.SheetId
        && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
        && row.GetColumn "GROUPID" = groupId
        && row.GetColumn "TEAMID" = teamId
        && row.GetColumn "DELETE_TIMESTAMP" <= snapshotTimestamp
    )
    |> fun (filtered: Runtime.CsvFile<CsvRow>) -> filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "PHYSICAL_FILENAME")


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getAllSnapshots (taskInfo: TaskInfo) (groupAndTeamId: string): (string * string list) seq =
    getSnapshotTimestamps taskInfo groupAndTeamId
    // get all relevant submissions
    |> Seq.map (fun (snapshotTimestamp: string) ->
        let relevantFiles: string array =
            groupAndTeamId
            |> taskInfo.GetSubmissionsPath
            |> Directory.GetFiles
            |> Array.filter (fun (submission: string) -> getTimestamp (submission |> Path.GetFileName) <= snapshotTimestamp)
        snapshotTimestamp, relevantFiles
    )
    // remove all files that were deleted before the snapshot timestamp
    |> Seq.map (fun (snapshotTimestamp: string, submissions: string array) ->
        let filteredSubmissions: string array =
            submissions
            |> Array.filter (fun (submission: string) ->
                getDeletedFiles taskInfo groupAndTeamId snapshotTimestamp
                |> Seq.contains (submission |> Path.GetFileName)
                |> not
            )
        snapshotTimestamp, filteredSubmissions
    )
    // only take the latest version of each file
    |> Seq.map (fun (snapshotTimestamp: string, submissions: string array) ->
        let onlyLatestFilesAsList: string list =
            submissions
            |> Array.groupBy getFileNameWithoutTimestamp
            |> Array.map (fun (_: string, group: string array) -> group |> Array.maxBy getTimestamp)
            |> Array.toList
        snapshotTimestamp, onlyLatestFilesAsList
    )


/// <summary>
/// Process a specific group and team.
/// </summary>
/// <param name="taskInfo">The task to process.</param>
/// <param name="groupAndTeamId">The specific group and team.</param>
let processGroupAndTeam (taskInfo: TaskInfo) (groupAndTeamId: string): unit =
    if Directory.Exists (taskInfo.GetSubmissionsPath groupAndTeamId) then
        use dockerClientConfiguration: DockerClientConfiguration = new DockerClientConfiguration ()
        use dockerClient: DockerClient = dockerClientConfiguration.CreateClient ()

        let imageId: string = "meyerhoefer/fsharpdev:latest"
        let workingDirectory: string = "/home/coder/Error-Pattern/"
        let command: string = "dotnet"
        let xunitPackageArguments: string array = [| "add"; "package"; "XunitXml.TestLogger"; "--version"; "4.1.0" |]
        let containerBuildResultsPath: string = $"%s{workingDirectory}buildResults.log"
        let buildArguments: string array = [| "build"; taskInfo.ProjectFileName; $"-flp:\"Summary;Verbosity=normal;LogFile=%s{containerBuildResultsPath}\"" |]
        let containerTestResultsPath: string = $"%s{workingDirectory}testResults.xml"
        let testArguments: string array = [| "test"; taskInfo.ProjectFileName; "-l"; $"\"xunit;LogFilePath=%s{containerTestResultsPath}\"" |]
        let stacktracePath: string = taskInfo.GetGroupAndTeamStacktracePath groupAndTeamId

        getAllSnapshots taskInfo groupAndTeamId
        |> Seq.iter (fun (snapshotTimestamp: string, submissions: string list) ->
            // snapshotTimestamp <=> containerId
            let createAndRunOperation: bool Async = createAndRunContainer dockerClient imageId snapshotTimestamp taskInfo submissions
            let createAndRunFailureMessage: string = "Failed to create and run container."

            let addXunitTestLoggerOperation: bool Async = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command xunitPackageArguments
            let addXunitTestLoggerFailureMessage: string = "Failed to add 'Xunit.TestLogger' package."

            let dotnetBuildOperation: bool Async = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command buildArguments
            let _dotnetBuildFailureMessage: string = "Failed to run 'dotnet build'."

            let hostBuildResultsPath: string = Path.Combine (stacktracePath, snapshotTimestamp, $"%s{snapshotTimestamp}-buildResults.log")
            let extractBuildResultsOperation: bool Async = copyFilesFromContainer dockerClient snapshotTimestamp containerBuildResultsPath hostBuildResultsPath
            let extractBuildResultsFailureMessage: string = "Failed to extract build results from container."

            let dotnetTestOperation: bool Async = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command testArguments
            let _dotnetTestFailureMessage: string = "Failed to run 'dotnet test'."

            let hostTestResultsPath: string = Path.Combine (stacktracePath, snapshotTimestamp, $"%s{snapshotTimestamp}-testResults.xml")
            let extractTestResultsOperation: bool Async = copyFilesFromContainer dockerClient snapshotTimestamp containerTestResultsPath hostTestResultsPath
            let extractTestResultsFailureMessage: string = "Failed to extract test results from container."

            let stopAndRemoveOperation: bool Async = stopAndRemoveContainer dockerClient snapshotTimestamp
            let stopAndRemoveFailureMessage: string = "Failed to stop and remove container."

            let log (message: string): unit =
                let outputLogPath: string = Path.Combine (taskInfo.GetStacktracePath (), "output.log")
                let logMessage: string = $"TaskInfo: %A{taskInfo}\nGroupAndTeamId: %s{groupAndTeamId}\nSubmissions: %A{submissions}\nMessage: %s{message}\n\n"
                File.AppendAllText (outputLogPath, logMessage)

            Path.Combine (stacktracePath, snapshotTimestamp)
            |> Directory.CreateDirectory
            |> ignore

            try
                if not (createAndRunOperation |> Async.RunSynchronously) then log createAndRunFailureMessage
                elif not (dotnetBuildOperation |> Async.RunSynchronously) then
                    if not (extractBuildResultsOperation |> Async.RunSynchronously) then log extractBuildResultsFailureMessage
                    elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
                    else log "Finished after failed build."
                elif not (extractBuildResultsOperation |> Async.RunSynchronously) then log extractBuildResultsFailureMessage
                elif taskInfo.ExerciseId <> "08" && taskInfo.AssignmentId <> "2" then
                    if not (addXunitTestLoggerOperation |> Async.RunSynchronously) then log addXunitTestLoggerFailureMessage
                    elif not (dotnetTestOperation |> Async.RunSynchronously) then
                        if not (extractTestResultsOperation |> Async.RunSynchronously) then log extractTestResultsFailureMessage
                        elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
                        log "Finished after failed test."
                    elif not (extractTestResultsOperation |> Async.RunSynchronously) then log extractTestResultsFailureMessage
                    elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
                    else log "Finished after successful test."
                elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
                else log "Finished after successful build."
            with
            | ex ->
                if not (stopAndRemoveOperation |> Async.RunSynchronously) then log $"%s{ex.Message}\n%s{stopAndRemoveFailureMessage}"
                else log ex.Message
        )


/// <summary>
/// Process data for a single task.
/// </summary>
/// <param name="taskInfo">The task to process.</param>
let processTask (taskInfo: TaskInfo): unit =
    taskInfo.GetGroupAndTeamIds ()
    |> Array.iter (processGroupAndTeam taskInfo)


/// <summary>
/// Process the whole data set.
/// </summary>
/// <param name="relevantTasks">The array of relevant tasks in the data set.</param>
let processData (relevantTasks: TaskInfo list): unit =
    relevantTasks
    |> List.iter processTask


// EOF