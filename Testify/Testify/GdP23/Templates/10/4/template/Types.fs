namespace GdP23.S10.A4.Template

[<AutoOpen>]
module Types =
    open Mini

    ////start)
    type Item<'a> =
        { mutable elem: 'a
          mutable next: Option<Item<'a>> }

    type MList<'a> =
        { mutable first: Option<Item<'a>>
          mutable last: Option<Item<'a>>
          mutable size: Nat }
    ////end)

