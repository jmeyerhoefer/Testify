namespace GdP23.S06.A2.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini
    open TreeTypes

    [<StructuredFormatDisplay("{ToString}")>]
    type TestInput =
        | TI of Tree<Nat> * Tree<Nat> * Nat * Nat * Nat * List<Nat> // tree, mirror, countNodes, countLeaves, height, inorder Elements
        member this.ToString =
            let (TI (t, _, _, _, _, _)) = this
            sprintf "%A" t

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

        static member TestInput() =
            Arb.fromGen (
                let rec generator lo hi size =
                    gen {
                        if size = 0 || lo > hi then return TI (Leaf, Leaf, 0N, 1N, 0N, [])
                        else
                            let! sizeL = Gen.choose(0, size/2)
                            let! sizeR = Gen.choose(0, size/2)
                            let! x = Gen.choose(lo, hi)
                            let! TI (tl, mtl, cnl, cll, hl, iol) = generator lo (x - 1) sizeL
                            let! TI (tr, mtr, cnr, clr, hr, ior) = generator (x + 1) hi sizeR
                            return TI (Node (tl, Nat.Make x, tr), Node (mtr, Nat.Make x, mtl), 1N + cnl + cnr, cll + clr, 1N + max hl hr, iol @ [Nat.Make x] @ ior)
                    }
                Gen.sized (generator 0 50)
            )

    let config = {
        Config.QuickThrowOnFailure with
            MaxTest = 1000
        }

    let ex = Node (Node (Leaf, 1N, (Node (Leaf, 2N, Leaf))), 3N, (Node (Leaf, 4N, Leaf)))

    let rec inorder<'a> (t: Tree<'a>): List<'a> =
        match t with
        | Leaf -> []
        | Node (l, x, r) -> inorder l @ [x] @ inorder r

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) countLeaves Beispiele`` (): unit =
            test <@ Tree.countLeaves Leaf = 1N @>
            test <@ Tree.countLeaves ex = 5N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) countLeaves Zufallstest`` (): unit =
            Check.One(config, fun (TI (t, _, _, n, _, _)) ->
                Assert.AreEqual(
                    n,
                    Tree.countLeaves t
                )
            )

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) height Beispiele`` (): unit =
            test <@ Tree.height Leaf = 0N @>
            test <@ Tree.height ex = 3N @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``b) height Zufallstest`` (): unit =
            Check.One(config, fun (TI (t, _, _, _, h, _)) ->
                Assert.AreEqual(
                    h,
                    Tree.height t
                )
            )

        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) map Beispiele`` (): unit =
            test <@ Tree.map (fun x -> x * 2N) Leaf = Leaf @>
            test <@ Tree.map (fun x -> x * 2N) ex = Node (Node (Leaf, 2N, (Node (Leaf, 4N, Leaf))), 6N, (Node (Leaf, 8N, Leaf))) @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``c) map Zufallstest`` (): unit =
            Check.One(config, fun (TI (t, _, _, _, _, _)) (f: Nat -> Nat) ->
                Assert.AreEqual(
                    List.map f (inorder t),
                    inorder (Tree.map f t)
                )
            )

