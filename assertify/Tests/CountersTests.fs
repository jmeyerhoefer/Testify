namespace Tests.CountersTests


open Assertify


type Action = | I | R

[<TestClass>]
type CountersTests () =
    [<TestMethod; Timeout(10000)>]
    member _.``Teil a Zufallstests`` (): unit =
        Assertify.Check
            <@ fun (actions: Action list) ->
                let mutable value = 0N
                Student.Counters.reset ()
                for action in actions do
                    match action with
                    | I ->
                        value <- value + 1N
                        Student.Counters.increment ()
                    | R ->
                        value <- 0N
                        Student.Counters.reset ()
                    (?) <@ Student.Counters.get() = value @> @>

    [<TestMethod; Timeout(10000)>]
    member _.``Teil b Zufallstests`` (): unit =
        Assertify.Check
            <@ fun (actions: (Action * Nat) list) ->
                if not <| List.isEmpty actions then
                    let maxCounter = actions |> List.map snd |> List.max |> int
                    let counters = [| for _ in 0..maxCounter -> Student.Counters.create() |]
                    let values = [| for _ in 0..maxCounter -> 0N |]
                    for (action, i) in actions do
                        let i = int i
                        match action with
                        | I ->
                            values.[i] <- values.[i] + 1N
                            Student.Counters.increment2(counters.[i])
                        | R ->
                            values.[i] <- 0N
                            Student.Counters.reset2(counters.[i])
                        for j in 0..maxCounter do (?) <@ Student.Counters.get2(counters.[j]) = values.[j] @> @>
