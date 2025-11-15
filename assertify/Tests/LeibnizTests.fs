namespace Tests.LeibnizTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators


[<TestClass>]
type LeibnizTests () =
    // a)
    [<TestMethod; Timeout 1000>]
    member _.``a) log2 Beispiele`` (): unit =
        (?) <@ Student.Leibniz.log2 0N = 0N@>
        (?) <@ Student.Leibniz.log2 1N = 0N@>
        (?) <@ Student.Leibniz.log2 2N = 1N@>
        (?) <@ Student.Leibniz.log2 4711N = 12N@>

    // TODO: How does the output look like?
    // TODO: Maybe change to expected = actual?
    [<TestMethod; Timeout 5000>]
    member _.``a) log2 Zufallstest`` (): unit =
        // TODO: maybe add method to CheckBool or something similar to check a single boolean expression like <@ fun (a: int) -> a > 0 @>
        Checkify.Check (
            <@ fun (n: Nat) ->
                let n = n + 1N // ensure > 0N so as not to try to take the log of 0
                let m = Student.Leibniz.log2 n
                2N ** m <= n && n < 2N ** (m + 1N) @>,
            DefaultConfig.WithEndSize 1000
        )

    [<TestMethod; Timeout 5000>]
    member _.``a) log2 Zufallstest (actual = expected)`` (): unit =
        Checkify.Check (
            <@ fun (n: Nat) -> n = 0N || Student.Leibniz.log2 n = Solution.Leibniz.log2 n @>,
            DefaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) sortedDigits Beispiele`` (): unit =
        (?) <@ Student.Leibniz.sortedDigits 0N = true @>
        (?) <@ Student.Leibniz.sortedDigits 5N = true @>
        (?) <@ Student.Leibniz.sortedDigits 159N = true @>
        (?) <@ Student.Leibniz.sortedDigits 1111N = true @>
        (?) <@ Student.Leibniz.sortedDigits 42N = false @>
        (?) <@ Student.Leibniz.sortedDigits 543N = false @>
        (?) <@ Student.Leibniz.sortedDigits 1101N = false @>

    [<TestMethod; Timeout 5000>]
    member _.``b) sortedDigits Zufallstest`` (): unit =
        let solution (n: Nat): bool =
            n.ToString ()
            |> Seq.map (string >> int)
            |> Seq.pairwise
            |> Seq.forall (fun (a, b) -> a <= b)
        Checkify.Check (
            <@ fun (n: Nat) -> Student.Leibniz.sortedDigits n = solution n @>,
            DefaultConfig.WithEndSize 1000
        )

