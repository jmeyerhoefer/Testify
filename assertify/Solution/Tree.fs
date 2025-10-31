module Solution.Tree


open Types.TreeTypes


////ex)
let ex = Node (Node (Leaf, 1N, (Node (Leaf, 2N, Leaf))), 3N, (Node (Leaf, 4N, Leaf)))

////a)
let rec countLeaves<'a> (t: Tree<'a>): Nat =
    match t with
    | Leaf -> 1N
    | Node (l, _, r) -> countLeaves l + countLeaves r

////b)
let rec height<'a> (t: Tree<'a>): Nat =
    match t with
    | Leaf -> 0N
    | Node (l, _, r) -> 1N + max (height l) (height r)

////c)
let rec map<'a, 'b> (f: 'a -> 'b) (t: Tree<'a>): Tree<'b> =
    match t with
    | Leaf -> Leaf
    | Node (l, x, r) -> Node (map f l, f x, map f r)

////end)
