module Tests


open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck
open Swensen.Unquote
open TestFramework


let addCorrect (a: int) (b: int): int =
    a + b


let addWrong (a: int) (b: int): int =
    a + b + 1


[<TestClass>]
type Tests () =
    let a: int = 1
    let b: int = 2

    [<TestMethod>] [<Timeout(1000)>]
    member _.``test Beispiel 1`` (): unit =
        AssertWrapper.AreEqual (
            expected = addCorrect a b,
            actual = addWrong a b,
            methodName = nameof addWrong,
            input = [| a; b |]
        )


// EOF