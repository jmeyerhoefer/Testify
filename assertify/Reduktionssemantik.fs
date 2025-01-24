module Reduktionssemantik


open Mini
open Types


let ex1RegExp: RegExp<char> = Cat (Lit 'a', Lit 'b')
let ex2RegExp: RegExp<char> = Cat (Lit 'a', Eps)
let ex3RegExp: RegExp<char> = Cat (Eps, Lit 'b')
let ex4RegExp: RegExp<char> = Or (Lit 'a', Lit 'b')
let ex5RegExp: RegExp<char> = Star (Lit 'a')


let rec isWord<'a> (r: RegExp<'a>): 'a list option =
    match r with
    | Eps -> Some []
    | Lit a -> Some [ a ]
    | Cat (r1, r2) ->
        match isWord r1, isWord r2 with
        | Some l1, Some l2 -> Some (l1 @ l2)
        | _ -> None
    | _ -> None


let rec reduceStep<'a> (r: RegExp<'a>): RegExp<'a> list =
    match r with
    | Cat (r, Eps)
    | Cat (Eps, r) -> [ r ]
    | Cat (r1, r2) ->
        [ Or (r1, r2) ]
        @ (reduceStep r1 |> List.map (fun (r: RegExp<'a>) -> Cat (r, r2)))
        @ (reduceStep r2 |> List.map (fun (r: RegExp<'a>) -> Cat (r1, r)))
    | Or (r1, r2) -> [ r1; r2; Empty ]
    | Star r -> [ Empty; Cat (r, Star r) ]
    | _ -> []


let rec reduce<'a when 'a: equality> (r: RegExp<'a>) (n: Nat): RegExp<'a> list =
    if n = 0N then
        [ r ]
    else
        let rest: RegExp<'a> list =
            reduce r (n - 1N)
            |> List.collect reduceStep
            |> List.distinct
        r :: []


let rec words<'a when 'a: equality> (r: RegExp<'a>) (n: Nat): 'a list list =
    reduce r n
    |> List.choose isWord
    |> List.distinct


let rec generates<'a when 'a: equality> (r: RegExp<'a>) (word: 'a list) (n: Nat): bool =
    words r n
    |> List.contains word