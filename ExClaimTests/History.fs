//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// HISTORY %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.History


open Microsoft.FSharp.Quotations
open Assertify.Core
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
type History () =
    /// <summary>TODO</summary>
    member val Evaluated: Expr<unit> list = [] with get, set


    /// <summary>TODO</summary>
    new (expr: Expr<unit>) as this =
        History () then
            try expr.Eval () with _ ->
                Core.failNow
                <| AssertifyResult.MakeResult (
                    "HistoryEval",
                    expression = expr.Decompile (),
                    message = "Failed to execute expression"
                )
            this.Evaluated <- [expr]


    /// <summary>TODO</summary>
    new (exprs: Expr<unit> list) as this =
        History () then
            exprs |> List.iter (fun (e: Expr<unit>) ->
                try e.Eval () with _ ->
                    Core.failNow
                    <| AssertifyResult.MakeResult (
                        "HistoryEval",
                        expression = e.Decompile (),
                        message = "Failed to execute expression")
                    )
            this.Evaluated <- exprs


    /// <summary>TODO</summary>
    member this.IsEmpty: bool = this.Evaluated.IsEmpty


    /// <summary>TODO</summary>
    member this.EvalAndAdd (expr: Expr<unit>): unit =
        try expr.Eval () with _ ->
            Core.failNow
            <| AssertifyResult.MakeResult (
                "HistoryEval",
                expression = expr.Decompile (),
                message = "Failed to execute expression"
            )
        this.Evaluated <- this.Evaluated @ [expr]


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%