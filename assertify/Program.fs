module Program


open Microsoft.FSharp.Quotations


[<EntryPoint>]
let main (_: string array): int =
    let content = """
        [FieldGet (Some (ValueWithName (Tests+Tests, _)), ex1RegExp)])
        Call (None, reduceStep, [FieldGet (Some (Value (Tests+Tests))
        Call (None, OfList, [Call (None, reduceStep, [ValueWithName (Star Eps, r)])])
        Call (None, OfList, [Call (None, reduce, [FieldGet (Some (ValueWithName (Tests+Tests, _)), ex4RegExp), Call (None, FromOne, [])])])
        Call (None, OfList, [Call (None, reduce, [ValueWithName (Star Eps, r), ValueWithName (1N, n)])])
    """

    let print a b c = printfn $"a: %d{a}, b: %s{b}, c: %b{c}"
    let f = fun (a: int) (b: string) (c: bool) -> <@ print a b c @>
    let x = f 1 "Hallo" true
    printfn $"{x.ToString ()}"
    0