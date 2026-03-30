namespace GdP23.S03.A1.Template

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
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) add Beispiele`` (): unit =
            test <@ Entwurfsmuster.add 0N = 0N @>
            test <@ Entwurfsmuster.add 2N = 3N @>
            test <@ Entwurfsmuster.add 3N = 6N @>
            test <@ Entwurfsmuster.add 10N = 55N @>

        [<TestMethod>] [<Timeout(30000)>]
        member this.``b) add Zufallstest`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 1000}, fun (x: Nat) ->
                Assert.AreEqual([1..(int x)] |> List.sum |> Nat.Make, Entwurfsmuster.add x)
            )

        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) mod5 Beispiele`` (): unit =
            test <@ Entwurfsmuster.mod5 0N = 0N @>
            test <@ Entwurfsmuster.mod5 1N = 1N @>
            test <@ Entwurfsmuster.mod5 2N = 2N @>
            test <@ Entwurfsmuster.mod5 3N = 3N @>
            test <@ Entwurfsmuster.mod5 4N = 4N @>
            test <@ Entwurfsmuster.mod5 5N = 0N @>
            test <@ Entwurfsmuster.mod5 6N = 1N @>
            test <@ Entwurfsmuster.mod5 7N = 2N @>
            test <@ Entwurfsmuster.mod5 19N = 4N @>
            test <@ Entwurfsmuster.mod5 20N = 0N @>
            test <@ Entwurfsmuster.mod5 21N = 1N @>
            test <@ Entwurfsmuster.mod5 22N = 2N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``c) mod5 Zufallstest`` (): unit =
            Check.One ({Config.QuickThrowOnFailure with EndSize = 1000}, fun (n: Nat) ->
                Assert.AreEqual(
                    n % 5N,
                    Entwurfsmuster.mod5 n
                )
            )

        // ------------------------------------------------------------------------
        // d)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) mult42 Beispiele`` (): unit =
            test <@ Entwurfsmuster.mult42 0N = 0N @>
            test <@ Entwurfsmuster.mult42 1N = 42N @>
            test <@ Entwurfsmuster.mult42 2N = 84N @>
            test <@ Entwurfsmuster.mult42 5N = 210N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``b) mult42 Zufallstest`` (): unit =
            Check.One ({Config.QuickThrowOnFailure with EndSize = 1000}, fun (n: Nat) ->
                Assert.AreEqual(n * 42N, Entwurfsmuster.mult42 n)
            )

            
        // ------------------------------------------------------------------------
        // e)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) count5 Beispiele`` (): unit =
            test <@ Entwurfsmuster.count5 3N = 0N @>
            test <@ Entwurfsmuster.count5 76567N = 1N @>
            test <@ Entwurfsmuster.count5 1234N = 0N @>
            test <@ Entwurfsmuster.count5 445566N = 2N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``d) count5 Zufallstest`` (): unit =
            Check.One ({Config.QuickThrowOnFailure with EndSize = 1000}, fun (n: Nat) ->
                let char2int (n: Char) = int n - int '0'
                Assert.AreEqual(
                    n |> string |> Seq.toList |> List.map char2int |> List.filter (fun x -> x = 5) |> List.length |> Nat.Make,
                    Entwurfsmuster.count5 n
                )
            )

