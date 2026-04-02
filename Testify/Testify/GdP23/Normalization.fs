namespace GdP23

module Normalization =
    open System
    open System.Collections.Generic
    open System.Globalization
    open System.IO
    open System.Text
    open System.Text.Json
    open System.Text.RegularExpressions
    open System.Xml.Linq
    open TaskPipeline

    let private tryReadJsonDocument (path: string) : JsonDocument option =
        if File.Exists path then
            File.ReadAllText(path, Encoding.UTF8)
            |> JsonDocument.Parse
            |> Some
        else
            None

    let private tryGetChildValue (parent: XElement) (childName: string) : string option =
        parent.Elements()
        |> Seq.tryFind (fun element -> element.Name.LocalName = childName)
        |> Option.map _.Value
        |> Option.bind (fun value ->
            if String.IsNullOrWhiteSpace value then
                None
            else
                Some value)

    let private parseDurationSeconds (value: string option) : float option =
        match value with
        | Some rawValue ->
            match TimeSpan.TryParse(rawValue, CultureInfo.InvariantCulture) with
            | true, duration -> Some duration.TotalSeconds
            | _ -> None
        | None -> None

    type private BuildErrorInfo =
        {
            Code: string
            Message: string
            File: string option
            Line: int option
            Column: int option
        }

    let private hintFromOutputPattern =
        Regex(@"(?:^|\r?\n)Hint:\s*(.+?)(?:\r?\n|$)", RegexOptions.Compiled)

    let private buildErrorWithLocationPattern =
        Regex(
            @"^(?<file>.+?)\((?<line>\d+)(?:,(?<column>\d+))?\):\s*error\s+(?<code>FS\d+):\s*(?<message>.*?)(?:\s+\[[^\]]+\])?$",
            RegexOptions.Compiled)

    let private buildErrorWithoutLocationPattern =
        Regex(
            @"^(?:\w+\s*:\s*)?error\s+(?<code>FS\d+):\s*(?<message>.*?)(?:\s+\[[^\]]+\])?$",
            RegexOptions.Compiled)

    let private tryExtractHintFromOutput (output: string) : string option =
        let matchResult = hintFromOutputPattern.Match output

        if matchResult.Success && matchResult.Groups.Count > 1 then
            let hint = matchResult.Groups[1].Value.Trim()
            if String.IsNullOrWhiteSpace hint then None else Some hint
        else
            None

    let private sanitizeBuildErrorText (value: string) : string =
        value.Replace('\u001d', ' ').Replace('\u001c', ' ').Replace('\u001e', ' ').Trim()

    let private sanitizeBuildErrorFile (value: string) : string =
        let trimmed = sanitizeBuildErrorText value
        Regex.Replace(trimmed, @"^\d+>", String.Empty)

    let private tryParseBuildErrorLine (line: string) : BuildErrorInfo option =
        let trimmed = line.Trim()
        let withLocation = buildErrorWithLocationPattern.Match trimmed

        if withLocation.Success then
            Some {
                Code = withLocation.Groups["code"].Value
                Message = sanitizeBuildErrorText withLocation.Groups["message"].Value
                File = Some(sanitizeBuildErrorFile withLocation.Groups["file"].Value)
                Line = Some(Int32.Parse(withLocation.Groups["line"].Value, CultureInfo.InvariantCulture))
                Column =
                    if withLocation.Groups["column"].Success then
                        Some(Int32.Parse(withLocation.Groups["column"].Value, CultureInfo.InvariantCulture))
                    else
                        None
            }
        else
            let withoutLocation = buildErrorWithoutLocationPattern.Match trimmed

            if withoutLocation.Success then
                Some {
                    Code = withoutLocation.Groups["code"].Value
                    Message = sanitizeBuildErrorText withoutLocation.Groups["message"].Value
                    File = None
                    Line = None
                    Column = None
                }
            else
                None

    let private tryParsePrimaryBuildError (path: string) : BuildErrorInfo option =
        if not (File.Exists path) then
            None
        else
            File.ReadLines path
            |> Seq.tryPick tryParseBuildErrorLine

    let private parseTrxResults (path: string) : ParsedTestResult list =
        if not (File.Exists path) then
            []
        else
            let document = XDocument.Load path

            let definitionsById =
                document.Descendants()
                |> Seq.filter (fun element -> element.Name.LocalName = "UnitTest")
                |> Seq.choose (fun unitTest ->
                    let testIdAttribute = unitTest.Attribute(XName.Get "id")

                    if isNull testIdAttribute then
                        None
                    else
                        let className =
                            unitTest.Descendants()
                            |> Seq.tryFind (fun element -> element.Name.LocalName = "TestMethod")
                            |> Option.bind (fun methodElement ->
                                methodElement.Attribute(XName.Get "className")
                                |> Option.ofObj
                                |> Option.map _.Value)
                            |> Option.defaultValue String.Empty

                        Some(testIdAttribute.Value, className))
                |> dict

            document.Descendants()
            |> Seq.filter (fun element -> element.Name.LocalName = "UnitTestResult")
            |> Seq.map (fun resultElement ->
                let testName = resultElement.Attribute(XName.Get "testName").Value
                let testIdAttribute = resultElement.Attribute(XName.Get "testId")
                let className =
                    if isNull testIdAttribute then
                        String.Empty
                    else
                        match definitionsById.TryGetValue testIdAttribute.Value with
                        | true, value -> value
                        | false, _ -> String.Empty

                let suiteName =
                    if className.Contains("TestifyTests", StringComparison.Ordinal) then
                        "Testify"
                    else
                        "Original"

                let outputElement =
                    resultElement.Elements()
                    |> Seq.tryFind (fun element -> element.Name.LocalName = "Output")

                let stdout = outputElement |> Option.bind (fun output -> tryGetChildValue output "StdOut")

                let errorInfo =
                    outputElement
                    |> Option.bind (fun output ->
                        output.Elements()
                        |> Seq.tryFind (fun element -> element.Name.LocalName = "ErrorInfo"))

                let failureSummary = errorInfo |> Option.bind (fun error -> tryGetChildValue error "Message")
                let stackTrace = errorInfo |> Option.bind (fun error -> tryGetChildValue error "StackTrace")

                let output =
                    [ stdout; failureSummary; stackTrace ]
                    |> List.choose id
                    |> function
                        | [] -> None
                        | values -> Some(String.Join(Environment.NewLine + Environment.NewLine, values))

                {
                    SuiteName = suiteName
                    MethodName = testName
                    Outcome = resultElement.Attribute(XName.Get "outcome").Value
                    Output = output
                    Hint = output |> Option.bind tryExtractHintFromOutput
                    FailureSummary = failureSummary
                    DurationSeconds =
                        resultElement.Attribute(XName.Get "duration")
                        |> Option.ofObj
                        |> Option.map _.Value
                        |> parseDurationSeconds
                })
            |> Seq.toList

    let private parseTestifyXmlResults (directoryPath: string) : IDictionary<string, ParsedTestResult> =
        if not (Directory.Exists directoryPath) then
            dict []
        else
            Directory.GetFiles(directoryPath, "TestResult.xml", SearchOption.AllDirectories)
            |> Seq.choose (fun path ->
                let document = XDocument.Load path
                let root = document.Root

                if isNull root then
                    None
                else
                    tryGetChildValue root "TestName"
                    |> Option.map (fun methodName ->
                        methodName,
                        {
                            SuiteName = "Testify"
                            MethodName = methodName
                            Outcome = tryGetChildValue root "Outcome" |> Option.defaultValue "Unknown"
                            Output = tryGetChildValue root "Output"
                            Hint = tryGetChildValue root "Hint"
                            FailureSummary = tryGetChildValue root "MessageSummary"
                            DurationSeconds = None
                        }))
            |> dict

    let normalizeSnapshot (snapshot: SnapshotInfo) (artifacts: RawRunArtifacts) : SnapshotComparison =
        let buildSucceeded =
            match tryReadJsonDocument artifacts.RunMetadataPath with
            | Some document ->
                let mutable buildExitCodeElement = Unchecked.defaultof<JsonElement>

                if document.RootElement.TryGetProperty("buildExitCode", &buildExitCodeElement)
                   && buildExitCodeElement.ValueKind = JsonValueKind.Number then
                    buildExitCodeElement.GetInt32() = 0
                else
                    false
            | None -> false

        let primaryBuildError = tryParsePrimaryBuildError artifacts.BuildLogPath

        let parsedTrxResults = parseTrxResults artifacts.TestResultsPath

        let originalResultsFromTrx =
            parsedTrxResults
            |> List.filter (fun result -> result.SuiteName = "Original")
            |> List.map (fun result -> result.MethodName, result)
            |> dict

        let testifyResultsFromTrx =
            parsedTrxResults
            |> List.filter (fun result -> result.SuiteName = "Testify")
            |> List.map (fun result -> result.MethodName, result)
            |> dict

        let testifyResultsFromXml = parseTestifyXmlResults artifacts.TestifyResultsDirectory

        let mergedTestifyResults =
            Set.union (testifyResultsFromTrx.Keys |> Set.ofSeq) (testifyResultsFromXml.Keys |> Set.ofSeq)
            |> Seq.map (fun methodName ->
                let fromTrx =
                    match testifyResultsFromTrx.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let fromXml =
                    match testifyResultsFromXml.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let merged =
                    match fromTrx, fromXml with
                    | Some trx, Some xml ->
                        {
                            trx with
                                Output = xml.Output |> Option.orElse trx.Output
                                Hint = xml.Hint |> Option.orElse trx.Hint
                                FailureSummary = xml.FailureSummary |> Option.orElse trx.FailureSummary
                                Outcome =
                                    if String.Equals(trx.Outcome, "Unknown", StringComparison.OrdinalIgnoreCase) then
                                        xml.Outcome
                                    else
                                        trx.Outcome
                        }
                    | Some trx, None -> trx
                    | None, Some xml -> xml
                    | None, None -> failwith "Unreachable merged Testify result case."

                methodName, merged)
            |> dict

        let methodNames =
            Set.union
                (snapshot.Task.AllExpectedMethodNames |> Set.ofList)
                (Set.union (originalResultsFromTrx.Keys |> Set.ofSeq) (mergedTestifyResults.Keys |> Set.ofSeq))
            |> Set.toList
            |> List.sort

        let rows =
            methodNames
            |> List.map (fun methodName ->
                let originalResult =
                    match originalResultsFromTrx.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let testifyResult =
                    match mergedTestifyResults.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let pairStatus =
                    if not buildSucceeded then
                        "build-failed"
                    else
                        match originalResult, testifyResult with
                        | Some _, Some _ -> "paired"
                        | Some _, None -> "missing-testify"
                        | None, Some _ -> "missing-original"
                        | None, None -> "missing-both"

                {
                    SheetId = snapshot.Task.SheetId
                    AssignmentId = snapshot.Task.AssignmentId
                    GroupIdTeamId = snapshot.GroupIdTeamId
                    Timestamp = snapshot.Timestamp
                    MethodName = methodName
                    OriginalOutcome = originalResult |> Option.map (fun result -> result.Outcome)
                    TestifyOutcome = testifyResult |> Option.map (fun result -> result.Outcome)
                    OriginalOutput = originalResult |> Option.bind (fun result -> result.Output)
                    TestifyOutput = testifyResult |> Option.bind (fun result -> result.Output)
                    TestifyHint = testifyResult |> Option.bind (fun result -> result.Hint)
                    OriginalFailureSummary = originalResult |> Option.bind (fun result -> result.FailureSummary)
                    TestifyFailureSummary = testifyResult |> Option.bind (fun result -> result.FailureSummary)
                    OriginalDuration = originalResult |> Option.bind (fun result -> result.DurationSeconds)
                    TestifyDuration = testifyResult |> Option.bind (fun result -> result.DurationSeconds)
                    BuildSucceeded = buildSucceeded
                    BuildErrorCode = primaryBuildError |> Option.map (fun error -> error.Code)
                    BuildErrorMessage = primaryBuildError |> Option.map (fun error -> error.Message)
                    BuildErrorFile = primaryBuildError |> Option.bind (fun error -> error.File)
                    BuildErrorLine = primaryBuildError |> Option.bind (fun error -> error.Line)
                    BuildErrorColumn = primaryBuildError |> Option.bind (fun error -> error.Column)
                    PairStatus = pairStatus
                    SourceFilePresent = snapshot.SourceFilePresent
                })

        let comparison =
            {
                SheetId = snapshot.Task.SheetId
                AssignmentId = snapshot.Task.AssignmentId
                GroupIdTeamId = snapshot.GroupIdTeamId
                Timestamp = snapshot.Timestamp
                BuildSucceeded = buildSucceeded
                BuildErrorCode = primaryBuildError |> Option.map (fun error -> error.Code)
                BuildErrorMessage = primaryBuildError |> Option.map (fun error -> error.Message)
                BuildErrorFile = primaryBuildError |> Option.bind (fun error -> error.File)
                BuildErrorLine = primaryBuildError |> Option.bind (fun error -> error.Line)
                BuildErrorColumn = primaryBuildError |> Option.bind (fun error -> error.Column)
                SourceFilePresent = snapshot.SourceFilePresent
                Rows = rows
            }

        writeJson
            (Path.Combine(snapshot.ResultDirectory, "comparison.json"))
            {| sheetId = comparison.SheetId
               assignmentId = comparison.AssignmentId
               groupIdTeamId = comparison.GroupIdTeamId
               timestamp = comparison.Timestamp
               buildSucceeded = comparison.BuildSucceeded
               buildErrorCode = comparison.BuildErrorCode
               buildErrorMessage = comparison.BuildErrorMessage
               buildErrorFile = comparison.BuildErrorFile
               buildErrorLine = comparison.BuildErrorLine
               buildErrorColumn = comparison.BuildErrorColumn
               sourceFilePresent = comparison.SourceFilePresent
               rows =
                   comparison.Rows
                   |> List.map (fun row ->
                       {| sheetId = row.SheetId
                          assignmentId = row.AssignmentId
                          groupIdTeamId = row.GroupIdTeamId
                          timestamp = row.Timestamp
                          methodName = row.MethodName
                          originalOutcome = row.OriginalOutcome
                          testifyOutcome = row.TestifyOutcome
                          originalOutput = row.OriginalOutput
                          testifyOutput = row.TestifyOutput
                          testifyHint = row.TestifyHint
                          originalFailureSummary = row.OriginalFailureSummary
                          testifyFailureSummary = row.TestifyFailureSummary
                          originalDuration = row.OriginalDuration
                          testifyDuration = row.TestifyDuration
                          buildSucceeded = row.BuildSucceeded
                          buildErrorCode = row.BuildErrorCode
                          buildErrorMessage = row.BuildErrorMessage
                          buildErrorFile = row.BuildErrorFile
                          buildErrorLine = row.BuildErrorLine
                          buildErrorColumn = row.BuildErrorColumn
                          pairStatus = row.PairStatus
                          sourceFilePresent = row.SourceFilePresent |}) |}

        comparison

    let normalizeExistingSnapshot (snapshot: SnapshotInfo) : unit =
        normalizeSnapshot
            snapshot
            {
                ResultDirectory = snapshot.ResultDirectory
                BuildLogPath = Path.Combine(snapshot.ResultDirectory, "build.log")
                TestResultsPath = Path.Combine(snapshot.ResultDirectory, "test-results.xml")
                TestifyResultsDirectory = Path.Combine(snapshot.ResultDirectory, "testify-results")
                RunMetadataPath = Path.Combine(snapshot.ResultDirectory, "run-metadata.json")
            }
        |> ignore

    let rewriteAggregateJsonl () : unit =
        TaskPipeline.ensureDirectory DockerResultsRoot

        use writer = new StreamWriter(Path.Combine(DockerResultsRoot, "comparisons.jsonl"), false, Encoding.UTF8)

        for comparisonPath in Directory.GetFiles(DockerResultsRoot, "comparison.json", SearchOption.AllDirectories) |> Array.sort do
            match tryReadJsonDocument comparisonPath with
            | None -> ()
            | Some document ->
                let mutable rowsElement = Unchecked.defaultof<JsonElement>

                if document.RootElement.TryGetProperty("rows", &rowsElement) && rowsElement.ValueKind = JsonValueKind.Array then
                    for row in rowsElement.EnumerateArray() do
                        writer.WriteLine(row.GetRawText())

    let private tryGetJsonString (element: JsonElement) (propertyName: string) : string option =
        let mutable value = Unchecked.defaultof<JsonElement>

        if element.TryGetProperty(propertyName, &value) then
            match value.ValueKind with
            | JsonValueKind.String ->
                let raw = value.GetString()
                if String.IsNullOrWhiteSpace raw then None else Some raw
            | JsonValueKind.Number
            | JsonValueKind.True
            | JsonValueKind.False -> Some(value.ToString())
            | _ -> None
        else
            None

    let private escapeCsvValue (value: string option) : string =
        let raw = value |> Option.defaultValue String.Empty
        "\"" + raw.Replace("\"", "\"\"") + "\""

    let private selectedCsvHeader : string =
        [
            "sheetId"
            "assignmentId"
            "groupIdTeamId"
            "timestamp"
            "methodName"
            "buildSucceeded"
            "sourceFilePresent"
            "pairStatus"
            "originalOutcome"
            "testifyOutcome"
            "testifyHint"
            "buildErrorCode"
            "buildErrorMessage"
            "buildErrorFile"
            "buildErrorLine"
            "buildErrorColumn"
            "originalMessage"
            "testifyMessage"
            "originalFailureSummary"
            "testifyFailureSummary"
            "originalOutput"
            "testifyOutput"
            "originalDuration"
            "testifyDuration"
        ]
        |> String.concat ","

    let private writeSelectedCsvRows
        (writer: StreamWriter)
        (includeRow: JsonElement -> bool)
        (snapshots: SnapshotInfo list)
        : unit =
        writer.WriteLine selectedCsvHeader

        for snapshot in snapshots do
            let comparisonPath = Path.Combine(snapshot.ResultDirectory, "comparison.json")

            match tryReadJsonDocument comparisonPath with
            | None -> ()
            | Some document ->
                let mutable rowsElement = Unchecked.defaultof<JsonElement>

                if document.RootElement.TryGetProperty("rows", &rowsElement) && rowsElement.ValueKind = JsonValueKind.Array then
                    for row in rowsElement.EnumerateArray() do
                        if includeRow row then
                            let originalFailureSummary = tryGetJsonString row "originalFailureSummary"
                            let testifyFailureSummary = tryGetJsonString row "testifyFailureSummary"
                            let originalOutput = tryGetJsonString row "originalOutput"
                            let testifyOutput = tryGetJsonString row "testifyOutput"
                            let originalMessage = originalFailureSummary |> Option.orElse originalOutput
                            let testifyMessage = testifyFailureSummary |> Option.orElse testifyOutput

                            [
                                tryGetJsonString row "sheetId"
                                tryGetJsonString row "assignmentId"
                                tryGetJsonString row "groupIdTeamId"
                                tryGetJsonString row "timestamp"
                                tryGetJsonString row "methodName"
                                tryGetJsonString row "buildSucceeded"
                                tryGetJsonString row "sourceFilePresent"
                                tryGetJsonString row "pairStatus"
                                tryGetJsonString row "originalOutcome"
                                tryGetJsonString row "testifyOutcome"
                                tryGetJsonString row "testifyHint"
                                tryGetJsonString row "buildErrorCode"
                                tryGetJsonString row "buildErrorMessage"
                                tryGetJsonString row "buildErrorFile"
                                tryGetJsonString row "buildErrorLine"
                                tryGetJsonString row "buildErrorColumn"
                                originalMessage
                                testifyMessage
                                originalFailureSummary
                                testifyFailureSummary
                                originalOutput
                                testifyOutput
                                tryGetJsonString row "originalDuration"
                                tryGetJsonString row "testifyDuration"
                            ]
                            |> List.map escapeCsvValue
                            |> String.concat ","
                            |> writer.WriteLine

    let private isFailureRow (row: JsonElement) : bool =
        let pairStatus = tryGetJsonString row "pairStatus" |> Option.defaultValue String.Empty
        let originalOutcome = tryGetJsonString row "originalOutcome" |> Option.defaultValue String.Empty
        let testifyOutcome = tryGetJsonString row "testifyOutcome" |> Option.defaultValue String.Empty

        pairStatus = "build-failed"
        || (not (String.Equals(originalOutcome, "Passed", StringComparison.OrdinalIgnoreCase))
            && not (String.IsNullOrWhiteSpace originalOutcome))
        || (not (String.Equals(testifyOutcome, "Passed", StringComparison.OrdinalIgnoreCase))
            && not (String.IsNullOrWhiteSpace testifyOutcome))

    let rewriteSelectedCsv (snapshots: SnapshotInfo list) : unit =
        TaskPipeline.ensureDirectory DockerResultsRoot

        let outputPath = Path.Combine(DockerResultsRoot, "selected-comparisons.csv")
        use writer = new StreamWriter(outputPath, false, Encoding.UTF8)
        writeSelectedCsvRows writer (fun _ -> true) snapshots

    let rewriteSelectedFailuresCsv (snapshots: SnapshotInfo list) : unit =
        TaskPipeline.ensureDirectory DockerResultsRoot

        let outputPath = Path.Combine(DockerResultsRoot, "selected-failures-only.csv")
        use writer = new StreamWriter(outputPath, false, Encoding.UTF8)
        writeSelectedCsvRows writer isFailureRow snapshots
