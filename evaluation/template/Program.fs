module Program


open Implementation


[<EntryPoint>]
let main (args: string array): int =
    let num: Nat = queryNat "Test"
    printfn $"%A{num}"
    0