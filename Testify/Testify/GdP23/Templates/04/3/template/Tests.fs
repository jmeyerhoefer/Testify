namespace GdP23.S04.A3.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini
    open NatsType

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

    let rec toList (xs: Nats): Nat list =
        match xs with
        | Nil -> []
        | Cons (x, ys) -> x::toList ys

    let rec fromList (xs: Nat list): Nats =
        match xs with
        | [] -> Nil
        | x::ys -> Cons (x, fromList ys)

    let config = {
        Config.QuickThrowOnFailure with
            EndSize = 10000
            MaxTest = 1000
        }

    let ex = Cons (2N, Cons (4N, Cons (3N, Cons(4N, Cons(2N, Cons (1N, Nil))))))

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) double Beispiele`` (): unit =
            test <@ Nats.double Nil = Nil @>
            test <@ Nats.double (Cons (1N, Cons (2N, Nil))) = Cons (2N,Cons (4N,Nil)) @>
            test <@ Nats.double ex = Cons (4N, Cons (8N, Cons (6N, Cons (8N, Cons (4N, Cons (2N, Nil)))))) @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) double Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (xs: Nats) (ys: Nats) ->
                Assert.AreEqual(
                    List.map (fun n -> 2N*n) (toList xs) |> fromList,
                    Nats.double xs
                )
            )

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) isSorted Beispiele`` (): unit =
            test <@ Nats.isSorted Nil = true @>
            test <@ Nats.isSorted (Cons (1N, Cons (2N, Nil))) = true @>
            test <@ Nats.isSorted (Cons (2N, Cons (1N, Nil))) = false @>
            test <@ Nats.isSorted (Cons (1N, Cons (1N, Nil))) = true @>
            test <@ Nats.isSorted ex  = false @>


        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) isSorted Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (xs: Nats) ->
                Assert.AreEqual(
                    (toList xs |> Seq.pairwise |> Seq.forall (fun (x, y) -> x <= y)),
                    Nats.isSorted xs
                )
            )

        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) filter Beispiele`` (): unit =
            test <@ Nats.filter (fun _ -> true) Nil =  Nil @>
            test <@ Nats.filter (fun _ -> false) Nil =  Nil @>
            test <@ Nats.filter (fun n -> n%2N=0N) (Cons (2N, Cons (4N, Nil))) = Cons (2N, Cons (4N, Nil)) @>
            test <@ Nats.filter (fun n -> n%2N=0N) (Cons (1N, Cons (6N, Nil))) = Cons (6N, Nil) @>
            test <@ Nats.filter (fun n -> n > 3N) ex = Cons (4N, Cons (4N, Nil)) @>


        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) filter Zufallstest`` (): unit =
            Check.QuickThrowOnFailure(fun (p: Nat -> Bool) (xs: Nats) ->
                Assert.AreEqual(
                    List.filter p (toList xs) |> fromList,
                    Nats.filter p xs
                )
            )

