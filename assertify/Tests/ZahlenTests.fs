namespace Tests.ZahlenTests


open Assertify


[<TestClass>]
type ZahlenTests () =
    // a)
    [<TestMethod; Timeout(1000)>]
    member _.``a) Beispiel 1`` (): unit =
        (?) <@ Student.Zahlen.inOrder 2N 2N 8N = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``a) Beispiel 2`` (): unit =
        (?) <@ Student.Zahlen.inOrder 5N 2N 3N = false @>

    [<TestMethod; Timeout(1000)>]
    member _.``a) Beispiel 3`` (): unit =
        (?) <@ Student.Zahlen.inOrder 2N 5N 7N = true @>

    [<TestMethod; Timeout(1000)>]
    member _.``a) Beispiel 4`` (): unit =
        (?) <@ Student.Zahlen.inOrder 6N 13N 0N = false @>

    // Variant 1: using Solution.Zahlen.inOrder
    [<TestMethod; Timeout(1000)>]
    member _.``a) `inOrder` Zufallstest`` (): unit =
        Assertify.Check (
            // <@ fun (x: Nat) (y: Nat) (z: Nat) -> Student.Zahlen.inOrder x y z = Solution.Zahlen.inOrder x y z @>
            <@ fun (x: Nat) (y: Nat) (z: Nat) -> Student.Zahlen.inOrder x y z = (x <= y && y <= z) @>,
            DefaultConfig.WithEndSize 10000
        )

    // ------------------------------------------------------------------------
    // b)
    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 1`` (): unit =
        (?) <@ Student.Zahlen.median3 2N 5N 8N = 5N @>

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 2`` (): unit =
        (?) <@ Student.Zahlen.median3 5N 2N 3N = 3N @>

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 3`` (): unit =
        (?) <@ Student.Zahlen.median3 12N 7N 5N = 7N @>

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 4`` (): unit =
        (?) <@ Student.Zahlen.median3 13N 6N 6N = 6N @>

    // Variant 2: using solution helper
    // Such statements like (let [_; b; _] = ...) in expression do not work for parsing 
    [<TestMethod; Timeout(1000)>]
    member _.``b) `median3` Zufallstest`` (): unit =
        let solution (x: Nat) (y: Nat) (z: Nat): Nat =
            [x; y; z]
            |> List.sort
            |> List.item 1
        Assertify.Check (
            <@ fun (x: Nat) (y: Nat) (z: Nat) -> Student.Zahlen.median3 x y z = solution x y z @>,
            DefaultConfig.WithEndSize 10000
        )

    // ------------------------------------------------------------------------
    // c)
    [<TestMethod; Timeout(1000)>]
    member _.``c) Beispiel 1`` (): unit =
        (?) <@ (Student.Zahlen.takeMeTo true) 1N 2N = 1N @>

    [<TestMethod; Timeout(1000)>]
    member _.``c) Beispiel 2`` (): unit =
        (?) <@ (Student.Zahlen.takeMeTo false) 4N 3N = 3N @>
