namespace MiniLib.Testify


module CheckOperators =
    let inline (|=>) expr reference =
        Check.shouldEqual expr reference

    let inline (||=>) expr (reference, arb) =
        Check.shouldEqualUsing arb expr reference

    let inline (|=>?) expr (reference, expectation) =
        Check.should expr reference expectation
