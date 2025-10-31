module Tests.FindTests

open Assertify
open Types.FindTypes



let swap<'a> (ar: Array<'a>) (i: Int, j: Int): Array<'a> =
    Array.permute (fun x -> if x = i then j elif x = j then i else x) ar

let expectException (name: string) (f: unit -> unit): unit =
    try
        f ()
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail $"Es wurde keine %s{name} Ausnahme ausgelöst!"
    with
    | :? Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException -> reraise()
    | e ->
        let eType = e.GetType()
        if eType.FullName <> "Types+"+name then
            if eType.FullName.StartsWith "BibExceptions+" then
                (?) <@eType.Name = name @>
            else failwithf $"Es wurde eine %s{eType.FullName} ausgelöst statt einer eigenen %s{name} Ausname!"

let ex = [1..5]

[<TestClass>]
type FindTests () =
    [<TestMethod; Timeout(1000)>]
    member _.``a) tryFindLast Beispiele`` (): unit =
        (?) <@ Student.Find.tryFindLast (fun x -> x % 2 = 0) ex = Some 4 @>
        (?) <@ Student.Find.tryFindLast (fun x -> false) ex = None @>
        (?) <@ Student.Find.tryFindLast (fun x -> true) [] = None @>

    [<TestMethod; Timeout(10000)>]
    member _.``a) tryFindLast Zufallstests`` (): unit =
        Assertify.Check <@ fun (pred: Nat -> Bool) (xs: List<Nat>) -> Student.Find.tryFindLast pred xs = List.tryFindBack pred xs @>


    [<TestMethod; Timeout(1000)>]
    member _.``b) findLast Beispiele`` (): unit =
        (?) <@ Student.Find.findLast (fun x -> x % 2 = 0) ex = 4 @>
        expectException "NotFound" (fun () -> Student.Find.findLast (fun x -> false) ex |> ignore)
        expectException "NotFound" (fun () -> Student.Find.findLast (fun x -> true) [] |> ignore)

    [<TestMethod; Timeout(10000)>]
    member _.``b) findLast Zufallstests`` (): unit =
        Assertify.Check
            <@ fun (pred: Nat -> Bool) (xs: List<Nat>) ->
                match List.tryFindBack pred xs with
                | None -> expectException "NotFound" (fun () -> Student.Find.findLast pred xs |> ignore)
                | Some e -> (?) <@ Student.Find.findLast pred xs = e @> @>

    [<TestMethod; Timeout(1000)>]
    member _.``c) tryFindLast2 Beispiele`` (): unit =
        (?) <@ Student.Find.tryFindLast2 (fun x -> x % 2 = 0) ex = Some 4 @>
        (?) <@ Student.Find.tryFindLast2 (fun x -> false) ex = None @>
        (?) <@ Student.Find.tryFindLast2 (fun x -> true) [] = None @>

    [<TestMethod; Timeout(10000)>]
    member _.``c) tryFindLast2 Zufallstests`` (): unit =
        Assertify.Check <@ fun (pred: Nat -> Bool) (xs: List<Nat>) -> Student.Find.tryFindLast2 pred xs = List.tryFindBack pred xs @>


    [<TestMethod; Timeout(1000)>]
    member _.``d) findLast Beispiele`` (): unit =
        (?) <@ Student.Find.findLast2 (fun x -> x % 2 = 0) ex = 4 @>
        expectException "NotFound" (fun () -> Student.Find.findLast2 (fun x -> false) ex |> ignore)
        expectException "NotFound" (fun () -> Student.Find.findLast2 (fun x -> true) [] |> ignore)

    [<TestMethod; Timeout(10000)>]
    member _.``d) findLast Zufallstests`` (): unit =
        Assertify.Check
            <@ fun (pred: Nat -> Bool) (xs: List<Nat>) ->
                match List.tryFindBack pred xs with
                | None -> expectException "NotFound" (fun () -> Student.Find.findLast2 pred xs |> ignore)
                | Some e -> (?) <@ Student.Find.findLast2 pred xs = e @> @>