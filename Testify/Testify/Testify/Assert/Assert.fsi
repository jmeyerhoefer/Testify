namespace Testify

open System.Threading.Tasks
open Microsoft.FSharp.Quotations

/// <summary>Structured information describing one failed assertion.</summary>
/// <remarks>
/// <para>
/// Testify fills this record when an <c>Assert</c> operation does not satisfy its expectation.
/// The values are already formatted for diagnostics and report rendering.
/// </para>
/// </remarks>
type AssertFailure =
    {
        /// <summary>The stable expectation label, such as <c>EqualTo</c> or <c>Throws</c>.</summary>
        Label: string
        /// <summary>The rendered test expression or explicit label that was checked.</summary>
        Test: string
        /// <summary>The human-readable expectation description shown in failure output.</summary>
        Expected: string
        /// <summary>The rendered observed value or exception.</summary>
        Actual: string
        /// <summary>An optional explanation describing why the assertion failed.</summary>
        Because: string option
        /// <summary>Optional structured nested details used by the rich failure renderer.</summary>
        Details: FailureDetails option
        /// <summary>The most relevant source location that Testify could recover for the failure.</summary>
        SourceLocation: Diagnostics.SourceLocation option
    }

/// <summary>Outcome of an assertion check.</summary>
type AssertResult =
    /// <summary>The quoted expression satisfied the expectation.</summary>
    | Passed
    /// <summary>The quoted expression did not satisfy the expectation.</summary>
    | Failed of AssertFailure

[<RequireQualifiedAccess>]
module Assert =
    /// <summary>Evaluates an assertion expectation against a quoted expression.</summary>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted expression under test.</param>
    /// <returns>
    /// <c>Passed</c> when the quoted expression satisfies the expectation; otherwise a structured
    /// <c>Failed</c> result describing the mismatch.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The quotation is evaluated by this call. Use <c>check</c> when you want to inspect the result
    /// yourself instead of failing immediately.
    /// </para>
    /// </remarks>
    /// <example id="assert-check-1">
    /// <code lang="fsharp">
    /// let result =
    ///     &lt;@ 1 + 2 @&gt;
    ///     |> Assert.check (AssertExpectation.equalTo 3)
    /// </code>
    /// </example>
    val check: expectation: AssertExpectation<'T> -> actual: Expr<'T> -> AssertResult

    /// <summary>
    /// Evaluates an assertion like <c>check</c>, but uses the supplied test label in failure output.
    /// </summary>
    /// <param name="test">The human-readable label to show instead of the rendered quotation.</param>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted expression under test.</param>
    /// <returns>
    /// <c>Passed</c> when the quoted expression satisfies the expectation; otherwise a structured
    /// <c>Failed</c> result that uses <paramref name="test" /> in its report output.
    /// </returns>
    /// <remarks>The quotation is still evaluated by this call even when a custom label is supplied.</remarks>
    val checkNamed: test: string -> expectation: AssertExpectation<'T> -> actual: Expr<'T> -> AssertResult

    /// <summary>Evaluates an asynchronous assertion and returns the result inside a task.</summary>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">
    /// The quoted asynchronous expression under test, typically producing <c>Async&lt;'T&gt;</c>,
    /// <c>Task&lt;'T&gt;</c>, or another awaitable value supported by Testify.
    /// </param>
    /// <returns>
    /// A task that completes with <c>Passed</c> when the asynchronous computation satisfies the
    /// expectation, or <c>Failed</c> when it does not.
    /// </returns>
    /// <remarks>The quoted asynchronous computation is awaited by this call.</remarks>
    val checkAsync: expectation: AssertExpectation<'T> -> actual: Expr -> Task<AssertResult>

    /// <summary>Converts a failed assertion into a structured Testify failure report.</summary>
    /// <param name="result">The assertion result to translate.</param>
    /// <returns>
    /// <c>Some</c> failure report when <paramref name="result" /> is <c>Failed</c>; otherwise <c>None</c>.
    /// </returns>
    val toFailureReport: result: AssertResult -> TestifyFailureReport option

    /// <summary>Renders an assertion result with the supplied report options.</summary>
    /// <param name="options">The report rendering options to use.</param>
    /// <param name="result">The assertion result to render.</param>
    /// <returns>A human-readable string suitable for terminal output or test failures.</returns>
    val toDisplayStringWith: options: TestifyReportOptions -> result: AssertResult -> string

    /// <summary>Renders an assertion result using the current Testify report options.</summary>
    /// <param name="result">The assertion result to render.</param>
    /// <returns>A human-readable string suitable for terminal output or test failures.</returns>
    val toDisplayString: result: AssertResult -> string

    /// <summary>Raises when the supplied assertion result is not <c>Passed</c>.</summary>
    /// <param name="result">The result to validate.</param>
    /// <exception cref="System.Exception">
    /// Raised when <paramref name="result" /> is <c>Failed</c>. The exception message contains the
    /// rendered assertion failure.
    /// </exception>
    val assertPassed: result: AssertResult -> unit

    /// <summary>Opaque collector that stores multiple assertion results for later aggregation.</summary>
    /// <remarks>
    /// Collectors are useful when you want several independent assertions to run and report together
    /// instead of stopping at the first failure.
    /// </remarks>
    type Collector

    /// <summary>Helpers for collecting multiple independent assertion results.</summary>
    /// <remarks>
    /// <para>
    /// Collector helpers use the non-throwing <c>check</c> APIs internally. Results are stored in
    /// insertion order and no exception is raised until <c>assertAll</c> is called.
    /// </para>
    /// </remarks>
    [<RequireQualifiedAccess>]
    module Collect =
        /// <summary>Creates an empty assertion collector.</summary>
        /// <returns>A new collector with no stored results.</returns>
        val create: unit -> Collector

        /// <summary>Runs an assertion, stores its result, and returns that result.</summary>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="expectation">The reusable expectation to verify.</param>
        /// <param name="actual">The quoted expression under test.</param>
        /// <returns>The stored assertion result.</returns>
        val add:
            collector: Collector ->
            expectation: AssertExpectation<'T> ->
            actual: Expr<'T> ->
                AssertResult

        /// <summary>Runs a named assertion, stores its result, and returns that result.</summary>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="test">The human-readable label to show in failure output.</param>
        /// <param name="expectation">The reusable expectation to verify.</param>
        /// <param name="actual">The quoted expression under test.</param>
        /// <returns>The stored assertion result.</returns>
        val addNamed:
            collector: Collector ->
            test: string ->
            expectation: AssertExpectation<'T> ->
            actual: Expr<'T> ->
                AssertResult

        /// <summary>Returns the collected assertion results in insertion order.</summary>
        /// <param name="collector">The collector whose results should be returned.</param>
        /// <returns>All stored results, oldest first.</returns>
        val toResultList: collector: Collector -> AssertResult list

        /// <summary>Raises once when any collected assertion failed.</summary>
        /// <param name="collector">The collector whose stored results should be validated.</param>
        /// <exception cref="System.Exception">
        /// Raised when the collector contains one or more failed assertions. The exception message
        /// aggregates the rendered failures.
        /// </exception>
        /// <remarks>
        /// This method lets you accumulate independent failures and report them together after all
        /// desired assertions have run.
        /// </remarks>
        val assertAll: collector: Collector -> unit

    /// <summary>Runs an assertion and raises immediately when it fails.</summary>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted expression under test.</param>
    /// <exception cref="System.Exception">
    /// Raised when the quoted expression does not satisfy <paramref name="expectation" />.
    /// </exception>
    /// <remarks>The quotation is evaluated by this call.</remarks>
    /// <example id="assert-should-1">
    /// <code lang="fsharp">
    /// &lt;@ "Mini" + "Lib" @&gt;
    /// |> Assert.should (AssertExpectation.equalTo "MiniLib")
    /// </code>
    /// </example>
    val should: expectation: AssertExpectation<'T> -> actual: Expr<'T> -> unit

    /// <summary>Runs a named assertion and raises immediately when it fails.</summary>
    /// <param name="test">The human-readable label to show in failure output.</param>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted expression under test.</param>
    /// <exception cref="System.Exception">
    /// Raised when the quoted expression does not satisfy <paramref name="expectation" />.
    /// </exception>
    val shouldNamed: test: string -> expectation: AssertExpectation<'T> -> actual: Expr<'T> -> unit

    /// <summary>Runs an asynchronous assertion and raises immediately when it fails.</summary>
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted asynchronous expression under test.</param>
    /// <returns>A task that completes when the assertion has finished running.</returns>
    /// <exception cref="System.Exception">
    /// Raised when the awaited computation does not satisfy <paramref name="expectation" />.
    /// </exception>
    val shouldAsync: expectation: AssertExpectation<'T> -> actual: Expr -> Task
