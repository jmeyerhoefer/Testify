namespace MiniLib.Testify

/// <summary>Represents one concrete property test case with the observed tested and reference outcomes.</summary>
type CheckCase<'Args, 'Actual, 'Expected> =
    {
        /// <summary>The generated arguments used for this case.</summary>
        Arguments: 'Args
        /// <summary>A readable rendering of the tested call.</summary>
        Test: string
        /// <summary>The observed result of the tested implementation.</summary>
        ActualObserved: Observed<'Actual>
        /// <summary>The observed result of the reference implementation.</summary>
        ExpectedObserved: Observed<'Expected>
    }

/// <summary>
/// Describes how a tested implementation should relate to a reference implementation for generated inputs.
/// </summary>
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
    /// <summary>Checks that tested code and the reference behave the same for each generated case.</summary>
    ///
    /// <example id="check-expectation-1">
    /// <code lang="fsharp">
    /// Check.should
    ///     &lt;@ List.rev &gt;&gt; List.rev @&gt;
    ///     id
    ///     CheckExpectation.equalToReference
    /// </code>
    /// </example>
    val equalToReference<'Args, 'T when 'T : equality> : CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks that both tested code and the reference equal a fixed expected value.</summary>
    val equalToWithDiff<'Args, 'T when 'T : equality> : diffOptions: DiffOptions -> expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks that both tested code and the reference equal a fixed expected value.</summary>
    val equalTo<'Args, 'T when 'T : equality> : expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks reference equality while using the supplied diff options for mismatch output.</summary>
    val equalToReferenceWithDiff<'Args, 'T when 'T : equality> : diffOptions: DiffOptions -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks reference equality after projecting both values.</summary>
    val equalToReferenceBy<'Args, 'T, 'Key when 'T : equality and 'Key : equality> :
        projection: ('T -> 'Key) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks reference equality using a custom comparison function.</summary>
    val equalToReferenceWith<'Args, 'T> :
        comparer: ('T -> 'T -> bool) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Checks that both tested code and the reference throw the same exception type.</summary>
    val throwsSameExceptionType<'Args, 'Actual, 'Expected> : CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Checks an arbitrary relation between arguments, tested output, and reference output.</summary>
    val satisfiesRelation :
        description: string ->
        relation: ('Args -> 'Actual -> 'Expected -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Checks a custom relation on successful values only.</summary>
    val satisfyWith :
        description: string ->
        predicate: ('Args -> 'Actual -> 'Expected -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Checks a custom relation against fully observed outcomes, including exceptions.</summary>
    val satisfyObservedWith :
        description: string ->
        predicate: ('Args -> Observed<'Actual> -> Observed<'Expected> -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that either one may succeed.</summary>
    val orElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    val andAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

type CheckExpectation<'Args, 'Actual, 'Expected> with
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    static member OrElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    static member AndAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>
