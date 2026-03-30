module Program

open Testify.AssertOperators

[<EntryPoint>]
let main (args: string array) : int =
    <@ 1 + 2 @> =? 4
    0