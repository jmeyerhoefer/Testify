module Solution.BSTs


open Types.BSTTypes


let ex1 = Node(Node(Empty,2N,Empty), 2N, Node(Empty,4N,Empty))
let ex2 = Node(Node(Empty,2N,Empty), 3N, Node(Empty,5N,Empty))
let ex3 = Node(Empty, 1N, Node(Empty,1N,Empty))
let inv1 = Node(Node(Empty,3N,Empty), 2N, Empty)
let inv2 = Node(Node(Node(Empty,4N,Empty), 2N, Empty), 3N, Empty)

////a)
let rec size<'a> (root: BST<'a>): Nat =
    match root with
    | Empty -> 0N
    | Node (left, _, right) -> 1N + size left + size right

let rec height<'a> (root: BST<'a>): Nat =
    match root with
    | Empty -> 0N
    | Node (left, _, right) -> 1N + max (height left) (height right)

////b)
let isBST<'a when 'a: comparison> (root: BST<'a>): Bool =
 let rec bounds (root: BST<'a>) : Option<Range<'a>> =
  match root with
  | Empty -> Some(EmptyR)
  | Node(left, p, right) ->
   match (bounds left, bounds right) with
   | Some EmptyR      , Some EmptyR       -> Some(Twixt(p, p))
   | Some EmptyR      , Some(Twixt(c, d)) -> if p <= c then Some(Twixt(p, d)) else None
   | Some(Twixt(a, b)), Some EmptyR       -> if b <= p then Some(Twixt(a, p)) else None
   | Some(Twixt(a, b)), Some(Twixt(c, d)) ->
       if b <= p && p <= c then Some(Twixt(a, d)) else None
   | _                                    -> None
 in match bounds root with
    | None -> false
    | _    -> true

////c)
let rec deleteMin<'a> (root: BST<'a>): Option<'a * BST<'a>> =
    match root with
    | Empty -> None
    | Node(l, x, r) ->
        match deleteMin l with
        | None -> Some (x, r) // l = Empty
        | Some(a, lMinusA) -> Some(a, Node(lMinusA, x, r))

////d)
let rec partition<'a when 'a: comparison> (xs: List<'a>) : BSTreeish<'a, List<'a>> =
    match xs with
    | [] -> Emptyish
    | x :: xs ->
        match partition xs with
        | Emptyish -> Nodeish([], x, [])
        | Nodeish(smaller, p, greater) ->
            if x <= p then
                Nodeish(x :: smaller, p, greater)
            else
                Nodeish(smaller, p, x :: greater)

////e)
let rec letIt (grow : 'b -> BSTreeish<'a,'b>) (seed : 'b) : BST<'a> =
    match grow seed with
    | Emptyish -> Empty
    | Nodeish(seedL,x,seedR) -> Node(letIt grow seedL, x, letIt grow seedR)

////f)
//Hilfsfunktion
let rec tracer (grow : 'b -> Option<'a * 'b>) (seed : 'b) : List<'a> =
    match grow seed with
    | None -> []
    | Some(a, seed') -> a :: tracer grow seed'

let rec toList<'a when 'a: comparison> (root: BST<'a>): List<'a> =
    tracer deleteMin root

////g)
let quickSort<'a when 'a: comparison> (xs: List<'a>): List<'a> =
    toList (letIt partition xs)
