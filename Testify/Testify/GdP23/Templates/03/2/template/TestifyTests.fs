namespace GdP23.S03.A2.Template

module TestifyTests =
    open Mini
    open Testify
    open Testify.AssertOperators
    open Testify.CheckOperators

    [< TestifyClass >]
    type TestifyTests () =
        let config = CheckConfig.defaultConfig.WithEndSize 100

        // ------------------------------------------------------------------------
        // a)

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) mult3 Beispiele`` () : unit =
            <@ Peano.mult3 0N @> =? 0N
            <@ Peano.mult3 1N @> =? 3N
            <@ Peano.mult3 2N @> =? 6N
            <@ Peano.mult3 5N @> =? 15N

        [< TestifyMethod; Timeout 5000 >]
        member _.``a) mult3 Zufallstest`` () : unit =
            <@ fun (n: Nat) -> Peano.mult3 n @>
            |=>? (config, fun n -> n * 3N)

        // ------------------------------------------------------------------------
        // b)

        [< TestifyMethod; Timeout 1000 >]
        member _.``b) divide3 Beispiele`` () : unit =
            <@ Peano.divide3 0N @> =? 0N
            <@ Peano.divide3 1N @> =? 0N
            <@ Peano.divide3 2N @> =? 0N
            <@ Peano.divide3 3N @> =? 1N
            <@ Peano.divide3 6N @> =? 2N
            <@ Peano.divide3 8N @> =? 2N
            <@ Peano.divide3 9N @> =? 3N
            <@ Peano.divide3 10N @> =? 3N
            <@ Peano.divide3 11N @> =? 3N

        [< TestifyMethod; Timeout 30000 >]
        member _.``b) divide3`` () : unit =
            <@ fun (x: Nat) -> Peano.divide3 x @>
            |=>? (config, fun x -> Nat.Make ((int x) / 3))


