namespace Testify


open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open System.Xml.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting


type PersistedTestResult =
    {
        DirectoryPath: string
        FilePath: string
    }


[<RequireQualifiedAccess>]
module TestResults =
    let private taskPathPattern =
        Regex(@"[\\/](\d{2})[\\/](\d+)[\\/](template|_solution)(?:[\\/]|$)", RegexOptions.Compiled)

    let private nonEmptyText
        (text: string | null)
        : string option =
        match text with
        | null -> None
        | value when String.IsNullOrWhiteSpace value -> None
        | value -> Some value

    let private tryFindProjectDirectory () : string option =
        let rec ascend (directory: string) =
            let projectFile = Path.Combine(directory, "Testify.fsproj")

            if File.Exists projectFile then
                Some directory
            else
                match Directory.GetParent directory with
                | null -> None
                | parent -> ascend parent.FullName

        [
            Directory.GetCurrentDirectory()
            AppContext.BaseDirectory
            __SOURCE_DIRECTORY__
        ]
        |> List.distinct
        |> List.tryPick (fun directory ->
            if String.IsNullOrWhiteSpace directory || not (Directory.Exists directory) then
                None
            else
                ascend directory)

    let private hashMethodIdentity
        (methodIdentity: string)
        : string =
        use sha = SHA256.Create()

        methodIdentity
        |> Encoding.UTF8.GetBytes
        |> sha.ComputeHash
        |> Array.map (fun value -> value.ToString("x2"))
        |> String.concat ""

    let private tryGetTaskSegments
        (filePath: string)
        : (string * string) option =
        let pathMatch = taskPathPattern.Match filePath

        if pathMatch.Success then
            Some (pathMatch.Groups[1].Value, pathMatch.Groups[2].Value)
        else
            None

    let private getFallbackRoot
        (state: TestExecutionState)
        : string =
        match TestifySettings.ResultRootOverride with
        | Some overrideRoot -> Path.Combine(overrideRoot, "misc")
        | None ->
            match tryFindProjectDirectory () with
            | Some projectDirectory -> Path.Combine(projectDirectory, "TestResults", "misc")
            | None -> Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "misc")

    let private getBaseDirectory
        (state: TestExecutionState)
        : string =
        match state.FirstTestedSourceLocation with
        | Some location ->
            match tryGetTaskSegments location.FilePath with
            | Some (sheet, task) ->
                let root =
                    match TestifySettings.ResultRootOverride with
                    | Some overrideRoot -> overrideRoot
                    | None ->
                        match tryFindProjectDirectory () with
                        | Some projectDirectory -> Path.Combine(projectDirectory, "TestResults")
                        | None -> Path.Combine(Directory.GetCurrentDirectory(), "TestResults")

                Path.Combine(root, sheet, task)
            | None ->
                getFallbackRoot state
        | None ->
            getFallbackRoot state

    let private getTargetDirectory
        (state: TestExecutionState)
        : string =
        let baseDirectory = getBaseDirectory state
        let leafDirectory = hashMethodIdentity state.MethodIdentity
        let candidateDirectory = Path.Combine(baseDirectory, leafDirectory)

        if TestifySettings.OverwriteExistingResults then
            candidateDirectory
        else
            let rec loop index =
                let directory =
                    if index = 0 then
                        candidateDirectory
                    else
                        Path.Combine(baseDirectory, $"{leafDirectory}-{index}")

                let filePath = Path.Combine(directory, "TestResult.xml")

                if Directory.Exists directory || File.Exists filePath then
                    loop (index + 1)
                else
                    directory

            loop 0

    let private summarizeOutcome
        (results: TestResult array)
        : string =
        let outcomes =
            results
            |> Array.map (fun result -> string result.Outcome)
            |> Array.distinct

        match outcomes |> Array.tryFind ((=) "Failed") with
        | Some failed -> failed
        | None ->
            match outcomes |> Array.tryHead with
            | Some outcome -> outcome
            | None -> "Unknown"

    let private summarizeOutput
        (results: TestResult array)
        : string =
        results
        |> Array.choose (fun result ->
            let parts =
                [
                    result.TestFailureException
                    |> Option.ofObj
                    |> Option.map (fun ex -> ex.Message)

                    nonEmptyText result.LogOutput
                    nonEmptyText result.LogError
                    nonEmptyText result.DebugTrace
                ]
                |> List.choose id

            match parts with
            | [] -> None
            | values -> Some (String.concat Environment.NewLine values))
        |> String.concat (Environment.NewLine + Environment.NewLine)

    let private failureReportElements
        (state: TestExecutionState)
        : XElement list =
        let reportOptions = state.ReportOptions |> TestifyReportOptions.normalize

        let observedInfoElement
            (name: string)
            (info: TestifyObservedInfo option)
            : XElement option =
            info
            |> Option.map (fun observed ->
                XElement(
                    XName.Get name,
                    [
                        match observed.Display |> Option.bind nonEmptyText with
                        | Some display -> XElement(XName.Get "Display", display)
                        | None -> ()

                        XElement(XName.Get "IsException", observed.IsException)

                        match observed.ExceptionType |> Option.bind nonEmptyText with
                        | Some exceptionType -> XElement(XName.Get "ExceptionType", exceptionType)
                        | None -> ()

                        match observed.ExceptionMessage |> Option.bind nonEmptyText with
                        | Some exceptionMessage -> XElement(XName.Get "ExceptionMessage", exceptionMessage)
                        | None -> ()
                    ]))

        [
            yield XElement(XName.Get "Verbosity", string reportOptions.Verbosity)
            yield XElement(XName.Get "MaxValueLines", reportOptions.MaxValueLines)

            match state.LastFailureReport with
            | None -> ()
            | Some report ->
                let report = TestifyReport.withInferredHints report
                yield XElement(XName.Get "FailureKind", string report.Kind)
                yield XElement(XName.Get "MessageSummary", report.Summary)
                yield
                    XElement(
                        XName.Get "Hints",
                        report.Hints
                        |> List.map (fun hint -> XElement(XName.Get "Hint", hint)))

                match report.Test with
                | Some test -> yield XElement(XName.Get "TestExpression", test)
                | None -> ()

                match report.Expectation with
                | Some expectation -> yield XElement(XName.Get "Expectation", expectation)
                | None -> ()

                match report.Expected with
                | Some expected -> yield XElement(XName.Get "Expected", expected)
                | None -> ()

                match report.Actual with
                | Some actual -> yield XElement(XName.Get "Actual", actual)
                | None -> ()

                match report.ExpectedValue with
                | Some expectedValue -> yield XElement(XName.Get "ExpectedValue", expectedValue)
                | None -> ()

                match report.ActualValue with
                | Some actualValue -> yield XElement(XName.Get "ActualValue", actualValue)
                | None -> ()

                match observedInfoElement "ExpectedObserved" report.ExpectedObservedInfo with
                | Some element -> yield element
                | None -> ()

                match observedInfoElement "ActualObserved" report.ActualObservedInfo with
                | Some element -> yield element
                | None -> ()

                match report.Because with
                | Some because -> yield XElement(XName.Get "Because", because)
                | None -> ()

                match report.DetailsText with
                | Some details -> yield XElement(XName.Get "Details", details)
                | None -> ()

                match report.DiffText with
                | Some diff -> yield XElement(XName.Get "Diff", diff)
                | None -> ()

                match report.OriginalTest with
                | Some originalTest -> yield XElement(XName.Get "OriginalTest", originalTest)
                | None -> ()

                match report.OriginalExpected with
                | Some originalExpected -> yield XElement(XName.Get "OriginalExpected", originalExpected)
                | None -> ()

                match report.OriginalActual with
                | Some originalActual -> yield XElement(XName.Get "OriginalActual", originalActual)
                | None -> ()

                match observedInfoElement "OriginalExpectedObserved" report.OriginalExpectedObservedInfo with
                | Some element -> yield element
                | None -> ()

                match observedInfoElement "OriginalActualObserved" report.OriginalActualObservedInfo with
                | Some element -> yield element
                | None -> ()

                match report.ShrunkTest with
                | Some shrunkTest -> yield XElement(XName.Get "ShrunkTest", shrunkTest)
                | None -> ()

                match report.ShrunkExpected with
                | Some shrunkExpected -> yield XElement(XName.Get "ShrunkExpected", shrunkExpected)
                | None -> ()

                match report.ShrunkActual with
                | Some shrunkActual -> yield XElement(XName.Get "ShrunkActual", shrunkActual)
                | None -> ()

                match observedInfoElement "ShrunkExpectedObserved" report.ShrunkExpectedObservedInfo with
                | Some element -> yield element
                | None -> ()

                match observedInfoElement "ShrunkActualObserved" report.ShrunkActualObservedInfo with
                | Some element -> yield element
                | None -> ()

                match report.NumberOfTests with
                | Some count -> yield XElement(XName.Get "NumberOfTests", count)
                | None -> ()

                match report.NumberOfShrinks with
                | Some count -> yield XElement(XName.Get "NumberOfShrinks", count)
                | None -> ()

                match report.Replay with
                | Some replay -> yield XElement(XName.Get "Replay", replay)
                | None -> ()

                match report.SourceLocation with
                | Some location ->
                    yield XElement(XName.Get "FailureSourceLocation", Diagnostics.formatLocation location)
                | None -> ()
        ]

    let createSyntheticFailure
        (testName: string)
        (ex: exn)
        : TestResult =
        let result = TestResult()
        result.DisplayName <- testName
        result.Outcome <- UnitTestOutcome.Failed
        result.TestFailureException <- ex
        result

    let writeResults
        (state: TestExecutionState option)
        (results: TestResult array)
        : PersistedTestResult option =
        match state with
        | None -> None
        | Some state ->
            let directoryPath = getTargetDirectory state
            Directory.CreateDirectory directoryPath |> ignore

            let filePath = Path.Combine(directoryPath, "TestResult.xml")
            let testedSourceLocation =
                state.FirstTestedSourceLocation
                |> Option.map Diagnostics.formatLocation

            let children =
                [
                    XElement(XName.Get "TestName", state.TestName)
                    XElement(XName.Get "MethodIdentity", state.MethodIdentity)
                    XElement(XName.Get "Outcome", summarizeOutcome results)
                    XElement(XName.Get "Timestamp", DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss"))
                    XElement(XName.Get "Output", summarizeOutput results)

                    match testedSourceLocation with
                    | Some location -> XElement(XName.Get "TestedSourceLocation", location)
                    | None -> ()

                    yield! failureReportElements state
                ]

            let document =
                XDocument(XElement(XName.Get "TestResult", children))

            use stream = File.Create filePath
            document.Save stream

            Some {
                DirectoryPath = directoryPath
                FilePath = filePath
            }
