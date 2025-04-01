module Tests


open Implementation
open Microsoft.VisualStudio.TestTools.UnitTesting
open Swensen.Unquote



[<TestClass>]
type Tests () =
    [<TestMethod>]
    member _.``avg3 Example Tests`` (): unit =
        test <@ avg3 2N 5N 8N = 5N @>
        test <@ avg3 5N 2N 3N = 3N @>
        test <@ avg3 2N 5N 7N = 4N @>
        test <@ avg3 13N 6N 0N = 6N @>