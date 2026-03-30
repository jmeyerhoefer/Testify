namespace GdP23.S04.A3.Template

module Nats =
    open Mini
    open NatsType

    ////ex)
    let ex = Cons (2N, Cons (4N, Cons (3N, Cons(4N, Cons(2N, Cons (1N, Nil))))))

    ////a)
    let rec double (xs: Nats): Nats =
        failwith "TODO"

    ////b)
    let rec isSorted (xs: Nats): Bool =
        failwith "TODO"

    ////c)
    let rec filter (p: Nat -> Bool) (xs: Nats): Nats =
        failwith "TODO"

    ////end)

