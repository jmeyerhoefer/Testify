namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Structured information describing a failing property-style test case.</summary>
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
    member TryGetReplayConfig: unit -> FsCheck.Config option

/// <summary>Outcome of a property-style check.</summary>
type CheckResult<'Args, 'Actual, 'Expected> =
    | Passed
    | Failed of CheckFailure<'Args, 'Actual, 'Expected>
    | Exhausted of string
    | Errored of string

[<RequireQualifiedAccess>]
module Check =
    /// <summary>Runs a property-style check against a reference implementation.</summary>
    ///
    /// <example id="check-1">
    /// <code lang="fsharp">
    /// let result =
    ///     Check.check
    ///         &lt;@ List.rev &gt;&gt; List.rev @&gt;
    ///         id
    ///         CheckExpectation.equalToReference
    /// </code>
    /// </example>
    val check:
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with an explicit FsCheck configuration.</summary>
    val checkWith:
        config: FsCheck.Config ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with an explicit arbitrary for the input domain.</summary>
    val checkUsing:
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a property-style check with explicit FsCheck configuration and arbitrary.</summary>
    val checkUsingWith:
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckResult<'Args, 'Actual, 'Expected>

    /// <summary>Runs a grouped two-argument property check, supplying a custom arbitrary for the second group.</summary>
    val checkGroupedUsing:
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedUsing</c>, but with an explicit FsCheck configuration.</summary>
    val checkGroupedUsingWith:
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a grouped two-argument property check with explicit arbitraries for both groups.</summary>
    val checkGroupedUsingBoth:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedUsingBoth</c>, but with an explicit FsCheck configuration.</summary>
    val checkGroupedUsingBothWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a grouped property check where the second arbitrary depends on the first generated group.</summary>
    val checkGroupedDependingOn:
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOn</c>, but with an explicit FsCheck configuration.</summary>
    val checkGroupedDependingOnWith:
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOn</c>, but with an explicit arbitrary for the first generated group.</summary>
    val checkGroupedDependingOnUsing:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Like <c>checkGroupedDependingOnUsing</c>, but with an explicit FsCheck configuration.</summary>
    val checkGroupedDependingOnUsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            CheckResult<'Group1 * 'Group2, 'Actual, 'Expected>

    /// <summary>Runs a two-argument property-style check.</summary>
    val check2:
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2</c>, but with an explicit FsCheck configuration.</summary>
    val check2With:
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2</c>, but with an explicit arbitrary for the first argument.</summary>
    val check2Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Like <c>check2Using</c>, but with an explicit FsCheck configuration.</summary>
    val check2UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected>

    /// <summary>Runs a three-argument property-style check.</summary>
    val check3:
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3</c>, but with an explicit FsCheck configuration.</summary>
    val check3With:
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3</c>, but with an explicit arbitrary for the first argument.</summary>
    val check3Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Like <c>check3Using</c>, but with an explicit FsCheck configuration.</summary>
    val check3UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>

    /// <summary>Checks that a quoted function matches the reference implementation.</summary>
    ///
    /// <example id="check-equal-1">
    /// <code lang="fsharp">
    /// Check.shouldEqual
    ///     &lt;@ List.rev &gt;&gt; List.rev @&gt;
    ///     id
    /// </code>
    /// </example>
    val checkEqual<'Args, 'T when 'T : equality> :
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqual</c>, but with an explicit FsCheck configuration.</summary>
    val checkEqualWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqual</c>, but with an explicit arbitrary for the input domain.</summary>
    val checkEqualUsing<'Args, 'T when 'T : equality> :
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    /// <summary>Like <c>checkEqualUsing</c>, but with an explicit FsCheck configuration.</summary>
    val checkEqualUsingWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    val checkEqualGroupedUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary2: FsCheck.Arbitrary<'Group1> ->
        actual: Expr<'Group2 -> 'Group1 -> 'T> ->
        reference: ('Group2 -> 'Group1 -> 'T) ->
            CheckResult<'Group2 * 'Group1, 'T, 'T>

    val checkEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group1> ->
        actual: Expr<'Group2 -> 'Group1 -> 'T> ->
        reference: ('Group2 -> 'Group1 -> 'T) ->
            CheckResult<'Group2 * 'Group1, 'T, 'T>

    val checkEqualGroupedUsingBoth<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqualGroupedUsingBothWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqualGroupedDependingOn<'Group1, 'Group2, 'T when 'T : equality> :
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqualGroupedDependingOnWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqualGroupedDependingOnUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqualGroupedDependingOnUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            CheckResult<'Group1 * 'Group2, 'T, 'T>

    val checkEqual2<'Arg1, 'Arg2, 'T when 'T : equality> :
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    val checkEqual2With<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    val checkEqual2Using<'Arg1, 'Arg2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    val checkEqual2UsingWith<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2, 'T, 'T>

    val checkEqual3<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    val checkEqual3With<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    val checkEqual3Using<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    val checkEqual3UsingWith<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T>

    val checkEqualBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    val checkEqualUsingBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    val checkEqualWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    val checkEqualUsingWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            CheckResult<'Args, 'T, 'T>

    val checkEqualUsingComparer<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Actual) ->
            CheckResult<'Args, 'Actual, 'Actual>

    val checkEqualUsingComparerUsing<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Actual) ->
            CheckResult<'Args, 'Actual, 'Actual>

    /// <summary>Converts a check result into a structured Testify failure report when it did not pass.</summary>
    val toFailureReport: result: CheckResult<'Args, 'Actual, 'Expected> -> TestifyFailureReport option

    /// <summary>Renders a check result with the supplied reporting options.</summary>
    val toDisplayStringWith: options: TestifyReportOptions -> result: CheckResult<'Args, 'Actual, 'Expected> -> string

    /// <summary>Renders a check result using the current Testify report options.</summary>
    val toDisplayString: result: CheckResult<'Args, 'Actual, 'Expected> -> string

    /// <summary>Raises an exception when a property-style check result does not pass.</summary>
    val assertPassed: result: CheckResult<'Args, 'Actual, 'Expected> -> unit

    /// <summary>Raises an exception when a property-style check fails.</summary>
    val should:
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            unit

    val shouldWith:
        config: FsCheck.Config ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            unit

    val shouldUsing:
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            unit

    val shouldUsingWith:
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> ->
            unit

    val shouldGroupedUsing:
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedUsingWith:
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedUsingBoth:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedUsingBothWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedDependingOn:
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedDependingOnWith:
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedDependingOnUsing:
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val shouldGroupedDependingOnUsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'Actual> ->
        reference: ('Group1 -> 'Group2 -> 'Expected) ->
        expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected> ->
            unit

    val should2:
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            unit

    val should2With:
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            unit

    val should2Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            unit

    val should2UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected> ->
            unit

    val should3:
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            unit

    val should3With:
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            unit

    val should3Using:
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            unit

    val should3UsingWith:
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected) ->
        expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ->
            unit

    /// <summary>Raises an exception when a reference-equality property check fails.</summary>
    ///
    /// <example id="check-should-equal-1">
    /// <code lang="fsharp">
    /// Check.shouldEqual &lt;@ List.sort @&gt; List.sort
    /// </code>
    /// </example>
    val shouldEqual<'Args, 'T when 'T : equality> : actual: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit

    val shouldEqualUsing<'Args, 'T when 'T : equality> :
        arbitrary: FsCheck.Arbitrary<'Args> -> actual: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit

    val shouldEqualWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config -> actual: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit

    val shouldEqualUsingWith<'Args, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            unit

    val shouldEqualGroupedUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary2: FsCheck.Arbitrary<'Group1> ->
        actual: Expr<'Group2 -> 'Group1 -> 'T> ->
        reference: ('Group2 -> 'Group1 -> 'T) ->
            unit

    val shouldEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary2: FsCheck.Arbitrary<'Group1> ->
        actual: Expr<'Group2 -> 'Group1 -> 'T> ->
        reference: ('Group2 -> 'Group1 -> 'T) ->
            unit

    val shouldEqualGroupedUsingBoth<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqualGroupedUsingBothWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        arbitrary2: FsCheck.Arbitrary<'Group2> ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqualGroupedDependingOn<'Group1, 'Group2, 'T when 'T : equality> :
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqualGroupedDependingOnWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqualGroupedDependingOnUsing<'Group1, 'Group2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqualGroupedDependingOnUsingWith<'Group1, 'Group2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Group1> ->
        provideArbitrary2: ('Group1 -> FsCheck.Arbitrary<'Group2>) ->
        actual: Expr<'Group1 -> 'Group2 -> 'T> ->
        reference: ('Group1 -> 'Group2 -> 'T) ->
            unit

    val shouldEqual2<'Arg1, 'Arg2, 'T when 'T : equality> :
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> reference: ('Arg1 -> 'Arg2 -> 'T) -> unit

    val shouldEqual2With<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config -> actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> reference: ('Arg1 -> 'Arg2 -> 'T) -> unit

    val shouldEqual2Using<'Arg1, 'Arg2, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> -> actual: Expr<'Arg1 -> 'Arg2 -> 'T> -> reference: ('Arg1 -> 'Arg2 -> 'T) -> unit

    val shouldEqual2UsingWith<'Arg1, 'Arg2, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'T) ->
            unit

    val shouldEqual3<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> -> reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) -> unit

    val shouldEqual3With<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            unit

    val shouldEqual3Using<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            unit

    val shouldEqual3UsingWith<'Arg1, 'Arg2, 'Arg3, 'T when 'T : equality> :
        config: FsCheck.Config ->
        arbitrary1: FsCheck.Arbitrary<'Arg1> ->
        actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T> ->
        reference: ('Arg1 -> 'Arg2 -> 'Arg3 -> 'T) ->
            unit

    val shouldEqualBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) -> actual: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit

    val shouldEqualUsingBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            unit

    val shouldEqualWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions -> actual: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit

    val shouldEqualUsingWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) ->
            unit

    val shouldEqualUsingComparer<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) -> actual: Expr<'Args -> 'Actual> -> reference: ('Args -> 'Actual) -> unit

    val shouldEqualUsingComparerUsing<'Actual, 'Args> :
        comparer: ('Actual -> 'Actual -> bool) ->
        arbitrary: FsCheck.Arbitrary<'Args> ->
        actual: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Actual) ->
            unit
