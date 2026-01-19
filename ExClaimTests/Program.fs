module Calculus.Program


[<EntryPoint>]
let main (_args: string array): int =
    [
        "1"
        "x"
    ]
    |> List.map Tests.Parser.parse
    |> List.iter (printfn "%A")
    0