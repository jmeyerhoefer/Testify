module Student.BSTs


open Types.BSTTypes


// Beispiele vom Übungsblatt (zur Verwendung im Interpreter):
let ex1 = Node(Node(Empty,2N,Empty), 2N, Node(Empty,4N,Empty))
let ex2 = Node(Node(Empty,2N,Empty), 3N, Node(Empty,5N,Empty))
let ex3 = Node(Empty, 1N, Node(Empty,1N,Empty))
let inv1 = Node(Node(Empty,3N,Empty), 2N, Empty)
let inv2 = Node(Node(Node(Empty,4N,Empty), 2N, Empty), 3N, Empty)


// a)
let rec size<'a> (root: BST<'a>): Nat =
    failwith "TODO"

let rec height<'a> (root: BST<'a>): Nat =
    failwith "TODO"

// b)
let rec isBST<'a when 'a: comparison> (root: BST<'a>): Bool =
    failwith "TODO"

// c)
let rec deleteMin<'a> (root: BST<'a>): Option<'a * BST<'a>> =
    failwith "TODO"

// d)
let rec partition<'a when 'a: comparison> (xs: List<'a>) : BSTreeish<'a, List<'a>> =
    failwith "TODO"

// e)
let rec letIt (grow : 'b -> BSTreeish<'a,'b>) (seed : 'b) : BST<'a> =
    failwith "TODO"

// f)
let rec toList<'a when 'a: comparison> (root: BST<'a>): List<'a> =
    failwith "TODO"

// g)
let quickSort<'a when 'a: comparison> (xs: List<'a>): List<'a> =
    failwith "TODO"
