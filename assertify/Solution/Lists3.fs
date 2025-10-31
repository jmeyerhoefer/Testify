module Solution.Lists3


open Types.Lists3Types


////a)
let length<'a> (l: MList<'a>): Nat =
    match !l with
    | None -> 0N
    | Some lst -> lst.length

////b)
let insertFirst<'a> (v: 'a) (l: MList<'a>): Unit =
    match !l with
    | None ->
        let item = { value = ref v; next = ref None }
        l := Some { first = item; last = item; length = 1N }
    | Some lst ->
        let item = { value = ref v; next = ref (Some lst.first) }
        l := Some { lst with first = item; length = lst.length + 1N }

////c)
let insertLast<'a> (v: 'a) (l: MList<'a>): Unit =
    let item = { value = ref v; next = ref None }
    match !l with
    | None -> 
        l := Some { first = item; last = item; length = 1N }
    | Some lst -> 
        lst.last.next := Some item
        l := Some { lst with last = item; length = lst.length + 1N }

////d)
let rec skip (n: Nat) (item: Item<'a>): Option<Item<'a>> =
    if n = 0N then Some item
    else
        match !item.next with
        | None -> None
        | Some next -> skip (n - 1N) next

////e)
let get<'a> (index: Nat) (l: MList<'a>): Option<'a> =
    match !l with
    | None -> None // leere Liste
    | Some lst ->
        match skip index lst.first with
        | None -> None // index ungültig
        | Some item -> Some !item.value

////f)
let update<'a> (index: Nat) (v: 'a) (l: MList<'a>): Unit =
    match !l with
    | None -> () // leere Liste
    | Some lst ->
        match skip index lst.first with
        | None -> () // index ungültig
        | Some item -> item.value := v

////h)
let remove<'a> (index: Nat) (l: MList<'a>): Unit =
  match !l with
  | None -> () // leere Liste
  | Some lst ->
      if index = 0N then
        // Erstes Element der Liste soll gelöscht werden
        match !lst.first.next with
        | None ->
            // Einziges Element wird gelöscht, Liste ist dann leer
            l := None
        | Some next ->
            // Erstes Element wird gelöscht, es gibt weitere Elemente
            l := Some { lst with first = next; length = lst.length - 1N }
      else
        // Finde Element, das vor dem zu löschenden Element liegt
        match skip (index - 1N) lst.first with
        | None -> () // index ungültig
        | Some prev ->
            // Untersuche das zu löschende Element
            match !prev.next with
            | None -> () // index ungültig
            | Some item ->
                // Untersuche das nächste Element
                match !item.next with
                | None ->
                    // Letztes Element der Liste wird gelöscht
                    prev.next := None
                    l := Some { lst with last = prev; length = lst.length - 1N }
                | Some next ->
                    // Zu löschendes Element ist weder das erste
                    // noch das letzte Element in der Liste
                    prev.next := Some next
                    l := Some { lst with length = lst.length - 1N}
////end)
