namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Structured information describing a failing property-style test case.</summary>
/// <typeparam name="'Args">
/// The generated input type used for the property run. This is the full argument object passed to both
/// the tested quotation and the reference function.
/// </typeparam>
/// <typeparam name="'Actual">
/// The successful result type produced by the tested quotation when it does not throw.
/// </typeparam>
/// <typeparam name="'Expected">
/// The successful result type produced by the reference function when it does not throw.
/// </typeparam>
type CheckFailure<'Args, 'Actual, 'Expected> =
    {
        /// <summary>The stable expectation label recorded for the failure.</summary>
        Label: string
        /// <summary>The human-readable expectation description recorded for the failure.</summary>
        Description: string
        /// <summary>The rendered failing test expression or shrunk property case.</summary>
        Test: string
        /// <summary>The rendered expected/reference side shown in failure output.</summary>
        Expected: string
        /// <summary>The rendered actual/tested side shown in failure output.</summary>
        Actual: string
        /// <summary>The raw rendered expected value, when Testify can provide one separately.</summary>
        ExpectedValueDisplay: string option
        /// <summary>The raw rendered actual value, when Testify can provide one separately.</summary>
        ActualValueDisplay: string option
        /// <summary>An optional explanation describing why the values or observations differ.</summary>
        Because: string option
        /// <summary>Optional structured nested details used by rich renderers.</summary>
        Details: FailureDetails option
        /// <summary>The original unshrunk failing case before FsCheck shrinking.</summary>
        Original: CheckCase<'Args, 'Actual, 'Expected>
        /// <summary>The final shrunk failing case, when shrinking found a smaller counterexample.</summary>
        Shrunk: CheckCase<'Args, 'Actual, 'Expected> option
        /// <summary>The number of generated tests executed before the failure was reported.</summary>
        NumberOfTests: int option
        /// <summary>The number of shrinking steps applied to reach the final counterexample.</summary>
        NumberOfShrinks: int option
        /// <summary>The replay token that can often be turned back into an FsCheck replay configuration.</summary>
        Replay: string option
        /// <summary>The most relevant source location that Testify could recover for the failing check.</summary>
        SourceLocation: Diagnostics.SourceLocation option
    }

type CheckFailure<'Args, 'Actual, 'Expected> with
    /// <summary>
    /// Attempts to reconstruct an FsCheck replay configuration for the failing run from the stored replay token.
    /// </summary>
    /// <returns>
    /// <c>Some</c> replay-enabled config when the failing result carried a replay token that Testify could
    /// parse; otherwise <c>None</c>.
    /// </returns>
    member TryGetReplayConfig: unit -> FsCheck.Config option

/// <summary>Outcome of a property-style check.</summary>
/// <typeparam name="'Args">
/// The generated input type used for the property run.
/// </typeparam>
/// <typeparam name="'Actual">
/// The successful result type produced by the tested quotation.
/// </typeparam>
/// <typeparam name="'Expected">
/// The successful result type produced by the reference function.
/// </typeparam>
type CheckResult<'Args, 'Actual, 'Expected> =
    /// <summary>The property passed for the configured generated inputs.</summary>
    | Passed
    /// <summary>The property found a counterexample and produced structured failure details.</summary>
    | Failed of failure: CheckFailure<'Args, 'Actual, 'Expected>
    /// <summary>FsCheck could not generate enough suitable inputs to complete the check.</summary>
    | Exhausted of reason: string
    /// <summary>Testify or user-supplied property logic encountered an unexpected runtime error.</summary>
    | Errored of message: string

/// <summary>Property-style runner that returns a structured <c>CheckResult</c>.</summary>
[<AbstractClass; Sealed>]
type Check =
    /// <summary>
    /// Runs a property-style check against a reference implementation and returns the structured result.
    /// </summary>
    /// <param name="expectation">The relation that should hold between tested code and the reference.</param>
    /// <param name="reference">
    /// The trusted reference implementation. It receives the generated argument value and produces the
    /// expected outcome for comparison.
    /// </param>
    /// <param name="actual">The quoted tested function under evaluation.</param>
    /// <param name="config">
    /// Optional FsCheck configuration override. When omitted, Testify starts from the default configuration
    /// installed through <c>Testify.currentConfiguration()</c> and related configuration helpers. Common
    /// sources are <c>CheckConfig.defaultConfig</c>, <c>CheckConfig.thorough</c>,
    /// <c>CheckConfig.withMaxTest</c>, <c>CheckConfig.withEndSize</c>, and <c>CheckConfig.withReplay</c>.
    /// </param>
    /// <param name="arbitrary">
    /// Optional custom arbitrary used to generate and shrink <c>'Args</c>. When omitted, Testify resolves
    /// the default arbitrary for <c>'Args</c> from the effective FsCheck configuration. Common sources are
    /// <c>Arbitraries.from&lt;'T&gt;</c>, <c>Arbitraries.fromGen</c>, <c>Arbitraries.tuple2</c>,
    /// <c>Arbitraries.tuple3</c>, and custom mapped/filter arbitraries.
    /// </param>
    /// <returns>
    /// <c>Passed</c>, <c>Failed</c>, <c>Exhausted</c>, or <c>Errored</c> depending on the FsCheck run
    /// and the observed tested/reference behavior.
    /// </returns>
    /// <remarks>
    /// Use <c>result</c> when you want to inspect or render failures yourself. Use <c>should</c> for the
    /// fail-fast twin.
    /// </remarks>
    /// <seealso cref="M:Testify.Check.should``3(Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config},Microsoft.FSharp.Core.FSharpOption{FsCheck.Arbitrary{``0}})">
    /// Use <c>should</c> when the surrounding test should fail immediately instead of returning a <c>CheckResult</c>.
    /// </seealso>
    /// <example id="check-result-1">
    /// <code lang="fsharp">
    /// let result =
    ///     Check.result(
    ///         CheckExpectation.equalToReference,
    ///         List.rev,
    ///         &lt;@ List.rev @&gt;)
    /// </code>
    /// </example>
    static member result :
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> *
        reference: ('Args -> 'Expected) *
        actual: Expr<'Args -> 'Actual> *
        ?config: FsCheck.Config *
        ?arbitrary: FsCheck.Arbitrary<'Args> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>
    /// Runs a property-style check against a reference implementation and raises immediately when it does
    /// not pass.
    /// </summary>
    /// <param name="expectation">The relation that should hold between tested code and the reference.</param>
    /// <param name="reference">The trusted reference implementation.</param>
    /// <param name="actual">The quoted tested function under evaluation.</param>
    /// <param name="config">
    /// Optional FsCheck configuration override. Typical sources are <c>CheckConfig</c> helpers.
    /// </param>
    /// <param name="arbitrary">
    /// Optional custom arbitrary for <c>'Args</c>. Typical sources are <c>Arbitraries</c> and
    /// <c>Generators</c> helpers.
    /// </param>
    /// <exception cref="System.Exception">
    /// Raised when the check result is anything other than <c>Passed</c>. The message contains the
    /// rendered property failure or infrastructure error.
    /// </exception>
    /// <seealso cref="M:Testify.Check.result``3(Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config},Microsoft.FSharp.Core.FSharpOption{FsCheck.Arbitrary{``0}})">
    /// Non-throwing twin that returns a <c>CheckResult</c> for later inspection or rendering.
    /// </seealso>
    /// <example id="check-should-1">
    /// <code lang="fsharp">
    /// Check.should(
    ///     CheckExpectation.equalToReference,
    ///     List.sort,
    ///     &lt;@ List.sort @&gt;)
    /// </code>
    /// </example>
    static member should :
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> *
        reference: ('Args -> 'Expected) *
        actual: Expr<'Args -> 'Actual> *
        ?config: FsCheck.Config *
        ?arbitrary: FsCheck.Arbitrary<'Args> ->
            unit

    /// <summary>
    /// Runs an advanced property-style check where the caller controls the surrounding FsCheck property
    /// structure and Testify supplies the per-case verifier.
    /// </summary>
    /// <param name="buildProperty">
    /// A callback that receives the Testify verifier and must build an FsCheck property around it. Use
    /// this for nested or dependent quantification that is awkward to express with one plain
    /// <c>Arbitrary&lt;'Args&gt;</c>.
    /// </param>
    /// <param name="expectation">The relation that should hold between tested code and the reference.</param>
    /// <param name="reference">The trusted reference implementation.</param>
    /// <param name="actual">The quoted tested function under evaluation.</param>
    /// <param name="config">
    /// Optional FsCheck configuration override. <c>resultBy</c> intentionally does not accept an
    /// <c>arbitrary</c> parameter because the custom property builder owns quantification.
    /// </param>
    /// <returns>
    /// <c>Passed</c>, <c>Failed</c>, <c>Exhausted</c>, or <c>Errored</c> depending on the property run.
    /// </returns>
    /// <seealso cref="M:Testify.Check.shouldBy``3(Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.FSharpFunc{``0,System.Boolean},FsCheck.Property},Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config})">
    /// Use <c>shouldBy</c> when the callback-built property should fail the surrounding test immediately.
    /// </seealso>
    /// <example id="check-resultby-1">
    /// <code lang="fsharp">
    /// let result =
    ///     Check.resultBy(
    ///         (fun verify ->
    ///             FsCheck.Prop.forAll Arbitraries.from&lt;int * int&gt; verify),
    ///         CheckExpectation.isTrue,
    ///         (fun _ -> true),
    ///         &lt;@ fun (a, b) -> a + b = b + a @&gt;)
    /// </code>
    /// </example>
    static member resultBy :
        buildProperty: (('Args -> bool) -> FsCheck.Property) *
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> *
        reference: ('Args -> 'Expected) *
        actual: Expr<'Args -> 'Actual> *
        ?config: FsCheck.Config ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>
    /// Runs a callback-built property check and raises immediately when it does not pass.
    /// </summary>
    /// <param name="buildProperty">The callback that constructs the surrounding FsCheck property.</param>
    /// <param name="expectation">The relation that should hold between tested code and the reference.</param>
    /// <param name="reference">The trusted reference implementation.</param>
    /// <param name="actual">The quoted tested function under evaluation.</param>
    /// <param name="config">Optional FsCheck configuration override.</param>
    /// <exception cref="System.Exception">
    /// Raised when the check result is anything other than <c>Passed</c>.
    /// </exception>
    /// <seealso cref="M:Testify.Check.resultBy``3(Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.FSharpFunc{``0,System.Boolean},FsCheck.Property},Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config})">
    /// Non-throwing twin that returns a <c>CheckResult</c> for callback-built properties.
    /// </seealso>
    /// <example id="check-shouldby-1">
    /// <code lang="fsharp">
    /// Check.shouldBy(
    ///     (fun verify ->
    ///         FsCheck.Prop.forAll Arbitraries.from&lt;int list&gt; verify),
    ///     CheckExpectation.isTrue,
    ///     (fun _ -> true),
    ///     &lt;@ fun xs -> List.rev (List.rev xs) = xs @&gt;)
    /// </code>
    /// </example>
    static member shouldBy :
        buildProperty: (('Args -> bool) -> FsCheck.Property) *
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> *
        reference: ('Args -> 'Expected) *
        actual: Expr<'Args -> 'Actual> *
        ?config: FsCheck.Config ->
            unit

    /// <summary>Raises when the supplied property result is not <c>Passed</c>.</summary>
    /// <param name="result">The property result to validate.</param>
    /// <exception cref="System.Exception">
    /// Raised when <paramref name="result" /> is <c>Failed</c>, <c>Exhausted</c>, or <c>Errored</c>.
    /// </exception>
    /// <seealso cref="M:Testify.Check.result``3(Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config},Microsoft.FSharp.Core.FSharpOption{FsCheck.Arbitrary{``0}})">
    /// Use <c>result</c> when you want to inspect the returned status before deciding whether to raise.
    /// </seealso>
    static member assertPassed :
        result: CheckResult<'Args, 'Actual, 'Expected> -> unit

    /// <summary>Converts a failing property result into a structured Testify failure report.</summary>
    /// <param name="result">The property result to translate.</param>
    /// <returns>
    /// <c>Some</c> structured failure report when <paramref name="result" /> is <c>Failed</c>,
    /// <c>Exhausted</c>, or <c>Errored</c>; otherwise <c>None</c>.
    /// </returns>
    static member toFailureReport :
        result: CheckResult<'Args, 'Actual, 'Expected> -> TestifyFailureReport option

    /// <summary>Renders a property result with the supplied report options.</summary>
    /// <param name="options">
    /// Rendering options that control output format, verbosity, and related report shaping.
    /// See <c>TestifyReportOptions</c> and its helper modules for available options.
    /// </param>
    /// <param name="result">The property result to render.</param>
    /// <returns>The configured rendered representation suitable for terminal output or test failures.</returns>
    static member toDisplayStringWith :
        options: TestifyReportOptions *
        result: CheckResult<'Args, 'Actual, 'Expected> ->
            string

    /// <summary>Renders a property result using the current Testify report options.</summary>
    /// <param name="result">The property result to render.</param>
    /// <returns>The configured rendered representation suitable for terminal output or test failures.</returns>
    static member toDisplayString :
        result: CheckResult<'Args, 'Actual, 'Expected> -> string

/// <summary>
/// Additional helpers around property-style result collection and aggregation.
/// </summary>
[<RequireQualifiedAccess>]
module Check =
    /// <summary>
    /// Collects several <c>CheckResult</c> values so they can be asserted together.
    /// </summary>
    /// <typeparam name="'Args">The generated input type used by the collected property results.</typeparam>
    /// <typeparam name="'Actual">The successful tested-result type used by the collected property results.</typeparam>
    /// <typeparam name="'Expected">The successful reference-result type used by the collected property results.</typeparam>
    type Collector<'Args, 'Actual, 'Expected> =
        private {
            Results: ResizeArray<CheckResult<'Args, 'Actual, 'Expected>>
        }

    /// <summary>Operations on <c>Check.Collector</c>.</summary>
    [<RequireQualifiedAccess>]
    module Collect =
        /// <summary>Creates an empty property-result collector.</summary>
        val create: unit -> Collector<'Args, 'Actual, 'Expected>

        /// <summary>Stores one property result in the collector and returns it.</summary>
        val add :
            collector: Collector<'Args, 'Actual, 'Expected> ->
            result: CheckResult<'Args, 'Actual, 'Expected> ->
                CheckResult<'Args, 'Actual, 'Expected>

        /// <summary>Returns the collected property results in insertion order.</summary>
        val toResultList :
            collector: Collector<'Args, 'Actual, 'Expected> ->
                CheckResult<'Args, 'Actual, 'Expected> list

        /// <summary>Raises once when any collected property result did not pass.</summary>
        val assertAll :
            collector: Collector<'Args, 'Actual, 'Expected> -> unit
