namespace Testify


open System.Threading.Tasks
open Microsoft.FSharp.Quotations


/// <summary>Structured information describing why an assertion failed.</summary>
type AssertFailure =
    {
        Label: string
        Test: string
        Expected: string
        Actual: string
        Because: string option
        Details: FailureDetails option
        SourceLocation: Diagnostics.SourceLocation option
    }


/// <summary>Outcome of an assertion check.</summary>
type AssertResult =
    | Passed
    | Failed of AssertFailure


[<RequireQualifiedAccess>]
module Assert =
    type Collector =
        private {
            Results: ResizeArray<AssertResult>
        }

    let private tryFindFallbackExceptionLocation
        (observed: Observed<'T>)
        : Diagnostics.SourceLocation option =
        match observed with
        | Result.Error ex -> Diagnostics.tryFindRelevantExceptionLocation ex
        | Result.Ok _ -> None

    let private tryFindSourceLocation
        (observed: Observed<'T>)
        : Diagnostics.SourceLocation option =
        tryFindFallbackExceptionLocation observed

    let private toFailure
        (test: string)
        (expectation: AssertExpectation<'T>)
        (observed: Observed<'T>)
        (location: Diagnostics.SourceLocation option)
        : AssertFailure =
        {
            Label = expectation.Label
            Test = test
            Expected = expectation.Description
            Actual = expectation.Format observed
            Because = expectation.Because observed
            Details = expectation.Details observed
            SourceLocation = location
        }

    /// <summary>
    /// Evaluates an assertion expectation against a quoted expression and returns a structured result.
    /// </summary>
    let check
        (expectation: AssertExpectation<'T>)
        (actual: Expr<'T>)
        : AssertResult =
        let observed = Observed.observe actual

        if expectation.Verify observed then
            Passed
        else
            let location = tryFindSourceLocation observed
            TestExecution.recordTestedSourceLocation location
            let test = Expressions.readable actual
            Failed (toFailure test expectation observed location)

    /// <summary>
    /// Like <c>check</c>, but uses an explicit human-readable test label in the failure output.
    /// </summary>
    let checkNamed
        (test: string)
        (expectation: AssertExpectation<'T>)
        (actual: Expr<'T>)
        : AssertResult =
        let observed = Observed.observe actual

        if expectation.Verify observed then
            Passed
        else
            let location = tryFindSourceLocation observed
            TestExecution.recordTestedSourceLocation location
            Failed (toFailure test expectation observed location)

    /// <summary>
    /// Evaluates an asynchronous assertion and returns the structured result inside a task.
    /// </summary>
    let checkAsync
        (expectation: AssertExpectation<'T>)
        (actual: Expr)
        : Task<AssertResult> =
        task {
            let! observed = Awaitable.observeUntyped<'T> actual

            if expectation.Verify observed then
                return Passed
            else
                let location = tryFindFallbackExceptionLocation observed
                TestExecution.recordTestedSourceLocation location
                let test = Expressions.readable actual
                return Failed (toFailure test expectation observed location)
        }

    /// <summary>Converts an assertion result into a structured Testify failure report when it failed.</summary>
    let toFailureReport
        (result: AssertResult)
        : TestifyFailureReport option =
        match result with
        | Passed ->
            None
        | Failed failure ->
            let details = TestifyReport.detailsText failure.Details

            {
                TestifyReport.create
                    AssertionFailure
                    (Some failure.Label)
                    $"[{failure.Label}] Failed test: {failure.Test}" with
                    Test = Some failure.Test
                    Expectation = Some failure.Expected
                    Expected = Some failure.Expected
                    Actual = Some failure.Actual
                    Because = failure.Because
                    DetailsText = details
                    DiffText = TestifyReport.diffText failure.Because details
                    SourceLocation = failure.SourceLocation
            }
            |> TestifyReport.withInferredHint
            |> Some

    /// <summary>Renders an assertion result with the supplied reporting options.</summary>
    let toDisplayStringWith
        (options: TestifyReportOptions)
        (result: AssertResult)
        : string =
        match result with
        | Passed ->
            "Test passed."
        | Failed _ ->
            result
            |> toFailureReport
            |> Option.map (TestifyReport.renderWith options)
            |> Option.defaultValue "Test failed."

    /// <summary>Renders an assertion result using the current Testify report options.</summary>
    let toDisplayString (result: AssertResult) : string =
        toDisplayStringWith (TestExecution.currentReportOptions ()) result

    /// <summary>Raises an exception when an assertion result does not pass.</summary>
    let assertPassed (result: AssertResult) : unit =
        match result with
        | Passed -> ()
        | Failed failure ->
            TestExecution.recordTestedSourceLocation failure.SourceLocation
            result
            |> toFailureReport
            |> Option.iter TestExecution.recordFailureReport

            failwith ("\n" + toDisplayString result)

    [<RequireQualifiedAccess>]
    module Collect =
        let create () : Collector =
            { Results = ResizeArray () }

        let add
            (collector: Collector)
            (expectation: AssertExpectation<'T>)
            (actual: Expr<'T>)
            : AssertResult =
            let result = check expectation actual
            collector.Results.Add result
            result

        let addNamed
            (collector: Collector)
            (test: string)
            (expectation: AssertExpectation<'T>)
            (actual: Expr<'T>)
            : AssertResult =
            let result = checkNamed test expectation actual
            collector.Results.Add result
            result

        let toResultList
            (collector: Collector)
            : AssertResult list =
            collector.Results
            |> Seq.toList

        let assertAll
            (collector: Collector)
            : unit =
            let failures =
                collector.Results
                |> Seq.filter (function
                    | Passed -> false
                    | Failed _ -> true)
                |> Seq.toList

            if not failures.IsEmpty then
                failures
                |> List.iter (fun result ->
                    result
                    |> toFailureReport
                    |> Option.iter TestExecution.recordFailureReport)

                let message =
                    failures
                    |> List.map toDisplayString
                    |> String.concat "\n\n---\n\n"

                failwith
                    $"\nCollected {failures.Length} assertion failure(s).\n\n{message}"

    /// <summary>Runs an assertion and raises an exception when it fails.</summary>
    let should
        (expectation: AssertExpectation<'T>)
        (actual: Expr<'T>)
        : unit =
        check expectation actual
        |> assertPassed

    /// <summary>Runs a named assertion and raises an exception when it fails.</summary>
    let shouldNamed
        (test: string)
        (expectation: AssertExpectation<'T>)
        (actual: Expr<'T>)
        : unit =
        checkNamed test expectation actual
        |> assertPassed

    /// <summary>Runs an asynchronous assertion and raises an exception when it fails.</summary>
    let shouldAsync
        (expectation: AssertExpectation<'T>)
        (actual: Expr)
        : Task =
        task {
            let! result = checkAsync expectation actual
            assertPassed result
        }
