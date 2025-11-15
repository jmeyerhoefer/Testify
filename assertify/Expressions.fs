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


    /// <summary>TODO</summary>
    let eval<'a> (expr: Expr): 'a =
        LeafExpressionConverter.EvaluateQuotation expr :?> 'a


    /// <summary>TODO</summary>
    let simplifyExpression (expr: Expr) (args: obj list): string =
        match expr |> applyArgs args with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
            let rightSide = "" // TODO: Fix <null> for option types that are 'None'
            $"%s{left.Decompile ()} = %A{right.Eval ()}"
        | _ -> expr.Decompile ()


    /// <summary>TODO</summary>
    let simplifyInExpression (expr: Expr): Expr =
        let rec simplify expr =
            match expr with
            | Patterns.Let (v, value, body) -> substituteVar v (simplify value) body |> simplify
            | Patterns.TupleGet (Patterns.Value (tupleObj, tupleTy), idx) ->
                let values: objnull array =
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


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%