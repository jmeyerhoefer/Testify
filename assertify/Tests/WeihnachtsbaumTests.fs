module Tests.WeihnachtsbaumTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.WeihnachtsbaumTypes


let decorationWeight = function
    | Kugel -> 2N
    | Lametta -> 1N

let rec treeWeight<'a> (tree: Tree<'a>) (f: 'a -> Nat): Nat =
    match tree with
    | Leaf -> 0N
    | Node(l, s, r) -> f s + treeWeight l f + treeWeight r f
    | ENode(l, r) -> treeWeight l f + treeWeight r f

let istBalanciert<'a> (tree: Tree<'a>) (f: 'a -> Nat): Bool =
    let rec balanciertMitGewicht (tree: Tree<'a>): Option<Nat> =
        match tree with
        | Leaf -> Some 0N
        | ENode (l, r) ->
            match (balanciertMitGewicht l, balanciertMitGewicht r) with
            | (Some gl, Some gr) when gl = gr -> Some (gl + gr)
            | _                               -> None
        | Node (l, s, r) ->
            match (balanciertMitGewicht l, balanciertMitGewicht r) with
            | (Some gl, Some gr) when gl = gr -> Some (gl + f s + gr)
            | _                               -> None
    match balanciertMitGewicht tree with
    | None -> false
    | _    -> true

let isect<'a when 'a: equality>(l1: List<'a>, l2: List<'a>): List<'a> =
    List.filter (fun x -> List.contains x l2) l1

let rec moeglicheGewichte<'a> (tree: Tree<'a>): List<Nat> =
    let intersect (xs: List<Nat>) (ys: List<Nat>): List<Nat> =
        Set.intersect (Set.ofList xs) (Set.ofList ys)
        |> Set.toList

    match tree with
    | Leaf -> [0N]
    | ENode (l, r) ->
        intersect (moeglicheGewichte l) (moeglicheGewichte r)
        |> List.map (fun x -> 2N * x)
    | Node (l, _, r) ->
        intersect (moeglicheGewichte l) (moeglicheGewichte r)
        |> List.collect (fun x -> [2N * x + 1N; 2N * x + 2N])
        |> List.distinct

let rec sameShape<'a, 'b>(t: Tree<'a>) (u: Tree<'b>): Bool =
    match t, u with
    | Leaf, Leaf -> true
    | Node(l1, _, r1), Node(l2, _, r2) -> sameShape l1 l2 && sameShape r1 r2
    | ENode(l1, r1), ENode(l2, r2) -> sameShape l1 l2 && sameShape r1 r2
    | _ -> false


[<TestClass>]
type WeihnachtsbaumTests () =
    let ex1 = Node(Leaf, Kugel, Leaf)
    let ex2 = ENode(Node(Leaf, Kugel, Leaf), ENode(Node(Leaf, Lametta, Leaf), Node(Leaf, Lametta, Leaf)))
    let inv1 = Node(Leaf, Kugel, Node(Leaf, Lametta, Leaf))
    let inv2 = ENode(Node(Leaf, Lametta, Leaf), ENode(Leaf, Leaf))

    let eex1 = Node(Leaf, (), Leaf)
    let eex2 = Node(Node(Leaf, (), Leaf), (), Node(Leaf, (), Leaf))
    let eex3 = ENode(Node(Leaf, (), Leaf), Node(Leaf, (), Leaf))
    let einv1 = Node(ENode(Leaf, Leaf), (), Node(Leaf, (), Leaf))
    let einv2 = ENode(Node(Leaf, (), Leaf), Leaf)

    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``schmuckGewicht`` (): unit =
        (?) <@ Student.Weihnachtsbaum.schmuckGewicht Lametta = 1N @>
        (?) <@ Student.Weihnachtsbaum.schmuckGewicht Kugel = 2N @>

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``baumGewicht Beispiel 2`` (): unit =
        (?) <@ Student.Weihnachtsbaum.baumGewicht ex1 = 2N @>
        (?) <@ Student.Weihnachtsbaum.baumGewicht ex2 = 4N @>
        (?) <@ Student.Weihnachtsbaum.baumGewicht inv1 = 3N @>
        (?) <@ Student.Weihnachtsbaum.baumGewicht inv2 = 1N @>

    [<TestMethod; Timeout 1000>]
    member _.``baumGewicht Zufall`` (): unit =
        Checkify.Check (
            <@ fun (t: Weihnachtsbaum) -> Student.Weihnachtsbaum.baumGewicht t = treeWeight t decorationWeight @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``istBalanciert Beispiel 1`` (): unit =
        (?) <@ Student.Weihnachtsbaum.istBalanciert ex1 @>
        (?) <@ Student.Weihnachtsbaum.istBalanciert ex2 @>

    [<TestMethod; Timeout 1000>]
    member _.``istBalanciert Beispiel 2`` (): unit =
        (?) <@ Student.Weihnachtsbaum.istBalanciert inv1 = false @>
        (?) <@ Student.Weihnachtsbaum.istBalanciert inv2 = false @>

    [<TestMethod; Timeout 1000>]
    member _.``istBalanciert Zufall`` (): unit =
        Checkify.Check (
            <@ fun (t: Weihnachtsbaum) -> Student.Weihnachtsbaum.istBalanciert t = istBalanciert t decorationWeight @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``moeglicheGewichte Beispiel 1`` (): unit =
        (?) <@ Student.Weihnachtsbaum.moeglicheGewichte eex1 |> Set.ofList = Set.ofList [1N; 2N] @>
        (?) <@ Student.Weihnachtsbaum.moeglicheGewichte eex2 |> Set.ofList = Set.ofList [3N; 4N; 5N; 6N] @>
        (?) <@ Student.Weihnachtsbaum.moeglicheGewichte eex3 |> Set.ofList = Set.ofList [2N; 4N] @>

    [<TestMethod; Timeout 1000>]
    member _.``moeglicheGewichte Beispiel 2`` (): unit =
        (?) <@ Student.Weihnachtsbaum.moeglicheGewichte einv1 = [] @>
        (?) <@ Student.Weihnachtsbaum.moeglicheGewichte einv2 = [] @>

    [<TestMethod; Timeout 1000>]
    member _.``moeglicheGewichte Zufall`` (): unit =
        Checkify.Check (
            <@ fun (t: Weihnachtsbaum) -> Set.ofList (Student.Weihnachtsbaum.moeglicheGewichte t) = Set.ofList (moeglicheGewichte t) @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Beispiel 1`` (): unit =
        (?) <@ Student.Weihnachtsbaum.schmuecken eex1 1N = Some (Node(Leaf, Lametta, Leaf)) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex1 2N = Some (Node(Leaf, Kugel, Leaf)) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex2 3N = Some (Node(Node(Leaf, Lametta, Leaf), Lametta, Node(Leaf, Lametta, Leaf))) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex2 4N = Some (Node(Node(Leaf, Lametta, Leaf), Kugel, Node(Leaf, Lametta, Leaf))) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex2 5N = Some (Node(Node(Leaf, Kugel, Leaf), Lametta, Node(Leaf, Kugel, Leaf))) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex2 6N = Some (Node(Node(Leaf, Kugel, Leaf), Kugel, Node(Leaf, Kugel, Leaf))) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex3 2N = Some (ENode(Node(Leaf, Lametta, Leaf), Node(Leaf, Lametta, Leaf))) @>
        (?) <@ Student.Weihnachtsbaum.schmuecken eex3 4N = Some (ENode(Node(Leaf, Kugel, Leaf), Node(Leaf, Kugel, Leaf))) @>

    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Beispiel 2`` (): unit =
        (?) <@ Student.Weihnachtsbaum.schmuecken einv1 1N = None @>
        (?) <@ Student.Weihnachtsbaum.schmuecken einv1 2N = None @>

    // TODO: Find alternative solution
    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Zufall Balanced`` (): unit =
        Checkify.Check (
            <@ fun (t: Tree<Unit>) (w: Nat) ->
                match Student.Weihnachtsbaum.schmuecken t w with
                | Some t' -> istBalanciert t' (fun _ -> 1N)
                | None -> true @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Zufall Shape`` (): unit =
        Checkify.Check (
            <@ fun (t: Tree<Unit>) (w: Nat) ->
                match Student.Weihnachtsbaum.schmuecken t w with
                | Some t' -> sameShape t t'
                | None -> true @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Zufall Weight`` (): unit =
        Checkify.Check (
            <@ fun (t: Tree<Unit>) (w: Nat) ->
                match Student.Weihnachtsbaum.schmuecken t w with
                | Some t' -> treeWeight t' decorationWeight = w
                | None -> true @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    [<TestMethod; Timeout 1000>]
    member _.``schmuecken Zufall moeglicheGewichte`` (): unit =
        Checkify.Check (
            <@ fun (t: Tree<Unit>) (w: Nat) ->
                if moeglicheGewichte t |> List.contains w then
                    match Student.Weihnachtsbaum.schmuecken t w with
                    | Some t' -> true
                    | None -> false
                else
                    match Student.Weihnachtsbaum.schmuecken t w with
                    | Some t' -> false
                    | None -> true @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )

    // ------------------------------------------------------------------------
    // f)

    [<TestMethod; Timeout 1000>]
    member _.``schmueckungen Beispiel 1`` (): unit =
        (?) <@ Student.Weihnachtsbaum.schmueckungen eex1 |> Set.ofList = Set.ofList [Node(Leaf, Lametta, Leaf); Node(Leaf, Kugel, Leaf)] @>
        (?) <@ Student.Weihnachtsbaum.schmueckungen eex2 |> Set.ofList = Set.ofList [Node(Node(Leaf, Lametta, Leaf), Lametta, Node(Leaf, Lametta, Leaf));
                                                                                 Node(Node(Leaf, Lametta, Leaf), Kugel, Node(Leaf, Lametta, Leaf));
                                                                                 Node(Node(Leaf, Kugel, Leaf), Lametta, Node(Leaf, Kugel, Leaf));
                                                                                 Node(Node(Leaf, Kugel, Leaf), Kugel, Node(Leaf, Kugel, Leaf))] @>
        (?) <@ Student.Weihnachtsbaum.schmueckungen eex3 |> Set.ofList = Set.ofList [ENode(Node(Leaf, Lametta, Leaf), Node(Leaf, Lametta, Leaf));
                                                                                  ENode(Node(Leaf, Kugel, Leaf), Node(Leaf, Kugel, Leaf))] @>

    [<TestMethod; Timeout 1000>]
    member _.``schmueckungen Beispiel 2`` (): unit =
        (?) <@ Student.Weihnachtsbaum.schmueckungen einv1 = [] @>
        (?) <@ Student.Weihnachtsbaum.schmueckungen einv2 = [] @>

    [<TestMethod; Timeout 1000>]
    member _.``schmueckungen Zufall`` (): unit =
        Checkify.Check (
            <@ fun (t: Tree<Unit>) ->
                Set.ofList (Student.Weihnachtsbaum.schmueckungen t) =
                    Set.ofList (Student.Weihnachtsbaum.moeglicheGewichte t |> List.choose (Student.Weihnachtsbaum.schmuecken t)) @>,
            DefaultConfig.WithMaxTest(100).WithEndSize(5)
        )