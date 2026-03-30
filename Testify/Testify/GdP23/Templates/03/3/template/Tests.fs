namespace GdP23.S03.A3.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini

    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    [<TestClass>]
    type Tests() =
        let config =
            Config.QuickThrowOnFailure
                .WithEndSize(1000)
                .WithArbitrary [typeof<ArbitraryModifiers>]

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) quersumme Beispiele`` (): unit =
            test <@ Leibniz.quersumme 123N = 6N @>
            test <@ Leibniz.quersumme 1234N = 10N @>
            test <@ Leibniz.quersumme 42N = 6N @>
            test <@ Leibniz.quersumme 105N = 6N @>
            test <@ Leibniz.quersumme 0N = 0N @>
            test <@ Leibniz.quersumme 4711N = 13N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) quersumme Zufallstest`` (): unit =
            Check.One (config, fun (n: Nat) ->
                if n <> 0N then
                    let expected =
                        n.ToString() |> Seq.fold (fun s c -> s + int(string c)) 0 |> Nat.Make
                    Assert.AreEqual<Nat>(expected, Leibniz.quersumme n)
            )

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) sortedDigits Beispiele`` (): unit =
            test <@ Leibniz.sortedDigits 0N = true @>
            test <@ Leibniz.sortedDigits 5N = true @>
            test <@ Leibniz.sortedDigits 159N = true @>
            test <@ Leibniz.sortedDigits 1111N = true @>
            test <@ Leibniz.sortedDigits 42N = false @>
            test <@ Leibniz.sortedDigits 543N = false @>
            test <@ Leibniz.sortedDigits 1101N = false @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``b) sortedDigits Zufallstest`` (): unit =
            Check.One (config, fun (n: Nat) ->
                let expected =
                       n.ToString() |> Seq.map (fun c -> int(string c)) |> Seq.pairwise |> Seq.forall (fun (a, b) -> a <= b)
                Assert.AreEqual<bool>(expected, Leibniz.sortedDigits n)
            )


