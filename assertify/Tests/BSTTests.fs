module Tests.BSTTests


open Assertify
open System
open Types.BSTTypes


type ValidBST<'a> = Valid of BST<'a> * List<'a> * Nat // (BST, BST als Liste, Höhe des BSTs)
type InvalidBST<'a> = Invalid of BST<'a>

// need this for type inference purposes in `natRange`
let makeNat (n: int): Nat = Nat.Make n
// need this for type inference purposes in `AssertEquals`
let empty<'a, 'b>: BSTreeish<'a,'b> = Emptyish

[<StructuralEquality;StructuralComparison>]
type Extended<'a when 'a: comparison> =
    | NegInfty
    | Just of 'a
    | PosInfty

let natRange (lo: Nat) (hi: Nat): Gen<Nat> = FsCheck.FSharp.Gen.map makeNat (FsCheck.FSharp.Gen.choose (int lo , int hi))

let genRange: Extended<Nat> * Extended<Nat> -> Gen<Nat> =
    let defaultGen = FsCheck.FSharp.ArbMap.defaults |> FsCheck.FSharp.ArbMap.generate
    function
    | Just lo , Just hi  -> natRange lo hi
    | NegInfty, Just hi  -> natRange 0N hi
    | NegInfty, PosInfty -> defaultGen
    | Just lo, PosInfty  -> FsCheck.FSharp.Gen.map (fun x -> x + lo) defaultGen
    | _ -> failwith "purposely no pattern for(_,-∞)"


let isBST<'a when 'a: comparison> (root: BST<'a>): Bool =
    let rec bounds (root: BST<'a>) : Option<Range<'a>> =
        match root with
        | Empty -> Some(EmptyR)
        | Node(left, p, right) ->
        match (bounds left, bounds right) with
        | Some EmptyR      , Some EmptyR       -> Some(Twixt(p, p))
        | Some EmptyR      , Some(Twixt(c, d)) -> if p <= c then Some(Twixt(p, d)) else None
        | Some(Twixt(a, b)), Some EmptyR       -> if b <= p then Some(Twixt(a, p)) else None
        | Some(Twixt(a, b)), Some(Twixt(c, d)) ->
            if b <= p && p <= c then Some(Twixt(a, d)) else None
        | _                                    -> None
    in match bounds root with
        | None -> false
        | _    -> true


let rec deleteMin<'a> (root: BST<'a>): Option<'a * BST<'a>> =
    match root with
    | Empty -> None
    | Node(l, x, r) ->
        match deleteMin l with
        | None -> Some (x, r) // l = Empty
        | Some(a, lMinusA) -> Some(a, Node(lMinusA, x, r))

let rec tracer (grow: 'b -> Option<'a * 'b>) (seed: 'b): List<'a> =
    match grow seed with
    | None -> []
    | Some(a, seed') -> a :: tracer grow seed'

let rec toList<'a when 'a: comparison> (root: BST<'a>): List<'a> =
    tracer deleteMin root

let rec genAnyTree (size: int): Gen<BST<'a>> =
    if size <= 0 then FsCheck.FSharp.Gen.constant Empty
    else
        gen {
            let! root = FsCheck.FSharp.ArbMap.defaults |> FsCheck.FSharp.ArbMap.generate
            let! sizeL = FsCheck.FSharp.Gen.choose(0, size - 1)
            let! sizeR = FsCheck.FSharp.Gen.choose(0, size - 1)
            let! treeL = genAnyTree sizeL
            let! treeR = genAnyTree sizeR
            return Node (treeL, root, treeR)
        }

// Generator für gültigen BST mit gegebener Tiefe und Range die die Werte annehmen können
let rec generateValidBST (size: int) (range: Extended<Nat> * Extended<Nat>): Gen<BST<Nat> * Nat list * Nat> =
    if size <= 0 then FsCheck.FSharp.Gen.constant (Empty, [], 0N)
    else
        gen {
            let lo, hi = range
            let! p = genRange(range)
            let! sizeL = FsCheck.FSharp.Gen.choose(0, size - 1)
            let! sizeR = FsCheck.FSharp.Gen.choose(0, size - 1)
            let! treeL, listL, heightL = generateValidBST sizeL (lo, (Just p))
            let! treeR, listR, heightR = generateValidBST sizeR ((Just p), hi)
            return (
                Node (treeL, p, treeR),
                listL @ [p] @ listR,
                1N + max heightL heightR
            )
        }

type ArbitraryModifiers =
    inherit NatModifier

    static member ValidBST (): Arbitrary<ValidBST<Nat>> =
        FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.sized <| fun size ->
            gen {
                let! result = generateValidBST size (NegInfty, PosInfty)
                return Valid result
            }

    static member InvalidBST (): Arbitrary<InvalidBST<Nat>> =
        FsCheck.FSharp.Arb.fromGen <<
        FsCheck.FSharp.Gen.map Invalid <<
        FsCheck.FSharp.Gen.filter (fun p -> not (isBST p)) <<
        FsCheck.FSharp.Gen.sized <| genAnyTree


let mutable counter: int = 0


[<CustomEquality; CustomComparison>]
type ComparisonCount<'a when 'a :> IComparable<'a> and 'a: equality> =
    CC of 'a
    with
    static member wrap (value: 'a): ComparisonCount<'a> = CC value
    static member unwrap (x: ComparisonCount<'a>): 'a = let (CC me) = x in me
    interface IComparable<ComparisonCount<'a>> with
        member this.CompareTo (CC other) =
            counter <- counter + 1
            (ComparisonCount.unwrap this :> IComparable<'a>).CompareTo other
    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null -> 1
            | :? ComparisonCount<'a> as other -> (this :> IComparable<_>).CompareTo other
            | _ -> invalidArg "obj" "not a ComparisonCount<'a>"
    override this.Equals obj =
        match obj with
        | :? ComparisonCount<'a> as other -> ComparisonCount.unwrap this = ComparisonCount.unwrap other
        | _ -> false
    override this.GetHashCode() = hash (ComparisonCount.unwrap this)


[<TestClass>]
type BSTTests () =
    let config: Config =
        Config
            .QuickThrowOnFailure
            .WithArbitrary([typeof<ArbitraryModifiers>])

    let ex1 = Node(Node(Empty,1N,Empty), 2N, Node(Empty,4N,Empty))
    let ex2 = Node(Node(Empty,2N,Empty), 3N, Node(Empty,5N,Empty))
    let ex3 = Node(Node(Empty,1N,Empty), 1N, Empty)
    let inv1 = Node(Node(Empty,3N,Empty), 2N, Empty)
    let inv2 = Node(Node(Node(Empty,4N,Empty), 2N, Empty), 3N, Empty)

    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout(1000)>]
    member _.``size Beispiel 1`` (): unit =
        (?) <@ Student.BSTs.size Empty = 0N @>

    [<TestMethod; Timeout(1000)>]
    member _.``size Beispiel 2`` (): unit =
        (?) <@ Student.BSTs.size ex1 = 3N @>

    [<TestMethod; Timeout(1000)>]
    member _.``size Beispiel 3`` (): unit =
        (?) <@ Student.BSTs.size ex3 = 2N @>

    [<TestMethod; Timeout(10000)>]
    member _.``size Zufall`` (): unit =
        Assertify.Check (
            <@ fun (Valid (bst, list, _): ValidBST<Nat>) -> Student.BSTs.size bst = Nat.Make(list.Length) @>,
            config.WithMaxTest(100).WithEndSize(5)
        )

    [<TestMethod; Timeout(1000)>]
    member _.``height Beispiel 1`` (): unit =
        (?) <@ Student.BSTs.height Empty = 0N @>

    [<TestMethod; Timeout(1000)>]
    member _.``height Beispiel 2`` (): unit =
        (?) <@ Student.BSTs.height ex1 = 2N @>

    [<TestMethod; Timeout(1000)>]
    member _.``height Beispiel 3`` (): unit =
        (?) <@ Student.BSTs.height ex3 = 2N @>

    [<TestMethod; Timeout(10000)>]
    member _.``height Zufall`` (): unit =
        Assertify.Check (
            <@ fun (Valid (bst, _, h): ValidBST<Nat>) -> Student.BSTs.height bst = h @>,
            config.WithMaxTest(100).WithEndSize(5)
        )


    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 1`` (): unit =
        (?) <@ Student.BSTs.isBST<Nat> Empty = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 2`` (): unit =
        (?) <@ Student.BSTs.isBST ex1 = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 3`` (): unit =
        (?) <@ Student.BSTs.isBST ex2 = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 4`` (): unit =
        (?) <@ Student.BSTs.isBST ex3 = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 5`` (): unit =
        (?) <@ Student.BSTs.isBST inv1 = false @>

    [<TestMethod; Timeout(1000)>]
    member _.``isBST Beispiel 6`` (): unit =
        (?) <@ Student.BSTs.isBST inv2 = false @>

    [<TestMethod; Timeout(60000)>]
    member _.``isBST Zufall Gültig`` (): unit =
        Assertify.Check (
            <@ fun (Valid (bst, _, _): ValidBST<Nat>) -> Student.BSTs.isBST bst = true @>,
            config.WithMaxTest(1000).WithEndSize(5)
        )

    [<TestMethod; Timeout(60000)>]
    member _.``isBST Zufall Ungültig`` (): unit =
        Assertify.Check (
            <@ fun (Invalid bst: InvalidBST<Nat>) -> Student.BSTs.isBST bst = false @>,
            config.WithMaxTest(1000).WithEndSize(5)
        )


    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout(1000)>]
    member _.``deleteMin leer`` (): unit =
        (?) <@ Student.BSTs.deleteMin Empty = None @>

    // TODO: Figure out a way to make this work as well!
    [<TestMethod; Timeout(20000)>]
    member _.``deleteMin Zufall`` (): unit =
        Assertify.Check (
            <@ fun (Valid (bst, _, _): ValidBST<Nat>) ->
                match Student.BSTs.deleteMin bst with
                | None -> failwith "Aus nicht-leerem Baum konnte nicht gelöscht werden"
                | Some (n, rest) ->
                    match deleteMin bst with
                    | Some (m, restm) ->
                        (?) <@ m = n @>
                        (?) <@ toList restm = toList rest@> @>,
            config.WithStartSize(1).WithEndSize(5).WithMaxTest(30)
        )

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout(1000)>]
    member _.``partition leer`` (): unit =
        (?) <@ Student.BSTs.partition<Nat> [] = empty<Nat,List<Nat>> @>

    [<TestMethod>][<Timeout(20000)>]
    member this.``partition Zufall``() : unit =
        Assertify.Check (
            <@ fun (FsCheck.NonEmptyArray ar) ->
                let (list : List<Nat>) = List.ofArray ar
                match Student.BSTs.partition list with
                | Emptyish -> failwith "Nicht-leere Liste konnte nicht zweigeteilt werden"
                | Nodeish(l, p, r) ->
                    (?) <@ p = List.last list @>
                    (?) <@ List.sort (l @ [ p ] @ r) = List.sort list @>
                    (?) <@ List.forall (fun x -> x <= p) l = true @>
                    (?) <@ List.forall (fun x -> x >= p) r = true @> @>,
            config.WithStartSize(1).WithEndSize(5).WithMaxTest(30)
        )


    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout(10000)>]
    member _.``letIt Beispiele`` (): unit =
        let fibTree = fun n -> if n = 0N then Emptyish else Nodeish(n - 1N, n, n - 2N) in
        (?) <@ Student.BSTs.letIt fibTree 3N = Node(Node (Node (Empty, 1N, Empty), 2N, Empty), 3N, Node (Empty, 1N, Empty)) @>
        (?) <@ Student.BSTs.letIt fibTree 4N = Node (Node        (Node (Node (Empty, 1N, Empty), 2N, Empty), 3N, Node (Empty, 1N, Empty)),      4N, Node (Node (Empty, 1N, Empty), 2N, Empty)) @>

    // ------------------------------------------------------------------------
    // f)

    [<TestMethod; Timeout(20000)>]
    member _.``toList Zufall`` (): unit =
        Assertify.Check (
            <@ fun (Valid (bst, list, _): ValidBST<Nat>) -> Student.BSTs.toList bst = list @>,
            config.WithEndSize(5).WithMaxTest(30)
        )


    // ------------------------------------------------------------------------
    // g)

    [<TestMethod; Timeout(1000)>]
    member _.``quickSort Beispiel 1`` (): unit =
        (?) <@ Student.BSTs.quickSort<Nat> [] = [] @>

    [<TestMethod; Timeout(1000)>]
    member _.``quickSort Beispiel 2`` (): unit =
        (?) <@ Student.BSTs.quickSort [2N; 3N; 1N; 2N] = [1N; 2N; 2N; 3N] @>

    [<TestMethod; Timeout(10000)>]
    member _.``quickSort Zufall`` (): unit =
        Assertify.Check (
            <@ fun (xs: Nat list) -> Student.BSTs.quickSort xs = List.sort xs @>,
            config.WithMaxTest(100).WithEndSize(50)
        )
