module Program


open System
open Microsoft.FSharp.Quotations
open Swensen.Unquote
open Types


[<EntryPoint>]
let main (_: string array): int =
    printfn "Hello, World!"
    0