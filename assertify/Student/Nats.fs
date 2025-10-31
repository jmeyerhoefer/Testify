module Student.Nats


open Types.NatsTypes


//ex)
let ex = Cons (2N, Cons (4N, Cons (3N, Cons(4N, Cons(2N, Cons (1N, Nil))))))

//a)
let rec trace (f: Nat -> Nat) (start: Nat): Nats =
    failwith "TODO"

//b)
let rec isSortedBy (lessEqual: Nat * Nat -> Bool ) (xs: Nats): Bool =
    failwith "TODO"

//c)
let rec exists (p: Nat -> Bool) (xs: Nats): Bool =
    failwith "TODO"
