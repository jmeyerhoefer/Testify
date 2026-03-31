namespace GdP23.S03.A1.Template


module TestifyTests =
    open Mini
    open Testify
    open Testify.AssertOperators
    open Testify.CheckOperators

    [< TestifyClass>]
    type TestifyTests () =
        let config = CheckConfig.defaultConfig.WithEndSize 1000

        // ------------------------------------------------------------------------
        // b)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert b) add Beispiele`` () : unit =
            <@ Entwurfsmuster.add 0N @> =? 0N
            <@ Entwurfsmuster.add 2N @> =? 3N
            <@ Entwurfsmuster.add 3N @> =? 6N
            <@ Entwurfsmuster.add 10N @> =? 55N

        [< TestifyMethod; Timeout 30000 >]
        member _.``#testify-check b) add Zufallstest`` () : unit =
            <@ fun (x: Nat) -> Entwurfsmuster.add x @>
            |=>? (config, fun x -> [ 1 .. int x ] |> List.sum |> Nat.Make)

        // ------------------------------------------------------------------------
        // c)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert c) mod5 Beispiele`` () : unit =
            <@ Entwurfsmuster.mod5 0N @> =? 0N
            <@ Entwurfsmuster.mod5 1N @> =? 1N
            <@ Entwurfsmuster.mod5 2N @> =? 2N
            <@ Entwurfsmuster.mod5 3N @> =? 3N
            <@ Entwurfsmuster.mod5 4N @> =? 4N
            <@ Entwurfsmuster.mod5 5N @> =? 0N
            <@ Entwurfsmuster.mod5 6N @> =? 1N
            <@ Entwurfsmuster.mod5 7N @> =? 2N
            <@ Entwurfsmuster.mod5 19N @> =? 4N
            <@ Entwurfsmuster.mod5 20N @> =? 0N
            <@ Entwurfsmuster.mod5 21N @> =? 1N
            <@ Entwurfsmuster.mod5 22N @> =? 2N

        [< TestifyMethod; Timeout 5000 >]
        member _.``#testify-check c) mod5 Zufallstest`` () : unit =
            <@ fun (n: Nat) -> Entwurfsmuster.mod5 n @>
            |=>? (config, fun n -> n % 5N)

        // ------------------------------------------------------------------------
        // d)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert b) mult42 Beispiele`` () : unit =
            <@ Entwurfsmuster.mult42 0N @> =? 0N
            <@ Entwurfsmuster.mult42 1N @> =? 42N
            <@ Entwurfsmuster.mult42 2N @> =? 84N
            <@ Entwurfsmuster.mult42 5N @> =? 210N

        [< TestifyMethod; Timeout 5000 >]
        member _.``#testify-check b) mult42 Zufallstest`` () : unit =
            <@ fun (n: Nat) -> Entwurfsmuster.mult42 n @>
            |=>? (config, fun n -> n * 42N)

        // ------------------------------------------------------------------------
        // e)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert d) count5 Beispiele`` () : unit =
            <@ Entwurfsmuster.count5 3N @> =? 0N
            <@ Entwurfsmuster.count5 76567N @> =? 1N
            <@ Entwurfsmuster.count5 1234N @> =? 0N
            <@ Entwurfsmuster.count5 445566N @> =? 2N

        [< TestifyMethod; Timeout 5000 >]
        member _.``#testify-check d) count5 Zufallstest`` () : unit =
            <@ fun (n: Nat) -> Entwurfsmuster.count5 n @>
            |=>? (config, fun n ->
                let char2int (n: Char) : int =
                    int n - int '0'

                n
                |> string
                |> Seq.toList
                |> List.map char2int
                |> List.filter (fun x -> x = 5)
                |> List.length
                |> Nat.Make)

