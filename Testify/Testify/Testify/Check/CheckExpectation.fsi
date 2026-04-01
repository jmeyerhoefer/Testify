namespace Testify

/// <summary>Represents one concrete property-test case with the observed tested and reference outcomes.</summary>
/// <remarks>
/// A <c>CheckCase</c> captures the generated arguments together with the observed result of the quoted
/// implementation and the reference implementation for one run.
/// </remarks>
type CheckCase<'Args, 'Actual, 'Expected> =
    {
        /// <summary>The generated arguments used for this case.</summary>
        Arguments: 'Args
        /// <summary>A readable rendering of the tested invocation.</summary>
        Test: string
        /// <summary>The observed result of the tested implementation.</summary>
        ActualObserved: Observed<'Actual>
        /// <summary>The observed result of the reference implementation.</summary>
        ExpectedObserved: Observed<'Expected>
    }

/// <summary>
/// Describes how a tested implementation should relate to a reference implementation for generated inputs.
/// </summary>
/// <remarks>
/// <c>CheckExpectation</c> values are reusable. Build one once, then apply it with <c>Check.check</c>
/// or <c>Check.should</c> against one or more quoted implementations and reference functions.
/// </remarks>
type CheckExpectation<'Args, 'Actual, 'Expected> =
    {
        /// <summary>A short stable label used in reports and branch details.</summary>
        Label: string
        /// <summary>A human-readable description of the expected relation.</summary>
        Description: string
        /// <summary>Verifies the relation for one generated case.</summary>
        Verify: 'Args -> Observed<'Actual> -> Observed<'Expected> -> bool
        /// <summary>Formats the tested result for display.</summary>
        FormatActual: 'Args -> Observed<'Actual> -> string
        /// <summary>Formats the reference result for display.</summary>
        FormatExpected: 'Args -> Observed<'Expected> -> string
        /// <summary>Optionally explains why the relation failed.</summary>
        Because: 'Args -> Observed<'Actual> -> Observed<'Expected> -> string option
        /// <summary>Optionally provides structured nested details for rich rendering.</summary>
        Details: 'Args -> Observed<'Actual> -> Observed<'Expected> -> FailureDetails option
    }

[<RequireQualifiedAccess>]
module CheckExpectation =
    /// <summary>Builds an expectation that requires tested code and the reference to behave identically.</summary>
    /// <returns>An equality-based relation expectation for property-style checks.</returns>
    /// <example id="check-expectation-1">
    /// <code lang="fsharp">
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt;
    /// |> Check.should CheckExpectation.equalToReference id
    /// </code>
    /// </example>
    val equalToReference<'Args, 'T when 'T : equality> : CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an expectation that requires both tested code and the reference to equal a fixed expected value.
    /// </summary>
    /// <param name="diffOptions">The diff configuration to use when rendering a mismatch.</param>
    /// <param name="expected">The value both implementations must produce.</param>
    /// <returns>An expectation for fixed-value comparisons with diff-aware rendering.</returns>
    val equalToWithDiff<'Args, 'T when 'T : equality> : diffOptions: DiffOptions -> expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an expectation that requires both tested code and the reference to equal a fixed expected value.
    /// </summary>
    /// <param name="expected">The value both implementations must produce.</param>
    /// <returns>An expectation for fixed-value comparisons.</returns>
    val equalTo<'Args, 'T when 'T : equality> : expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an equality expectation against the reference implementation using the supplied diff options.
    /// </summary>
    /// <param name="diffOptions">The diff configuration to use when rendering a mismatch.</param>
    /// <returns>An expectation that compares tested output and reference output using F# equality.</returns>
    val equalToReferenceWithDiff<'Args, 'T when 'T : equality> : diffOptions: DiffOptions -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an equality expectation after projecting both tested and reference values.</summary>
    /// <param name="projection">The projection used to derive the comparison key for both values.</param>
    /// <returns>An expectation that compares the projected keys using F# equality.</returns>
    val equalToReferenceBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an equality expectation using a custom comparer.</summary>
    /// <param name="comparer">The comparison function used to compare tested and reference values.</param>
    /// <returns>An expectation that delegates equality to the supplied comparer.</returns>
    val equalToReferenceWith<'Args, 'T> :
        comparer: ('T -> 'T -> bool) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an expectation that requires both implementations to throw the same exception type.</summary>
    /// <returns>An expectation for exception-shape comparisons.</returns>
    val throwsSameExceptionType<'Args, 'Actual, 'Expected> : CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Builds an expectation from an arbitrary relation over arguments, tested output, and reference output.</summary>
    /// <param name="description">A short human-readable description of the required relation.</param>
    /// <param name="relation">
    /// A relation over the generated arguments, the tested value, and the reference value.
    /// </param>
    /// <returns>A reusable expectation based on the supplied relation.</returns>
    /// <remarks>
    /// Use <c>satisfiesRelation</c> when you only care about successful return values. If you need to
    /// inspect exceptions as well, use <c>satisfyObservedWith</c>.
    /// </remarks>
    val satisfiesRelation :
        description: string ->
        relation: ('Args -> 'Actual -> 'Expected -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Builds an expectation from a predicate over successful values only.</summary>
    /// <param name="description">A short human-readable description of the required relation.</param>
    /// <param name="predicate">
    /// A predicate over the generated arguments, the tested value, and the reference value.
    /// </param>
    /// <returns>A reusable expectation based on the supplied predicate.</returns>
    val satisfyWith :
        description: string ->
        predicate: ('Args -> 'Actual -> 'Expected -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Builds an expectation from a predicate over fully observed outcomes, including exceptions.</summary>
    /// <param name="description">A short human-readable description of the required relation.</param>
    /// <param name="predicate">
    /// A predicate over the generated arguments and the fully observed tested and reference outcomes.
    /// </param>
    /// <returns>A reusable expectation based on the supplied observed-outcome predicate.</returns>
    val satisfyObservedWith :
        description: string ->
        predicate: ('Args -> Observed<'Actual> -> Observed<'Expected> -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>An expectation that succeeds when either input expectation succeeds.</returns>
    val orElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>An expectation that succeeds only when both input expectations succeed.</returns>
    val andAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

type CheckExpectation<'Args, 'Actual, 'Expected> with
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>An expectation that succeeds when either input expectation succeeds.</returns>
    static member OrElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>An expectation that succeeds only when both input expectations succeed.</returns>
    static member AndAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>
