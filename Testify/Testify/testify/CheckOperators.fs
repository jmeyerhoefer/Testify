namespace Testify

open Microsoft.FSharp.Quotations


module CheckOperators =
    let inline (|=>) expr reference =
        Check.shouldEqual reference expr

    let inline (|=>>)
        (expr: Expr<'Args -> 'Testable>)
        (reference: 'Args -> 'Testable)
        : Expr<'Args -> 'Testable> =
        Check.shouldEqual reference expr
        expr

    let inline (|=>?) expr (config, reference) =
        Check.shouldEqualWith config reference expr

    let inline (|=>??) expr (arb, reference) =
        Check.shouldEqualUsing arb reference expr

    let inline (|=>???) expr (expectation, reference) =
        Check.should expectation reference expr

    let inline (|=>>?) (expr: Expr<'Args -> 'Actual>) (expectation, reference) : Expr<'Args -> 'Actual> =
        Check.should expectation reference expr
        expr

    // (configOpt, arbOpt, expectationOpt, reference):
    //     FsCheck.Config option
    //     * FsCheck.Arbitrary<'Args> option
    //     * CheckExpectation<'Args, 'Testable, 'Testable> option
    //     * ('Args -> 'Testable)
    let inline (||=>?)
        (expr: Expr<'Args -> 'Testable>)
        ((configOpt: FsCheck.Config option,
            arbOpt: FsCheck.Arbitrary<'Args> option,
            expectationOpt: CheckExpectation<'Args, 'Testable, 'Testable> option,
            reference: 'Args -> 'Testable))
        : unit =
        let expectation =
            defaultArg expectationOpt CheckExpectation.equalToReference

        match configOpt, arbOpt with
        | Some config, Some arbitrary ->
            Check.shouldUsingWith config arbitrary expectation reference expr
        | Some config, None ->
            Check.shouldWith config expectation reference expr
        | None, Some arbitrary ->
            Check.shouldUsing arbitrary expectation reference expr
        | None, None ->
            Check.should expectation reference expr
