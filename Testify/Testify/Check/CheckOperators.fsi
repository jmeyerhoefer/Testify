namespace Testify

open Microsoft.FSharp.Quotations

/// <summary>Concise operator syntax for property-style checks.</summary>
/// <remarks>
/// These operators keep the quoted implementation on the left while delegating to the pipe-friendly
/// <c>Check</c> function API underneath.
/// Bool-returning property tests intentionally use named helpers such as <c>Check.shouldBeTrue</c>
/// and <c>Check.shouldBeFalse</c> instead of a dedicated operator.
/// </remarks>
module CheckOperators =
    /// <summary>Combines two expectations so that either one may succeed.</summary>
    /// <param name="a">The first alternative expectation.</param>
    /// <param name="b">The second alternative expectation.</param>
    /// <returns>An expectation that succeeds when either input expectation succeeds.</returns>
    val inline (<|>) :
        a: ^T -> b: ^T -> ^T
        when ^T: (static member OrElse: ^T * ^T -> ^T)

    /// <summary>Combines two expectations so that both must succeed.</summary>
    /// <param name="a">The first required expectation.</param>
    /// <param name="b">The second required expectation.</param>
    /// <returns>An expectation that succeeds only when both input expectations succeed.</returns>
    /// <example id="check-operators-0">
    /// <code lang="fsharp">
    /// open Testify.CheckOperators
    ///
    /// let expectation =
    ///     CheckExpectation.equalToReference
    ///     <&> CheckExpectation.equalToReference
    /// </code>
    /// </example>
    val inline (<&>) :
        a: ^T -> b: ^T -> ^T
        when ^T: (static member AndAlso: ^T * ^T -> ^T)

    /// <summary>Checks that a quoted function behaves like the reference implementation.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="reference">The reference implementation to compare against.</param>
    /// <exception cref="System.Exception">
    /// Raised when the quoted implementation does not satisfy the default equality-based check.
    /// </exception>
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

    /// <summary>Runs the default equality-based check, then returns the original quotation for chaining.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="reference">The reference implementation to compare against.</param>
    /// <returns>The original quoted implementation.</returns>
    /// <exception cref="System.Exception">
    /// Raised when the quoted implementation does not satisfy the default equality-based check.
    /// </exception>
    /// <remarks>
    /// The returned quotation is not cached. If it is checked again later in the chain, it will be
    /// evaluated again.
    /// </remarks>
    val inline (|=>>) :
        expr: Expr<'Args -> 'Testable> ->
        reference: ('Args -> 'Testable) ->
        Expr<'Args -> 'Testable>
        when 'Testable : equality

    /// <summary>Runs the default equality-based check with an explicit FsCheck configuration.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="config">A tuple containing the FsCheck configuration and the reference implementation.</param>
    /// <exception cref="System.Exception">Raised when the check fails.</exception>
    val inline (|=>?) :
        expr: Expr<'Args -> 'Testable> ->
        config: FsCheck.Config * ('Args -> 'Testable) ->
        unit
        when 'Testable : equality

    /// <summary>Runs the default equality-based check with an explicit arbitrary for the generated input domain.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="arb">A tuple containing the arbitrary and the reference implementation.</param>
    /// <exception cref="System.Exception">Raised when the check fails.</exception>
    val inline (|=>??) :
        expr: Expr<'Args -> 'Testable> ->
        arb: FsCheck.Arbitrary<'Args> * ('Args -> 'Testable) ->
        unit
        when 'Testable : equality

    /// <summary>Runs a property-style check with a custom expectation.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="expectation">A tuple containing the custom expectation and the reference implementation.</param>
    /// <exception cref="System.Exception">Raised when the check fails.</exception>
    /// <example id="check-operators-2">
    /// <code lang="fsharp">
    /// &lt;@ List.sort @&gt; |=&gt;??? (CheckExpectation.equalToReference, List.sort)
    /// </code>
    /// </example>
    val inline (|=>???) :
        expr: Expr<'Args -> 'Actual> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> * ('Args -> 'Expected) ->
        unit

    /// <summary>Runs a custom property-style check, then returns the original quotation for further chaining.</summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="expectation">A tuple containing the custom expectation and the reference implementation.</param>
    /// <returns>The original quoted implementation.</returns>
    /// <exception cref="System.Exception">Raised when the check fails.</exception>
    /// <remarks>
    /// The returned quotation is not cached. If it is checked again later in the chain, it will be
    /// evaluated again.
    /// </remarks>
    /// <example id="check-operators-2b">
    /// <code lang="fsharp">
    /// &lt;@ List.sort @&gt;
    /// |=&gt;&gt;? (CheckExpectation.equalToReference, List.sort)
    /// |=&gt;&gt;? (CheckExpectation.throwsSameExceptionType, List.sort)
    /// |&gt; ignore
    /// </code>
    /// </example>
    val inline (|=>>?) :
        expr: Expr<'Args -> 'Actual> ->
        expectation: CheckExpectation<'Args, 'Actual, 'Expected> * ('Args -> 'Expected) ->
        Expr<'Args -> 'Actual>

    /// <summary>
    /// Advanced convenience operator for equality/reference checks with optional configuration, arbitrary, and expectation.
    /// </summary>
    /// <param name="expr">The quoted implementation under test.</param>
    /// <param name="options">
    /// A tuple containing an optional FsCheck configuration, an optional arbitrary, an optional
    /// expectation, and the reference implementation.
    /// </param>
    /// <exception cref="System.Exception">Raised when the resolved check fails.</exception>
    /// <remarks>
    /// <para>This operator is a power-user shortcut rather than the primary recommended API.</para>
    /// <para>
    /// When the configuration is omitted, Testify uses <c>CheckConfig.defaultConfig</c>. When the
    /// arbitrary is omitted, Testify resolves the default arbitrary for the input type. When the
    /// expectation is omitted, Testify uses <c>CheckExpectation.equalToReference</c>.
    /// </para>
    /// </remarks>
    /// <example id="check-operators-3">
    /// <code lang="fsharp">
    /// &lt;@ List.rev &gt;&gt; List.rev @&gt;
    /// ||=&gt;? (None, None, None, id)
    /// </code>
    /// </example>
    val inline (||=>?) :
        expr: Expr<'Args -> 'Testable> ->
        options: FsCheck.Config option * FsCheck.Arbitrary<'Args> option * CheckExpectation<'Args, 'Testable, 'Testable> option * ('Args -> 'Testable) ->
        unit
        when 'Testable : equality
