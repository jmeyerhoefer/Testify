module Tests.Lists1Tests


open Assertify.Types
open Assertify.Checkify
open Assertify.Assertify.Operators


let ex: Nat list = [ 2N; 4N; 3N; 4N; 2N; 1N ]


[<TestClass>]
type Lists1Tests () =
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``a) plusOne Beispiele`` (): unit =
        (?) <@ Student.Lists1.plusOne [] = [] @>
        (?) <@ Student.Lists1.plusOne ex = [3N; 5N; 4N; 5N; 3N; 2N] @>

    [<TestMethod; Timeout 5000>]
    member _.``a) plusOne Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: Nat list) -> Student.Lists1.plusOne xs = List.map (fun x -> x + 1N) xs @>

    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``b) filter Beispiele`` (): unit =
        (?) <@ Student.Lists1.filter (fun x -> x % 2N = 0N) [] = [] @>
        (?) <@ Student.Lists1.filter (fun x -> x % 2N = 0N) ex = [2N; 4N; 4N; 2N] @>
        (?) <@ Student.Lists1.filter (fun x -> x % 2N = 1N) ex = [3N; 1N] @>

    [<TestMethod; Timeout 5000>]
    member _.``b) filter Zufallstest`` (): unit =
        Checkify.Check <@ fun (p: Nat -> Bool) (xs: List<Nat>) -> Student.Lists1.filter p xs = List.filter p xs @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) concat Beispiele`` (): unit =
        (?) <@ Student.Lists1.concat [] ex = ex @>
        (?) <@ Student.Lists1.concat ex [] = ex @>
        (?) <@ Student.Lists1.concat [1N] [2N] = [1N; 2N] @>

    [<TestMethod; Timeout 5000>]
    member _.``c) concat Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) (ys: List<Nat>) -> Student.Lists1.concat xs ys = xs @ ys @>

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``d) mirror Beispiele`` (): unit =
        (?) <@ Student.Lists1.mirror [] = [] @>
        (?) <@ Student.Lists1.mirror ex  = [1N; 2N; 4N; 3N; 4N; 2N] @>


    [<TestMethod; Timeout 5000>]
    member _.``d) mirror Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists1.mirror xs = List.rev xs @>

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``e) sum Beispiele`` (): unit =
        (?) <@ Student.Lists1.sum [] =  0N @>
        (?) <@ Student.Lists1.sum ex  = 16N @>


    [<TestMethod; Timeout 5000>]
    member _.``e) sum Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists1.sum xs = List.sum xs @>