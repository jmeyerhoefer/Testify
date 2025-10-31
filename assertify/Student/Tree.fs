module Student.Tree


open Types.TreeTypes


// Beispiel vom Übungsblatt
let ex = Node (Node (Leaf, 1N, (Node (Leaf, 2N, Leaf))), 3N, (Node (Leaf, 4N, Leaf)))

// a)
let rec countLeaves<'a> (t: Tree<'a>): Nat =
    failwith "TODO"

// b)
let rec height<'a> (t: Tree<'a>): Nat =
    failwith "TODO"

// c)
let rec map<'a, 'b> (f: 'a -> 'b) (t: Tree<'a>): Tree<'b> =
    failwith "TODO"
