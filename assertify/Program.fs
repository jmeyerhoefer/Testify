//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// PROGRAM %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


module Program

open Assertify.Expressions
open Assertify.Expressions.Operators
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.ExprShape
open Microsoft.FSharp.Reflection
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

let rec substitute (var: Var) (replacement: Expr) (expr: Expr) : Expr =
    match expr with
    | Var v when v = var -> replacement
    | Lambda(v, body) when v = var -> expr  // shadowed
    | Lambda(v, body) -> Expr.Lambda(v, substitute var replacement body)
    | Let(v, value, body) ->
        if v = var then
            Expr.Let(v, substitute var replacement value, body)
        else
            Expr.Let(v, substitute var replacement value, substitute var replacement body)
    | ShapeCombination(shape, args) ->
        RebuildShapeCombination(shape, args |> List.map (substitute var replacement))
    | _ -> expr


let applyOne (arg: Expr) (expr: Expr) =
    match expr with
    | Lambda(var, body) -> substitute var arg body
    | _ -> failwith "Not a lambda"


let applyArgs (args: Expr list) (expr: Expr) =
    (expr, args) ||> List.fold (fun e a -> applyOne a e)


/// Flatten a lambda expression into a list of parameters and the body
let rec flattenLambda (expr: Expr) : Var list * Expr =
    match expr with
    | Lambda(v, body) ->
        let vars, body = flattenLambda body
        v :: vars, body
    | _ -> [], expr

/// Convert an argument to match the expected variable type:
/// - If the variable is a tuple and the argument is tupled, reconstruct it
/// - Otherwise, use the argument directly
let rec makeArg (v: Var) (arg: Expr) =
    if FSharpType.IsTuple v.Type && not (FSharpType.IsTuple arg.Type) then
        // The variable is a tuple but argument is single? wrap in tuple
        let elems = FSharpType.GetTupleElements v.Type
        let values = Array.mapi (fun i t -> if i = 0 then Expr.Coerce(arg, t) else Expr.DefaultValue t) elems
        Expr.NewTuple(values |> Array.toList)
    else
        Expr.Coerce(arg, v.Type)

/// Substitute variables by arguments recursively
let rec substituteVars (vars: Var list) (args: Expr list) (body: Expr) =
    match vars, args with
    | [], [] -> body
    | v::vs, a::as' ->
        let a = makeArg v a
        let body' = substitute' v a body
        substituteVars vs as' body'
    | _, _ -> failwith "Number of arguments does not match lambda parameters"

/// Substitute a single variable by an expression
and substitute' (v: Var) (replacement: Expr) (expr: Expr) =
    match expr with
    | Var var when var = v -> replacement
    | Lambda(var, body) when var = v -> expr // shadowed, skip
    | Lambda(var, body) -> Expr.Lambda(var, substitute' v replacement body)
    | Let(var, value, body) -> Expr.Let(var, substitute' v replacement value, substitute' v replacement body)
    | ShapeCombination(shape, args) -> RebuildShapeCombination(shape, args |> List.map (substitute' v replacement))
    | _ -> expr


/// Recursively flatten lambda, including tuple parameters
let rec flattenLambdaFull (expr: Expr) : Var list * Expr =
    match expr with
    | Lambda(var, body) ->
        if FSharpType.IsTuple var.Type then
            // Split tuple into individual vars
            let elems = FSharpType.GetTupleElements var.Type
            let tupleVars =
                elems
                |> Array.mapi (fun i t -> Var(var.Name + "_" + string i, t))
                |> Array.toList
            let body' =
                // Replace original tuple var with tuple of new vars
                let tupleExpr = Expr.NewTuple(tupleVars |> List.map Expr.Var)
                substitute' var tupleExpr body
            let innerVars, innerBody = flattenLambdaFull body'
            tupleVars @ innerVars, innerBody
        else
            let innerVars, innerBody = flattenLambdaFull body
            var :: innerVars, innerBody
    | _ -> [], expr



/// Universal apply function: works for curried and tupled lambdas
let applyArgsUniversal (args: Expr list) (expr: Expr) =
    let vars, body = flattenLambda expr
    substituteVars vars args body





[<EntryPoint>]
let main (_: string array): int =
    let f1 = <@ fun a b -> a + b @>
    let r1 = applyArgs [ <@ 1 @>; <@ 2 @> ] f1
    printfn $"%s{r1.Decompile ()}" // 1 + 2

    let f2 = <@ fun (a, b) -> a + b @>
    let r2 = applyArgs [ <@ 1, 2 @> ] f2
    printfn $"%s{r2.Decompile ()}" // 1 + 2

    let f3 = <@ fun (a, b) c -> a + b + c @>
    let r3 = applyArgs [ <@ (1, 2) @>; <@ 3 @> ] f3
    printfn $"%s{r3.Decompile ()}" // 1 + 2 + 3

    // let f4 = <@ fun (a, b) c (d, e) -> a + b + c + d + e @>
    // let r4 = applyArgsUniversal [ <@ (1, 2) @>; <@ 3 @>; <@ (4, 5) @> ] f4
    // printfn "%s" (r4.Decompile()) // 1 + 2 + 3 + 4 + 5

    // let f = <@ fun (f: int -> int list, xs: int list) -> List.collect f xs = [] @>
    //
    // let applied = f |> applyArgs [ <@ (fun (x: int) -> [x]), [1; 2] @> ]
    //
    // printfn $"Hallo: %s{applied.Decompile()}"

    // let rec applyLambda (args: Expr list) (expr: Expr): Expr =
    //     match expr with
    //     | Patterns.Lambda _ ->
    //         match args with
    //         | [] -> expr
    //         | arg :: rest ->
    //             Expr.Application (expr, arg)
    //             |> applyLambda rest
    //     | Patterns.Let (var, value, body) -> Expr.Let (var, value, applyLambda args body)
    //     | _ -> args |> List.fold (fun e a -> Expr.Application (e, a)) expr
    //
    // let inline (@@@) (a: Expr) (b: Expr list): Expr =
    //     applyLambda b a
    //
    // let f = <@ fun (f: Nat -> Nat list) (xs: Nat list) -> List.collect f xs = [] @>
    // let applied = f @@@ [ <@ fun (x: Nat) -> [ x ] @>; <@ [ 1N; 2N ] @> ]
    // printfn $"{applied.Decompile()}"

    // let rec substitute (var: Var) (replacement: Expr) (expr: Expr): Expr =
    //     match expr with
    //     | Patterns.Var v when v = var -> replacement
    //     | Patterns.Lambda (v, _) when v = var -> expr
    //     | Patterns.Lambda (v, body) -> Expr.Lambda (v, substitute var replacement body)
    //     | Patterns.Application (f, a) -> Expr.Application (substitute var replacement f, substitute var replacement a)
    //     | Patterns.Let (v, value, body) ->
    //         if v = var then
    //             Expr.Let (v, substitute var replacement value, body)
    //         else
    //             Expr.Let (v, substitute var replacement value, substitute var replacement body)
    //     | ExprShape.ShapeCombination (shape, args) ->
    //         ExprShape.RebuildShapeCombination (shape, args |> List.map (substitute var replacement))
    //
    //     | _ -> expr
    //
    // let applyOneExpr (expr: Expr) (arg: Expr) =
    //     match expr with
    //     | Patterns.Lambda (var, body) -> substitute var arg body
    //     | _ -> failwith "Expression is not a lambda."
    //
    // let applyExpr (args: Expr list) (expr: Expr) =
    //     (expr, args) ||> List.fold applyOneExpr
    //
    // let f = <@ fun (f: int -> int list, xs: int list) -> List.collect f xs = [] @>
    // f |> applyExpr [ <@ (fun (x: int) -> [x * 2; x + 3]), [1] @> ]
    // |> _.Decompile()
    // |> printfn "%s"
    // |> List.iter  (decompile >> printfn "%s")

    0
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%