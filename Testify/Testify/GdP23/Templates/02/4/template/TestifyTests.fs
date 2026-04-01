namespace GdP23.S02.A4.Template


module TestifyTests =
    open Mini
    open Testify
    open Testify.AssertOperators
    open Testify.CheckOperators

    [< TestifyClass >]
    type TestifyTests () =
        let config = CheckConfig.defaultConfig.WithEndSize 10000

        // ------------------------------------------------------------------------
        // a)
        [< TestifyMethod; Timeout 1000 >]
        member _.``a) Beispiel 1`` () : unit =
            <@ Zahlen.avg3 2N 2N 2N @> =? 2N

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) Beispiel 2`` () : unit =
            <@ Zahlen.avg3 2N 5N 5N @> =? 4N

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) Beispiel 3`` () : unit =
            <@ Zahlen.avg3 5N 5N 2N @> =? 4N

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) Beispiel 4`` () : unit =
            <@ Zahlen.avg3 11N 6N 2N @> =? 6N

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) Avg3 Zufallstest`` () : unit =
            <@ fun (x: Nat, y: Nat, z: Nat) -> Zahlen.avg3 x y z @>
            |=>? (config, fun (x, y, z) ->
                abs (((int x) + (int y) + (int z)) / 3)
                |> Nat.Make)

        // ------------------------------------------------------------------------
        // b)
        [< TestifyMethod; Timeout 1000 >]
        member _.``b) Beispiel 1`` () : unit =
            <@ Zahlen.min3 1N 3N 2N @> =? 1N

        [< TestifyMethod; Timeout 1000 >]
        member _.``b) Beispiel 2`` () : unit =
            <@ Zahlen.min3 7N 7N 7N @> =? 7N

        [< TestifyMethod; Timeout 1000 >]
        member _.``b) Beispiel 3`` () : unit =
            <@ Zahlen.min3 815N 42N 4711N @> =? 42N

        [< TestifyMethod; Timeout 1000 >]
        member _.``b) Maximum Zufallstest`` () : unit =
            <@ fun (x: Nat, y: Nat, z: Nat) -> Zahlen.min3 x y z @>
            |=>? (config, fun (x, y, z) -> List.min [x; y; z])

        // ------------------------------------------------------------------------
        // c)
        [< TestifyMethod; Timeout 1000 >]
        member _.``c) Beispiel 1`` () : unit =
            <@ Zahlen.ceil10 1N @> =? 10N

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) Beispiel 2`` () : unit =
            <@ Zahlen.ceil10 0N @> =? 0N

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) Beispiel 3`` () : unit =
            <@ Zahlen.ceil10 11N @> =? 20N

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) Beispiel 4`` () : unit =
            <@ Zahlen.ceil10 20N @> =? 20N

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) ceil10 Zufallstest`` () : unit =
            <@ fun (x: Nat) -> Zahlen.ceil10 x @>
            |=>? (config, fun x ->
                if x % 10N = 0N then x
                else 10 + (10 * (int x / 10)) |> Nat.Make)
