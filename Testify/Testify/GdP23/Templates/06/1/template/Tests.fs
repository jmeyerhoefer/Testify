namespace GdP23.S06.A1.Template

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

    let config = {
        Config.QuickThrowOnFailure with
            EndSize = 1000
            MaxTest = 1000
        }

    let ex = [2N; 4N; 3N; 4N; 2N; 1N]

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) map Beispiele`` (): unit =
            test <@ Lists.map (fun x -> x + 1N) [] = [] @>
            test <@ Lists.map (fun x -> x + 1N) ex = [3N; 5N; 4N; 5N; 3N; 2N] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) map Zufallstest`` (): unit =
            Check.One(config, fun (f: Nat -> Nat) (xs: List<Nat>) ->
                Assert.AreEqual(
                    List.map f xs,
                    Lists.map f xs
                )
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) double mit map Beispiele`` (): unit =
            test <@ Lists.double [] = [] @>
            test <@ Lists.double ex = [4N; 8N; 6N; 8N; 4N; 2N] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) double mit map Zufallstest`` (): unit =
            Check.One(config, fun (xs: List<Nat>) ->
                Assert.AreEqual(
                    [for x in xs do yield x+x],
                    Lists.double xs
                )
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) firstComponents mit map Beispiele`` (): unit =
            test <@ Lists.firstComponents [] = [] @>
            test <@ Lists.firstComponents [(1N, "Harry"); (2N, "Lisa")] = [1N; 2N] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) firstComponents mit map Zufallstest`` (): unit =
            Check.One(config, fun (xs: List<Nat>) ->
                let ys = List.zip xs (List.map (fun x -> x < 10N) xs)
                Assert.AreEqual(
                    xs,
                    Lists.firstComponents ys
                )
            )


        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) collect Beispiele`` (): unit =
            test <@ Lists.collect (fun x -> [x]) [] = [] @>
            test <@ Lists.collect (fun x -> [x]) ex = ex @>
            test <@ Lists.collect (fun x -> [x; x+1N]) [1N] = [1N; 2N] @>
            test <@ Lists.collect (fun x -> [x; x+1N]) ex = [2N; 3N; 4N; 5N; 3N; 4N; 4N; 5N; 2N; 3N; 1N; 2N] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``b) collect Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (f: Nat -> List<Nat>) (xs: List<Nat>) ->
                Assert.AreEqual(
                    List.collect f xs,
                    Lists.collect f xs
                )
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) cloneElements mit collect Beispiele`` (): unit =
            test <@ Lists.cloneElements [] = [] @>
            test <@ Lists.cloneElements [1N; 2N; 3N] = [1N; 1N; 2N; 2N; 3N; 3N] @>
            test <@ Lists.cloneElements ex = [2N; 2N; 4N; 4N; 3N; 3N; 4N; 4N; 2N; 2N; 1N; 1N] @>

