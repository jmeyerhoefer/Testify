namespace GdP23.S05.A2.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini
    open PriorityQueueTypes

    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let config =
        Config.QuickThrowOnFailure
            .WithEndSize(1000)
            .WithMaxTest(1000)

    let exQueue1 =
        [ {priority=1N; value=4711N}
        ; {priority=3N; value=815N}
        ; {priority=9N; value=42N}
        ]

    let exQueue2 =
        [ {priority=4N; value=123N}
        ; {priority=6N; value=456N}
        ; {priority=8N; value=789N}
        ]

    let exElem = {priority=5N; value=7N}

    let rec listDeleteNth<'a>(n: Nat) (xs: List<'a>) =
        List.zip [0..(List.length xs - 1)] xs
        |> List.filter (fun (i, _) -> i <> (int n))
        |> List.map snd

    [<TestClass>]
    type Tests() =
        let config = Config.QuickThrowOnFailure.WithArbitrary [typeof<ArbitraryModifiers>]

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) isEmpty Beispiele`` (): unit =
            test <@ PriorityQueue.isEmpty [] = true @>
            test <@ PriorityQueue.isEmpty exQueue1 = false @>
            test <@ PriorityQueue.isEmpty exQueue2 = false @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``a) isEmpty Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(config, fun (xs: PQ<Nat>) ->
                Assert.AreEqual<bool>(
                    List.isEmpty xs,
                    PriorityQueue.isEmpty xs
                )
            )


        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) insert Beispiele`` (): unit =
            test <@ PriorityQueue.insert exElem [] = [exElem] @>
            test <@ PriorityQueue.insert exElem exQueue1 = [ {priority=1N; value=4711N}; {priority=3N; value=815N}; {priority=5N; value=7N}; {priority=9N; value=42N} ] @>
            test <@ PriorityQueue.insert exElem exQueue2 = [ {priority=4N; value=123N}; {priority=5N; value=7N}; {priority=6N; value=456N}; {priority=8N; value=789N} ] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``b) insert Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(config, fun (x: QElem<Nat>) (gen: PQ<Nat>) ->
                let xs = List.sortBy (fun n -> n.priority) gen // enforce PriorityQueue invariant
                let expected = List.sortBy (fun n -> n.priority) (x::xs)
                let getKeys = List.map (fun n -> n.priority)
                Assert.AreEqual(
                    expected |> getKeys,
                    PriorityQueue.insert x xs |> getKeys
                )
            )


        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) extractMin Beispiele`` (): unit =
            test <@ PriorityQueue.extractMin [] = (None, []) @>
            test <@ PriorityQueue.extractMin exQueue1 = (Some {priority=1N; value=4711N}, [ {priority=3N; value=815N} ; {priority=9N; value=42N} ]) @>
            test <@ PriorityQueue.extractMin exQueue2 = (Some {priority=4N; value=123N}, [ {priority=6N; value=456N} ; {priority=8N; value=789N} ]) @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``c) extractMin Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(config, fun (gen: PQ<Nat>) ->
                let xs = List.sortBy (fun n -> n.priority) gen
                let getKeys (r : Option<QElem<'a>> * PQ<'a>): Option<Nat> * List<Nat> =
                    match r with
                    | (None, l) -> (None, List.map (fun n -> n.priority) l)
                    | (Some h, l) -> (Some h.priority, List.map (fun n -> n.priority) l)
                let expected =
                    try
                        (Some (List.head xs), List.tail xs)
                    with
                    | :? System.ArgumentException -> (None, gen)
                Assert.AreEqual(
                    expected |> getKeys,
                    PriorityQueue.extractMin xs |> getKeys
                )
            )


        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) merge Beispiele`` (): unit =
            test <@ PriorityQueue.merge [] [] = [] @>
            test <@ PriorityQueue.merge exQueue1 [] = exQueue1 @>
            test <@ PriorityQueue.merge [] exQueue1 = exQueue1 @>
            test <@ PriorityQueue.merge exQueue2 [] = exQueue2 @>
            test <@ PriorityQueue.merge [] exQueue2 = exQueue2 @>
            test <@ PriorityQueue.merge exQueue1 exQueue2 = [ {priority=1N; value=4711N} ; {priority=3N; value=815N} ; {priority=4N; value=123N} ; {priority=6N; value=456N} ; {priority=8N; value=789N} ; {priority=9N; value=42N} ] @>
            test <@ PriorityQueue.merge exQueue2 exQueue1 = [ {priority=1N; value=4711N} ; {priority=3N; value=815N} ; {priority=4N; value=123N} ; {priority=6N; value=456N} ; {priority=8N; value=789N} ; {priority=9N; value=42N} ] @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``d) merge Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(config, fun (gen1: PQ<Nat>) (gen2: PQ<Nat>) ->
                let xs = List.sortBy (fun n -> n.priority) gen1
                let ys = List.sortBy (fun n -> n.priority) gen2
                let expected = List.sortBy (fun n -> n.priority) (xs @ ys)
                let getKeys = List.map (fun n -> n.priority)
                Assert.AreEqual(
                    expected |> getKeys,
                    PriorityQueue.merge xs ys |> getKeys
                )
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``e) deleteNth Beispiele`` (): unit =
            test <@ PriorityQueue.deleteNth 0N [] = [] @>
            test <@ PriorityQueue.deleteNth 0N exQueue1 = [ {priority=3N; value=815N} ; {priority=9N; value=42N} ] @>
            test <@ PriorityQueue.deleteNth 1N exQueue1 = [ {priority=1N; value=4711N} ; {priority=9N; value=42N} ] @>
            test <@ PriorityQueue.deleteNth 2N exQueue1 = [ {priority=1N; value=4711N} ; {priority=3N; value=815N} ] @>
            test <@ PriorityQueue.deleteNth 3N exQueue1 = exQueue1 @>
            test <@ PriorityQueue.deleteNth 0N exQueue2 = [ {priority=6N; value=456N} ; {priority=8N; value=789N} ] @>
            test <@ PriorityQueue.deleteNth 1N exQueue2 = [ {priority=4N; value=123N} ; {priority=8N; value=789N} ] @>
            test <@ PriorityQueue.deleteNth 2N exQueue2 = [ {priority=4N; value=123N} ; {priority=6N; value=456N} ] @>
            test <@ PriorityQueue.deleteNth 3N exQueue2 = exQueue2 @>

        [<TestMethod>] [<Timeout(5000)>]
        member this.``e) deleteNth Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(config, fun (n: Nat) (gen1: PQ<Nat>) ->
                let xs = List.sortBy (fun n -> n.priority) gen1
                let expected = List.sortBy (fun n -> n.priority) (listDeleteNth n xs)
                let getKeys = List.map (fun n -> n.priority)
                Assert.AreEqual(
                    expected |> getKeys,
                    PriorityQueue.deleteNth n xs |> getKeys
                )
            )

