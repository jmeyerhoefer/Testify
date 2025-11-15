namespace Tests.PeanoTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators


[<TestClass>]
type PeanoTests () =
    // a)
    [<TestMethod; Timeout 1000>]
    member _.``iterate Beispiele`` (): unit =
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 0N 0N = 0N @>
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 1N 0N = 1N @>
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 2N 0N = 2N @>
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 3N 0N = 5N @>
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 4N 0N = 26N @>
        (?) <@ Student.Peano.iterate (fun x -> 1N + x * x) 5N 0N = 677N @>

    [<TestMethod; Timeout 5000>]
    member _.``a) iterate Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (f: Nat -> Nat) (n: Nat) (x: Nat) -> Student.Peano.iterate f n x = Solution.Peano.iterate f n x @>,
            DefaultConfig.WithEndSize 100
        )

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) lt Beispiele`` (): unit =
        (?) <@ Student.Peano.lt 0N 1N = true @>
        (?) <@ Student.Peano.lt 0N 0N = false @>
        (?) <@ Student.Peano.lt 5N 3N = false @>
        (?) <@ Student.Peano.lt 1N 6N = true @>

    [<TestMethod; Timeout 30000>]
    member _.``b) lt`` (): unit =
        Checkify.Check (
            <@ fun (n: Nat) (m: Nat) -> Student.Peano.lt n m = (n < m) @>,
            DefaultConfig.WithEndSize 100
        )

