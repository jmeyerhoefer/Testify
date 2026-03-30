namespace GdP23.S07.A2.Template

module TestsMapSortedList =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini

    type Action<'v> =
        | IsEmpty
        | Add of Nat * 'v
        | TryFind of Nat
        | Comma of MapSortedList.MapSortedList<Nat, 'v>
        | Delete of Nat

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let m1: MapSortedList.MapSortedList<Nat, String> = []
    let m2: MapSortedList.MapSortedList<Nat, String> = [(1N, "Lisa"); (4N, "Harry")]
    let m3: MapSortedList.MapSortedList<Nat, String> = [(1N, "Lisa"); (4N, "Harry"); (5N, "Bob"); (6N, "Schorsch")]
    let m4: MapSortedList.MapSortedList<Nat, String> = [(1N, "Lista"); (4N, "Hacker"); (5N, "Bob"); (6N, "Schorsch")]

    let rec merge<'k, 'v when 'k: comparison> (m1: Map<'k, 'v>) (m2: MapSortedList.MapSortedList<'k, 'v>): Map<'k, 'v> =
        match m2 with
        | [] -> m1
        | (k, v)::rest -> merge (m1.Add(k, v)) rest

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) empty`` (): unit =
            test <@ List.length MapSortedList.empty = 0 @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) lookup Beispiele`` (): unit =
            test <@ MapSortedList.lookup 1N m1 = None @>
            test <@ MapSortedList.lookup 1N m2 = Some "Lisa" @>
            test <@ MapSortedList.lookup 5N m3 = Some "Bob" @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) set Beispiele`` (): unit =
            test <@ MapSortedList.set 1N "Harry" [] = [(1N, "Harry")] @>
            let e1 = 2N
            let e2 = "Eddy"
            let m4 = MapSortedList.set e1 e2 m2
            Assert.IsTrue(List.contains (e1,e2) m4)
            Assert.AreEqual(List.sortBy fst m4, m4)
            let m5 = MapSortedList.set e1 e2 m3
            Assert.IsTrue(List.contains (e1,e2) m5)
            Assert.AreEqual(List.sortBy fst m5, m5)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) comma Beispiele`` (): unit =
            test <@ MapSortedList.comma m1 m2 = m2 @>
            test <@ MapSortedList.comma m2 m1 = m2 @>
            test <@ MapSortedList.comma m2 m3 = [(1N, "Lisa"); (4N, "Harry"); (5N, "Bob"); (6N, "Schorsch")] @>
            test <@ MapSortedList.comma m3 m2 = [(1N, "Lisa"); (4N, "Harry"); (5N, "Bob"); (6N, "Schorsch")] @>
            test <@ MapSortedList.comma m3 m4 = [(1N, "Lista"); (4N, "Hacker"); (5N, "Bob"); (6N, "Schorsch")] @>
            test <@ MapSortedList.comma m4 m3 = [(1N, "Lisa"); (4N, "Harry"); (5N, "Bob"); (6N, "Schorsch")] @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``e) delete Beispiele`` (): unit =
            let remove = 1N
            test <@ MapSortedList.delete remove m1 = [] @>
            test <@ MapSortedList.delete remove m2 = [(4N, "Harry")] @>
            Assert.AreEqual(None, List.tryFind (fun x -> fst x = remove) (MapSortedList.delete remove m2))

        [<TestMethod>] [<Timeout(20000)>]
        member this.``Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (actions: Action<int> list) ->
                let rec h (actions: Action<int> list) (referenceQueue: Map<Nat, int>) (actualQueue: MapSortedList.MapSortedList<Nat, int>): unit =
                    match actions with
                    | [] -> ()
                    | IsEmpty::rest ->
                        Assert.AreEqual(Map.isEmpty referenceQueue, List.isEmpty actualQueue)
                        h rest referenceQueue actualQueue
                    | Add(k, v)::rest ->
                        let new_referenceQueue = referenceQueue.Add(k, v)
                        let new_actualQueue = MapSortedList.set k v actualQueue
                        Assert.IsTrue(List.contains (k, v) new_actualQueue)
                        Assert.AreEqual(List.sortBy fst new_actualQueue, new_actualQueue)
                        h rest new_referenceQueue new_actualQueue
                    | (TryFind k)::rest ->
                        Assert.AreEqual(referenceQueue.TryFind k, MapSortedList.lookup k actualQueue)
                        h rest referenceQueue actualQueue
                    | (Comma m)::rest ->
                        let m_operand = List.distinctBy fst m |> List.sortBy fst
                        let new_referenceQueue = merge referenceQueue m_operand
                        let new_actualQueue = MapSortedList.comma actualQueue m_operand
                        Assert.AreEqual(List.sortBy fst new_actualQueue, new_actualQueue)
                        h rest new_referenceQueue new_actualQueue
                    | (Delete k)::rest ->
                        let new_referenceQueue = referenceQueue.Remove(k)
                        let new_actualQueue = MapSortedList.delete k actualQueue
                        Assert.AreEqual(None, List.tryFind (fun x -> fst x = k) new_actualQueue)
                        Assert.AreEqual(List.sortBy fst new_actualQueue, new_actualQueue)
                        h rest new_referenceQueue new_actualQueue
                h actions Map.empty []
            )

