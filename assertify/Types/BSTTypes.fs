module Types.BSTTypes


type BST<'a> =
    | Empty
    | Node of BST<'a> * 'a * BST<'a>

type BSTreeish<'a, 'b> =
    | Emptyish
    | Nodeish of 'b * 'a * 'b

type Range<'a> =
    | EmptyR
    | Twixt of 'a * 'a //Zwischen `lo` und `hi`
