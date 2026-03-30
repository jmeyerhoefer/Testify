namespace GdP23.S02.A4.Template

module Tests =
    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open FsCheck.FSharp
    open Mini

    // Machen Sie sich bitte aktuell nicht die Mühe, das hier verstehen zu wollen.
    // Das können wir zu einem späteren Zeitpunkt versuchen.

    type ArbitraryModifiers =
        static member Nat() =
            ArbMap.defaults
            |> ArbMap.arbitrary<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    [<TestClass>]
    type Tests() =
        let config =
            Config.QuickThrowOnFailure
                .WithEndSize(10000)
                .WithArbitrary [ typeof<ArbitraryModifiers> ]

        // ------------------------------------------------------------------------
        // a)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Beispiel 1`` (): unit =
            Assert.AreEqual<Nat> (2N, Zahlen.avg3 2N 2N 2N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Beispiel 2`` (): unit =
            Assert.AreEqual<Nat> (4N, Zahlen.avg3 2N 5N 5N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Beispiel 3`` (): unit =
            Assert.AreEqual<Nat> (4N, Zahlen.avg3 5N 5N 2N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Beispiel 4`` (): unit =
            Assert.AreEqual<Nat> (6N, Zahlen.avg3 11N 6N 2N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Avg3 Zufallstest`` (): unit =
            Check.One(config, fun (x: Nat) (y: Nat) (z: Nat) ->
                let result = Zahlen.avg3 x y z
                let expected = abs (((int x) + (int y) + (int z)) / 3) |> Nat.Make
                Assert.AreEqual<Nat> (expected, result)
            )

        // ------------------------------------------------------------------------
        // b)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) Beispiel 1`` (): unit =
            Assert.AreEqual<Nat>(1N, Zahlen.min3 1N 3N 2N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) Beispiel 2`` (): unit =
            Assert.AreEqual<Nat>(7N, Zahlen.min3 7N 7N 7N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) Beispiel 3`` (): unit =
            Assert.AreEqual<Nat>(42N, Zahlen.min3 815N 42N 4711N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) Maximum Zufallstest`` (): unit =
            Check.One(config, fun (x: Nat) (y: Nat) (z: Nat) ->
                let result = Zahlen.min3 x y z
                let expected = List.min [x; y; z]
                Assert.AreEqual<Nat>(expected, result)
            )

        // ------------------------------------------------------------------------
        // c)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Beispiel 1`` (): unit =
            Assert.AreEqual<Nat>(10N, Zahlen.ceil10 1N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Beispiel 2`` (): unit =
            Assert.AreEqual<Nat>(0N, Zahlen.ceil10 0N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Beispiel 3`` (): unit =
            Assert.AreEqual<Nat>(20N, Zahlen.ceil10 11N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Beispiel 4`` (): unit =
            Assert.AreEqual<Nat>(20N, Zahlen.ceil10 20N)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) ceil10 Zufallstest`` (): unit =
            Check.One(config, fun (x: Nat) ->
                let result = Zahlen.ceil10 x
                let expected = if x % 10N = 0N then x else 10 + (10 * (int x / 10)) |> Nat.Make
                Assert.AreEqual<Nat>(expected, result)
            )
