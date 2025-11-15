module Tests.NatsTests


open Assertify.Types
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.NatsTypes


let rec toList (xs: Nats): Nat list =
    match xs with
    | Nil -> []
    | Cons (x, ys) -> x :: toList ys


let rec fromList (xs: Nat list): Nats =
    match xs with
    | [] -> Nil
    | x :: ys -> Cons (x, fromList ys)


let ex: Nats = Cons (2N, Cons (4N, Cons (3N, Cons(4N, Cons(2N, Cons (1N, Nil))))))


[<TestClass>]
type NatsTests () =
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``a) trace Beispiele`` (): unit =
        (?) <@ Student.Nats.trace (fun x -> x - 1N) 2N = Cons (2N , Cons (1N, Nil)) @>
        (?) <@ Student.Nats.trace (fun x -> x - 1N) 1N = Cons (1N, Nil) @>
        (?) <@ Student.Nats.trace (fun x -> x - 1N) 0N = Cons (0N, Nil) @>
        (?) <@ Student.Nats.trace (fun x -> x - 2N) 5N = Cons (5N , Cons (3N , Cons (1N, Nil))) @>


    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) isSortedBy Beispiele`` (): unit =
        (?) <@ Student.Nats.isSortedBy (fun (m, n) -> m > n) Nil = true @>
        (?) <@ Student.Nats.isSortedBy (fun (m, n) -> m > n) (Cons (1N, Cons (2N, Nil))) = false @>
        (?) <@ Student.Nats.isSortedBy (fun (m, n) -> m > n) (Cons (2N, Cons (1N, Nil))) = true @>
        (?) <@ Student.Nats.isSortedBy (fun (m, n) -> m > n) (Cons (1N, Cons (1N, Nil))) = false @>
        (?) <@ Student.Nats.isSortedBy (fun (m, n) -> m > n) ex  = false @>


    [<TestMethod; Timeout 1000>]
    member _.``b) isSortedBy Zufallstest`` (): unit =
        let solution (xs: Nats): bool =
            toList xs
            |> Seq.pairwise
            |> Seq.forall (fun (x, y) -> x <= y)
        Checkify.Check <@ fun (xs: Nats) -> Student.Nats.isSortedBy (fun (m, n) -> m <= n) xs = solution xs @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) exists Beispiele`` (): unit =
        (?) <@ Student.Nats.exists (fun _ -> true) Nil =  false @>
        (?) <@ Student.Nats.exists (fun _ -> false) Nil =  false @>
        (?) <@ Student.Nats.exists (fun n -> n%2N=0N) (Cons (2N, Cons (4N, Nil))) = true @>
        (?) <@ Student.Nats.exists (fun n -> n%2N=0N) (Cons (1N, Cons (6N, Nil))) = true @>
        (?) <@ Student.Nats.exists (fun n -> n > 3N) ex = true @>


    [<TestMethod; Timeout 1000>]
    member _.``c) exists Zufallstest`` (): unit =
        Checkify.Check <@ fun (p: Nat -> Bool) (xs: Nats) -> Student.Nats.exists p xs = List.exists p (toList xs) @>
