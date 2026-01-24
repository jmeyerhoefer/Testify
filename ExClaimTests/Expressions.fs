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
    let rec apply (args: obj list) (expr: Expr): Expr =
        let args =
            if args.Length = 2 then
                args |> List.rev
            else
                args
        match args, expr with
        | [], _ -> expr
        | h :: t, Patterns.Lambda (var, body) ->
            body.Substitute (fun (x: Var) -> if x = var then Some (Expr.Value (h, var.Type)) else None)
            |> apply t
        | _ -> failwith $"Too many arguments applied. Number of remaining arguments: %d{args.Length}, Expr: %s{expr.Decompile ()}"


    /// <summary>TODO</summary>
    let applySingle (arg: obj) (expr: Expr): Expr =
        expr |> apply [ arg ]


    /// <summary>TODO</summary>
    let eval<'a> (expr: Expr): 'a =
        LeafExpressionConverter.EvaluateQuotation expr :?> 'a


    /// <summary>TODO</summary>
    let simplifyExpression (expr: Expr) (args: obj list): string =
        match expr |> apply args with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) as x ->
            printfn $"{x.Decompile()}"
            let rightSide: string =
                try right.Eval () with _ ->
                    try right.Eval().ToString () with _ ->
                        try right.Decompile () with _ -> "<non-evaluatable>"
            $"%s{left.Decompile ()} = %s{rightSide}"
        | _ -> expr.Decompile ()


    /// <summary>TODO</summary>
    let simplifyInExpression (expr: Expr): Expr =
        let rec simplify expr =
            match expr with
            | Patterns.Let (v, value, body) -> substituteVar v (simplify value) body |> simplify
            | Patterns.TupleGet (Patterns.Value (tupleObj, tupleTy), idx) ->
                let values: obj array = Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields tupleObj
                let elemType: System.Type = (Microsoft.FSharp.Reflection.FSharpType.GetTupleElements tupleTy)[idx]
                Expr.Value (values[idx], elemType)
            | ExprShape.ShapeVar v -> Expr.Var v
            | ExprShape.ShapeLambda (v, body) -> Expr.Lambda (v, simplify body)
            | ExprShape.ShapeCombination (shape, args) -> ExprShape.RebuildShapeCombination (shape, args |> List.map simplify)
        and substituteVar (var: Var) (replacement: Expr) (body: Expr): Expr =
            match body with
            | ExprShape.ShapeVar v when obj.ReferenceEquals(v, var) -> replacement
            | ExprShape.ShapeLambda (v, _) as e when obj.ReferenceEquals (v, var) -> e
            | ExprShape.ShapeLambda (v, b) -> Expr.Lambda (v, substituteVar var replacement b)
            | ExprShape.ShapeCombination (shape, args) -> ExprShape.RebuildShapeCombination (shape, args |> List.map (substituteVar var replacement))
            | _ -> body
        simplify expr


    let toReadableExpression (shrunkOptions: obj list option) (expr: Expr): string =
        shrunkOptions
        |> Option.map (simplifyExpression expr)
        |> Option.defaultValue (expr.Decompile ())


    let rec extractList (expr: Expr): Expr list =
        let rec helper (expr: Expr): Expr =
            match expr with
            | Patterns.Let (_, _, e) -> helper e
            | _ -> expr

        let rec extracter (expr: Expr): Expr list =
            match helper expr with
            | Patterns.NewUnionCase (uci, [head; tail]) when uci.Name = "Cons" -> head :: extracter tail
            | Patterns.NewUnionCase (uci, []) when uci.Name = "Empty" -> []
            | _ -> failwith $"Not a list: %A{expr}"

        match expr with
            | Patterns.Lambda (_, body) ->
                match helper body with
                | Patterns.NewUnionCase _ as listExpr -> extracter listExpr
                | s -> failwith $"Not a list: {s.Decompile ()}"
            | s -> failwith $"Not a lambda: {s.Decompile()}"


    let evalAndApply<'a> (args: obj list) (expr: Expr): 'a =
        expr
        |> apply args
        |> eval<'a>


    let evalActual (expr: Expr): string =
        // TODO: <null> when option type 'None'
        try expr.Eval () with _ ->
            try expr.Eval().ToString () with _ ->
                try expr.Reduce().Decompile () with _ ->
                    "<non-evaluatable> (Is the method still implemented with 'failwith \"TODO\"'?)"


    let evalExpected (expr: Expr): string =
        // TODO: <null> when option type 'None'
        try expr.Eval().ToString() with _ ->
            try expr.Reduce().Decompile () with _ ->
                "<non-evaluatable> (Try to contact: Me)"


    let extractActualAndExpected (expr: Expr) (shrunkResults: obj list option): string option * string option =
        match shrunkResults with
            | Some shrunk ->
                match expr |> apply shrunk with
                | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                    printfn $"left: {left.Eval()}"
                    printfn $"right: {right.Decompile()}"
                    Some (evalActual left), Some (evalExpected right)
                | _ -> None, None
            | _ -> None, None


    let deconstructEquality (expr: Expr): (Expr * Expr) option =
        match expr with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) -> Some (left, right)
        | _ -> None


module Operators =
    let inline (@@) (expr: Expr) (arg: obj list): Expr =
        expr |> Expressions.apply arg


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%