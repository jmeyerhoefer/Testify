module Solution.Reduktionssemantik


open Types.ReduktionssemantikTypes


////a)
let rec isWord<'a>(r: RegEx<'a>): Option<List<'a>> =
    match r with
    | Eps -> Some []
    | Lit a -> Some [a]
    | Cat (r1, r2) -> 
        match (isWord r1, isWord r2) with
        | (Some l1, Some l2) -> Some (l1 @ l2)
        | _ -> None
    | _ -> None

////b)
let rec reduceStep<'a>(r: RegEx<'a>): List<RegEx<'a>> =
    match r with
    | Cat (r, Eps) | Cat (Eps, r) -> [r]
    | Cat (r1, r2) ->
        List.map (fun r1' -> Cat (r1', r2)) (reduceStep r1) @
        List.map (fun r2' -> Cat (r1, r2')) (reduceStep r2)
    | Or (r1, r2) -> [r1; r2]
    | Star r -> [Eps; Cat (r, Star r)]
    | _ -> []

////c)
let rec reduce<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<RegEx<'a>> =
    if n = 0N then [r]
    else r :: List.collect reduceStep (reduce r (n - 1N)) |> List.distinct

////d)
let rec words<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<List<'a>> =
    reduce r n |> List.choose isWord |> List.distinct

// List.choose wendet isWord auf alle Elemente der Liste an, entfernt die None
// Einträge und gibt die verbleibenden Einträge jeweils ohne das Some zurück.
// Statt List.choose können wir auch eine eigene Hilfsfunktion verwenden:
let rec words'<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<List<'a>> =
    let rec choose (xs: List<RegEx<'a>>): List<List<'a>> =
        match xs with
        | []    -> []
        | x::xs -> match isWord x with
                   | Some w -> w :: choose xs
                   | None   ->      choose xs
    reduce r n |> choose |> List.distinct

////e)
let rec generates<'a when 'a: equality>(r: RegEx<'a>) (word: List<'a>) (n: Nat): Bool =
    words r n |> List.contains word
