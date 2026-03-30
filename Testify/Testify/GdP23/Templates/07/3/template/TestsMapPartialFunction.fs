namespace GdP23.S07.A3.Template

module TestsMapPartialFunction =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Mini
    open MapPartialFunction
    open Swensen.Unquote

    type Action<'v> =
        | Add of Nat * 'v
        | TryFind of Nat
        | Comma of Map<Nat, 'v>
        | Delete of Nat

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let rec merge<'k, 'v when 'k: comparison> (m1: Map<'k, 'v>) (m2: Map<'k, 'v>): Map<'k, 'v> =
        Map.fold (fun acc key value -> Map.add key value acc) m1 m2

    [<TestClass>]
    type Tests() =

        [<TestMethod>] [<Timeout(1000)>]
        member self.``empty, set, lookup Beispiel``() : Unit =
            let m1 = MapPartialFunction.set 1N 1N MapPartialFunction.empty
            let m2 = MapPartialFunction.set 2N 4N m1
            let m3 = MapPartialFunction.set 4N 16N m2
            Assert.AreEqual(MapPartialFunction.lookup 1N m3, Some 1N)
            Assert.AreEqual(MapPartialFunction.lookup 2N m3, Some 4N)
            Assert.AreEqual(MapPartialFunction.lookup 3N m3, None)
            Assert.AreEqual(MapPartialFunction.lookup 4N m3, Some 16N)
            Assert.AreEqual(MapPartialFunction.lookup 5N m3, None)
            Assert.AreEqual(MapPartialFunction.lookup 4N m2, None)

        [<TestMethod>] [<Timeout(1000)>]
        member self.``comma Beispiel``() : Unit =
            let m1 = MapPartialFunction.set 1N 1N MapPartialFunction.empty
            let m2 = MapPartialFunction.set 2N 4N m1
            let m3 = MapPartialFunction.set 4N 16N m2
            let n1 = MapPartialFunction.set 1N 2N MapPartialFunction.empty
            let n2 = MapPartialFunction.set 3N 6N n1
            let n3 = MapPartialFunction.set 10N 20N n2
            let c = MapPartialFunction.comma m3 n3
            Assert.AreEqual(MapPartialFunction.lookup 1N c, Some 2N)
            Assert.AreEqual(MapPartialFunction.lookup 2N c, Some 4N)
            Assert.AreEqual(MapPartialFunction.lookup 3N c, Some 6N)
            Assert.AreEqual(MapPartialFunction.lookup 4N c, Some 16N)
            Assert.AreEqual(MapPartialFunction.lookup 5N c, None)
            Assert.AreEqual(MapPartialFunction.lookup 6N c, None)
            Assert.AreEqual(MapPartialFunction.lookup 10N c, Some 20N)

        [<TestMethod>] [<Timeout(1000)>]
        member self.``delete Beispiel``() : Unit =
            let m1 = MapPartialFunction.set 1N 1N MapPartialFunction.empty
            let m2 = MapPartialFunction.set 2N 4N m1
            let m3 = MapPartialFunction.set 4N 16N m2
            let m4 = MapPartialFunction.delete 2N m3
            Assert.AreEqual(MapPartialFunction.lookup 1N m4, Some 1N)
            Assert.AreEqual(MapPartialFunction.lookup 2N m4, None)
            Assert.AreEqual(MapPartialFunction.lookup 3N m4, None)
            Assert.AreEqual(MapPartialFunction.lookup 4N m4, Some 16N)
            Assert.AreEqual(MapPartialFunction.lookup 5N m4, None)
            Assert.AreEqual(MapPartialFunction.lookup 4N m2, None)

        [<TestMethod>] [<Timeout(20000)>]
        member this.``Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (actions: Action<int> list) ->
                let rec h (actions: Action<int> list) (referenceQueue: Map<Nat, int>) (actualQueue: MapPartialFunction.MapPartialFunction<Nat, int>): unit =
                    match actions with
                    | [] -> ()
                    | Add(k, v)::rest ->
                        let new_referenceQueue = referenceQueue.Add(k, v)
                        let new_actualQueue = MapPartialFunction.set k v actualQueue
                        Assert.AreEqual(new_actualQueue k, Some v)
                        h rest new_referenceQueue new_actualQueue
                    | (TryFind k)::rest ->
                        Assert.AreEqual(referenceQueue.TryFind k, MapPartialFunction.lookup k actualQueue)
                        h rest referenceQueue actualQueue
                    | (Comma m)::rest ->
                        let new_referenceQueue = merge referenceQueue m
                        let new_actualQueue = MapPartialFunction.comma actualQueue (fun key -> m.TryFind key)
                        h rest new_referenceQueue new_actualQueue
                    | (Delete k)::rest ->
                        let new_referenceQueue = referenceQueue.Remove(k)
                        let new_actualQueue = MapPartialFunction.delete k actualQueue
                        h rest new_referenceQueue new_actualQueue
                h actions Map.empty MapPartialFunction.empty
            )

            

