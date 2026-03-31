namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Structured information describing a failing property-style test case.</summary>
/// <remarks>
/// A <c>CheckFailure</c> captures the shrunk counterexample, rendered values, and optional replay
/// information needed to understand or reproduce a failing property-style check.
/// </remarks>
type CheckFailure<'Args, 'Actual, 'Expected> =
    {
        /// <summary>The expectation label used for the failing check.</summary>
        Label: string
        /// <summary>A human-readable description of the expected relation.</summary>
        Description: string
        /// <summary>The rendered failing test case.</summary>
        Test: string
        /// <summary>The rendered expected/reference behavior.</summary>
        Expected: string
        /// <summary>The rendered tested behavior.</summary>
        Actual: string
        /// <summary>An optional display string for the expected value itself.</summary>
        ExpectedValueDisplay: string option
        /// <summary>An optional display string for the actual value itself.</summary>
        ActualValueDisplay: string option
        /// <summary>An optional explanation describing why the case failed.</summary>
        Because: string option
        /// <summary>Optional structured detail branches used by the rich failure renderer.</summary>
        Details: FailureDetails option
        /// <summary>The original failing case before shrinking.</summary>
        Original: CheckCase<'Args, 'Actual, 'Expected>
        /// <summary>The final shrunk counterexample, when shrinking succeeded.</summary>
        Shrunk: CheckCase<'Args, 'Actual, 'Expected> option
        /// <summary>The number of generated tests that ran before failure.</summary>
        NumberOfTests: int option
        /// <summary>The number of shrinking steps performed after failure.</summary>
        NumberOfShrinks: int option
        /// <summary>The replay token for reproducing the run, when available.</summary>
        Replay: string option
        /// <summary>The most relevant source location Testify could recover for the failure.</summary>
        SourceLocation: Diagnostics.SourceLocation option
    }

type CheckFailure<'Args, 'Actual, 'Expected> with
    /// <summary>Attempts to reconstruct an FsCheck replay configuration for the failing case.</summary>
    /// <returns>
    /// <c>Some</c> replay configuration when the stored replay token can be parsed; otherwise <c>None</c>.
    /// </returns>
    /// <remarks>
    /// Use this to rerun a property-style check with the same generated data that produced the recorded failure.
    /// </remarks>
    member TryGetReplayConfig: unit -> FsCheck.Config option

/// <summary>Outcome of a property-style check.</summary>
type CheckResult<'Args, 'Actual, 'Expected> =
    /// <summary>The quoted implementation satisfied the expectation for the generated run.</summary>
    | Passed
    /// <summary>The quoted implementation produced a counterexample that violated the expectation.</summary>
    | Failed of CheckFailure<'Args, 'Actual, 'Expected>
    /// <summary>Input generation was exhausted before Testify could report a passing run.</summary>
    | Exhausted of string
    /// <summary>The property infrastructure encountered an error outside the modeled failure relation.</summary>
    | Errored of string

[<RequireQualifiedAccess>]
module Check =
    /// <summary>Runs a property-style check against a reference implementation.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>
    /// <c>Passed</c> when the generated run satisfies the expectation, <c>Failed</c> when a
    /// counterexample is found, <c>Exhausted</c> when generation runs out, or <c>Errored</c> when
    /// the check cannot complete normally.
    /// </returns>
    /// <remarks>
    /// This overload uses <c>CheckConfig.defaultConfig</c> and the default arbitrary resolved for
    /// <c>'Args</c>.
    /// </remarks>
    ///
    /// <example id="check-1">
    /// <code lang="fsharp">
    /// let result =
    ///     &lt;@ List.rev &gt;&gt; List.rev @&gt;
    ///     |> Check.check CheckExpectation.equalToReference id
    /// </code>
    /// </example>
    val check:
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val checkWith:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val checkUsing:
        arbitrary: FsCheck.Arbitrary<'Args> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val checkUsingWith:
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a grouped two-argument property check, supplying a custom arbitrary for the second group.</summary>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    /// <remarks>The first group still uses the default arbitrary resolved from the active configuration.</remarks>
    val checkGroupedUsing:
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedUsing</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedUsingWith:
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a grouped two-argument property check with explicit arbitraries for both groups.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedUsingBoth:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedUsingBoth</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedUsingBothWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a grouped property check where the second arbitrary depends on the first generated group.</summary>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedDependingOn:
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOn</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedDependingOnWith:
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOn</c>, but with an explicit arbitrary for the first generated group.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedDependingOnUsing:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOnUsing</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped check.</returns>
    val checkGroupedDependingOnUsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a two-argument property-style check.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check2:
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check2With:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2</c>, but with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check2Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2Using</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check2UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Runs a three-argument property-style check.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check3:
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check3With:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3</c>, but with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check3Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3Using</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the check.</returns>
    val check3UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Runs a bool-returning property check that requires every generated case to return <c>true</c>.</summary>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    /// <remarks>
    /// This is the preferred API for bool-returning property tests when you do not want to provide a
    /// dummy reference function by hand.
    /// </remarks>
    val checkBeTrue<'Args> :
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeTrue</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeTrueWith<'Args> :
        config: FsCheck.Config ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeTrue</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeTrueUsing<'Args> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeTrue</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeTrueUsingWith<'Args> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs a bool-returning property check that requires every generated case to return <c>false</c>.</summary>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    /// <remarks>
    /// This is the preferred API for bool-returning property tests when you do not want to provide a
    /// dummy reference function by hand.
    /// </remarks>
    val checkBeFalse<'Args> :
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeFalse</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeFalseWith<'Args> :
        config: FsCheck.Config ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeFalse</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeFalseUsing<'Args> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Runs <c>checkBeFalse</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <returns>The structured outcome of the bool check.</returns>
    val checkBeFalseUsingWith<'Args> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            CheckResult<'Args, bool, bool>

    /// <summary>Checks that a quoted function matches the reference implementation by equality.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    ///
    /// <example id="check-equal-1">
    /// <code lang="fsharp">
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt;
    /// |> Check.checkEqual id
    /// </code>
    /// </example>
    val checkEqual<'Args, 'T when 'T : equality> :
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqual</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqualWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqual</c>, but with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqualUsing<'Args, 'T when 'T : equality> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqualUsing</c>, but with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqualUsingWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Runs an equality-based grouped check with an explicit arbitrary for the second group.</summary>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs <c>checkEqualGroupedUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs an equality-based grouped check with explicit arbitraries for both groups.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedUsingBoth<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs <c>checkEqualGroupedUsingBoth</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedUsingBothWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>
    /// Runs an equality-based grouped check where the second arbitrary depends on the generated first group.
    /// </summary>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedDependingOn<'Group1, 'Group2, 'T when 'T : equality> :
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs <c>checkEqualGroupedDependingOn</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedDependingOnWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>
    /// Runs <c>checkEqualGroupedDependingOn</c> with an explicit arbitrary for the first generated group.
    /// </summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedDependingOnUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs <c>checkEqualGroupedDependingOnUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the grouped equality check.</returns>
    val checkEqualGroupedDependingOnUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    /// <summary>Runs an equality-based two-argument property-style check.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual2<'Arg1, 'Arg2, 'T when 'T : equality> :
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    /// <summary>Runs <c>checkEqual2</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual2With<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    /// <summary>Runs <c>checkEqual2</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual2Using<'Arg1, 'Arg2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    /// <summary>Runs <c>checkEqual2Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual2UsingWith<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    /// <summary>Runs an equality-based three-argument property-style check.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual3<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    /// <summary>Runs <c>checkEqual3</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual3With<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    /// <summary>Runs <c>checkEqual3</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual3Using<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    /// <summary>Runs <c>checkEqual3Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the equality check.</returns>
    val checkEqual3UsingWith<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    /// <summary>Runs an equality-based check after projecting tested and reference values to a key.</summary>
    /// <param name="projection">The projection used to derive the comparison key.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the projected equality check.</returns>
    val checkEqualBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Runs <c>checkEqualBy</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="projection">The projection used to derive the comparison key.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the projected equality check.</returns>
    val checkEqualUsingBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Runs an equality-based check with explicit diff options for mismatch rendering.</summary>
    /// <param name="diffOptions">The diff configuration to use when formatting mismatches.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the diff-aware equality check.</returns>
    val checkEqualWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Runs <c>checkEqualWithDiff</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="diffOptions">The diff configuration to use when formatting mismatches.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the diff-aware equality check.</returns>
    val checkEqualUsingWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Runs an equality-based check using a custom comparer.</summary>
    /// <param name="comparer">The comparison function used to compare tested and reference values.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the comparer-based equality check.</returns>
    val checkEqualUsingComparer<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        reference: ('Args -> 'Actual) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Actual>

    /// <summary>Runs <c>checkEqualUsingComparer</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="comparer">The comparison function used to compare tested and reference values.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <returns>The structured outcome of the comparer-based equality check.</returns>
    val checkEqualUsingComparerUsing<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'Actual) ->
        actual: Expr<'Args -> 'Actual> ->
            CheckResult<'Args, 'Actual, 'Actual>

    /// <summary>Converts a check result into a structured Testify failure report when it did not pass.</summary>
    /// <param name="result">The check result to translate.</param>
    /// <returns>
    /// <c>Some</c> failure report when <paramref name="result" /> is <c>Failed</c>, <c>Exhausted</c>,
    /// or <c>Errored</c>; otherwise <c>None</c>.
    /// </returns>
    val toFailureReport: result: CheckResult<'Args, 'Actual, 'Expected> -> TestifyFailureReport option

    /// <summary>Renders a check result with the supplied reporting options.</summary>
    /// <param name="options">The report rendering options to use.</param>
    /// <param name="result">The check result to render.</param>
    /// <returns>A human-readable string suitable for terminal output or test failures.</returns>
    val toDisplayStringWith: options: TestifyReportOptions -> result: CheckResult<'Args, 'Actual, 'Expected> -> string

    /// <summary>Renders a check result using the current Testify report options.</summary>
    /// <param name="result">The check result to render.</param>
    /// <returns>A human-readable string suitable for terminal output or test failures.</returns>
    val toDisplayString: result: CheckResult<'Args, 'Actual, 'Expected> -> string

    /// <summary>Raises an exception when a property-style check result does not pass.</summary>
    /// <param name="result">The result to validate.</param>
    /// <exception cref="System.Exception">
    /// Raised when <paramref name="result" /> is <c>Failed</c>, <c>Exhausted</c>, or <c>Errored</c>.
    /// The exception message contains the rendered check result.
    /// </exception>
    val assertPassed: result: CheckResult<'Args, 'Actual, 'Expected> -> unit

    /// <summary>Opaque collector that stores multiple property-check results for later aggregation.</summary>
    /// <remarks>
    /// Collectors are useful when you want several independent property checks to run and report
    /// together instead of stopping at the first failure.
    /// </remarks>
    type Collector<'Args, 'Actual, 'Expected>

    /// <summary>Helpers for collecting multiple independent property-check results.</summary>
    /// <remarks>
    /// Collector helpers use the non-throwing <c>check</c> APIs internally. Results are stored in
    /// insertion order and no exception is raised until <c>assertAll</c> is called.
    /// </remarks>
    [<RequireQualifiedAccess>]
    module Collect =
        /// <summary>Creates an empty check collector.</summary>
        /// <returns>A new collector with no stored results.</returns>
        val create: unit -> Collector<'Args, 'Actual, 'Expected>

        /// <summary>Runs a check, stores its result, and returns that result.</summary>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
        /// <param name="reference">The reference implementation that defines the expected behavior.</param>
        /// <param name="actual">The quoted implementation under test.</param>
        /// <returns>The stored check result.</returns>
        val add:
            collector: Collector<'Args, 'Actual, 'Expected> ->
            expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            reference: ('Args -> 'Expected) ->
            actual: Expr<'Args -> 'Actual> ->
                CheckResult<'Args, 'Actual, 'Expected>

        /// <summary>Runs a check with an explicit FsCheck configuration, stores its result, and returns that result.</summary>
        /// <param name="config">The FsCheck configuration to use for the run.</param>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
        /// <param name="reference">The reference implementation that defines the expected behavior.</param>
        /// <param name="actual">The quoted implementation under test.</param>
        /// <returns>The stored check result.</returns>
        val addWith:
            config: FsCheck.Config ->
            collector: Collector<'Args, 'Actual, 'Expected> ->
            expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            reference: ('Args -> 'Expected) ->
            actual: Expr<'Args -> 'Actual> ->
                CheckResult<'Args, 'Actual, 'Expected>

        /// <summary>Runs a check with an explicit arbitrary, stores its result, and returns that result.</summary>
        /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
        /// <param name="reference">The reference implementation that defines the expected behavior.</param>
        /// <param name="actual">The quoted implementation under test.</param>
        /// <returns>The stored check result.</returns>
        val addUsing:
            arbitrary: FsCheck.Arbitrary<'Args> ->
            collector: Collector<'Args, 'Actual, 'Expected> ->
            expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            reference: ('Args -> 'Expected) ->
            actual: Expr<'Args -> 'Actual> ->
                CheckResult<'Args, 'Actual, 'Expected>

        /// <summary>Runs a check with explicit configuration and arbitrary, stores its result, and returns that result.</summary>
        /// <param name="config">The FsCheck configuration to use for the run.</param>
        /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
        /// <param name="collector">The collector that should receive the result.</param>
        /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
        /// <param name="reference">The reference implementation that defines the expected behavior.</param>
        /// <param name="actual">The quoted implementation under test.</param>
        /// <returns>The stored check result.</returns>
        val addUsingWith:
            config: FsCheck.Config ->
            arbitrary: FsCheck.Arbitrary<'Args> ->
            collector: Collector<'Args, 'Actual, 'Expected> ->
            expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            reference: ('Args -> 'Expected) ->
            actual: Expr<'Args -> 'Actual> ->
                CheckResult<'Args, 'Actual, 'Expected>

        /// <summary>Returns the collected check results in insertion order.</summary>
        /// <param name="collector">The collector whose results should be returned.</param>
        /// <returns>All stored results, oldest first.</returns>
        val toResultList:
            collector: Collector<'Args, 'Actual, 'Expected> ->
                CheckResult<'Args, 'Actual, 'Expected> list

        /// <summary>Throws once when any collected check did not pass.</summary>
        /// <param name="collector">The collector whose stored results should be validated.</param>
        /// <exception cref="System.Exception">
        /// Raised when the collector contains one or more non-passing checks. The exception message
        /// aggregates the rendered failures.
        /// </exception>
        /// <remarks>
        /// This lets you accumulate independent property-check failures and report them together after
        /// all desired checks have run.
        /// </remarks>
        val assertAll: collector: Collector<'Args, 'Actual, 'Expected> -> unit

    /// <summary>Runs a property-style check and raises immediately when it does not pass.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">
    /// Raised when the check result is <c>Failed</c>, <c>Exhausted</c>, or <c>Errored</c>.
    /// </exception>
    val should:
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            unit

    /// <summary>Runs <c>should</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldWith:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            unit

    /// <summary>Runs <c>should</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldUsing:
        arbitrary: FsCheck.Arbitrary<'Args> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            unit

    /// <summary>Runs <c>should</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldUsingWith:
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
        reference: ('Args -> 'Expected) ->
        actual: Expr<'Args -> 'Actual> ->
            unit

    /// <summary>Runs a grouped property-style check and raises immediately when it does not pass.</summary>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedUsing:
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>shouldGroupedUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedUsingWith:
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs a grouped property-style check with explicit arbitraries for both groups.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedUsingBoth:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>shouldGroupedUsingBoth</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedUsingBothWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>
    /// Runs a grouped property-style check where the second arbitrary depends on the generated first group.
    /// </summary>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedDependingOn:
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>shouldGroupedDependingOn</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedDependingOnWith:
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>
    /// Runs <c>shouldGroupedDependingOn</c> with an explicit arbitrary for the first generated group.
    /// </summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedDependingOnUsing:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>shouldGroupedDependingOnUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="expectation">The reusable relation that should hold for each generated pair of groups.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldGroupedDependingOnUsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
            unit

    /// <summary>Runs a two-argument property-style check and raises immediately when it does not pass.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should2:
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should2</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should2With:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should2</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should2Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should2Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input pair.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should2UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
            unit

    /// <summary>Runs a three-argument property-style check and raises immediately when it does not pass.</summary>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should3:
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should3</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should3With:
        config: FsCheck.Config ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should3</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should3Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            unit

    /// <summary>Runs <c>should3Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="expectation">The reusable relation that should hold for each generated input triple.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val should3UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
            unit

    /// <summary>Runs a bool-returning property check and raises when any generated case returns <c>false</c>.</summary>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    /// <example id="check-should-be-true-1">
    /// <code lang="fsharp">
    /// &lt;@ fun value -&gt; value = value @&gt;
    /// |&gt; Check.shouldBeTrue
    /// </code>
    /// </example>
    val shouldBeTrue<'Args> :
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeTrue</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeTrueWith<'Args> :
        config: FsCheck.Config ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeTrue</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    /// <example id="check-should-be-true-using-1">
    /// <code lang="fsharp">
    /// let arb = Arbitraries.from&lt;int&gt;
    ///
    /// &lt;@ fun value -&gt; value = value @&gt;
    /// |&gt; Check.shouldBeTrueUsing arb
    /// </code>
    /// </example>
    val shouldBeTrueUsing<'Args> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeTrue</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeTrueUsingWith<'Args> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs a bool-returning property check and raises when any generated case returns <c>true</c>.</summary>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeFalse<'Args> :
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeFalse</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeFalseWith<'Args> :
        config: FsCheck.Config ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeFalse</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeFalseUsing<'Args> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs <c>shouldBeFalse</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="actual">The quoted bool-returning implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the bool check does not pass.</exception>
    val shouldBeFalseUsingWith<'Args> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> bool> ->
            unit

    /// <summary>Runs an equality-based property check and raises immediately when it does not pass.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    ///
    /// <example id="check-should-equal-1">
    /// <code lang="fsharp">
    /// &lt;@ List.sort @&gt;
    /// |> Check.shouldEqual List.sort
    /// </code>
    /// </example>
    val shouldEqual<'Args, 'T when 'T : equality> : reference: ('Args -> 'T) -> actual: Expr<'Args -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsing<'Args, 'T when 'T : equality> :
        arbitrary: FsCheck.Arbitrary<'Args> -> reference: ('Args -> 'T) -> actual: Expr<'Args -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config -> reference: ('Args -> 'T) -> actual: Expr<'Args -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual</c> with explicit FsCheck configuration and arbitrary.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsingWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            unit

    /// <summary>Runs an equality-based grouped check and raises immediately when it does not pass.</summary>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqualGroupedUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs an equality-based grouped check with explicit arbitraries for both groups.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedUsingBoth<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqualGroupedUsingBoth</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="arbitrary2">The arbitrary used to generate the second argument group.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedUsingBothWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>
    /// Runs an equality-based grouped check where the second arbitrary depends on the generated first group.
    /// </summary>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedDependingOn<'Group1, 'Group2, 'T when 'T : equality> :
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqualGroupedDependingOn</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedDependingOnWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>
    /// Runs <c>shouldEqualGroupedDependingOn</c> with an explicit arbitrary for the first generated group.
    /// </summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedDependingOnUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqualGroupedDependingOnUsing</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument group.</param>
    /// <param name="provideArbitrary2">
    /// A function that builds the second-group arbitrary from the already generated first-group value.
    /// </param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualGroupedDependingOnUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
            unit

    /// <summary>Runs an equality-based two-argument property check and raises immediately when it does not pass.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual2<'Arg1, 'Arg2, 'T when 'T : equality> :
        reference: ('Arg1 -> 'Arg2 -> 'T) -> actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual2</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual2With<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config -> reference: ('Arg1 -> 'Arg2 -> 'T) -> actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual2</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual2Using<'Arg1, 'Arg2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> -> reference: ('Arg1 -> 'Arg2 -> 'T) -> actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual2Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual2UsingWith<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
            unit

    /// <summary>Runs an equality-based three-argument property check and raises immediately when it does not pass.</summary>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual3<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) -> actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> -> unit

    /// <summary>Runs <c>shouldEqual3</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual3With<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqual3</c> with an explicit arbitrary for the first argument.</summary>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual3Using<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            unit

    /// <summary>Runs <c>shouldEqual3Using</c> with an explicit FsCheck configuration.</summary>
    /// <param name="config">The FsCheck configuration to use for the run.</param>
    /// <param name="arbitrary1">The arbitrary used to generate the first argument.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqual3UsingWith<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
            unit

    /// <summary>Runs an equality-based check after projecting tested and reference values to a key.</summary>
    /// <param name="projection">The projection used to derive the comparison key.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) -> reference: ('Args -> 'T) -> actual: Expr<'Args -> 'T> -> unit

    /// <summary>Runs <c>shouldEqualBy</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="projection">The projection used to derive the comparison key.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsingBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            unit

    /// <summary>Runs an equality-based check with explicit diff options for mismatch rendering.</summary>
    /// <param name="diffOptions">The diff configuration to use when formatting mismatches.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions -> reference: ('Args -> 'T) -> actual: Expr<'Args -> 'T> -> unit

    /// <summary>Runs <c>shouldEqualWithDiff</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="diffOptions">The diff configuration to use when formatting mismatches.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsingWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'T) ->
        actual: Expr<'Args -> 'T> ->
            unit

    /// <summary>Runs an equality-based check using a custom comparer.</summary>
    /// <param name="comparer">The comparison function used to compare tested and reference values.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsingComparer<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) -> reference: ('Args -> 'Actual) -> actual: Expr<'Args -> 'Actual> -> unit

    /// <summary>Runs <c>shouldEqualUsingComparer</c> with an explicit arbitrary for the input domain.</summary>
    /// <param name="comparer">The comparison function used to compare tested and reference values.</param>
    /// <param name="arbitrary">The arbitrary used to generate <c>'Args</c> inputs.</param>
    /// <param name="reference">The reference implementation that defines the expected behavior.</param>
    /// <param name="actual">The quoted implementation under test.</param>
    /// <exception cref="System.Exception">Raised when the check does not pass.</exception>
    val shouldEqualUsingComparerUsing<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        reference: ('Args -> 'Actual) ->
        actual: Expr<'Args -> 'Actual> ->
            unit
