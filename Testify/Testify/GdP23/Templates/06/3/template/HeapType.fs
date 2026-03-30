namespace GdP23.S06.A3.Template

module HeapType =

    type Heap<'a> =
        | Empty
        | Node of Heap<'a> * 'a * Heap<'a>

