module Types.WeihnachtsbaumTypes


type Tree<'a> =
    | Leaf
    | ENode of Tree<'a> *      Tree<'a>
    | Node  of Tree<'a> * 'a * Tree<'a>

type Schmuck =
    | Kugel
    | Lametta

type Weihnachtsbaum = Tree<Schmuck>