namespace GdP23.S11.A4.Template

module Tests =

    open Mini
    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck

    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let swap<'a> (ar: Array<'a>) (i: Int, j: Int): Array<'a> =
        Array.permute (fun x -> if x = i then j elif x = j then i else x) ar

    let rotate<'a> (ar: Array<'a>) (i: Int): Array<'a> =
        let len = ar.Length
        Array.init len (fun j -> ar.[(j + (len - i)) % len])


    [<TestClass>]
    type Tests() =
        let config = Config.QuickThrowOnFailure.WithArbitrary [typeof<ArbitraryModifiers>]

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) swap Beispiel`` (): unit =
            let ar = [| 1;4;3;2;5 |]
            let expected = swap ar (1, 3)
            Arrays.swap ar (1, 3)
            Assert.IsTrue(Array.toList expected = Array.toList ar, sprintf "expected %A but got %A" expected ar)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``a) swap Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (ar: Array<Nat>, i: Int, j: Int) ->
                let n = ar.Length
                if n > 0 && i >= 0 && j >= 0 && i < n && j < n then
                    // do not modify ar, so the original value is printed if the test fails
                    let actual = Array.copy ar
                    let expected = swap ar (i, j)
                    Arrays.swap actual (i, j)
                    Assert.IsTrue(Array.toList expected = Array.toList actual, sprintf "expected %A but got %A when swapping elements at positions %A and %A" expected actual i j)
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) insertionsort Beispiel 1`` (): unit =
            let ar = [| 7;8;4;6;5 |]
            let expected = Array.sort ar
            Arrays.insertionsort ar
            Assert.IsTrue(Array.toList expected = Array.toList ar, sprintf "expected %A but got %A" expected ar)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) insertionsort Beispiel 2`` (): unit =
            let ar = [| |]
            Arrays.insertionsort ar
            Assert.IsTrue([] = Array.toList ar, sprintf "expected %A but got %A" [] ar)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``b) insertionsort Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (ar: Array<Nat>) ->
                let actual = Array.copy ar
                let expected = Array.sort ar
                Arrays.insertionsort actual
                Assert.IsTrue(Array.toList expected = Array.toList actual, sprintf "expected %A but got %A" expected actual)
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) rotate Beispiel 1`` (): unit =
            let ar = [| 7;8;4;6;5 |]
            let expected = [| 5;7;8;4;6 |]
            Arrays.rotate ar
            Assert.IsTrue(Array.toList expected = Array.toList ar, sprintf "expected %A but got %A" expected ar)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) rotate Beispiel 2`` (): unit =
            let ar = [| |]
            Arrays.rotate ar
            Assert.IsTrue([] = Array.toList ar, sprintf "expected %A but got %A" [] ar)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``c) rotate Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (ar: Array<Nat>) ->
                let actual = Array.copy ar
                let expected = rotate ar 1
                Arrays.rotate actual
                Assert.IsTrue(Array.toList expected = Array.toList actual, sprintf "expected %A but got %A" expected actual))

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) same Beispiel 1`` (): unit =
            let ar = [| 7;8;4;6;5 |]
            let xs = Array.toList ar
            Assert.IsTrue(Arrays.same xs ar, sprintf "expected that same returns true for list %A and array %A" xs ar)
            Assert.IsFalse(Arrays.same (List.rev xs) ar, sprintf "expected that same returns false for list %A and array %A" (List.rev xs) ar)
            Assert.IsFalse(Arrays.same xs (Array.rev ar), sprintf "expected that same returns false for list %A and array %A" xs (Array.rev ar))
            Assert.IsFalse(Arrays.same (xs @ [1]) ar, sprintf "expected that same returns false for list %A and array %A" (xs @ [1]) ar)
            Assert.IsFalse(Arrays.same xs (xs @ [1] |> List.toArray), sprintf "expected that same returns false for list %A and array %A" xs (xs @ [1] |> List.toArray))

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) same Beispiel 2`` (): unit =
            Assert.IsTrue(Arrays.same<Nat> [] [||], sprintf "expected that same returns true for an empty list and an empty array")

        [<TestMethod>] [<Timeout(10000)>]
        member this.``d) same Zufallstests`` (): unit =
            Check.QuickThrowOnFailure (fun (xs: List<Nat>) (ar: Array<Nat>) ->
                Assert.AreEqual((xs = Array.toList ar), Arrays.same xs ar, sprintf "list %A and array %A" xs ar)
                Assert.IsTrue(Arrays.same xs (Array.ofList xs))
                Assert.IsTrue(Arrays.same (List.ofArray ar) ar)
            )

