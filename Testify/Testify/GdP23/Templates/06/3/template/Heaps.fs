namespace GdP23.S06.A3.Template

module Heaps =
    open Mini
    open HeapType

    // Beispiele vom Übungsblatt (zur Verwendung im Interpreter):
    let ex1 = Node(Node(Empty,6N,Empty), 2N, Node(Empty,4N,Empty))
    let ex2 = Node(Node(Empty,7N,Empty), 3N, Node(Empty,5N,Empty))
    let ex3 = Node(Node(Empty,1N,Empty), 1N, Empty)
    let inv1 = Node(Node(Empty,2N,Empty), 3N, Empty)
    let inv2 = Node(Node(Node(Empty,4N,Empty), 5N, Empty), 3N, Empty)


    // a)
    let rec size<'a> (root: Heap<'a>): Nat =
        failwith "TODO"

    let rec height<'a> (root: Heap<'a>): Nat =
        failwith "TODO"

    // b)
    let rec isHeap<'a when 'a: comparison> (root: Heap<'a>): bool =
        failwith "TODO"

    // c)
    let head<'a > (root: Heap<'a>): Option<'a> =
        failwith "TODO"

    // d)
    let rec merge<'a when 'a: comparison> (root1: Heap<'a>) (root2: Heap<'a>): Heap<'a> =
        failwith "TODO"

    // e)
    let tail<'a when 'a: comparison> (root: Heap<'a>): Heap<'a> =
        failwith "TODO"

    // f)
    let insert<'a when 'a: comparison> (root: Heap<'a>) (x: 'a): Heap<'a> =
        failwith "TODO"

    // g)
    let rec ofList<'a when 'a: comparison> (xs: List<'a>): Heap<'a> =
        failwith "TODO"

    let rec toList<'a when 'a: comparison> (root: Heap<'a>): List<'a> =
        failwith "TODO"

    // h)
    let heapsort<'a when 'a: comparison> (xs: List<'a>): List<'a> =
        failwith "TODO"

