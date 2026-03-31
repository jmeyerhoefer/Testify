namespace GdP23.S03.A3.Template

module TestifyTests =
    open Mini
    open Testify
    open Testify.AssertOperators
    open Testify.CheckOperators

    [< TestifyClass >]
    type TestifyTests () =
        let config = CheckConfig.defaultConfig.WithEndSize 1000

        // ------------------------------------------------------------------------
        // a)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert a) quersumme Beispiele`` () : unit =
            <@ Leibniz.quersumme 123N @> =? 6N
            <@ Leibniz.quersumme 1234N @> =? 10N
            <@ Leibniz.quersumme 42N @> =? 6N
            <@ Leibniz.quersumme 105N @> =? 6N
            <@ Leibniz.quersumme 0N @> =? 0N
            <@ Leibniz.quersumme 4711N @> =? 13N

        [< TestifyMethod; Timeout 5000 >]
        member _.``#testify-check a) quersumme Zufallstest`` () : unit =
            let posNat =
                Arbitraries.from<Nat>
                |> Arbitraries.filter (fun n -> n > 0N)

            <@ fun (n: Nat) -> Leibniz.quersumme n @>
            ||=>? (Some config, Some posNat, None, fun n ->
                n.ToString ()
                |> Seq.fold (fun s c -> s + int (string c)) 0
                |> Nat.Make)

        // ------------------------------------------------------------------------
        // b)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert b) sortedDigits Beispiele`` () : unit =
            (?) <@ Leibniz.sortedDigits 0N @>
            (?) <@ Leibniz.sortedDigits 5N @>
            (?) <@ Leibniz.sortedDigits 159N @>
            (?) <@ Leibniz.sortedDigits 1111N @>
            (!?) <@ Leibniz.sortedDigits 42N @>
            (!?) <@ Leibniz.sortedDigits 543N @>
            (!?) <@ Leibniz.sortedDigits 1101N @>

        [< TestifyMethod; Timeout 5000 >]
        member _.``#testify-check b) sortedDigits Zufallstest`` () : unit =
            <@ fun (n: Nat) -> Leibniz.sortedDigits n @>
            |=>? (config, fun n ->
                n.ToString ()
                |> Seq.map (fun c -> int (string c))
                |> Seq.pairwise
                |> Seq.forall (fun (a, b) -> a <= b))


