module Solution.Lists2


//aus der Präsenzaufgabe
let rec concat<'a> (xs: List<'a>) (ys: List<'a>): List<'a> =
    match xs with
    | [] -> ys
    | x::zs -> x::(concat zs ys)

// Beispiel vom Übungsblatt
let ex = [2N; 4N; 3N; 4N; 2N; 1N]

//a)
let rec minAndMax<'a when 'a: comparison>(xs: List<'a>): Option<'a * 'a> =
    match xs with
    | [] -> None
    | x::xs -> 
        match minAndMax xs with
        | None -> Some(x, x)
        | Some(low, high) -> Some(min x low, max x high)

//b)
let rec map<'a, 'b> (f: 'a -> 'b) (xs: List<'a>): List<'b> =
    match xs with
    | [] -> []
    | x::xs -> f x :: map f xs

//c)
let rec duplicate<'a> (xs: List<'a>): List<'a> =
    match xs with
    | [] -> []
    | x::xs -> x :: x :: duplicate xs

//d)
// Hilfsfunktion um eine Liste von Listen zu verketten
let rec concatAll<'a> (xs: List<List<'a>>) : List<'a> =
    match xs with
    | [] -> []
    | x :: xs -> concat x (concatAll xs)

let rec collect<'a, 'b> (f: 'a -> List<'b>) (xs: List<'a>): List<'b> =
    concatAll (map f xs)

//e)
let rec intersperse<'a> (sep: 'a) (xs: List<'a>): List<'a> =
    match xs with
    | [] -> []
    | one & [_] -> one
    | x :: xs -> x :: sep :: intersperse sep xs

//f)
let rec runs<'a when 'a: comparison> (xs: List<'a>) : List<List<'a>> =
    match xs with
    | [] -> []
    | x :: xs ->
        match runs xs with
        | rest & ((current & (y :: _)) :: ys) ->
            if x <= y then (x :: current) :: ys else [ x ] :: rest
        | rest -> [ x ] :: rest

// Alternative ohne konjunktive (&) Patterns
// NB: Die Bezeichner mit & machen deutlich welche Unterausdrücke als solche
// tatsächlich verwendet werden, und erlauben es, sprechende Bezeichner
// einzuführen

let rec runs'<'a when 'a: comparison> (xs: List<'a>) : List<List<'a>> =
    match xs with
    | [] -> []
    | x :: xs ->
        match runs' xs with
        | (y :: ys) :: yss ->
            if x <= y then (x :: (y :: ys)) :: yss else [ x ] :: (y :: ys) :: yss
        | rest -> [ x ] :: rest

// Eine dritte Alternative mit `when`:

let rec runs''<'a when 'a: comparison> (xs: List<'a>) : List<List<'a>> =
    match xs with
    | [] -> []
    | x :: xs ->
        match runs'' xs with
        | (y :: ys) :: yss when x <= y -> (x :: (y :: ys)) :: yss
        | rest -> [ x ] :: rest
