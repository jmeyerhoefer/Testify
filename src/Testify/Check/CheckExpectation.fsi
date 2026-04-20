namespace Testify

/// <summary>Represents one concrete property-test case with the observed tested and reference outcomes.</summary>
/// <typeparam name="'Args">The generated input type used to invoke both sides of the property check.</typeparam>
/// <typeparam name="'Actual">The successful result type produced by the tested quotation.</typeparam>
/// <typeparam name="'Expected">The successful result type produced by the reference function.</typeparam>
type CheckCase<'Args, 'Actual, 'Expected> =
    {
        /// <summary>The concrete generated arguments for this property case.</summary>
        Arguments: 'Args
        /// <summary>The rendered test text for this case.</summary>
        Test: string
        /// <summary>The observed tested-side outcome, including exceptions.</summary>
        ActualObserved: Observed<'Actual>
        /// <summary>The observed reference-side outcome, including exceptions.</summary>
        ExpectedObserved: Observed<'Expected>
    }

/// <summary>
/// Describes how a tested implementation should relate to a reference implementation for generated inputs.
/// </summary>
/// <remarks>
/// Build reusable relations here, then execute them with <c>Check.result</c>, <c>Check.should</c>,
/// <c>Check.resultBy</c>, or <c>Check.shouldBy</c>.
/// </remarks>
/// <typeparam name="'Args">The generated input type fed into the tested quotation and the reference function.</typeparam>
/// <typeparam name="'Actual">The successful tested-side result type.</typeparam>
/// <typeparam name="'Expected">The successful reference-side result type.</typeparam>
type CheckExpectation<'Args, 'Actual, 'Expected> =
    {
        /// <summary>A short stable label used in reports and structured failure details.</summary>
        Label: string
        /// <summary>A human-readable description of the relation.</summary>
        Description: string
        /// <summary>Determines whether the tested and reference observations satisfy the relation.</summary>
        Verify: 'Args -> Observed<'Actual> -> Observed<'Expected> -> bool
        /// <summary>Formats the tested-side observation for display in reports.</summary>
        FormatActual: 'Args -> Observed<'Actual> -> string
        /// <summary>Formats the reference-side observation for display in reports.</summary>
        FormatExpected: 'Args -> Observed<'Expected> -> string
        /// <summary>Optionally explains why the relation failed for one case.</summary>
        Because: 'Args -> Observed<'Actual> -> Observed<'Expected> -> string option
        /// <summary>Optionally provides structured nested details for rich failure rendering.</summary>
        Details: 'Args -> Observed<'Actual> -> Observed<'Expected> -> FailureDetails option
    }

/// <summary>
/// Builders and combinators for property-style expectations.
/// </summary>
[<RequireQualifiedAccess>]
module CheckExpectation =
    /// <summary>Builds an expectation that requires tested code and the reference to behave identically.</summary>
    /// <returns>
    /// A reusable expectation that succeeds when both sides return equal successful values or throw the
    /// same exception type.
    /// </returns>
    /// <seealso cref="M:Testify.CheckExpectation.equalBy``3(Microsoft.FSharp.Core.FSharpFunc{``1,``2})">
    /// Use <c>equalBy</c> when you want equality after projecting both successful values.
    /// </seealso>
    /// <example id="check-expectation-equaltoreference-1">
    /// <code lang="fsharp">
    /// Check.should(
    ///     CheckExpectation.equalToReference,
    ///     List.sort,
    ///     &lt;@ List.sort @&gt;)
    /// </code>
    /// </example>
    val equalToReference<'Args, 'T when 'T : equality> : CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an expectation that requires both tested code and the reference to equal a fixed expected value.
    /// </summary>
    /// <param name="diffOptions">
    /// Diff settings used when rendering mismatches. Use <c>Diff.defaultOptions</c> for the standard
    /// Testify explanation strategy, or create a custom <c>DiffOptions</c> value when you want different
    /// truncation or structural-diff behavior.
    /// </param>
    /// <param name="expected">The fixed value both sides must produce.</param>
    /// <returns>A reusable expectation for fixed-value property checks with custom diff rendering.</returns>
    val equalToWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions -> expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an expectation that requires both tested code and the reference to equal a fixed expected value.
    /// </summary>
    /// <param name="expected">The fixed value both sides must produce.</param>
    /// <returns>
    /// A reusable expectation for fixed-value property checks that uses <c>Diff.defaultOptions</c> for
    /// mismatch rendering.
    /// </returns>
    val equalTo<'Args, 'T when 'T : equality> :
        expected: 'T -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an equality expectation against the reference implementation using the supplied diff options.
    /// </summary>
    /// <param name="diffOptions">
    /// Diff settings used when the tested code and reference return different successful values.
    /// </param>
    /// <returns>
    /// A reusable expectation that compares tested code and reference behavior with custom diff rendering.
    /// </returns>
    val equalToReferenceWithDiff<'Args, 'T when 'T : equality> :
        diffOptions: DiffOptions -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an equality expectation after projecting both tested and reference values.</summary>
    /// <param name="projection">The projection applied to both successful values before comparison.</param>
    /// <returns>
    /// A reusable expectation that compares projected keys with F# equality. If either side throws,
    /// the expectation falls back to exception-shape comparison and mismatch reporting.
    /// </returns>
    /// <seealso cref="M:Testify.CheckExpectation.equalByKey``3(Microsoft.FSharp.Core.FSharpFunc{``1,``2},``2)">
    /// Use <c>equalByKey</c> when you want both projected values to match one explicit key.
    /// </seealso>
    /// <example id="check-expectation-equalby-1">
    /// <code lang="fsharp">
    /// type Person = { Name: string; Age: int }
    ///
    /// Check.should(
    ///     CheckExpectation.equalBy (fun person -> person.Age),
    ///     (fun n -> { Name = $"ref-{n}"; Age = n }),
    ///     &lt;@ fun n -> { Name = $"actual-{n}"; Age = n } @&gt;)
    /// </code>
    /// </example>
    val equalBy<'Args, 'T, 'Key when 'Key : equality> :
        projection: ('T -> 'Key) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>
    /// Builds an equality expectation that requires both projected values to equal one fixed key.
    /// </summary>
    /// <param name="projection">The projection applied to both successful values.</param>
    /// <param name="expectedKey">
    /// The fixed projected key both the tested code and the reference must produce.
    /// </param>
    /// <returns>
    /// A reusable expectation for “both sides land on this key” scenarios, such as comparing records by
    /// one field or normalized representation.
    /// </returns>
    /// <seealso cref="M:Testify.CheckExpectation.equalBy``3(Microsoft.FSharp.Core.FSharpFunc{``1,``2})">
    /// Use <c>equalBy</c> when the expected side should stay a full value instead of a fixed key.
    /// </seealso>
    /// <example id="check-expectation-equalbykey-1">
    /// <code lang="fsharp">
    /// Check.should(
    ///     CheckExpectation.equalByKey String.length 3,
    ///     (fun (value: string) -> value.ToUpperInvariant()),
    ///     &lt;@ fun value -> value.Trim() @&gt;)
    /// </code>
    /// </example>
    val equalByKey<'Args, 'T, 'Key when 'Key : equality> :
        projection: ('T -> 'Key) -> expectedKey: 'Key -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an equality expectation using a custom comparer.</summary>
    /// <param name="comparer">
    /// The custom equality relation. It receives the tested successful value first and the reference
    /// successful value second.
    /// </param>
    /// <returns>
    /// A reusable expectation that delegates successful-value comparison to <paramref name="comparer" />.
    /// </returns>
    /// <seealso cref="M:Testify.CheckExpectation.equalBy``3(Microsoft.FSharp.Core.FSharpFunc{``1,``2})">
    /// Use <c>equalBy</c> when one projected comparison key is enough.
    /// </seealso>
    /// <example id="check-expectation-equalwith-1">
    /// <code lang="fsharp">
    /// type Person = { Name: string; Age: int }
    ///
    /// Check.should(
    ///     CheckExpectation.equalWith (fun actual expected -> actual.Age = expected.Age),
    ///     (fun n -> { Name = $"ref-{n}"; Age = n }),
    ///     &lt;@ fun n -> { Name = $"actual-{n}"; Age = n } @&gt;)
    /// </code>
    /// </example>
    val equalWith<'Args, 'T> :
        comparer: ('T -> 'T -> bool) -> CheckExpectation<'Args, 'T, 'T>

    /// <summary>Builds an expectation that requires both implementations to throw the same exception type.</summary>
    /// <returns>
    /// A reusable expectation for exception-shape comparisons. It succeeds only when both sides throw and
    /// the thrown exception types match.
    /// </returns>
    /// <seealso cref="M:Testify.CheckExpectation.satisfyObservedWith``3(System.String,Microsoft.FSharp.Core.FSharpFunc{``0,Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.FSharpResult{``1,System.Exception},Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.FSharpResult{``2,System.Exception},System.Boolean}}})">
    /// Use <c>satisfyObservedWith</c> when you need a custom relation over full observed outcomes.
    /// </seealso>
    /// <example id="check-expectation-throwssame-1">
    /// <code lang="fsharp">
    /// Check.should(
    ///     CheckExpectation.throwsSameExceptionType,
    ///     (fun (n: int) -> 10 / (n - n)),
    ///     &lt;@ fun n -> 20 / (n - n) @&gt;)
    /// </code>
    /// </example>
    val throwsSameExceptionType<'Args, 'Actual, 'Expected> : CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Builds a bool expectation that requires the tested code and reference to be <c>true</c>.</summary>
    /// <returns>A reusable bool expectation for “both sides are true”.</returns>
    val isTrue<'Args> : CheckExpectation<'Args, bool, bool>

    /// <summary>Builds a bool expectation that requires the tested code and reference to be <c>false</c>.</summary>
    /// <returns>A reusable bool expectation for “both sides are false”.</returns>
    val isFalse<'Args> : CheckExpectation<'Args, bool, bool>

    /// <summary>Builds an expectation from a predicate over successful values only.</summary>
    /// <param name="description">A short human-readable description of the relation.</param>
    /// <param name="predicate">
    /// The predicate that receives generated arguments, the tested successful value, and the reference
    /// successful value. It is only called when both sides returned successfully.
    /// </param>
    /// <returns>A reusable successful-value expectation.</returns>
    val satisfyWith :
        description: string ->
        predicate: ('Args -> 'Actual -> 'Expected -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Builds an expectation from a predicate over fully observed outcomes, including exceptions.</summary>
    /// <param name="description">A short human-readable description of the relation.</param>
    /// <param name="predicate">
    /// The predicate that receives generated arguments and both fully observed outcomes, including
    /// exceptions.
    /// </param>
    /// <returns>A reusable observed-outcome expectation.</returns>
    val satisfyObservedWith :
        description: string ->
        predicate: ('Args -> Observed<'Actual> -> Observed<'Expected> -> bool) ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>A reusable expectation that succeeds when either input expectation succeeds.</returns>
    val orElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>A reusable expectation that succeeds only when both input expectations succeed.</returns>
    val andAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> ->
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

type CheckExpectation<'Args, 'Actual, 'Expected> with
    static member OrElse :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>

    static member AndAlso :
        a: CheckExpectation<'Args, 'Actual, 'Expected> *
        b: CheckExpectation<'Args, 'Actual, 'Expected> ->
            CheckExpectation<'Args, 'Actual, 'Expected>
