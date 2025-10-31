module Types.Lists3Types


type Item<'a> =
    { value: Ref<'a>
      next: Ref<Option<Item<'a>>> }

type NEList<'a> =
    { first: Item<'a>
      last: Item<'a>
      length: Nat }

type MList<'a> = Ref<Option<NEList<'a>>>
