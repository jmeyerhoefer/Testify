module Solution.Find


open Types.FindTypes


////a)
let rec tryFindLast<'a> (pred: 'a -> Bool) (xs: List<'a>): Option<'a> =
    match xs with
    | [] -> None
    | y::ys ->
        match tryFindLast pred ys with
        | None -> if pred y then Some y else None
        | Some z -> Some z

////b)
let rec findLast<'a> (pred: 'a -> Bool) (xs: List<'a>): 'a =
    match xs with
    | [] -> raise NotFound
    | y::ys ->
        try findLast pred ys with
        | NotFound -> if pred y then y else raise NotFound

////c)
let tryFindLast2<'a> (pred: 'a -> Bool) (xs: List<'a>): Option<'a> =
    let mutable last: Option<'a> = None
    for x in xs do
        if pred x then last <- Some x
    last

////d)
let findLast2<'a> (pred: 'a -> Bool) (xs: List<'a>): 'a =
    let mutable last: Option<'a> = None
    for x in xs do
        if pred x then last <- Some x
    match last with
    | None -> raise NotFound
    | Some x -> x

////end)
