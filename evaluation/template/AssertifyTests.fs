module AssertifyTests


open Assertify
open Implementation


[<TestClass>]
type Tests () =
    do Assertify.ShowHistory <- true
    do Assertify.ShowReductions <- true

    [<TestMethod>]
    member _.test1 (): unit =
        (?) <@ avg3 2N 5N 8N = 5N @>
        (?) <@ avg3 5N 2N 3N = 3N @>
        (?) <@ avg3 2N 5N 7N = 4N @>
        (?) <@ avg3 13N 6N 0N = 6N @>