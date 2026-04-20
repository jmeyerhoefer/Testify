namespace Testify.Expecto

open System
open System.Collections.Concurrent
open System.IO
open System.Reflection
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks
open Expecto
open Expecto.Logging
open Expecto.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.MSTest

/// <summary>Configuration options for Testify's Expecto integration helpers.</summary>
/// <remarks>
/// Start from <c>TestifyExpectoConfig.Default</c> or <c>TestifyExpectoConfig.defaults</c>, then
/// override only the rendering and execution behaviors your suite needs.
/// </remarks>
type TestifyExpectoConfig =
    {
        ReportOptions: TestifyReportOptions option
        ShowFrameworkPrefix: bool
        ShowSummary: bool
        ShowUnexpectedExceptionDetails: bool
        Parallel: bool
    }
    /// <summary>The neutral Expecto integration configuration.</summary>
    static member Default =
        {
            ReportOptions = None
            ShowFrameworkPrefix = false
            ShowSummary = false
            ShowUnexpectedExceptionDetails = false
            Parallel = false
        }


[<RequireQualifiedAccess>]
module TestifyExpectoConfig =
    /// <summary>Convenience alias for <c>TestifyExpectoConfig.Default</c>.</summary>
    let defaults =
        TestifyExpectoConfig.Default


type internal TestifyExpectoFailureException
    (
        report: TestifyFailureReport,
        rendered: string,
        testPath: string list
    ) =
    inherit Exception(rendered)

    member _.Report = report
    member _.Rendered = rendered
    member _.TestPath = testPath


type private RecordedOutcome =
    | Passed
    | TestifyFailure of report: TestifyFailureReport * rendered: string
    | UnexpectedFailure of exn


type private RecordedRunResult =
    {
        Sequence: int
        TestPath: string list
        Outcome: RecordedOutcome
    }


type private RunContext =
    {
        Config: TestifyExpectoConfig
        Results: ConcurrentQueue<RecordedRunResult>
        Sequence: int ref
    }


[<RequireQualifiedAccess>]
module TestifyExpecto =
    let private currentRun = AsyncLocal<RunContext option> ()
    let private currentPath = AsyncLocal<string list option> ()
    let private jsonRenderOptions = JsonSerializerOptions(WriteIndented = true)

    let private defaultReportOptions () =
        Testify.currentConfiguration().ReportOptions
        |> TestifyReportOptions.normalize

    let private resolveReportOptions
        (config: TestifyExpectoConfig)
        : TestifyReportOptions =
        config.ReportOptions
        |> Option.defaultWith defaultReportOptions
        |> TestifyReportOptions.normalize

    let private currentPathSegments () =
        match currentPath.Value with
        | None -> []
        | Some values -> values

    let private joinTestPath
        (testPath: string list)
        : string =
        match testPath with
        | [] -> "Expecto test"
        | values -> String.concat "/" values

    let private nextSequence
        (context: RunContext)
        : int =
        lock context.Sequence (fun () ->
            context.Sequence := !context.Sequence + 1
            !context.Sequence)

    let private recordResult
        (testPath: string list)
        (outcome: RecordedOutcome)
        : unit =
        match currentRun.Value with
        | Some context ->
            context.Results.Enqueue(
                {
                    Sequence = nextSequence context
                    TestPath = testPath
                    Outcome = outcome
                })
        | None -> ()

    let private createCompletedResult
        (testName: string)
        (outcome: UnitTestOutcome)
        : TestResult =
        let result = TestResult()
        result.DisplayName <- testName
        result.Outcome <- outcome
        result

    let private persistResult
        (testPath: string list)
        (outcome: RecordedOutcome)
        (state: TestExecutionState option)
        : unit =
        let testName = joinTestPath testPath

        let result =
            match outcome with
            | Passed ->
                createCompletedResult testName UnitTestOutcome.Passed
            | TestifyFailure (report, rendered) ->
                TestResults.createSyntheticFailure
                    testName
                    (TestifyExpectoFailureException(
                        report,
                        rendered,
                        testPath
                    ))
            | UnexpectedFailure ex ->
                TestResults.createSyntheticFailure testName ex

        TestResults.writeResults state [| result |]
        |> ignore

    let private formatUnexpectedFailure
        (config: TestifyExpectoConfig)
        (testPath: string list)
        (ex: exn)
        : string =
        let pathText = joinTestPath testPath
        let header =
            if config.ShowFrameworkPrefix then
                $"FAIL {pathText}"
            else
                ""

        let body =
            if config.ShowUnexpectedExceptionDetails then
                ex.ToString()
            else
                $"Unexpected failure: {ex.Message}"

        [ header; body ]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> String.concat "\n"

    let private addTestPathMetadata
        (testPath: string list)
        (target: JsonObject)
        : unit =
        let pathArray = JsonArray()

        for segment in testPath do
            pathArray.Add(segment)

        target["testPath"] <- pathArray
        target["testName"] <- JsonValue.Create(joinTestPath testPath)

    let private formatUnexpectedFailureJson
        (config: TestifyExpectoConfig)
        (testPath: string list)
        (ex: exn)
        : string =
        let json = JsonObject()
        let details =
            if config.ShowUnexpectedExceptionDetails then
                ex.ToString()
            else
                $"Unexpected failure: {ex.Message}"

        json["kind"] <- JsonValue.Create("test")
        json["outcome"] <- JsonValue.Create("failed")
        json["summary"] <- JsonValue.Create($"Unexpected failure: {ex.Message}")
        json["details"] <- JsonValue.Create(details)
        addTestPathMetadata testPath json
        json.ToJsonString(jsonRenderOptions)

    let private formatRecordedFailureJson
        (reportOptions: TestifyReportOptions)
        (testPath: string list)
        (report: TestifyFailureReport)
        : string =
        let json = JsonObject()

        json["kind"] <- JsonValue.Create("test")
        json["outcome"] <- JsonValue.Create("failed")
        addTestPathMetadata testPath json
        json["failure"] <- TestifyReport.toJsonObjectWith reportOptions report
        json.ToJsonString(jsonRenderOptions)

    let private printRecordedResult
        (config: TestifyExpectoConfig)
        (result: RecordedRunResult)
        : unit =
        let reportOptions = resolveReportOptions config

        let lines =
            match result.Outcome with
            | Passed -> None
            | TestifyFailure (report, rendered) ->
                match reportOptions.OutputFormat with
                | OutputFormat.Json ->
                    formatRecordedFailureJson reportOptions result.TestPath report
                    |> Some
                | OutputFormat.WallOfText ->
                    let prefix =
                        if config.ShowFrameworkPrefix then
                            Some $"FAIL {joinTestPath result.TestPath}"
                        else
                            None

                    [
                        yield! prefix |> Option.toList
                        yield rendered
                    ]
                    |> String.concat "\n"
                    |> Some
                | _ ->
                    let prefix =
                        if config.ShowFrameworkPrefix then
                            Some $"FAIL {joinTestPath result.TestPath}"
                        else
                            None

                    [
                        yield! prefix |> Option.toList
                        yield rendered
                    ]
                    |> String.concat "\n"
                    |> Some
            | UnexpectedFailure ex ->
                match reportOptions.OutputFormat with
                | OutputFormat.Json ->
                    formatUnexpectedFailureJson config result.TestPath ex
                    |> Some
                | OutputFormat.WallOfText ->
                    formatUnexpectedFailure config result.TestPath ex
                    |> Some
                | _ ->
                    formatUnexpectedFailure config result.TestPath ex
                    |> Some

        match lines with
        | Some text when not (String.IsNullOrWhiteSpace text) ->
            Console.Out.WriteLine(text)
        | _ -> ()

    let private printSummary
        (config: TestifyExpectoConfig)
        (results: RecordedRunResult list)
        : unit =
        let reportOptions = resolveReportOptions config

        let passed =
            results
            |> List.filter (fun result ->
                match result.Outcome with
                | Passed -> true
                | _ -> false)
            |> List.length

        let failed =
            results.Length - passed

        match reportOptions.OutputFormat with
        | OutputFormat.Json ->
            let json = JsonObject()
            json["kind"] <- JsonValue.Create("summary")
            json["outcome"] <- JsonValue.Create("completed")
            json["summary"] <- JsonValue.Create($"Summary: {passed} passed, {failed} failed.")
            json["passed"] <- JsonValue.Create(passed)
            json["failed"] <- JsonValue.Create(failed)
            Console.Out.WriteLine(json.ToJsonString(jsonRenderOptions))
        | OutputFormat.WallOfText ->
            Console.Out.WriteLine($"Summary: {passed} passed, {failed} failed.")
        | _ ->
            Console.Out.WriteLine($"Summary: {passed} passed, {failed} failed.")

    let private withSuppressedConsoleOutput
        (enabled: bool)
        (action: unit -> 'T)
        : 'T =
        if not enabled then
            action ()
        else
            let originalOut = Console.Out
            let originalError = Console.Error
            use sink = new StringWriter()

            Console.SetOut(sink)
            Console.SetError(sink)

            try
                action ()
            finally
                Console.SetOut(originalOut)
                Console.SetError(originalError)

    let private withPathPrefix
        (prefix: string list)
        (action: unit -> 'T)
        : 'T =
        let previous = currentPath.Value
        currentPath.Value <- Some (currentPathSegments () @ prefix)

        try
            action ()
        finally
            currentPath.Value <- previous

    let private withPathPrefixAsync
        (prefix: string list)
        (action: Async<'T>)
        : Async<'T> =
        async {
            let previous = currentPath.Value
            currentPath.Value <- Some (currentPathSegments () @ prefix)

            try
                return! action
            finally
                currentPath.Value <- previous
        }

    let private executeLeafTest
        (testPath: string list)
        (code: unit -> unit)
        : unit =
        match currentRun.Value with
        | None ->
            code ()
        | Some run ->
            let reportOptions = resolveReportOptions run.Config
            let testName = joinTestPath testPath
            TestExecution.beginTest "Expecto" testName reportOptions

            let mutable outcome = Passed

            let dispatcher
                (payload: TestifyFailureDispatchPayload)
                : unit =
                outcome <- TestifyFailure(payload.Report, payload.Rendered)

                raise (
                    TestifyExpectoFailureException(
                        payload.Report,
                        payload.Rendered,
                        testPath
                    ))

            try
                try
                    TestifyRunnerContext.withFailureDispatcher dispatcher code
                with
                | :? TestifyExpectoFailureException -> ()
                | ex ->
                    outcome <- UnexpectedFailure ex
            finally
                let state = TestExecution.endTest ()
                recordResult testPath outcome
                persistResult testPath outcome state

    let private executeLeafTestAsync
        (testPath: string list)
        (code: Async<unit>)
        : Async<unit> =
        async {
            match currentRun.Value with
            | None ->
                return! code
            | Some run ->
                let reportOptions = resolveReportOptions run.Config
                let testName = joinTestPath testPath
                TestExecution.beginTest "Expecto" testName reportOptions

                let mutable outcome = Passed

                let dispatcher
                    (payload: TestifyFailureDispatchPayload)
                    : unit =
                    outcome <- TestifyFailure(payload.Report, payload.Rendered)

                    raise (
                        TestifyExpectoFailureException(
                            payload.Report,
                            payload.Rendered,
                            testPath
                        ))

                let wrappedCode =
                    TestifyRunnerContext.withFailureDispatcherAsync dispatcher code

                try
                    try
                        return! wrappedCode
                    with
                    | :? TestifyExpectoFailureException -> return ()
                    | ex ->
                        outcome <- UnexpectedFailure ex
                        return ()
                finally
                let state = TestExecution.endTest ()
                recordResult testPath outcome
                persistResult testPath outcome state
        }

    let rec private prefixTestCode
        (prefix: string list)
        (code: TestCode)
        : TestCode =
        match code with
        | TestCode.Sync test ->
            TestCode.Sync (fun () ->
                withPathPrefix prefix test)
        | TestCode.SyncWithCancel test ->
            TestCode.SyncWithCancel (fun cancellationToken ->
                withPathPrefix prefix (fun () ->
                    test cancellationToken))
        | TestCode.Async test ->
            TestCode.Async (
                withPathPrefixAsync prefix test
            )
        | TestCode.AsyncFsCheck (testConfig, stressConfig, test) ->
            TestCode.AsyncFsCheck (
                testConfig,
                stressConfig,
                (fun fsCheckConfig ->
                    withPathPrefixAsync prefix (test fsCheckConfig))
            )

    let rec private prefixTest
        (prefix: string list)
        (test: Test)
        : Test =
        match test with
        | Test.TestCase (code, state) ->
            Test.TestCase (prefixTestCode prefix code, state)
        | Test.TestList (tests, state) ->
            tests
            |> List.map (prefixTest prefix)
            |> fun wrappedTests -> Test.TestList (wrappedTests, state)
        | Test.TestLabel (label, inner, state) ->
            Test.TestLabel (label, prefixTest prefix inner, state)
        | Test.Sequenced (sequenceMethod, inner) ->
            Test.Sequenced (sequenceMethod, prefixTest prefix inner)

    let private minimalCliArgs =
        [
            CLIArguments.No_Spinner
            CLIArguments.Verbosity LogLevel.Fatal
        ]

    let private shouldSuppressFrameworkOutput
        (args: string array)
        : bool =
        args
        |> Array.exists (fun arg ->
            String.Equals(arg, "--list-tests", StringComparison.OrdinalIgnoreCase)
            || String.Equals(arg, "--version", StringComparison.OrdinalIgnoreCase))
        |> not

    let private discoverAssemblyTests
        ()
        : Test =
        let expectoImplType =
            match typeof<Test>.Assembly.GetType("Expecto.Impl", true) with
            | null ->
                invalidOp "Expecto implementation type 'Expecto.Impl' was not found."
            | value ->
                value

        let methodInfo =
            match expectoImplType.GetMethod(
                "testFromThisAssembly",
                BindingFlags.Public ||| BindingFlags.Static
            ) with
            | null ->
                invalidOp "Expecto test discovery entry point 'Expecto.Impl.testFromThisAssembly' was not found."
            | value ->
                value

        match methodInfo.Invoke(null, [||]) with
        | null ->
            invalidOp "Expecto test discovery returned null instead of a test tree."
        | discovered ->
            match discovered with
            | :? Test as test -> test
            | _ -> invalidOp "Expecto test discovery returned an unexpected value."

    /// <summary>Creates one synchronous Expecto test case that records Testify output.</summary>
    /// <param name="name">The Expecto test name.</param>
    /// <param name="code">The synchronous test body.</param>
    /// <returns>An Expecto <c>Test</c> node that records Testify failures in the current run context.</returns>
    /// <example id="expecto-testcase-1">
    /// <code lang="fsharp">
    /// let tests =
    ///     TestifyExpecto.testCase "reverse is involutive" (fun () ->
    ///         &lt;@ List.rev (List.rev [1; 2; 3]) @&gt;
    ///         |> Assert.should (AssertExpectation.equalTo [1; 2; 3]))
    /// </code>
    /// </example>
    let testCase
        (name: string)
        (code: unit -> unit)
        : Test =
        Tests.testCase name (fun () ->
            executeLeafTest (currentPathSegments () @ [ name ]) code)

    /// <summary>Creates an Expecto test list while preserving Testify path metadata.</summary>
    /// <param name="name">The group label.</param>
    /// <param name="tests">The child tests that belong to the group.</param>
    /// <returns>An Expecto <c>Test</c> list with the supplied label.</returns>
    let testList
        (name: string)
        (tests: Test list)
        : Test =
        Tests.testList name tests
        |> prefixTest [ name ]

    /// <summary>Creates one asynchronous Expecto test case that records Testify output.</summary>
    /// <param name="name">The Expecto test name.</param>
    /// <param name="code">The asynchronous test body.</param>
    /// <returns>An asynchronous Expecto <c>Test</c> node.</returns>
    let testCaseAsync
        (name: string)
        (code: Async<unit>)
        : Test =
        Tests.testCaseAsync name (
            async {
                let path = currentPathSegments () @ [ name ]
                return! executeLeafTestAsync path code
            }
        )

    /// <summary>Creates one task-based Expecto test case that records Testify output.</summary>
    /// <param name="name">The Expecto test name.</param>
    /// <param name="code">The task-returning test body.</param>
    /// <returns>A task-based Expecto <c>Test</c> node.</returns>
    let testCaseTask
        (name: string)
        (code: unit -> Task<unit>)
        : Test =
        Tests.testCaseTask name (fun () ->
            task {
                let path = currentPathSegments () @ [ name ]
                do!
                    executeLeafTestAsync path (code () |> Async.AwaitTask)
                    |> Async.StartAsTask
            })

    /// <summary>Discovers MSTest-style Testify methods on a fixture type and exposes them as an Expecto test list.</summary>
    /// <param name="name">The displayed group name for the fixture.</param>
    /// <returns>
    /// An Expecto <c>Test</c> list that runs public parameterless methods marked with
    /// <c>TestifyMethodAttribute</c>.
    /// </returns>
    let testFixture<'T>
        (name: string)
        : Test =
        let fixtureType = typeof<'T>

        let methods =
            fixtureType.GetMethods(
                BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.DeclaredOnly
            )
            |> Array.filter (fun methodInfo ->
                methodInfo.GetParameters().Length = 0
                && methodInfo.GetCustomAttributes(typeof<TestifyMethodAttribute>, true).Length > 0)
            |> Array.sortBy _.MetadataToken
            |> Array.toList

        let tests =
            methods
            |> List.map (fun methodInfo ->
                testCase methodInfo.Name (fun () ->
                    let instance = Activator.CreateInstance fixtureType

                    try
                        match methodInfo.Invoke(instance, [||]) with
                        | :? Task as task ->
                            task.GetAwaiter().GetResult()
                        | _ ->
                            ()
                    with
                    | :? TargetInvocationException as invocationEx
                        ->
                        match invocationEx.InnerException with
                        | null -> raise invocationEx
                        | inner -> raise inner))

        testList name tests

    let private runWithContext
        (config: TestifyExpectoConfig)
        (suppressFrameworkOutput: bool)
        (action: unit -> int)
        : int * RecordedRunResult list =
        let context =
            {
                Config = config
                Results = ConcurrentQueue ()
                Sequence = ref 0
            }

        let previous = currentRun.Value
        currentRun.Value <- Some context

        try
            let exitCode =
                withSuppressedConsoleOutput suppressFrameworkOutput action

            let results =
                context.Results.ToArray()
                |> Array.toList
                |> List.sortBy (fun result -> result.Sequence)

            exitCode, results
        finally
            currentRun.Value <- previous

    let private applyParallelPreference
        (config: TestifyExpectoConfig)
        (tests: Test)
        : Test =
        if config.Parallel then
            tests
        else
            Tests.testSequenced tests

    /// <summary>Runs an explicit Expecto test tree with Testify-aware result recording and CLI argument handling.</summary>
    /// <param name="config">The integration configuration controlling rendering and execution behavior.</param>
    /// <param name="args">The CLI arguments passed to the Expecto runner.</param>
    /// <param name="tests">The Expecto test tree to execute.</param>
    /// <returns>The resulting process exit code, with Testify failures forcing a non-zero result.</returns>
    let runTestsWithCLIArgs
        (config: TestifyExpectoConfig)
        (args: string array)
        (tests: Test)
        : int =
        let preparedTests =
            tests
            |> applyParallelPreference config

        let suppressFrameworkOutput = shouldSuppressFrameworkOutput args

        let expectoExit, results =
            runWithContext config suppressFrameworkOutput (fun () ->
                Tests.runTestsWithCLIArgs minimalCliArgs args preparedTests)

        let failures =
            results
            |> List.filter (fun result ->
                match result.Outcome with
                | Passed -> false
                | _ -> true)

        failures
        |> List.iter (printRecordedResult config)

        if config.ShowSummary then
            printSummary config results

        match failures with
        | _ :: _ -> 1
        | [] -> expectoExit

    /// <summary>Discovers tests from the current assembly and runs them through the Testify-aware Expecto runner.</summary>
    /// <param name="config">The integration configuration controlling rendering and execution behavior.</param>
    /// <param name="args">The CLI arguments passed to the Expecto runner.</param>
    /// <returns>The resulting process exit code.</returns>
    let runTestsInAssemblyWithCLIArgs
        (config: TestifyExpectoConfig)
        (args: string array)
        : int =
        let discoveredTests = discoverAssemblyTests ()

        let preparedTests =
            discoveredTests
            |> applyParallelPreference config

        let suppressFrameworkOutput = shouldSuppressFrameworkOutput args

        let expectoExit, results =
            runWithContext config suppressFrameworkOutput (fun () ->
                Tests.runTestsWithCLIArgs minimalCliArgs args preparedTests)

        let failures =
            results
            |> List.filter (fun result ->
                match result.Outcome with
                | Passed -> false
                | _ -> true)

        failures
        |> List.iter (printRecordedResult config)

        if config.ShowSummary then
            printSummary config results

        match failures with
        | _ :: _ -> 1
        | [] -> expectoExit
