module Tests.ArrayMapTests


open Assertify.Types
open Assertify.Assertify.Operators
open Assertify.Checkify


let ex = [| 1; 2; 3; 4 |]


[<TestClass>]
type ArrayMapTests () =
    [<TestMethod; Timeout 1000>]
    member _.``a) map Beispiele`` (): unit =
        (?) <@ Student.ArrayMap.map (fun x -> x * 2) ex |> Array.toList = [ 2; 4; 6; 8 ] @>
        (?) <@ Student.ArrayMap.map id ex |> Array.toList = [ 1; 2; 3; 4 ] @>
        (?) <@ ex |> Array.toList = [ 1; 2; 3; 4 ] @>

    // TODO: maybe let check accept multiple properties
    [<TestMethod; Timeout 10000>]
    member _.``a) map Zufallstests`` (): unit =
        Checkify.Check <@ fun (f: Nat -> Bool) (xs: Array<Nat>) ->
            Array.toList (Student.ArrayMap.map f xs) = Array.toList (Array.map f xs) &&
            Array.toList xs = Array.toList (Array.copy xs) @>

    [<TestMethod; Timeout 1000>]
    member _.``b) inplaceMap Beispiele`` (): unit =
        let xs = [| 1; 2; 3; 4 |]
        Student.ArrayMap.inplaceMap (fun x -> x * 2) xs
        (?) <@ xs |> Array.toList = [ 2; 4; 6; 8 ] @>

    // TODO: Does this work?
    [<TestMethod; Timeout 10000>]
    member _.``b) findLast Zufallstests`` (): unit =
        Checkify.Check
            <@ fun (f: Nat -> Nat) (xs: Array<Nat>) ->
                let xs_expected = Array.map f xs
                Student.ArrayMap.inplaceMap f xs
                (?) <@ xs |> Array.toList = (xs_expected |> Array.toList) @> @>