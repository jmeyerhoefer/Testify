namespace GdP23.S06.A3.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open System
    open Mini
    open HeapType

    type ValidHeap<'a> = Valid of Heap<'a> * List<'a> * Nat // (Heap, Heap als Liste, Höhe des Heaps)
    type InvalidHeap<'a> = Invalid of Heap<'a>

    // Generatoren für >= und > größere Elemente
    let generateGE min = Arb.from<'a> |> Arb.toGen |> Gen.filter (fun x -> x >= min)
    let generateGT min = Arb.from<'a> |> Arb.toGen |> Gen.filter (fun x -> x > min)

    // Generator für gültigen Heap mit gegebener Größe und Wurzel-Element (falls Größe > 0)
    let rec generateValidHeap root size =
        if size <= 0 then Gen.constant (Empty, [], 0N)
        else
            gen {
                let! rootL = generateGE root
                let! rootR = generateGE root
                let! sizeL = Gen.choose(0, size)
                let! (heapL, listL, heightL) = generateValidHeap rootL sizeL
                let! (heapR, listR, heightR) = generateValidHeap rootR (size - sizeL - 1)
                let rec mergeLists xs ys =
                    match (xs, ys) with
                    | ([], zs) | (zs, []) -> zs
                    | (x::xs, y::ys) ->
                        if x <= y then x :: mergeLists xs (y::ys)
                        else y :: mergeLists (x::xs) ys
                return (
                    Node (heapL, root, heapR),
                    root :: mergeLists listL listR,
                    1N + max heightL heightR
                )
            }

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

        static member ValidHeap<'a when 'a: comparison>() =
            Arb.fromGen << Gen.sized <| fun size ->
                gen {
                    let! root = Arb.generate<'a>
                    let! result = generateValidHeap root size
                    return Valid result
                }

        static member InvalidHeap<'a when 'a: comparison>() =
            Arb.fromGen << Gen.sized <| fun size ->
                let size = max size 2 // ungültiger Heap muss mindestens zwie Einträge haben
                gen {
                    // Einen kleinen Eintrag generieren
                    let! violating = Arb.generate<'a>

                    // Einen Heap mit ausschließlich echt größeren Einträgen generieren
                    let! root = generateGT violating
                    let! (heap, _, _) = generateValidHeap root size

                    // Eine bestimmte Position (preorder) durch den zu kleinen Eintrag ersetzen
                    let rec invalidatePos (heap: Heap<'a>) (pos: int): Heap<'a> * int =
                        if pos = -1 then (heap, -1) // -1: fertig, rekursiver Aufstieg
                        else
                            match heap with
                            | Node (left, x, right) when pos = 0 -> // 0: aktuelle Position ersetzen
                                (Node (left, violating, right), -1)
                            | Node (left, x, right) ->
                                let (left', pos') = invalidatePos left (pos - 1)
                                let (right', pos'') = invalidatePos right pos'
                                (Node (left', x, right'), pos'')
                            | Empty -> (Empty, pos)

                    // Position auswählen und Ersetzung vornehmen
                    let! violatingPosition = Gen.choose(1, size - 1)
                    let (result, _) = invalidatePos heap violatingPosition
                    return Invalid result
                }

    let mutable counter: int = 0;

    [<CustomEquality>] [<CustomComparison>]
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
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        let ex1 = Node(Node(Empty,6N,Empty), 2N, Node(Empty,4N,Empty))
        let ex2 = Node(Node(Empty,7N,Empty), 3N, Node(Empty,5N,Empty))
        let ex3 = Node(Node(Empty,1N,Empty), 1N, Empty)
        let inv1 = Node(Node(Empty,2N,Empty), 3N, Empty)
        let inv2 = Node(Node(Node(Empty,4N,Empty), 5N, Empty), 3N, Empty)

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``size Beispiel 1`` (): unit =
            Assert.AreEqual(0N, Heaps.size Empty)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``size Beispiel 2`` (): unit =
            Assert.AreEqual(3N, Heaps.size ex1)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``size Beispiel 3`` (): unit =
            Assert.AreEqual(2N, Heaps.size ex3)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``size Zufall`` (): unit =
            Check.QuickThrowOnFailure(fun (Valid (heap, list, _): ValidHeap<Nat>) ->
                Assert.AreEqual(Nat.Make(list.Length), Heaps.size heap)
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``height Beispiel 1`` (): unit =
            Assert.AreEqual(0N, Heaps.height Empty)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``height Beispiel 2`` (): unit =
            Assert.AreEqual(2N, Heaps.height ex1)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``height Beispiel 3`` (): unit =
            Assert.AreEqual(2N, Heaps.height ex3)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``height Zufall`` (): unit =
            Check.QuickThrowOnFailure(fun (Valid (heap, _, h): ValidHeap<Nat>) ->
                Assert.AreEqual(h, Heaps.height heap)
            )


        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 1`` (): unit =
            Assert.IsTrue(Heaps.isHeap<Nat> Empty)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 2`` (): unit =
            Assert.IsTrue(Heaps.isHeap ex1)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 3`` (): unit =
            Assert.IsTrue(Heaps.isHeap ex2)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 4`` (): unit =
            Assert.IsTrue(Heaps.isHeap ex3)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 5`` (): unit =
            Assert.IsFalse(Heaps.isHeap inv1)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``isHeap Beispiel 6`` (): unit =
            Assert.IsFalse(Heaps.isHeap inv2)

        [<TestMethod>] [<Timeout(60000)>]
        member this.``isHeap Zufall Gültig`` (): unit =
            Check.One({Config.QuickThrowOnFailure with MaxTest = 1000; EndSize = 50}, fun (Valid (heap, _, _): ValidHeap<Nat>) ->
                Assert.IsTrue(
                    Heaps.isHeap heap,
                    "Heap wurde nicht als gültig erkannt."
                )
            )

        [<TestMethod>] [<Timeout(60000)>]
        member this.``isHeap Zufall Ungültig`` (): unit =
            Check.One({Config.QuickThrowOnFailure with MaxTest = 1000; EndSize = 50}, fun (Invalid heap: InvalidHeap<Nat>) ->
                Assert.IsFalse(
                    Heaps.isHeap heap,
                    "Heap wurde nicht als ungültig erkannt."
                )
            )


        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``head Beispiel`` (): unit =
            Assert.AreEqual(Some 2N, Heaps.head ex1)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``head Zufall`` (): unit =
            Check.QuickThrowOnFailure(fun (Valid (heap, list, _): ValidHeap<Nat>) ->
                match list with
                | [] -> Assert.AreEqual(None, Heaps.head heap)
                | x::xs -> Assert.AreEqual(Some x, Heaps.head heap)
            )


        // ------------------------------------------------------------------------
        // d)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``merge Beispiel`` (): unit =
            Assert.AreEqual(
                  Node(Node(Node(Node(Empty,5N,Empty), 4N, Empty), 3N, Node(Empty,7N,Empty)), 2N, Node(Empty,6N,Empty)),
                  Heaps.merge ex1 ex2
            )

        [<TestMethod>] [<Timeout(20000)>]
        member this.``merge Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 30; MaxTest = 30}, fun (Valid (heap1, list1, _): ValidHeap<Nat>) (Valid (heap2, list2, _): ValidHeap<Nat>) ->
                let result = Heaps.merge heap1 heap2
                Assert.IsTrue(
                    Heaps.isHeap result,
                    sprintf "Merge ergibt `%A`, erfüllt aber laut isHeap die Heap-Bedingung nicht." result
                )
                let size1 = Nat.Make(list1.Length)
                let size2 = Nat.Make(list2.Length)
                let size12 = size1 + size2
                let sizeM = Heaps.size result
                Assert.AreEqual(
                    size12,
                    sizeM,
                    sprintf "Merge ergibt `%A`, hat falsche Größe: %A Erwartete Größe: %A = %A + %A."
                        result
                        sizeM
                        size12
                        size1
                        size2
                )
            )


        // ------------------------------------------------------------------------
        // e)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``tail Beispiel`` (): unit =
            Assert.AreEqual(Node(Empty,1N,Empty), Heaps.tail ex3)

        [<TestMethod>] [<Timeout(20000)>]
        member this.``tail Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 30; MaxTest = 30}, fun (Valid (heap, list, _): ValidHeap<Nat>) ->
                let result = Heaps.tail heap
                Assert.AreEqual(
                    Nat.Make(list.Length - 1),
                    Heaps.size result,
                    sprintf "tail ergibt `%A`, hat falsche Größe." result
                )
            )


        // ------------------------------------------------------------------------
        // f)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``insert Beispiel`` (): unit =
            Assert.AreEqual(Node(Empty,3N,Empty), Heaps.insert Empty 3N)

        [<TestMethod>] [<Timeout(20000)>]
        member this.``insert Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 30; MaxTest = 30}, fun (Valid (heap, list, _): ValidHeap<Nat>) (x: Nat) ->
                let result = Heaps.insert heap x
                Assert.IsTrue(
                    Heaps.isHeap result,
                    sprintf "Insert ergibt `%A`, erfüllt aber laut isHeap die Heap-Bedingung nicht." result
                )
                Assert.AreEqual(
                    Nat.Make(list.Length + 1),
                    Heaps.size result,
                    sprintf "Insert ergibt `%A`, hat falsche Größe." result
                )
            )


        // ------------------------------------------------------------------------
        // g)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``ofList Beispiel 1`` (): unit =
            let expected: Heap<Nat> = Empty;
            Assert.AreEqual(expected, Heaps.ofList<Nat> [])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``ofList Beispiel 2`` (): unit =
            Assert.AreEqual(Node(Empty,3N,Empty), Heaps.ofList [3N])

        [<TestMethod>] [<Timeout(20000)>]
        member this.``ofList Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 30; MaxTest = 30}, fun (list: Nat list) ->
                let result = Heaps.ofList list
                Assert.IsTrue(
                    Heaps.isHeap result,
                    sprintf "ofList ergibt `%A`, erfüllt aber laut isHeap die Heap-Bedingung nicht." result
                )
                Assert.AreEqual(
                    Nat.Make(list.Length),
                    Heaps.size result,
                    sprintf "ofList ergibt `%A`, hat falsche Größe." result
                )
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``toList Beispiel 1`` (): unit =
            let expected: Nat list = []
            Assert.AreEqual(expected, Heaps.toList<Nat> Empty)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``toList Beispiel 2`` (): unit =
            Assert.AreEqual([2N; 4N; 6N], Heaps.toList ex1)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``toList Beispiel 3`` (): unit =
            Assert.AreEqual([3N; 5N; 7N], Heaps.toList ex2)

        [<TestMethod>] [<Timeout(20000)>]
        member this.``toList Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 30; MaxTest = 30}, fun (Valid (heap, list, _): ValidHeap<Nat>) ->
                let result = Heaps.toList heap
                Assert.AreEqual(list, Heaps.toList heap)
            )


        // ------------------------------------------------------------------------
        // h)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``heapsort Beispiel 1`` (): unit =
            let expected: Nat list = []
            Assert.AreEqual(expected, Heaps.heapsort<Nat> [])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``heapsort Beispiel 2`` (): unit =
            Assert.AreEqual([1N; 2N; 2N; 3N], Heaps.heapsort [2N; 3N; 1N; 2N])

        [<TestMethod>] [<Timeout(10000)>]
        member this.``heapsort Zufall`` (): unit =
            Check.QuickThrowOnFailure(fun (xs: Nat list) ->
                Assert.AreEqual(List.sort xs, Heaps.heapsort xs)
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``heapsort Zufall Effizienz`` (): unit =
            Check.QuickThrowOnFailure(fun (xs: Nat list) ->
                counter <- 0
                let result =
                    xs
                    |> List.map ComparisonCount.wrap
                    |> Heaps.heapsort
                    |> List.map ComparisonCount.unwrap
                Assert.AreEqual(List.sort xs, result)
                let limit =
                    if xs.Length = 0 then 0
                    else let l = float xs.Length in int (floor (2. * l * Math.Log(l, 2.)))
                Assert.IsTrue(
                    limit >= counter,
                    sprintf "Sortieren von %A (Länge n=%i) benötigt %i Vergleiche, maximal erlaubt sind jedoch nur 2 * n * log2(n) = %i Vergleiche."
                        xs
                        xs.Length
                        counter
                        limit
                )
            )

