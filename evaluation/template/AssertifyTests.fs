module AssertifyTests


open Assertify
open Implementation


[<TestClass>]
type Tests () =
    do Assertify.ShowHistory <- true
    do Assertify.ShowReductions <- true

    [<TestMethod>]
    member _.test1 (): unit =
        (?) <@ 1 = 2 @>