namespace Tests.RegExpTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators
open System.Text.RegularExpressions
open Types.RegExpTypes


[<TestClass>]
type RegExpTests () =
    [<TestMethod; Timeout 1000>]
    member _.``accept Beispiel 1`` (): unit =
        (?) <@ Student.RegExp.accept [B;A] @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Beispiel 2`` (): unit =
        (?) <@ Student.RegExp.accept [A] = true @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Beispiel 3`` (): unit =
        (?) <@ Student.RegExp.accept [B;B;B;B;B;B;B;B;B;A] = true @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Gegenbeispiel 1`` (): unit =
        (?) <@ Student.RegExp.accept [] = false @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Gegenbeispiel 2`` (): unit =
        (?) <@ Student.RegExp.accept [A;A;A;A;A] = false @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Gegenbeispiel 3`` (): unit =
        (?) <@ Student.RegExp.accept [B;B;B;B;B;B;B;B;A;A] = false @>

    [<TestMethod; Timeout 1000>]
    member _.``accept Gegenbeispiel 4`` (): unit =
        (?) <@ Student.RegExp.accept [B;A;A;A;A;A;A] = false @>

    // TODO: Check output and find alternative solution
    [<TestMethod; Timeout 10000>]
    member _.``accept Zufall`` (): unit =
        Checkify.Check (
            <@ fun (input: Alphabet list) ->
                let rec toString (acc: String) (xs: Alphabet list): String =
                    match xs with
                    | [] -> acc
                    | A::rest -> toString (acc + "a") rest
                    | B::rest -> toString (acc + "b") rest
                let inputStr = toString "" input
                let m = Regex.Match (inputStr, "b*a")
                m.Success && m.Value = inputStr, Student.RegExp.accept input @>,
            DefaultConfig.WithEndSize 100
        )
