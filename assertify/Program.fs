module Program


open System
open Microsoft.FSharp.Quotations
open Swensen.Unquote
open Types


[<EntryPoint>]
let main (_: string array): int =
    let addCorrect (a: int) (b: int): int = a + b
    let addWrong (a: int) (b: int): int = a + b + 1

    let expr1 = <@ addCorrect 1 2 @>
    let expr2 = <@ addWrong 1 2 @>

    let expr3 = <@ (%%expr1: int) = (%%expr2: int) @>
    printfn $"{expr3.Decompile ()}"
    0