module Student.Lists3


open Types.Lists3Types


// a)
let length<'a> (l: MList<'a>): Nat =
    failwith "TODO"

// b)
let insertFirst<'a> (v: 'a) (l: MList<'a>): Unit =
    failwith "TODO"

// c)
let insertLast<'a> (v: 'a) (l: MList<'a>): Unit =
    failwith "TODO"

// d)
let rec skip<'a> (n: Nat) (item: Item<'a>): Option<Item<'a>> =
    failwith "TODO"

// e)
let get<'a> (index: Nat) (l: MList<'a>): Option<'a> =
    failwith "TODO"

// f)
let update<'a> (index: Nat) (v: 'a) (l: MList<'a>): Unit =
    failwith "TODO"

// h) (freiwillig)
let remove<'a> (index: Nat) (l: MList<'a>): Unit =
    failwith "TODO"


// Beispiel Verwendung (im Interpreter):
// let xs: MList<Nat> = ref None // erstelle leere Liste
// insertFirst 10N xs
// insertFirst 20N xs
// insertFirst 30N xs
// insertLast 5N xs
// get 0N xs // erwartet: Some 30N
// get 1N xs // erwartet: Some 20N
// get 2N xs // erwartet: Some 10N
// get 3N xs // erwartet: Some 5N
// get 4N xs // erwartet: None
