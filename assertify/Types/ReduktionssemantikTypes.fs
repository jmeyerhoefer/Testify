module Types.ReduktionssemantikTypes


type RegEx<'a> =
    | Eps
    | Lit of 'a
    | Cat of RegEx<'a> * RegEx<'a>
    | Empty
    | Or of RegEx<'a> * RegEx<'a>
    | Star of RegEx<'a>

