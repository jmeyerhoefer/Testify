module Solution.Zahlen


//a)
let inOrder (a: Nat) (b: Nat) (c: Nat): Bool =
    a <= b && b <= c

//b)
let median3 (a: Nat) (b: Nat) (c: Nat): Nat =
    if a <= b then
        if b <= c then b else max a c
    else
        if b >= c then b else min a c

//c)
let takeMeTo (a: Bool): (Nat -> Nat -> Nat) =
    if a then (fun (x : Nat) (_ : Nat) -> x)
    else (fun (_ : Nat) (y : Nat) -> y)