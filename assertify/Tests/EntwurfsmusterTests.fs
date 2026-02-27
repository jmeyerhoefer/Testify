namespace Tests.EntwurfsmusterTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Assertify.Operators
open Assertify.Checkify


[<TestClass>]
type EntwurfsmusterTests () =
    // b)
    [<TestMethod; Timeout 1000>]
    member _.``b) add Beispiele`` (): unit =
        (?) <@ Student.Entwurfsmuster.add 0N = 0N @>
        (?) <@ Student.Entwurfsmuster.add 2N = 3N @>
        (?) <@ Student.Entwurfsmuster.add 3N = 6N @>
        (?) <@ Student.Entwurfsmuster.add 10N = 55N @>

    [<TestMethod; Timeout 30000>]
    member _.``b) add Zufallstest`` (): unit =
        let solution (x: Nat): Nat = [ 1 .. int x ] |> List.sum |> Nat.Make
        Checkify.Check (
            <@ fun (x: Nat) -> Student.Entwurfsmuster.add x = solution x @>,
            defaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) mod5 Beispiele`` (): unit =
        (?) <@ Student.Entwurfsmuster.mod5 0N = 0N @>
        (?) <@ Student.Entwurfsmuster.mod5 1N = 1N @>
        (?) <@ Student.Entwurfsmuster.mod5 2N = 2N @>
        (?) <@ Student.Entwurfsmuster.mod5 3N = 3N @>
        (?) <@ Student.Entwurfsmuster.mod5 4N = 4N @>
        (?) <@ Student.Entwurfsmuster.mod5 5N = 0N @>
        (?) <@ Student.Entwurfsmuster.mod5 6N = 1N @>
        (?) <@ Student.Entwurfsmuster.mod5 7N = 2N @>
        (?) <@ Student.Entwurfsmuster.mod5 19N = 4N @>
        (?) <@ Student.Entwurfsmuster.mod5 20N = 0N @>
        (?) <@ Student.Entwurfsmuster.mod5 21N = 1N @>
        (?) <@ Student.Entwurfsmuster.mod5 22N = 2N @>

    // Simple calculations are ok
    // TODO: What is ok? What is not?
    [<TestMethod; Timeout 5000>]
    member _.``c) mod5 Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (n: Nat) -> Student.Entwurfsmuster.mod5 n = n % 5N @>,
            defaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``b) mult42 Beispiele`` (): unit =
        (?) <@ Student.Entwurfsmuster.mult42 0N = 0N @>
        (?) <@ Student.Entwurfsmuster.mult42 1N = 42N @>
        (?) <@ Student.Entwurfsmuster.mult42 2N = 84N @>
        (?) <@ Student.Entwurfsmuster.mult42 5N = 210N @>

    [<TestMethod; Timeout 5000>]
    member _.``b) mult42 Zufallstest`` (): unit =
        Checkify.Check (
            <@ fun (n: Nat) -> Student.Entwurfsmuster.mult42 n = n * 42N @>,
            defaultConfig.WithEndSize 1000
    )

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``d) count5 Beispiele`` (): unit =
        (?) <@ Student.Entwurfsmuster.count5 3N = 0N @>
        (?) <@ Student.Entwurfsmuster.count5 76567N = 1N @>
        (?) <@ Student.Entwurfsmuster.count5 1234N = 0N @>
        (?) <@ Student.Entwurfsmuster.count5 445566N = 2N @>

    [<TestMethod; Timeout 5000>]
    member _.``d) count5 Zufallstest`` (): unit =
        let solution (n: Nat): Nat =
            let char2int (n: Char) = int n - int '0'
            string n
            |> Seq.toList
            |> List.map char2int
            |> List.filter (fun (x: int) -> x = 5)
            |> List.length
            |> Nat.Make
        Checkify.Check (
            <@ fun (n: Nat) -> Student.Entwurfsmuster.count5 n = solution n @>,
            defaultConfig.WithEndSize 1000
        )