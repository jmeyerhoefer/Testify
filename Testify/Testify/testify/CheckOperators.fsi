namespace MiniLib.Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for property-style checks.</summary>
module CheckOperators =
    /// <summary>Checks that a quoted function behaves like the reference implementation.</summary>
    ///
    /// <example id="check-operators-1">
    /// <code lang="fsharp">
    /// open MiniLib.Testify.CheckOperators
    ///
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt; |=&gt; id
    /// </code>
    /// </example>
    val inline (|=>) : expr: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit when 'T : equality

    /// <summary>Like <c>|=&gt;</c>, but uses an explicit arbitrary for the generated input domain.</summary>
    val inline (||=>) :
        expr: Expr<'Args -> 'T> ->
        reference: ('Args -> 'T) * FsCheck.Arbitrary<'Args> ->
            unit
                when 'T : equality

    /// <summary>Runs a property-style check with a custom expectation.</summary>
    ///
    /// <example id="check-operators-2">
    /// <code lang="fsharp">
    /// &lt;@ List.sort @&gt; |=&gt;? (List.sort, CheckExpectation.equalToReference)
    /// </code>
    /// </example>
    val inline (|=>?) :
        expr: Expr<'Args -> 'Actual> ->
        reference: ('Args -> 'Expected) * CheckExpectation<'Args, 'Actual, 'Expected> ->
            unit
