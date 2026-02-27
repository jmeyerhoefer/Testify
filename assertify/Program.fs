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
open Student
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


module Student2 =
    module Lists2 =
        let map (f: Nat -> Nat) (xs: Nat list): Nat list = List.map f (xs @ xs)


[<EntryPoint>]
let main (_: string array): int =
    let expr1 = <@ fun (a, b) -> [ a < 1N; b > 2N ] @>
    expr1 @@ [ 1N, 2N ]
    |> fun e -> printfn $"{e.Decompile ()}"; e
    |> Expressions.simplifyInExpression
    |> fun e -> printfn $"{e.Decompile ()}"
    // |> Expressions.eval<Nat list>
    // |> printfn "%A"
    // let expr2 = <@ fun (f: Nat -> Nat) (xs: Nat list) ->
    //     [
    //         Student2.Lists2.map f xs = [ 0N .. 4N ]
    //         Student2.Lists2.map id xs = xs
    //     ]
    // @>
    // expr2 @@ [
    //     (fun (n: Nat) -> n)
    //     [ 1N; 2N ]
    // ]
    // |> fun e -> printfn $"e1: {e.Decompile ()}"; e
    // |> Expressions.deconstructEquality
    // |> function
    // | Some (l, r) -> printfn $"l: {l.Decompile()}\nr: {r.Decompile()}"
    // | None -> printfn "x"

    0

