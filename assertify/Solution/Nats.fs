module Nats


open Types.NatsTypes


//ex)
let ex = Cons (2N, Cons (4N, Cons (3N, Cons(4N, Cons(2N, Cons (1N, Nil))))))

//a)
let rec trace (f: Nat -> Nat) (start: Nat): Nats =
    Cons (start,
        if start = 0N || start = 1N then Nil
        else trace f (f start)
    )

//b)
let rec isSortedBy (lessEqual: Nat * Nat -> Bool ) (xs: Nats): Bool =
    match xs with
    | Nil -> true
    | Cons (_, Nil) -> true
    | Cons (x, Cons (y, xs)) -> lessEqual (x,y) && isSortedBy lessEqual (Cons (y, xs))

//c)
let rec exists (p: Nat -> Bool) (xs: Nats): Bool =
    match xs with
    | Nil -> false
    | Cons (x, xs) -> p x || exists p xs