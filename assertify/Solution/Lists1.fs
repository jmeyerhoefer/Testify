module Solution.Lists1


//ex)
let ex = [2N; 4N; 3N; 4N; 2N; 1N]

//a)
let rec plusOne (xs: List<Nat>): List<Nat> =
    match xs with
    | []    -> []
    | x::ys -> (x + 1N)::(plusOne ys)

//b)
let rec filter<'a> (p: 'a -> Bool) (xs: List<'a>): List<'a> =
    match xs with
    | []    -> []
    | x::xs ->
        if p x
          then x::(filter p xs)
          else filter p xs

//c)
let rec concat<'a> (xs: List<'a>) (ys: List<'a>): List<'a> =
    match xs with
    | [] -> ys
    | x::zs -> x::(concat zs ys)

//d)
// Lösung mit Laufzeit quadratisch in der Länge von xs
let rec mirror<'a> (xs: List<'a>): List<'a> =
    match xs with
    | [] -> []
    | x::ys -> concat (mirror ys) [x]

// Effizientere Lösung (lineare Laufzeit)
let mirror'<'a> (xs: List<'a>): List<'a> =
    // Hilfsfunktion berechnet concat (mirror xs) zs
    let rec mirrorConcat (xs: List<'a>) (zs: List<'a>): List<'a> =
        match xs with
        | [] -> zs
        | x::ys -> mirrorConcat ys (x::zs)
    mirrorConcat xs []

//e)
let rec sum (xs: List<Nat>): Nat =
    match xs with
    | [] -> 0N
    | x::ys -> x + sum ys