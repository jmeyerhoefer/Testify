namespace GdP23.S08.A3.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open System.Text.RegularExpressions
    open FsCheck
    open Mini
    open Types

    [<TestClass>]
    type Tests() =
        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Beispiel 1`` (): unit =
            Assert.IsTrue(RegExp.accept [B;A])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Beispiel 2`` (): unit =
            Assert.IsTrue(RegExp.accept [B;A;B;A;B;A;B;A;B;A])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Beispiel 3`` (): unit =
            Assert.IsTrue(RegExp.accept [A;B;A;B;A;B])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Beispiel 4`` (): unit =
            Assert.IsTrue(RegExp.accept [A;B])


        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 1`` (): unit =
            Assert.IsFalse(RegExp.accept [])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 2`` (): unit =
            Assert.IsFalse(RegExp.accept [A;A])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 3`` (): unit =
            Assert.IsFalse(RegExp.accept [B;B])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 4`` (): unit =
            Assert.IsFalse(RegExp.accept [B])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 5`` (): unit =
            Assert.IsFalse(RegExp.accept [A])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 6`` (): unit =
            Assert.IsFalse(RegExp.accept [A;B;A])

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) accept Gegenbeispiel 7`` (): unit =
            Assert.IsFalse(RegExp.accept [B;A;B;A;B;A;B;A;B;A;B;A;B])


        [<TestMethod>] [<Timeout(10000)>]
        member this.``c) accept Zufall`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 100}, fun (input: Alphabet list) ->
                let rec toString (acc: String) (xs: Alphabet list): String =
                    match xs with
                    | [] -> acc
                    | A::rest -> toString (acc + "a") rest
                    | B::rest -> toString (acc + "b") rest
                let inputStr = toString "" input
                let m = Regex.Match(inputStr, "ab(ab)*|ba(ba)*")
                Assert.AreEqual(m.Success && m.Value = inputStr, RegExp.accept input)
            )

