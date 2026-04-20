namespace Testify

open Microsoft.FSharp.Quotations

module CheckOperators =
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

    let inline (|=>) expr reference =
        Check.should(CheckExpectation.equalToReference, reference, expr)

    let inline (|=>>)
        (expr: Expr<'Args -> 'Testable>)
        (reference: 'Args -> 'Testable)
        : Expr<'Args -> 'Testable> =
        Check.should(CheckExpectation.equalToReference, reference, expr)
        expr

    let inline (|?>)
        (expr: Expr<'Args -> bool>)
        (buildProperty: (('Args -> bool) -> FsCheck.Property))
        : unit =
        Check.shouldBy(
            buildProperty,
            CheckExpectation.isTrue,
            (fun _ -> true),
            expr
        )
