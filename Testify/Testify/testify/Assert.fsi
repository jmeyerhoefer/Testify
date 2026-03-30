namespace MiniLib.Testify

open System.Threading.Tasks
open Microsoft.FSharp.Quotations

/// <summary>Structured information describing why an assertion failed.</summary>
type AssertFailure =
    {
        /// <summary>The expectation label, such as <c>EqualTo</c> or <c>Throws</c>.</summary>
        Label: string
        /// <summary>The rendered test expression or explicit label that was checked.</summary>
        Test: string
        /// <summary>A human-readable description of the expected behavior.</summary>
        Expected: string
        /// <summary>The observed value or exception as rendered by Testify.</summary>
        Actual: string
        /// <summary>An optional explanation describing why the check failed.</summary>
        Because: string option
        /// <summary>Optional structured detail branches used by the rich failure renderer.</summary>
        Details: FailureDetails option
        /// <summary>The most relevant source location Testify could recover for the failure.</summary>
        SourceLocation: Diagnostics.SourceLocation option
    }

/// <summary>Outcome of an assertion check.</summary>
type AssertResult =
    | Passed
    | Failed of AssertFailure

[<RequireQualifiedAccess>]
module Assert =
    /// <summary>Evaluates an assertion expectation against a quoted expression.</summary>
    ///
    /// <param name="expectation">The reusable expectation to verify.</param>
    /// <param name="actual">The quoted expression under test.</param>
    ///
    /// <returns>
    /// <c>Passed</c> when the expression satisfies the expectation; otherwise a structured failure.
    /// </returns>
    ///
    /// <example id="assert-check-1">
    /// <code lang="fsharp">
    /// let result =
    ///     Assert.check (AssertExpectation.equalTo 3) &lt;@ 1 + 2 @&gt;
    /// </code>
    /// Evaluates to <c>Passed</c>.
    /// </example>
    val check: expectation: AssertExpectation<'T> -> actual: Expr<'T> -> AssertResult

    /// <summary>
    /// Evaluates an assertion expectation like <c>check</c>, but uses the supplied test label in failure output.
    /// </summary>
    val checkNamed: test: string -> expectation: AssertExpectation<'T> -> actual: Expr<'T> -> AssertResult

    /// <summary>Evaluates an asynchronous assertion and returns the result in a task.</summary>
    ///
    /// <remarks>
    /// Use this with quotations that produce <c>Async&lt;'T&gt;</c> or <c>Task&lt;'T&gt;</c>-style results.
    /// </remarks>
    val checkAsync: expectation: AssertExpectation<'T> -> actual: Expr -> Task<AssertResult>

    /// <summary>Converts a failed assertion into a structured Testify failure report.</summary>
    val toFailureReport: result: AssertResult -> TestifyFailureReport option

    /// <summary>Renders an assertion result using the supplied report options.</summary>
    val toDisplayStringWith: options: TestifyReportOptions -> result: AssertResult -> string

    /// <summary>Renders an assertion result using the current Testify report options.</summary>
    val toDisplayString: result: AssertResult -> string

    /// <summary>Raises an exception when the supplied result is not <c>Passed</c>.</summary>
    val assertPassed: result: AssertResult -> unit

    /// <summary>Runs an assertion and raises an exception when it fails.</summary>
    ///
    /// <example id="assert-should-1">
    /// <code lang="fsharp">
    /// Assert.should (AssertExpectation.equalTo "MiniLib") &lt;@ "Mini" + "Lib" @&gt;
    /// </code>
    /// </example>
    val should: expectation: AssertExpectation<'T> -> actual: Expr<'T> -> unit

    /// <summary>Runs a named assertion and raises an exception when it fails.</summary>
    val shouldNamed: test: string -> expectation: AssertExpectation<'T> -> actual: Expr<'T> -> unit

    /// <summary>Runs an asynchronous assertion and raises an exception when it fails.</summary>
    val shouldAsync: expectation: AssertExpectation<'T> -> actual: Expr -> Task
