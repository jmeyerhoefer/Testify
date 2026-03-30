namespace GdP23.S06.A2.Template

[<AutoOpen>]
module TreeTypes =
    open Mini

    type Tree<'a> =
        | Leaf                             // Blatt
        | Node of Tree<'a> * 'a * Tree<'a> // Knoten

