module Program


open Microsoft.FSharp.Quotations
open Swensen.Unquote


let inOrderStudent (a: int) (b: int) (c: int): bool = a <= b && b >= c
let inOrderSolution (a: int) (b: int) (c: int): bool = a <= b && b <= c


let applyArgsToCurriedQuotation (expr: Expr) (args: obj list): Expr =
    let rec loop (e: Expr) (remainingArgs: obj list): Expr =
        match e, remainingArgs with
        | Patterns.Lambda (var, body), arg :: tail ->
            let replacement: Expr = Expr.Value (arg, var.Type)
            let substituted: Expr = body.Substitute (fun (v: Var) -> if v = var then Some replacement else None)
            loop substituted tail
        | _, [] -> e // remainingArgs empty -> final expression
        | _, _ -> failwith "Too many arguments for the lambda function."
    loop expr args


[<EntryPoint>]
let main (_: string array): int =
    let quotedExpr = <@ fun (x: int) (y: int) (z: int) -> inOrderStudent x y z = inOrderSolution x y z @>
    let counterExample: obj list = [ box 0; box 0; box 0 ]
    let reduced: Expr = applyArgsToCurriedQuotation quotedExpr counterExample
    printfn $"%s{reduced.Decompile ()}"
    printfn $"%s{reduced.ToString ()}"
    match reduced with
    | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
        printfn $"Tested expression: %s{left.Decompile ()} = %A{right.Eval ()}"
    | _ -> printfn "ERROR"
    0