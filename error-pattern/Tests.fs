namespace Tests


open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck
open Swensen.Unquote


[<TestClass>]
type Tests() =
    [<TestMethod>] [<Timeout(1000)>]
    member this.test1 (): unit =
        Assert.IsTrue true


// EOF