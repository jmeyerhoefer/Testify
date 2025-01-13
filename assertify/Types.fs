module Types


type RegExp<'a> =
    | Eps
    | Lit of 'a
    | Cat of RegExp<'a> * RegExp<'a>
    | Empty
    | Or of RegExp<'a> * RegExp<'a>
    | Star of RegExp<'a>