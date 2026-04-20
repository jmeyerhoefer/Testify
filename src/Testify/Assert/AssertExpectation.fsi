namespace Testify

/// <summary>
/// Describes how one quoted value should be validated, rendered, and explained when it fails.
/// </summary>
/// <remarks>
/// <para>
/// An <c>AssertExpectation</c> is reusable. Build it once, then pass it to <c>Assert.result</c>,
/// <c>Assert.should</c>, or the Assert operator DSL.
/// </para>
/// <para>
/// In the DSL, expectations are usually applied with <c>|&gt;?</c>, composed with <c>&lt;|&gt;</c>
/// and <c>&lt;&amp;&gt;</c>, or used in readable multi-expectation lists with <c>||?</c> and
/// <c>&amp;&amp;?</c>.
/// </para>
/// </remarks>
/// <typeparam name="'T">
/// The successful result type produced by the quoted expression when it does not throw.
/// </typeparam>
type AssertExpectation<'T> =
    {
        /// <summary>A short stable label used in reports and structured failure details.</summary>
        Label: string
        /// <summary>A human-readable description of the expected behavior.</summary>
        Description: string
        /// <summary>Determines whether the observed result satisfies the expectation.</summary>
        Verify: Observed<'T> -> bool
        /// <summary>Formats the observed result for display in reports.</summary>
        Format: Observed<'T> -> string
        /// <summary>Optionally explains why the expectation failed.</summary>
        Because: Observed<'T> -> string option
        /// <summary>Optionally provides structured nested details for rich rendering.</summary>
        Details: Observed<'T> -> FailureDetails option
    }

/// <summary>
/// Builders and combinators for reusable assertion semantics.
/// </summary>
[<RequireQualifiedAccess>]
module AssertExpectation =
    /// <summary>Builds an expectation that checks equality with a fixed expected value.</summary>
    /// <param name="expected">The value the observed result must equal.</param>
    /// <returns>An expectation that uses F# equality for the comparison.</returns>
    /// <seealso cref="M:Testify.AssertExpectation.equalBy``2(Microsoft.FSharp.Core.FSharpFunc{``0,``1},``0)">
    /// Use <c>equalBy</c> when you want to compare two full values through one projected key.
    /// </seealso>
    /// <example id="assert-expectation-1">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ 6 * 7 @&gt; |&gt;? AssertExpectation.equalTo 42
    /// </code>
    /// </example>
    val equalTo: expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>
    /// Builds an equality expectation that uses the supplied diff options when a mismatch is rendered.
    /// </summary>
    /// <param name="diffOptions">The diff configuration to use when formatting a mismatch.</param>
    /// <param name="expected">The value the observed result must equal.</param>
    /// <returns>An expectation that uses F# equality and enriched diff output.</returns>
    val equalToWithDiff: diffOptions: DiffOptions -> expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>Builds an expectation that checks inequality against a fixed value.</summary>
    /// <param name="expected">The value the observed result must not equal.</param>
    /// <returns>An expectation that succeeds when the observed value differs from <paramref name="expected" />.</returns>
    val notEqualTo: expected: 'T -> AssertExpectation<'T> when 'T : equality

    /// <summary>Builds an expectation from a predicate over successful values.</summary>
    /// <param name="description">A short human-readable description of the expected behavior.</param>
    /// <param name="predicate">The predicate that must return <c>true</c> for successful observed values.</param>
    /// <returns>A reusable expectation based on the supplied predicate.</returns>
    /// <remarks>
    /// Use <c>satisfy</c> when you only care about the resulting value. If you need to inspect thrown
    /// exceptions as well, use <c>satisfyObserved</c>.
    /// </remarks>
    val satisfy: description: string -> predicate: ('T -> bool) -> AssertExpectation<'T>

    /// <summary>Builds an expectation from a predicate over the fully observed result.</summary>
    /// <param name="description">A short human-readable description of the expected behavior.</param>
    /// <param name="predicate">
    /// The predicate that receives the full <c>Observed</c> value, including exceptions.
    /// </param>
    /// <returns>A reusable expectation based on the supplied observed-result predicate.</returns>
    /// <remarks>
    /// This is the most flexible expectation builder because it can inspect both returned values and
    /// thrown exceptions.
    /// </remarks>
    val satisfyObserved: description: string -> predicate: (Observed<'T> -> bool) -> AssertExpectation<'T>

    /// <summary>Builds an expectation that succeeds when evaluation completes without throwing.</summary>
    /// <returns>An expectation for successful synchronous evaluation.</returns>
    val doesNotThrow<'T> : AssertExpectation<'T>

    /// <summary>Builds an expectation that succeeds when evaluation throws any exception.</summary>
    /// <returns>An expectation for exception-based assertions.</returns>
    val throwsAny<'T> : AssertExpectation<'T>

    /// <summary>Builds an expectation that succeeds when evaluation throws a specific exception type.</summary>
    /// <returns>An expectation that checks the thrown exception type.</returns>
    /// <seealso cref="M:Testify.AssertExpectation.throwsAny``1">
    /// Use <c>throwsAny</c> when the presence of an exception matters more than its concrete type.
    /// </seealso>
    /// <example id="assert-expectation-2">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ 1 / 0 @&gt; |&gt;? AssertExpectation.throws&lt;int, System.DivideByZeroException&gt;
    /// </code>
    /// </example>
    val throws<'T, 'TException when 'TException :> exn> : AssertExpectation<'T>

    /// <summary>
    /// Builds an expectation that succeeds when an async or task-based expression completes without throwing.
    /// </summary>
    /// <returns>An expectation for successful asynchronous evaluation.</returns>
    val doesNotThrowAsync<'T> : AssertExpectation<'T>

    /// <summary>
    /// Builds an expectation that succeeds when an async or task-based expression throws a specific exception type.
    /// </summary>
    /// <returns>An expectation that checks the thrown asynchronous exception type.</returns>
    val throwsAsync<'T, 'TException when 'TException :> exn> : AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether a value is less than a bound.</summary>
    /// <param name="expected">The exclusive upper bound.</param>
    /// <returns>An expectation that succeeds when the value is smaller than <paramref name="expected" />.</returns>
    val lessThan<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether a value is less than or equal to a bound.</summary>
    /// <param name="expected">The inclusive upper bound.</param>
    /// <returns>
    /// An expectation that succeeds when the value is smaller than or equal to
    /// <paramref name="expected" />.
    /// </returns>
    val lessThanOrEqualTo<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether a value is greater than a bound.</summary>
    /// <param name="expected">The exclusive lower bound.</param>
    /// <returns>An expectation that succeeds when the value is greater than <paramref name="expected" />.</returns>
    val greaterThan<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether a value is greater than or equal to a bound.</summary>
    /// <param name="expected">The inclusive lower bound.</param>
    /// <returns>
    /// An expectation that succeeds when the value is greater than or equal to
    /// <paramref name="expected" />.
    /// </returns>
    val greaterThanOrEqualTo<'T when 'T : comparison> : expected: 'T -> AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether a value lies between two inclusive bounds.</summary>
    /// <param name="lowerBound">The inclusive lower bound.</param>
    /// <param name="upperBound">The inclusive upper bound.</param>
    /// <returns>An expectation that succeeds when the value lies within the supplied range.</returns>
    val between<'T when 'T : comparison> : lowerBound: 'T -> upperBound: 'T -> AssertExpectation<'T>

    /// <summary>Builds an equality expectation after projecting both the observed and expected values.</summary>
    /// <param name="projection">The projection used to derive the comparison key from both values.</param>
    /// <param name="expected">The full expected value whose projected key must match the observed one.</param>
    /// <returns>An expectation that compares the projected keys using F# equality.</returns>
    /// <seealso cref="M:Testify.AssertExpectation.equalByKey``2(Microsoft.FSharp.Core.FSharpFunc{``0,``1},``1)">
    /// Use <c>equalByKey</c> when you already know the comparison key instead of a full expected value.
    /// </seealso>
    /// <example id="assert-expectation-equalby-1">
    /// <code lang="fsharp">
    /// type Person = { Name: string; Age: int }
    ///
    /// &lt;@ { Name = "Tony"; Age = 48 } @&gt;
    /// |&gt;? AssertExpectation.equalBy (fun person -> person.Age) { Name = "Anthony"; Age = 48 }
    /// </code>
    /// </example>
    val equalBy: projection: ('T -> 'Key) -> expected: 'T -> AssertExpectation<'T> when 'Key : equality

    /// <summary>Builds an equality expectation after projecting the observed value to a comparison key.</summary>
    /// <param name="projection">The projection used to derive the comparison key from the observed value.</param>
    /// <param name="expectedKey">The projected key that the observed value must match.</param>
    /// <returns>An expectation that compares the projected key using F# equality.</returns>
    /// <seealso cref="M:Testify.AssertExpectation.equalBy``2(Microsoft.FSharp.Core.FSharpFunc{``0,``1},``0)">
    /// Use <c>equalBy</c> when you want the expected side to remain a full value.
    /// </seealso>
    /// <example id="assert-expectation-equalbykey-1">
    /// <code lang="fsharp">
    /// &lt;@ "Testify" @&gt;
    /// |&gt;? AssertExpectation.equalByKey String.length 7
    /// </code>
    /// </example>
    val equalByKey: projection: ('T -> 'Key) -> expectedKey: 'Key -> AssertExpectation<'T> when 'Key : equality

    /// <summary>Builds an equality expectation using a custom comparison function.</summary>
    /// <param name="comparer">The comparison function used to compare the observed and expected values.</param>
    /// <param name="expected">The value the observed result must match according to <paramref name="comparer" />.</param>
    /// <returns>An expectation that delegates equality to the supplied comparer.</returns>
    /// <seealso cref="M:Testify.AssertExpectation.equalBy``2(Microsoft.FSharp.Core.FSharpFunc{``0,``1},``0)">
    /// Use <c>equalBy</c> when one projected key is enough and you do not need a full custom relation.
    /// </seealso>
    /// <example id="assert-expectation-equalwith-1">
    /// <code lang="fsharp">
    /// type Person = { Name: string; Age: int }
    ///
    /// &lt;@ { Name = "Tony"; Age = 48 } @&gt;
    /// |&gt;? AssertExpectation.equalWith (fun actual expected -> actual.Age = expected.Age) { Name = "Anthony"; Age = 48 }
    /// </code>
    /// </example>
    val equalWith: comparer: ('T -> 'T -> bool) -> expected: 'T -> AssertExpectation<'T>

    /// <summary>Builds an expectation that checks whether two sequences have the same elements in the same order.</summary>
    /// <param name="expected">The expected sequence contents.</param>
    /// <returns>An expectation for sequence equality.</returns>
    val sequenceEqual<'T when 'T : equality> : expected: seq<'T> -> AssertExpectation<'T seq>

    /// <summary>Builds an expectation that requires a boolean value to be <c>true</c>.</summary>
    /// <returns>An expectation for truthy boolean assertions.</returns>
    val isTrue: AssertExpectation<bool>

    /// <summary>Builds an expectation that requires a boolean value to be <c>false</c>.</summary>
    /// <returns>An expectation for falsy boolean assertions.</returns>
    val isFalse: AssertExpectation<bool>

    /// <summary>Builds an expectation that requires an option to be <c>Some _</c>.</summary>
    /// <returns>An expectation for present option values.</returns>
    val isSome<'T> : AssertExpectation<'T option>

    /// <summary>Builds an expectation that requires an option to be <c>None</c>.</summary>
    /// <returns>An expectation for missing option values.</returns>
    val isNone<'T> : AssertExpectation<'T option>

    /// <summary>Builds an expectation that requires a result value to be <c>Ok _</c>.</summary>
    /// <returns>An expectation for successful <c>Result</c> values.</returns>
    val isOk<'T, 'TError> : AssertExpectation<Result<'T, 'TError>>

    /// <summary>Builds an expectation that requires a result value to be <c>Error _</c>.</summary>
    /// <returns>An expectation for failing <c>Result</c> values.</returns>
    val isError<'T, 'TError> : AssertExpectation<Result<'T, 'TError>>

    /// <summary>Builds an expectation that checks whether a sequence contains a specific item.</summary>
    /// <param name="expectedItem">The item that must be present in the sequence.</param>
    /// <returns>An expectation for containment checks.</returns>
    val contains<'T when 'T : equality> : expectedItem: 'T -> AssertExpectation<'T seq>

    /// <summary>Builds an expectation that checks whether a string starts with a prefix.</summary>
    /// <param name="prefix">The required prefix.</param>
    /// <returns>An expectation for prefix-based string assertions.</returns>
    val startsWith: prefix: string -> AssertExpectation<string>

    /// <summary>Builds an expectation that checks whether a string ends with a suffix.</summary>
    /// <param name="suffix">The required suffix.</param>
    /// <returns>An expectation for suffix-based string assertions.</returns>
    val endsWith: suffix: string -> AssertExpectation<string>

    /// <summary>Builds an expectation that checks whether a sequence has a specific length.</summary>
    /// <param name="expectedLength">The required number of elements.</param>
    /// <returns>An expectation for sequence length checks.</returns>
    val hasLength: expectedLength: int -> AssertExpectation<'T seq>

    /// <summary>Negates an existing expectation.</summary>
    /// <param name="expectation">The expectation to invert.</param>
    /// <returns>An expectation that succeeds exactly when <paramref name="expectation" /> would fail.</returns>
    val not: expectation: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>An expectation that succeeds when either input expectation succeeds.</returns>
    val orElse: a: AssertExpectation<'T> -> b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>An expectation that succeeds only when both input expectations succeed.</returns>
    val andAlso: a: AssertExpectation<'T> -> b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines a sequence of expectations so that all of them must succeed.</summary>
    /// <param name="expectations">The expectations to combine.</param>
    /// <returns>An expectation that succeeds only when every supplied expectation succeeds.</returns>
    /// <example id="assert-expectation-3">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ "MiniLib" @&gt; &amp;&amp;?
    ///     [ AssertExpectation.startsWith "Mini"
    ///       AssertExpectation.endsWith "Lib" ]
    /// </code>
    /// </example>
    val all: expectations: seq<AssertExpectation<'T>> -> AssertExpectation<'T>

    /// <summary>Combines a sequence of expectations so that at least one of them must succeed.</summary>
    /// <param name="expectations">The expectations to combine.</param>
    /// <returns>An expectation that succeeds when any supplied expectation succeeds.</returns>
    val any: expectations: seq<AssertExpectation<'T>> -> AssertExpectation<'T>

type AssertExpectation<'T> with
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>An expectation that succeeds when either input expectation succeeds.</returns>
    static member OrElse: a: AssertExpectation<'T> * b: AssertExpectation<'T> -> AssertExpectation<'T>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>An expectation that succeeds only when both input expectations succeed.</returns>
    static member AndAlso: a: AssertExpectation<'T> * b: AssertExpectation<'T> -> AssertExpectation<'T>
