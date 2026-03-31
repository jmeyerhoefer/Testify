namespace Testify

open Microsoft.FSharp.Quotations


module AssertOperators =
    let inline (<|>)
        (a: ^T)
        (b: ^T)
        : ^T =
        ((^T or ^T) : (static member OrElse: ^T * ^T -> ^T) (a, b))

    let inline (<&>)
        (a: ^T)
        (b: ^T)
        : ^T =
        ((^T or ^T) : (static member AndAlso: ^T * ^T -> ^T) (a, b))

    let inline (|>?) expr expectation =
        Assert.should expectation expr

    let inline (>>?) (expr: Expr<'T>) (expectation: AssertExpectation<'T>) : Expr<'T> =
        Assert.should expectation expr
        expr

    let inline (<?) expr value =
        Assert.should (AssertExpectation.lessThan value) expr

    let inline (<=?) expr value =
        Assert.should (AssertExpectation.lessThanOrEqualTo value) expr

    let inline (=?) expr value =
        Assert.should (AssertExpectation.equalTo value) expr

    let inline (<>?) expr value =
        Assert.should (AssertExpectation.notEqualTo value) expr

    let inline (>?) expr value =
        Assert.should (AssertExpectation.greaterThan value) expr

    let inline (>=?) expr value =
        Assert.should (AssertExpectation.greaterThanOrEqualTo value) expr

    let inline (^?) expr =
        Assert.should AssertExpectation.throwsAny expr

    let inline (^!?) expr =
        Assert.should AssertExpectation.doesNotThrow expr

    let inline (?) expr =
        Assert.should (AssertExpectation.equalTo true) expr

    let inline (!?) expr =
        Assert.should (AssertExpectation.equalTo false) expr

    let inline (||?) expr expectations =
        Assert.should (AssertExpectation.any expectations) expr

    let inline (&&?) expr expectations =
        Assert.should (AssertExpectation.all expectations) expr
