namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for property-style checks.</summary>
module CheckOperators =
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>A combined expectation that succeeds when either input expectation succeeds.</returns>
    val inline (<|>) :
        a: ^T -> b: ^T -> ^T
        when ^T : (static member OrElse: ^T * ^T -> ^T)

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>A combined expectation that succeeds only when both input expectations succeed.</returns>
    val inline (<&>) :
        a: ^T -> b: ^T -> ^T
        when ^T : (static member AndAlso: ^T * ^T -> ^T)

    /// <summary>Runs the default equality-based throwing property check.</summary>
    /// <param name="expr">The quoted tested function under evaluation.</param>
    /// <param name="reference">The trusted reference implementation.</param>
    /// <exception cref="System.Exception">
    /// Raised when the property does not pass.
    /// </exception>
    /// <seealso cref="M:Testify.Check.should``3(Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config},Microsoft.FSharp.Core.FSharpOption{FsCheck.Arbitrary{``0}})">
    /// Named API equivalent using <c>CheckExpectation.equalToReference</c>.
    /// </seealso>
    /// <example id="check-operators-1">
    /// <code lang="fsharp">
    /// open Testify.CheckOperators
    ///
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt; |=&gt; id
    /// </code>
    /// </example>
    val inline (|=>) :
        expr: Expr<'Args -> 'Testable> ->
        reference: ('Args -> 'Testable) ->
            unit
        when 'Testable : equality

    /// <summary>Runs the default equality-based throwing property check, then returns the quotation.</summary>
    /// <param name="expr">The quoted tested function under evaluation.</param>
    /// <param name="reference">The trusted reference implementation.</param>
    /// <returns>The original quotation.</returns>
    /// <exception cref="System.Exception">
    /// Raised when the property does not pass.
    /// </exception>
    /// <seealso cref="M:Testify.CheckOperators.op_BarEqualsGreater``2(Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpFunc{``0,``1})">
    /// Use <c>|=&gt;</c> when you do not need to keep the quotation for further chaining.
    /// </seealso>
    /// <example id="check-operators-2">
    /// <code lang="fsharp">
    /// open Testify.CheckOperators
    ///
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt; |=&gt;&gt; id |&gt; ignore
    /// </code>
    /// </example>
    val inline (|=>>) :
        expr: Expr<'Args -> 'Testable> ->
        reference: ('Args -> 'Testable) ->
            Expr<'Args -> 'Testable>
        when 'Testable : equality

    /// <summary>Runs a callback-built bool property check and raises immediately on failure.</summary>
    /// <param name="expr">The quoted bool-returning function under evaluation.</param>
    /// <param name="buildProperty">
    /// The callback that wraps the supplied verifier in custom FsCheck quantification.
    /// </param>
    /// <exception cref="System.Exception">
    /// Raised when the property does not pass.
    /// </exception>
    /// <seealso cref="M:Testify.Check.shouldBy``3(Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.FSharpFunc{``0,System.Boolean},FsCheck.Property},Testify.CheckExpectation{``0,``1,``2},Microsoft.FSharp.Core.FSharpFunc{``0,``2},Microsoft.FSharp.Quotations.FSharpExpr{Microsoft.FSharp.Core.FSharpFunc{``0,``1}},Microsoft.FSharp.Core.FSharpOption{FsCheck.Config})">
    /// Named API equivalent for advanced callback-built properties.
    /// </seealso>
    /// <example id="check-operators-3">
    /// <code lang="fsharp">
    /// open Testify.CheckOperators
    ///
    /// &lt;@ fun (expectedLength, xs) -&gt; List.length xs = expectedLength @&gt;
    /// |?&gt; (fun verify -&gt;
    ///         FsCheck.Prop.forAll Arbitraries.from&lt;int * int list&gt; verify)
    /// </code>
    /// </example>
    val inline (|?>) :
        expr: Expr<'Args -> bool> ->
        buildProperty: (('Args -> bool) -> FsCheck.Property) ->
            unit
