module Solution.Counters


////a)
let mutable counter = 0N

let reset(): Unit =
    counter <- 0N

let increment(): Unit =
    counter <- counter + 1N

let get(): Nat =
    counter

////b)
type Counter = Ref<Nat>

let create(): Counter =
    ref 0N

let reset2(c: Counter): Unit =
    c := 0N

let increment2(c: Counter): Unit =
    c := !c + 1N

let get2(c: Counter): Nat =
    !c
////end)
