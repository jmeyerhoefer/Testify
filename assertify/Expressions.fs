//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EXPRESSIONS %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.Expressions


open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.RuntimeHelpers
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
module Expressions =
    /// <summary>TODO</summary>
    let rec applyArgs (args: 'a list) (expr: Expr): Expr =
        match args, expr with
        | arg :: rest, Patterns.Lambda (v, body) ->
            let replaced =
                body.Substitute (fun (x: Var) -> if x = v then Some (Expr.Value (arg, v.Type)) else None)
            applyArgs rest replaced
        | [], _ -> expr
        | _ -> failwith "Too many arguments applied."


    let apply (arg: 'a) (expr: Expr): Expr =
        match expr with
        | Patterns.Lambda (v, body) ->
            body.Substitute (fun (x: Var) -> if x = v then Some (Expr.Value (arg, v.Type)) else None)
        | _ -> failwith "Too many arguments applied."


    /// <summary>TODO</summary>
    let eval<'a> (expr: Expr): 'a =
        LeafExpressionConverter.EvaluateQuotation expr :?> 'a


    /// <summary>TODO</summary>
    let simplifyExpression (expr: Expr) (args: obj list): string =
        match expr |> applyArgs args with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
            let rightSide = "" // TODO: Fix <null> for option types that are 'None'
            printfn $"left: {left.Decompile()}"
            printfn $"right: {right.Decompile()}"
            $"%s{left.Decompile ()} = %A{right.Eval ()}"
        | _ -> expr.Decompile ()


    /// <summary>TODO</summary>
    let simplifyInExpression (expr: Expr): Expr =
        let rec simplify expr =
            match expr with
            | Patterns.Let (v, value, body) -> substituteVar v (simplify value) body |> simplify
            | Patterns.TupleGet (Patterns.Value (tupleObj, tupleTy), idx) ->
                let values =
                    Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields tupleObj
                let elemType: System.Type =
                    (Microsoft.FSharp.Reflection.FSharpType.GetTupleElements tupleTy)[idx]
                Expr.Value (values[idx], elemType)
            | ExprShape.ShapeVar v -> Expr.Var v
            | ExprShape.ShapeLambda (v, body) -> Expr.Lambda (v, simplify body)
            | ExprShape.ShapeCombination (shape, args) -> ExprShape.RebuildShapeCombination (shape, args |> List.map simplify)
        and substituteVar (v: Var) (replacement: Expr) (body: Expr): Expr =
            match body with
            | ExprShape.ShapeVar x when x = v -> replacement
            | ExprShape.ShapeLambda (var, b) -> Expr.Lambda (var, substituteVar v replacement b)
            | ExprShape.ShapeCombination (shape, args) -> ExprShape.RebuildShapeCombination (shape, args |> List.map (substituteVar v replacement))
            | _ -> body
        simplify expr


    let rec extractList (expr: Expr): Expr list =
        let rec helper (expr: Expr): Expr =
            match expr with
            | Patterns.Let (_, _, e) -> helper e
            | _ -> expr

        let rec extracter (expr: Expr): Expr list =
            match helper expr with
            | Patterns.NewUnionCase (uci, [head; tail]) when uci.Name = "Cons" ->
                head :: extracter tail
            | Patterns.NewUnionCase (uci, []) when uci.Name = "Empty" ->
                []
            | _ ->
                failwithf $"Not a list: %A{expr}"

        match expr with
            | Patterns.Lambda (_, body) ->
                match helper body with
                | Patterns.NewUnionCase _ as listExpr ->
                    extracter listExpr
                | s -> failwith $"Not a list: {s.Decompile ()}"
            | s -> failwith $"Not a lambda: {s.Decompile()}"


    let evalAndApply<'a> (args: obj list) (expr: Expr): 'a =
        expr
        |> applyArgs args
        |> eval<'a>


    let extractActualAndExpected (expr: Expr) (shrunkResults: obj list option): string option * string option =
        match shrunkResults with
            | Some shrunk ->
                match applyArgs (shrunk |> List.rev) expr with
                | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                    let expected' = // TODO: Fix <null> for option types that are 'None'
                        try left.Eval().ToString() with
                        | _ -> "Cannot be evaluated. Possible reason: Method still implemented with 'failwith \"TODO\"'"
                    let actual' = // TODO: Fix <null> for option types that are 'None'
                        try right.Eval().ToString() with
                        | _ -> "Cannot be evaluated"
                    Some expected', Some actual'
                | _ -> None, None
            | _ -> None, None


module Operators =
    let inline (@@) (expr: Expr) arg: Expr =
        expr |> Expressions.apply arg


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%