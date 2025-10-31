module Solution.Peano


//a)
let rec iterate (f: Nat -> Nat) (n: Nat): Nat -> Nat =
    if n = 0N then (fun n -> n)
    else (fun m -> f ((iterate f (n - 1N)) m))

//b)
let rec lt (n: Nat) (m: Nat): Bool =
    if m = 0N then false
    else
        let pre = (m - 1N)
        let nminlt = lt n pre
        (n = pre) || nminlt