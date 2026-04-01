module Peano
open Mini

////a)
let rec mult3 (n: Nat): Nat =
    if n = 0N then 0N
    else
        3N + mult3 (n - 1N)

////b)
let rec divide3 (x: Nat): Nat =
    if x = 0N then 0N
    else
        let m = divide3 (x - 1N)
        if m * 3N + 3N <= x
            then m + 1N
            else m
        
////end)
