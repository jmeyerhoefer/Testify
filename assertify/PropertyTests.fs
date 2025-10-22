module PropertyTests


open FsCheck.FSharp
open Microsoft.VisualStudio.TestTools.UnitTesting
open Mini
open Types


open AssertifyJSON
open FsCheck


type SimpleRegExp<'a> =
    | SEps
    | SLit of 'a
    | SCat of SimpleRegExp<'a> * SimpleRegExp<'a>
    | SEmpty
    | SOr of SimpleRegExp<'a> * SimpleRegExp<'a>
    | SStar of SimpleRegExp<'a>

;
let rec private toRegExp<'a> (r: SimpleRegExp<'a>): RegExp<'a> =
    match r with
    | SEmpty -> Empty
    | SEps -> Eps
    | SLit a -> Lit a
    | SCat (r1, r2) -> Cat (toRegExp r1, toRegExp r2)
    | SOr (r1, r2) -> Or (toRegExp r1, toRegExp r2)
    | SStar r1 -> Star (toRegExp r1)


let rec private isWord<'a> (r: RegExp<'a>): 'a list option =
    match r with
    | Eps -> Some []
    | Lit a -> Some [ a ]
    | Cat (r1, r2) -> 
        match isWord r1, isWord r2 with
        | Some l1, Some l2 -> Some (l1 @ l2)
        | _ -> None
    | _ -> None


[<TestClass>]
type Tests () =
    do Assertify.ShowHistory <- true
    do Assertify.ShowReductions <- true

    [<TestMethod; Timeout(10000)>]
    member _.``add Beispiele`` (): unit =
        // Swensen.Unquote.Assertions.test <@ Reduktionssemantik.isWord Eps = isWord Empty @>
        (?) <@ Reduktionssemantik.isWord Eps = isWord Empty @>
        (?) <@ Reduktionssemantik.isWord Empty = isWord Empty @>

    [<TestMethod; Timeout(10000)>]
    member _.``add Zufall`` (): unit =
        Assertify.CheckProperty (
            fun (r: SimpleRegExp<char>, n: Nat) ->
                let rr: RegExp<char> = toRegExp r
                <@ Reduktionssemantik.isWord rr = isWord rr @>
            , Config.QuickThrowOnFailure.WithMaxTest 1000
        )

    [<TestMethod; Timeout(10000)>]
    member _.``shrinking Example`` (): unit =
        Assertify.CheckProperty (
            fun (s: string) -> <@ s.Length = 5 @>
            , Config.QuickThrowOnFailure.WithMaxTest 1000
        )