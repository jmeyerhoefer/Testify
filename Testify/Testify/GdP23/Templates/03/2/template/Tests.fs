namespace GdP23.S03.A2.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) mult3 Beispiele`` (): unit =
            test <@ Peano.mult3 0N = 0N @>
            test <@ Peano.mult3 1N = 3N @>
            test <@ Peano.mult3 2N = 6N @>
            test <@ Peano.mult3 5N = 15N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) mult3 Zufallstest`` (): unit =
            Check.One ({Config.QuickThrowOnFailure with EndSize = 100}, fun (n: Nat) ->
                Assert.AreEqual(
                    n * 3N,
                    Peano.mult3 n
                )
            )

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) divide3 Beispiele`` (): unit =
            test <@ Peano.divide3 0N = 0N @>
            test <@ Peano.divide3 1N = 0N @>
            test <@ Peano.divide3 2N = 0N @>
            test <@ Peano.divide3 3N = 1N @>
            test <@ Peano.divide3 6N = 2N @>
            test <@ Peano.divide3 8N = 2N @>
            test <@ Peano.divide3 9N = 3N @>
            test <@ Peano.divide3 10N = 3N @>
            test <@ Peano.divide3 11N = 3N @>

        [<TestMethod>] [<Timeout(30000)>]
        member this.``b) divide3`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 100}, fun (x: Nat) ->
                Assert.AreEqual(Nat.Make ((int x) / 3), Peano.divide3 x)
            )


