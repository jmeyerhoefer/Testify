namespace GdP23.S10.A4.Template

module Tests =

    open Mini
    open Types
    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck


    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let rec toList<'a> (l: MList<'a>): List<'a> =
        let rec h (e: Option<Item<'a>>) =
            match e with
            | None -> []
            | Some elem -> elem.elem :: (h elem.next)
        h (l.first)

    let fromList<'a> (xs: List<'a>): MList<'a> =
        let l = { first=None; last=None; size=(List.length xs |> Nat.Make) }
        let mutable ys = xs
        let mutable start = true
        while not (List.isEmpty ys) do
            match ys with
            | [] -> ()
            | (z::zs) ->
                let i = Some { elem = z ; next = None }
                if start then l.first <- i ; start <- false
                match l.last with
                | None -> ()
                | Some sl -> sl.next <- i
                l.last <- i
                ys <- zs
        l

    let empty<'a> (): MList<'a> =
        { first = None; last = None; size = 0N }

    let update<'a> (index: Nat) (v: 'a) (xs: List<'a>): List<'a> =
        List.indexed xs
        |> List.map (fun (i,a) -> if Nat.Make i = index then (i,v) else (i,a))
        |> List.map snd

    let remove<'a> (index: Nat) (xs: List<'a>): List<'a> =
        List.indexed xs
        |> List.filter (fun (i,a) -> Nat.Make i <> index)
        |> List.map snd

    let ex1 () = { first = None ; last = None ; size = 0N }
    let ex2 () =
        let e = Some { elem = 4711N ; next = None }
        { first = e
          last  = e
          size  = 1N }
    let ex3 () =
        let e = Some { elem = 5N ; next = None }
        { first = Some { elem = 30N ; next = Some { elem = 20N ; next = Some { elem = 10N ; next = e } } }
          last  = e
          size  = 4N }


    [<TestClass>]
    type Tests() =
        let config = Config.QuickThrowOnFailure.WithArbitrary [typeof<ArbitraryModifiers>]

        [<TestMethod>] [<Timeout(10000)>]
        member this.``a) isEmpty Beispiele`` (): unit =
            Assert.IsTrue( Lists.isEmpty (ex1 ()) )
            Assert.IsFalse( Lists.isEmpty (ex2 ()) )
            Assert.IsFalse( Lists.isEmpty (ex3 ()) )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``a) isEmpty Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (xs: List<Nat>) ->
                let l = fromList xs
                Assert.AreEqual(List.isEmpty xs, Lists.isEmpty l)
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``b) appendFront Beispiele`` (): unit =
            let actual1 = ex1 ()
            let actual2 = ex2 ()
            let actual3 = ex3 ()
            Lists.appendFront 1N actual1
            Lists.appendFront 1N actual2
            Lists.appendFront 1N actual3
            let expected1 = (1N :: toList (ex1 ())) |> fromList
            let expected2 = (1N :: toList (ex2 ())) |> fromList
            let expected3 = (1N :: toList (ex3 ())) |> fromList
            Assert.AreEqual(expected1, actual1)
            Assert.AreEqual(expected2, actual2)
            Assert.AreEqual(expected3, actual3)
            match (actual1.first, actual1.last) with
            | (Some f1, Some l1) ->
                let eq = System.Object.ReferenceEquals(actual1.first, actual1.last) || System.Object.ReferenceEquals(f1, l1)
                Assert.IsTrue(eq, "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein")
            | _ -> ()

        [<TestMethod>] [<Timeout(10000)>]
        member this.``b) appendFront Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (xs: List<Nat>) ->
                let l = empty<Nat> ()
                List.map (fun x -> Lists.appendFront x l) (List.rev xs) |> ignore
                Assert.AreEqual(fromList xs, l)
                if l.size = 1N then
                    match (l.first, l.last) with
                    | (Some f1, Some l1) ->
                        let eq = System.Object.ReferenceEquals(l.first, l.last) || System.Object.ReferenceEquals(f1, l1)
                        Assert.IsTrue(eq, "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein")
                    | _ -> ()
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``c) appendBack Beispiele`` (): unit =
            let actual1 = ex1 ()
            let actual2 = ex2 ()
            let actual3 = ex3 ()
            Lists.appendBack 1N actual1
            Lists.appendBack 1N actual2
            Lists.appendBack 1N actual3
            let expected1 = (toList (ex1 ()) @ [1N]) |> fromList
            let expected2 = (toList (ex2 ()) @ [1N]) |> fromList
            let expected3 = (toList (ex3 ()) @ [1N]) |> fromList
            Assert.AreEqual(expected1, actual1)
            Assert.AreEqual(expected2, actual2)
            Assert.AreEqual(expected3, actual3)
            match (actual1.first, actual1.last) with
            | (Some f1, Some l1) ->
                let eq = System.Object.ReferenceEquals(actual1.first, actual1.last) || System.Object.ReferenceEquals(f1, l1)
                Assert.IsTrue(eq, "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein")
            | _ -> ()

        [<TestMethod>] [<Timeout(10000)>]
        member this.``c) appendBack Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (xs: List<Nat>) ->
                let l = empty<Nat> ()
                List.map (fun x -> Lists.appendBack x l) xs |> ignore
                Assert.AreEqual(fromList xs, l)
                if l.size = 1N then
                    match (l.first, l.last) with
                    | (Some f1, Some l1) ->
                        let eq = System.Object.ReferenceEquals(l.first, l.last) || System.Object.ReferenceEquals(f1, l1)
                        Assert.IsTrue(eq, "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein")
                    | _ -> ()
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``d) get Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (xs: List<Nat>) ->
                let l = fromList xs
                for i in 0..(10 + List.length xs) do
                    let actual = Lists.get (Nat.Make i) l
                    if i >= List.length xs then // out of range
                        Assert.AreEqual(None, actual, "Index liegt außerhalb der Liste, erwarte None als Ergebnis")
                    else
                        match actual with
                        | None -> Assert.Fail(sprintf "Wert erwartet, aber None erhalten")
                        | Some a -> Assert.AreEqual(List.item i xs, a)
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``e) update Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (v: Nat) (xs: List<Nat>) ->
                let l = fromList xs
                let mutable expected = xs
                for i in 0..(10 + List.length xs) do
                    expected <- update (Nat.Make i) v expected
                    Lists.update (Nat.Make i) v l
                    let actual = toList l
                    Assert.AreEqual(expected, actual)
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``f) remove Zufallstests (freiwillige Zusatzaufgabe)`` (): unit =
            Check.QuickThrowOnFailure (fun (i: Nat) (xs: List<Nat>) ->
                let j = if List.length xs = 0 then 0N else 1N + i % (10 + List.length xs |> Nat.Make)
                let actual = fromList xs
                let expected = remove j xs
                Lists.remove j actual
                Assert.AreEqual(fromList expected, actual, sprintf "Beim Löschen des Elementes an der Position index=%A ist ein Fehler aufgetreten!" j)
                if actual.size = 1N then
                    match (actual.first, actual.last) with
                    | (Some f1, Some l1) ->
                        let eq = System.Object.ReferenceEquals(actual.first, actual.last) || System.Object.ReferenceEquals(f1, l1)
                        Assert.IsTrue(eq, "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein")
                    | _ -> ()
            )

