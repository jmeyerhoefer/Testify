namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for example-based assertions.</summary>
/// <remarks>
/// <para>
/// The Assert DSL is expression-left: the quoted value stays on the left and the asserted logic reads
/// from left to right.
/// </para>
/// <para>
/// Recommended usage:
/// <c>|&gt;?</c> for one expectation, <c>&lt;|&gt;</c> and <c>&lt;&amp;&gt;</c> to compose a few reusable
/// expectations, <c>||?</c> when any expectation from a sequence may pass, and <c>&amp;&amp;?</c> when
/// every expectation from a sequence must pass.
/// </para>
/// <para>
/// Assert operators are fail-fast. They raise on the first failing assertion instead of collecting
/// multiple failures.
/// </para>
/// </remarks>
module AssertOperators =
    /// <summary>Combines two expectations with logical OR.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>A combined expectation that succeeds when either input expectation succeeds.</returns>
    /// <remarks>
    /// Use this to build reusable logic before applying it with <c>|&gt;?</c>.
    /// </remarks>
    val inline (<|>) :
        a: ^T -> b: ^T -> ^T
            when ^T : (static member OrElse: ^T * ^T -> ^T)

    /// <summary>Combines two expectations with logical AND.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>A combined expectation that succeeds only when both input expectations succeed.</returns>
    /// <remarks>
    /// Use this to build reusable logic before applying it with <c>|&gt;?</c>.
    /// </remarks>
    val inline (<&>) :
        a: ^T -> b: ^T -> ^T
            when ^T : (static member AndAlso: ^T * ^T -> ^T)

    /// <summary>Applies one expectation to a quoted expression and raises immediately on failure.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="expectation">The expectation to apply.</param>
    /// <exception cref="System.Exception">
    /// Raised when <paramref name="expr" /> does not satisfy <paramref name="expectation" />.
    /// </exception>
    /// <remarks>
    /// This is the primary Assert DSL operator. Use it for one expectation, or for a small composed
    /// expectation built with <c>&lt;|&gt;</c> or <c>&lt;&amp;&gt;</c>.
    /// </remarks>
    /// <seealso cref="M:Testify.Assert.should``1(Testify.AssertExpectation{``0},Microsoft.FSharp.Quotations.FSharpExpr{``0})">
    /// Named API equivalent that uses the same fail-fast execution semantics.
    /// </seealso>
    /// <example id="assert-operators-1">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ 2 + 3 @&gt; |&gt;? AssertExpectation.equalTo 5
    /// </code>
    /// </example>
    /// <example id="assert-operators-1-composed">
    /// <code lang="fsharp">
    /// &lt;@ 5 @&gt; |&gt;? (AssertExpectation.greaterThan 0 &lt;&amp;&gt; AssertExpectation.lessThan 10)
    /// </code>
    /// </example>
    val inline (|>?) : expr: Expr<'T> -> expectation: AssertExpectation<'T> -> unit

    /// <summary>Runs one assertion, then returns the original quotation for further chaining.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="expectation">The expectation to apply.</param>
    /// <returns>The original quoted expression.</returns>
    /// <exception cref="System.Exception">
    /// Raised when <paramref name="expr" /> does not satisfy <paramref name="expectation" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the chain-friendly Assert operator. It supports readable left-to-right pipelines while
    /// preserving fail-fast behavior.
    /// </para>
    /// <para>
    /// The returned quotation is not cached. If it is checked again later in the chain, it will be
    /// evaluated again.
    /// </para>
    /// </remarks>
    /// <seealso cref="M:Testify.AssertOperators.op_BarGreaterQmark``1(Microsoft.FSharp.Quotations.FSharpExpr{``0},Testify.AssertExpectation{``0})">
    /// Use <c>|&gt;?</c> when you do not need to keep the original quotation for further chaining.
    /// </seealso>
    /// <example id="assert-operators-1b">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ 5 @&gt;
    /// &gt;&gt;? AssertExpectation.greaterThan 0
    /// &gt;&gt;? AssertExpectation.lessThan 10
    /// |&gt; ignore
    /// </code>
    /// </example>
    val inline (>>?) : expr: Expr<'T> -> expectation: AssertExpectation<'T> -> Expr<'T>

    /// <summary>Asserts that a quoted value is less than a comparison value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The exclusive upper bound.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (<?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value is less than or equal to a comparison value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The inclusive upper bound.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (<=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value equals the provided value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The expected value.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    /// <seealso cref="M:Testify.AssertExpectation.equalTo``1(``0)">
    /// Shorthand for applying <c>AssertExpectation.equalTo</c> with <c>|&gt;?</c>.
    /// </seealso>
    /// <example id="assert-operators-2">
    /// <code lang="fsharp">
    /// &lt;@ List.length [1; 2; 3] @&gt; =? 3
    /// </code>
    /// </example>
    val inline (=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : equality

    /// <summary>Asserts that a quoted value does not equal the provided value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The value that must not be observed.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (<>?) : expr: Expr<'T> -> value: 'T -> unit when 'T : equality

    /// <summary>Asserts that a quoted value is greater than a comparison value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The exclusive lower bound.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (>?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value is greater than or equal to a comparison value.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="value">The inclusive lower bound.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (>=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that evaluating the quoted expression throws an exception.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <exception cref="System.Exception">Raised when no exception is observed.</exception>
    val inline (^?) : expr: Expr<'T> -> unit

    /// <summary>Asserts that evaluating the quoted expression completes without throwing.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <exception cref="System.Exception">Raised when an exception is observed.</exception>
    val inline (^!?) : expr: Expr<'T> -> unit

    /// <summary>Asserts that a quoted boolean expression evaluates to <c>true</c>.</summary>
    /// <param name="expr">The quoted boolean expression under test.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (?) : expr: Expr<bool> -> unit

    /// <summary>Asserts that a quoted boolean expression evaluates to <c>false</c>.</summary>
    /// <param name="expr">The quoted boolean expression under test.</param>
    /// <exception cref="System.Exception">Raised when the assertion fails.</exception>
    val inline (!?) : expr: Expr<bool> -> unit

    /// <summary>Asserts that at least one expectation from a sequence succeeds.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="expectations">The expectations to try.</param>
    /// <exception cref="System.Exception">Raised when no supplied expectation succeeds.</exception>
    /// <remarks>
    /// Use this when you have several alternative expectations and the expression may satisfy any one
    /// of them.
    /// </remarks>
    /// <seealso cref="M:Testify.AssertExpectation.any``1(System.Collections.Generic.IEnumerable{Testify.AssertExpectation{``0}})">
    /// Sequence-based convenience for trying several alternative expectations against one quotation.
    /// </seealso>
    /// <example id="assert-operators-3">
    /// <code lang="fsharp">
    /// &lt;@ 5 @&gt; ||? [ AssertExpectation.equalTo 4
    ///             AssertExpectation.equalTo 5 ]
    /// </code>
    /// </example>
    val inline (||?) : expr: Expr<'T> -> expectations: seq<AssertExpectation<'T>> -> unit

    /// <summary>Asserts that every expectation from a sequence succeeds.</summary>
    /// <param name="expr">The quoted expression under test.</param>
    /// <param name="expectations">The expectations that must all succeed.</param>
    /// <exception cref="System.Exception">Raised when any supplied expectation fails.</exception>
    /// <remarks>
    /// Use this when you want readable sequence-based logic instead of manually folding
    /// <c>&lt;&amp;&gt;</c>.
    /// </remarks>
    /// <seealso cref="M:Testify.AssertExpectation.all``1(System.Collections.Generic.IEnumerable{Testify.AssertExpectation{``0}})">
    /// Sequence-based convenience for requiring every supplied expectation to pass.
    /// </seealso>
    /// <example id="assert-operators-4">
    /// <code lang="fsharp">
    /// &lt;@ "Testify" @&gt; &amp;&amp;?
    ///     [ AssertExpectation.startsWith "Test"
    ///       AssertExpectation.endsWith "fy" ]
    /// </code>
    /// </example>
    val inline (&&?) : expr: Expr<'T> -> expectations: seq<AssertExpectation<'T>> -> unit
