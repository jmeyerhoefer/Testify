namespace Testify


module CheckOperators =
    let inline (|=>) expr reference =
        Check.shouldEqual expr reference

    let inline (|=>?) expr (config, reference) =
        Check.shouldEqualWith config expr reference

    let inline (|=>??) expr (arb, reference) =
        Check.shouldEqualUsing arb expr reference

    let inline (|=>???) expr (expectation, reference) =
        Check.should expr reference expectation
