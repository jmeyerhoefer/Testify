module Tests.TreeTests


open Assertify.Types
open Assertify.Types.GenBuilder
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.TreeTypes


[<StructuredFormatDisplay("{ToString}")>]
type TestInput =
    | TI of Tree<Nat> * Tree<Nat> * Nat * Nat * Nat * List<Nat> // tree, mirror, countNodes, countLeaves, height, inorder Elements

    member this.ToString: string =
        let (TI (t, _, _, _, _, _)) = this
        $"%A{t}"


type ArbitraryModifiers =
    inherit NatModifier

    static member TestInput (): Arbitrary<TestInput> =
        let rec generator (lo: int) (hi: int) (size: int): Gen<TestInput> =
            gen {
                if size = 0 || lo > hi then return TI (Leaf, Leaf, 0N, 1N, 0N, [])
                else
                    let! sizeL = FsCheck.FSharp.Gen.choose(0, size/2)
                    let! sizeR = FsCheck.FSharp.Gen.choose(0, size/2)
                    let! x = FsCheck.FSharp.Gen.choose(lo, hi)
                    let! TI (tl, mtl, cnl, cll, hl, iol) = generator lo (x - 1) sizeL
                    let! TI (tr, mtr, cnr, clr, hr, ior) = generator (x + 1) hi sizeR
                    return TI (Node (tl, Nat.Make x, tr), Node (mtr, Nat.Make x, mtl), 1N + cnl + cnr, cll + clr, 1N + max hl hr, iol @ [Nat.Make x] @ ior)
            }
        FsCheck.FSharp.Gen.sized (generator 0 50)
        |> FsCheck.FSharp.Arb.fromGen


let ex = Node (Node (Leaf, 1N, (Node (Leaf, 2N, Leaf))), 3N, (Node (Leaf, 4N, Leaf)))


let rec inorder<'a> (t: Tree<'a>): List<'a> =
    match t with
    | Leaf -> []
    | Node (l, x, r) -> inorder l @ [x] @ inorder r


[<TestClass>]
type TreeTests () =
    let config: Config =
        Config
            .QuickThrowOnFailure
            .WithArbitrary [typeof<ArbitraryModifiers>]

    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``a) countLeaves Beispiele`` (): unit =
        (?) <@ Student.Tree.countLeaves Leaf = 1N @>
        (?) <@ Student.Tree.countLeaves ex = 5N @>

    [<TestMethod; Timeout 5000>]
    member _.``a) countLeaves Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (TI (t, _, _, n, _, _)) -> Student.Tree.countLeaves t = n @>,
            config
        )

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) height Beispiele`` (): unit =
        (?) <@ Student.Tree.height Leaf = 0N @>
        (?) <@ Student.Tree.height ex = 3N @>

    [<TestMethod; Timeout 5000>]
    member _.``b) height Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (TI (t, _, _, _, h, _)) -> Student.Tree.height t = h @>,
            config
        )

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) map Beispiele`` (): unit =
        (?) <@ Student.Tree.map (fun x -> x * 2N) Leaf = Leaf @>
        (?) <@ Student.Tree.map (fun x -> x * 2N) ex = Node (Node (Leaf, 2N, (Node (Leaf, 4N, Leaf))), 6N, (Node (Leaf, 8N, Leaf))) @>

    [<TestMethod; Timeout 5000>]
    member _.``c) map Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (TI (t, _, _, _, _, _)) (f: Nat -> Nat) -> inorder (Student.Tree.map f t) = List.map f (inorder t) @>,
            config
        )
