namespace GdP23

module Normalization =
    open System
    open System.Collections.Generic
    open System.Globalization
    open System.IO
    open System.Text
    open System.Text.Json
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
                            FailureSummary = tryGetChildValue root "MessageSummary"
                            DurationSeconds = None
                        }))
            |> dict

    let private parseHistoricalOriginalResults (snapshot: SnapshotInfo) : IDictionary<string, ParsedTestResult> =
        let parseTest (testElement: JsonElement) : (string * ParsedTestResult) option =
            let tryGetString (propertyName: string) =
                let mutable value = Unchecked.defaultof<JsonElement>

                if testElement.TryGetProperty(propertyName, &value) && value.ValueKind <> JsonValueKind.Null then
                    Some(value.GetString())
                else
                    None
                |> Option.bind (fun value ->
                    if String.IsNullOrWhiteSpace value then
                        None
                    else
                        Some value)

            let tryGetBool (propertyName: string) =
                let mutable value = Unchecked.defaultof<JsonElement>

                if testElement.TryGetProperty(propertyName, &value) then
                    match value.ValueKind with
                    | JsonValueKind.True -> Some true
                    | JsonValueKind.False -> Some false
                    | _ -> None
                else
                    None

            match tryGetString "name" with
            | None -> None
            | Some methodName ->
                let failureSummary = tryGetString "error"
                let output =
                    [ tryGetString "output"; failureSummary ]
                    |> List.choose id
                    |> function
                        | [] -> None
                        | values -> Some(String.Join(Environment.NewLine + Environment.NewLine, values))

                Some(
                    methodName,
                    {
                        SuiteName = "Original"
                        MethodName = methodName
                        Outcome =
                            match tryGetBool "success" with
                            | Some true -> "Passed"
                            | Some false -> "Failed"
                            | None -> "Unknown"
                        Output = output
                        FailureSummary = failureSummary
                        DurationSeconds = None
                    })

        snapshot.HistoricalRecord
        |> Option.bind (fun record -> record.ResultJson)
        |> Option.map (fun rawJson ->
            use document = JsonDocument.Parse(rawJson)
            let mutable testsElement = Unchecked.defaultof<JsonElement>

            if document.RootElement.TryGetProperty("tests", &testsElement) && testsElement.ValueKind = JsonValueKind.Array then
                testsElement.EnumerateArray()
                |> Seq.choose parseTest
                |> dict
            else
                dict [])
        |> Option.defaultValue (dict [])

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

        let parsedTrxResults = parseTrxResults artifacts.TestResultsPath
        let historicalOriginalResults = parseHistoricalOriginalResults snapshot

        let originalResultsFromTrx =
            parsedTrxResults
            |> List.filter (fun result -> result.SuiteName = "Original")
            |> List.map (fun result -> result.MethodName, result)
            |> dict

        let mergedOriginalResults =
            Set.union (originalResultsFromTrx.Keys |> Set.ofSeq) (historicalOriginalResults.Keys |> Set.ofSeq)
            |> Seq.map (fun methodName ->
                let fromTrx =
                    match originalResultsFromTrx.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let fromHistorical =
                    match historicalOriginalResults.TryGetValue methodName with
                    | true, value -> Some value
                    | false, _ -> None

                let merged =
                    match fromHistorical, fromTrx with
                    | Some historical, Some trx ->
                        {
                            historical with
                                Output = historical.Output |> Option.orElse trx.Output
                                FailureSummary = historical.FailureSummary |> Option.orElse trx.FailureSummary
                                DurationSeconds = trx.DurationSeconds
                        }
                    | Some historical, None -> historical
                    | None, Some trx -> trx
                    | None, None -> failwith "Unreachable merged original result case."

                methodName, merged)
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
                (Set.union (mergedOriginalResults.Keys |> Set.ofSeq) (mergedTestifyResults.Keys |> Set.ofSeq))
            |> Set.toList
            |> List.sort

        let rows =
            methodNames
            |> List.map (fun methodName ->
                let originalResult =
                    match mergedOriginalResults.TryGetValue methodName with
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
                    OriginalFailureSummary = originalResult |> Option.bind (fun result -> result.FailureSummary)
                    TestifyFailureSummary = testifyResult |> Option.bind (fun result -> result.FailureSummary)
                    OriginalDuration = originalResult |> Option.bind (fun result -> result.DurationSeconds)
                    TestifyDuration = testifyResult |> Option.bind (fun result -> result.DurationSeconds)
                    BuildSucceeded = buildSucceeded
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
                          originalFailureSummary = row.OriginalFailureSummary
                          testifyFailureSummary = row.TestifyFailureSummary
                          originalDuration = row.OriginalDuration
                          testifyDuration = row.TestifyDuration
                          buildSucceeded = row.BuildSucceeded
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

    let rewriteSelectedCsv (snapshots: SnapshotInfo list) : unit =
        TaskPipeline.ensureDirectory DockerResultsRoot

        let outputPath = Path.Combine(DockerResultsRoot, "selected-comparisons.csv")
        let header =
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

        use writer = new StreamWriter(outputPath, false, Encoding.UTF8)
        writer.WriteLine header

        for snapshot in snapshots do
            let comparisonPath = Path.Combine(snapshot.ResultDirectory, "comparison.json")

            match tryReadJsonDocument comparisonPath with
            | None -> ()
            | Some document ->
                let mutable rowsElement = Unchecked.defaultof<JsonElement>

                if document.RootElement.TryGetProperty("rows", &rowsElement) && rowsElement.ValueKind = JsonValueKind.Array then
                    for row in rowsElement.EnumerateArray() do
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
