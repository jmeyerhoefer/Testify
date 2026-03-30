namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for example-based assertions.</summary>
module AssertOperators =
    /// <summary>Combines two expectations with logical OR.</summary>
    val inline (<|>) :
        a: ^T -> b: ^T -> ^T
            when ^T : (static member OrElse: ^T * ^T -> ^T)

    /// <summary>Combines two expectations with logical AND.</summary>
    val inline (<&>) :
        a: ^T -> b: ^T -> ^T
            when ^T : (static member AndAlso: ^T * ^T -> ^T)

    /// <summary>Applies an expectation to a quoted expression.</summary>
    ///
    /// <example id="assert-operators-1">
    /// <code lang="fsharp">
    /// open Testify.AssertOperators
    ///
    /// &lt;@ 2 + 3 @&gt; -?&gt; AssertExpectation.equalTo 5
    /// </code>
    /// </example>
    val inline (-?>) : expr: Expr<'T> -> expectation: AssertExpectation<'T> -> unit

    /// <summary>Asserts that a quoted value is less than the provided comparison value.</summary>
    val inline (<?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value is less than or equal to the provided comparison value.</summary>
    val inline (<=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value equals the provided value.</summary>
    ///
    /// <example id="assert-operators-2">
    /// <code lang="fsharp">
    /// &lt;@ List.length [1; 2; 3] @&gt; =? 3
    /// </code>
    /// </example>
    val inline (=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : equality

    /// <summary>Asserts that a quoted value does not equal the provided value.</summary>
    val inline (<>?) : expr: Expr<'T> -> value: 'T -> unit when 'T : equality

    /// <summary>Asserts that a quoted value is greater than the provided comparison value.</summary>
    val inline (>?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that a quoted value is greater than or equal to the provided comparison value.</summary>
    val inline (>=?) : expr: Expr<'T> -> value: 'T -> unit when 'T : comparison

    /// <summary>Asserts that evaluating the quoted expression throws an exception.</summary>
    val inline (^?) : expr: Expr<'T> -> unit

    /// <summary>Asserts that evaluating the quoted expression completes without throwing.</summary>
    val inline (^!?) : expr: Expr<'T> -> unit

    /// <summary>Asserts that a quoted boolean expression evaluates to <c>true</c>.</summary>
    val inline (?) : expr: Expr<bool> -> unit

    /// <summary>Asserts that a quoted boolean expression evaluates to <c>false</c>.</summary>
    val inline (!?) : expr: Expr<bool> -> unit

    /// <summary>Asserts that a quoted expression satisfies either of two expectations.</summary>
    val inline (|?|) : expr: Expr<'T> -> expectA: AssertExpectation<'T> * AssertExpectation<'T> -> unit

    /// <summary>Asserts that a quoted expression satisfies both of two expectations.</summary>
    val inline (&?&) : expr: Expr<'T> -> expectA: AssertExpectation<'T> * AssertExpectation<'T> -> unit

    /// <summary>Asserts that a quoted expression satisfies at least one expectation from a sequence.</summary>
    ///
    /// <example id="assert-operators-3">
    /// <code lang="fsharp">
    /// &lt;@ 5 @&gt; |?|&gt; [ AssertExpectation.equalTo 4
    ///               AssertExpectation.equalTo 5 ]
    /// </code>
    /// </example>
    val inline (|?|>) : expr: Expr<'T> -> expectations: seq<AssertExpectation<'T>> -> unit

    /// <summary>Asserts that a quoted expression satisfies every expectation from a sequence.</summary>
    ///
    /// <example id="assert-operators-4">
    /// <code lang="fsharp">
    /// &lt;@ "Testify" @&gt; &amp;?&amp;&gt;
    ///     [ AssertExpectation.startsWith "Mini"
    ///       AssertExpectation.endsWith "Lib" ]
    /// </code>
    /// </example>
    val inline (&?&>) : expr: Expr<'T> -> expectations: seq<AssertExpectation<'T>> -> unit
