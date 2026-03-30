namespace Testify

/// <summary>
/// Describes how a single quoted value should be checked, rendered, and explained when it fails.
/// </summary>
type AssertExpectation<'T> =
    {
        /// <summary>A short stable label used in reports and branch details.</summary>
        Label: string
        /// <summary>A human-readable description of the expected behavior.</summary>
        Description: string
        /// <summary>Verifies the observed result.</summary>
        Verify: Observed<'T> -> bool
        /// <summary>Formats the observed result for display.</summary>
        Format: Observed<'T> -> string
        /// <summary>Optionally explains why the expectation failed.</summary>
        Because: Observed<'T> -> string option
        /// <summary>Optionally provides structured nested details for rich rendering.</summary>
        Details: Observed<'T> -> FailureDetails option
    }

[<RequireQualifiedAccess>]
module AssertExpectation =
    /// <summary>Checks that a value equals the expected value using F# equality.</summary>
    ///
    /// <example id="assert-expectation-1">
    /// <code lang="fsharp">
    /// Assert.should (AssertExpectation.equalTo 42) &lt;@ 6 * 7 @&gt;
    /// </code>
    /// </example>
    val equalTo: expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>Checks equality using the supplied diff options when a mismatch is rendered.</summary>
    val equalToWithDiff: diffOptions: DiffOptions -> expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>Checks that a value does not equal the expected value.</summary>
    val notEqualTo: expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>Checks that a value satisfies the supplied predicate.</summary>
    val satisfy: description: string -> predicate: ('T -> bool) -> AssertExpectation<'T>

    /// <summary>
    /// Checks the fully observed result, which allows expectations to inspect exceptions as well as values.
    /// </summary>
    val satisfyObserved: description: string -> predicate: (Observed<'T> -> bool) -> AssertExpectation<'T>

    /// <summary>Checks that evaluating the expression completes without throwing.</summary>
    val doesNotThrow<'T> : AssertExpectation<'T>

    /// <summary>Checks that evaluating the expression throws some exception.</summary>
    val throwsAny<'T> : AssertExpectation<'T>

    /// <summary>Checks that evaluating the expression throws a specific exception type.</summary>
    ///
    /// <example id="assert-expectation-2">
    /// <code lang="fsharp">
    /// Assert.should AssertExpectation.throws&lt;int, System.DivideByZeroException&gt; &lt;@ 1 / 0 @&gt;
    /// </code>
    /// </example>
    val throws<'T, 'TException when 'TException :> exn> : AssertExpectation<'T>

    /// <summary>Checks that evaluating an async or task expression completes without throwing.</summary>
    val doesNotThrowAsync<'T> : AssertExpectation<'T>

    /// <summary>Checks that evaluating an async or task expression throws a specific exception type.</summary>
    val throwsAsync<'T, 'TException when 'TException :> exn> : AssertExpectation<'T>

    /// <summary>Checks that a value is less than the supplied bound.</summary>
    val lessThan<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Checks that a value is less than or equal to the supplied bound.</summary>
    val lessThanOrEqualTo<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Checks that a value is greater than the supplied bound.</summary>
    val greaterThan<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Checks that a value is greater than or equal to the supplied bound.</summary>
    val greaterThanOrEqualTo<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Checks that a value lies between two inclusive bounds.</summary>
    val between<'T when 'T : comparison> : lowerBound: 'T -> upperBound: 'T -> AssertExpectation<'T>

    /// <summary>Checks equality after projecting the value to a comparison key.</summary>
    val equalBy: projection: ('T -> 'Key) -> expected: 'Key -> AssertExpectation<'T> when 'Key : equality

    /// <summary>Checks equality using a custom comparison function.</summary>
    val equalWith: comparer: ('T -> 'T -> bool) -> expected: 'T -> AssertExpectation<'T>

    /// <summary>Checks that two sequences contain the same elements in the same order.</summary>
    val sequenceEqual<'T when 'T : equality> : expected: seq<'T> -> AssertExpectation<'T seq>

    /// <summary>Checks that a boolean value is <c>true</c>.</summary>
    val isTrue: AssertExpectation<bool>

    /// <summary>Checks that a boolean value is <c>false</c>.</summary>
    val isFalse: AssertExpectation<bool>

    /// <summary>Checks that an option value is <c>Some _</c>.</summary>
    val isSome<'T> : AssertExpectation<'T option>

    /// <summary>Checks that an option value is <c>None</c>.</summary>
    val isNone<'T> : AssertExpectation<'T option>

    /// <summary>Checks that a result value is <c>Ok _</c>.</summary>
    val isOk<'T, 'TError> : AssertExpectation<Result<'T, 'TError>>

    /// <summary>Checks that a result value is <c>Error _</c>.</summary>
    val isError<'T, 'TError> : AssertExpectation<Result<'T, 'TError>>

    /// <summary>Checks that a sequence contains the expected item.</summary>
    val contains<'T when 'T : equality> : expectedItem: 'T -> AssertExpectation<'T seq>

    /// <summary>Checks that a string starts with the supplied prefix.</summary>
    val startsWith: prefix: string -> AssertExpectation<string>

    /// <summary>Checks that a string ends with the supplied suffix.</summary>
    val endsWith: suffix: string -> AssertExpectation<string>

    /// <summary>Checks that a sequence has the specified length.</summary>
    val hasLength: expectedLength: int -> AssertExpectation<'T seq>

    /// <summary>Negates an existing expectation.</summary>
    val not: expectation: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that either one may succeed.</summary>
    val orElse: a: AssertExpectation<'T> -> b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    val andAlso: a: AssertExpectation<'T> -> b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines a sequence of expectations so that all must succeed.</summary>
    ///
    /// <example id="assert-expectation-3">
    /// <code lang="fsharp">
    /// let aboutMiniLib =
    ///     AssertExpectation.all
    ///         [ AssertExpectation.startsWith "Mini"
    ///           AssertExpectation.endsWith "Lib" ]
    /// </code>
    /// </example>
    val all: expectations: seq<AssertExpectation<'T>> -> AssertExpectation<'T>

    /// <summary>Combines a sequence of expectations so that at least one must succeed.</summary>
    val any: expectations: seq<AssertExpectation<'T>> -> AssertExpectation<'T>

type AssertExpectation<'T> with
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    static member OrElse: a: AssertExpectation<'T> * b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    static member AndAlso: a: AssertExpectation<'T> * b: AssertExpectation<'T> -> AssertExpectation<'T>
