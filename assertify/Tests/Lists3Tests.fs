module Tests.Lists3Tests


open Assertify.Types
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.Lists3Types
#nowarn "25"


let rec toList<'a> (l: MList<'a>): List<'a> =
    let rec h (e: Option<Item<'a>>): List<'a> =
        match e with
            | None -> []
            | Some item -> !item.value :: (h !item.next)
    match !l with
        | None -> []
        | Some lst -> h (Some lst.first)

let rec fromList<'a> (xs: List<'a>): MList<'a> =
    match xs with
        | [] -> ref None
        | [z] ->
            let i = { value = ref z; next = ref None }
            ref (Some {first = i; last = i; length = 1N })
        | z::zs ->
            let (Some rst) = !(fromList zs)
            let i = { value = ref z; next = ref (Some (rst.first)) }
            ref (Some {first = i; last = rst.last; length = rst.length + 1N})

let empty<'a> (): MList<'a> =
    ref None

let update<'a> (index: Nat) (v: 'a) (xs: List<'a>): List<'a> =
    List.indexed xs
    |> List.map (fun (i,a) -> if Nat.Make i = index then (i,v) else (i,a))
    |> List.map snd

let remove<'a> (index: Nat) (xs: List<'a>): List<'a> =
    List.indexed xs
    |> List.filter (fun (i,a) -> Nat.Make i <> index)
    |> List.map snd

let ex1 () = ref None
let ex2 () =
    let e = { value = ref 4711N ; next = ref None }
    ref ( Some ( { first = e
      ; last  = e
      ; length  = 1N }))
let ex3 () =
    let e = { value = ref 5N ; next = ref None }
    ref ( Some ({ first = { value = ref 30N ; next = ref (Some { value = ref 20N ; next = ref (Some { value = ref 10N ; next = ref (Some e) }) }) }
      ; last  = e
      ; length  = 4N }))


[<TestClass>]
type Lists3Tests () =

    [<TestMethod; Timeout 10000>]
    member _.``a) length Beispiele`` (): unit =
        (?) <@ Student.Lists3.length (ex1 ()) = 0N @>
        (?) <@ Student.Lists3.length (ex2 ()) = 1N @>
        (?) <@ Student.Lists3.length (ex3 ()) = 4N @>

    [<TestMethod; Timeout 10000>]
    member _.``a) length Zufallstests`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists3.length (fromList xs) = (List.length xs |> Nat.Make) @>

    [<TestMethod; Timeout 10000>]
    member _.``b) insertFirst Beispiele`` (): unit =
        let actual1 = ex1 ()
        let actual2 = ex2 ()
        let actual3 = ex3 ()
        Student.Lists3.insertFirst 1N actual1
        Student.Lists3.insertFirst 1N actual2
        Student.Lists3.insertFirst 1N actual3
        let expected1 = (1N :: toList (ex1 ())) |> fromList
        let expected2 = (1N :: toList (ex2 ())) |> fromList
        let expected3 = (1N :: toList (ex3 ())) |> fromList
        (?) <@ actual1 = expected1 @>
        (?) <@ actual2 = expected2 @>
        (?) <@ actual3 = expected3 @>
        let (Some actualList1) = !actual1
        let eq = System.Object.ReferenceEquals(actualList1.first, actualList1.last)
        <@ eq = true @> -?> "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein"

    // TODO: ???
    [<TestMethod; Timeout 10000>]
    member _.``b) insertFirst Zufallstests`` (): unit =
        Checkify.Check
            <@ fun (xs: List<Nat>) ->
                let l = empty<Nat> ()
                List.map (fun x -> Student.Lists3.insertFirst x l) (List.rev xs) |> ignore
                (?) <@ l = fromList xs @>
                match !l with
                | None -> ()
                | Some lst -> 
                    if lst.length = 1N then
                        let eq = System.Object.ReferenceEquals(lst.first, lst.last)
                        <@ eq = true @> -?> "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein" @>

    [<TestMethod; Timeout 10000>]
    member _.``c) insertLast Beispiele`` (): unit =
        let actual1 = ex1 ()
        let actual2 = ex2 ()
        let actual3 = ex3 ()
        Student.Lists3.insertLast 1N actual1
        Student.Lists3.insertLast 1N actual2
        Student.Lists3.insertLast 1N actual3
        let expected1 = (toList (ex1 ()) @ [1N]) |> fromList
        let expected2 = (toList (ex2 ()) @ [1N]) |> fromList
        let expected3 = (toList (ex3 ()) @ [1N]) |> fromList
        (?) <@ actual1 = expected1 @>
        (?) <@ actual2 = expected2 @>
        (?) <@ actual3 = expected3 @>
        let (Some actualList1) = !actual1
        let eq = System.Object.ReferenceEquals(actualList1.first, actualList1.last)
        <@ eq = true @> -?> "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein"

    [<TestMethod; Timeout 10000>]
    member _.``c) insertLast Zufallstests`` (): unit =
        Checkify.Check
            <@ fun (xs: List<Nat>) ->
                let l = empty<Nat> ()
                List.map (fun x -> Student.Lists3.insertLast x l) xs |> ignore
                (?) <@ l = fromList xs @>
                match !l with
                | None -> ()
                | Some lst -> 
                    if lst.length = 1N then
                        let eq = System.Object.ReferenceEquals(lst.first, lst.last)
                        <@ eq = true @> -?> "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein" @>

    [<TestMethod; Timeout 10000>]
    member _.``e) get Zufallstests`` (): unit =
        Checkify.Check
            <@ fun (xs: List<Nat>) ->
                let l = fromList xs
                for i in 0..(10 + List.length xs) do
                    let actual = Student.Lists3.get (Nat.Make i) l
                    if i >= List.length xs then // out of range
                        <@ actual = None @> -?> "Index liegt außerhalb der Liste, erwarte None als Ergebnis"
                    else
                        match actual with
                        | None -> (!!) "Wert erwartet, aber None erhalten"
                        | Some a -> (?) <@ a = List.item i xs @> @>

    [<TestMethod; Timeout 10000>]
    member _.``f) update Zufallstests`` (): unit =
        Checkify.Check
            <@ fun (v: Nat) (xs: List<Nat>) ->
                let l = fromList xs
                let mutable expected = xs
                for i in 0 .. (10 + List.length xs) do
                    expected <- update (Nat.Make i) v expected
                    Student.Lists3.update (Nat.Make i) v l
                    let actual = toList l
                    (?) <@ actual = expected @> @>

    [<TestMethod; Timeout 10000>]
    member _.``h) remove Zufallstests (freiwillige Zusatzaufgabe)`` (): unit =
        Checkify.Check
            <@ fun (i: Nat) (xs: List<Nat>) ->
                let j = if List.length xs = 0 then 0N else 1N + i % (10 + List.length xs |> Nat.Make)
                let actual = fromList xs
                let expected = remove j xs
                Student.Lists3.remove j actual
                <@ actual = fromList expected @> -?> $"Beim Löschen des Elementes an der Position index=%A{j} ist ein Fehler aufgetreten!"
                match !actual with
                | None -> ()
                | Some lst -> 
                    if lst.length = 1N then
                        let eq = System.Object.ReferenceEquals(lst.first, lst.last)
                        <@ eq = true @> -?> "Bei einer einelementigen Liste müssen first und last Referenzen auf dasselbe Objekt sein" @>