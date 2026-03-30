namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for property-style checks.</summary>
module CheckOperators =
    /// <summary>Checks that a quoted function behaves like the reference implementation.</summary>
    ///
    /// <example id="check-operators-1">
    /// <code lang="fsharp">
    /// open Testify.CheckOperators
    ///
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt; |=&gt; id
    /// </code>
    /// </example>
    val inline (|=>) : expr: Expr<'Args -> 'T> -> reference: ('Args -> 'T) -> unit when 'T : equality

    /// <summary>Like <c>|=&gt;</c>, but uses an explicit FsCheck configuration.</summary>
    val inline (|=>?) :
        expr: Expr<'Args -> 'T> ->
        config: FsCheck.Config * ('Args -> 'T) ->
            unit
                when 'T : equality

    /// <summary>Like <c>|=&gt;</c>, but uses an explicit arbitrary for the generated input domain.</summary>
    val inline (|=>??) :
        expr: Expr<'Args -> 'T> ->
        arb: FsCheck.Arbitrary<'Args> * ('Args -> 'T) ->
            unit
                when 'T : equality

    /// <summary>Runs a property-style check with a custom expectation.</summary>
    ///
    /// <example id="check-operators-2">
    /// <code lang="fsharp">
    /// &lt;@ List.sort @&gt; |=&gt;??? (CheckExpectation.equalToReference, List.sort)
    /// </code>
    /// </example>
    val inline (|=>???) :
        expr: Expr<'Args -> 'Actual> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> * ('Args -> 'Expected) ->
            unit
