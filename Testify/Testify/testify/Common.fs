namespace MiniLib.Testify


open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.RuntimeHelpers


type Observed<'T> = Result<'T, exn>


[<RequireQualifiedAccess>]
module internal Common =
    let inline capture (operation: unit -> 'T) : Observed<'T> =
        try
            Result.Ok (operation ())
        with ex ->
            Result.Error ex

    let inline evalQuotation (expr: Expr) : objnull =
        LeafExpressionConverter.EvaluateQuotation expr


[<RequireQualifiedAccess>]
module Observed =
    let inline observe (expr: Expr<'T>) : Observed<'T> =
        Common.capture (fun () -> Common.evalQuotation expr :?> 'T)

    let inline observeUntyped<'T> (expr: Expr) : Observed<'T> =
        Common.capture (fun () -> Common.evalQuotation expr :?> 'T)

    let format (observed: Observed<'T>) : string =
        match observed with
        | Result.Ok value -> Render.formatValue value
        | Result.Error ex -> Render.formatException ex

    let formatValueOrException
        (formatValue: 'T -> string)
        (observed: Observed<'T>)
        : string =
        match observed with
        | Result.Ok value -> formatValue value
        | Result.Error ex -> Render.formatException ex
