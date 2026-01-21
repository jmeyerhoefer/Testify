module Calculus.Program

open Calculus
open Calculus.Tests
open Calculus.Types


[<EntryPoint>]
let main (_args: string array): int =
    let f = Comp (Id, Id)
    let actual = apply f 0N
    let expected = (FunctionExpr.FromFunction f).Apply 0N
    printfn $"actual: %A{actual}\nexpected: %A{expected}"
    0