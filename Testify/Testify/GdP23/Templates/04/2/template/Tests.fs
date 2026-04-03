namespace GdP23.S04.A2.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini


    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())


    [<TestClass>]
    type Tests() =
        let config =
            Config.QuickThrowOnFailure
                .WithEndSize(10000)
                .WithArbitrary [typeof<ArbitraryModifiers>]
        let configFor methodName = ReplayCatalog.applyReplay methodName config

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) nextWeekday Beispiele`` (): unit =
            test <@ Dates.nextWeekday Dates.Sunday = Dates.Monday @>
            test <@ Dates.nextWeekday Dates.Monday = Dates.Tuesday @>
            test <@ Dates.nextWeekday Dates.Tuesday = Dates.Wednesday @>
            test <@ Dates.nextWeekday Dates.Wednesday = Dates.Thursday @>
            test <@ Dates.nextWeekday Dates.Thursday = Dates.Friday @>
            test <@ Dates.nextWeekday Dates.Friday = Dates.Saturday @>
            test <@ Dates.nextWeekday Dates.Saturday = Dates.Sunday @>

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) isLeapYear Beispiele`` (): unit =
            test <@ Dates.isLeapYear 2000N = true @>
            test <@ Dates.isLeapYear 2001N = false @>
            test <@ Dates.isLeapYear 2002N = false @>
            test <@ Dates.isLeapYear 2003N = false @>
            test <@ Dates.isLeapYear 2004N = true @>
            test <@ Dates.isLeapYear 1900N = false @>
            test <@ Dates.isLeapYear 1600N = true @>
            test <@ Dates.isLeapYear 2008N = true @>
            test <@ Dates.isLeapYear 1901N = false @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) isLeapYear Zufallstest`` (): unit =
            Check.One(configFor "b) isLeapYear Zufallstest", fun (y: Nat) ->
                if y > 0N then
                    let expected = System.DateTime.IsLeapYear(int y)
                    let actual = Dates.isLeapYear y
                    Assert.AreEqual<bool>(expected, actual)
            )


        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) daysInMonth Beispiele`` (): unit =
            test <@ Dates.daysInMonth 2000N 1N = 31N @>
            test <@ Dates.daysInMonth 2000N 2N = 29N @>
            test <@ Dates.daysInMonth 2000N 3N = 31N @>
            test <@ Dates.daysInMonth 2000N 4N = 30N @>
            test <@ Dates.daysInMonth 2000N 9N = 30N @>
            test <@ Dates.daysInMonth 2000N 10N = 31N @>
            test <@ Dates.daysInMonth 2000N 11N = 30N @>
            test <@ Dates.daysInMonth 2000N 12N = 31N @>
            test <@ Dates.daysInMonth 2001N 1N = 31N @>
            test <@ Dates.daysInMonth 2001N 2N = 28N @>


        // ------------------------------------------------------------------------
        // d)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) nextDate Beispiele`` (): unit =
            test <@ Dates.nextDate ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday}) = {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @>
            test <@ Dates.nextDate ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday}) = {Dates.year = 2023N; Dates.month = 11N; Dates.day = 23N; Dates.weekday = Dates.Thursday} @>
            test <@ Dates.nextDate ({Dates.year = 2000N; Dates.month = 2N; Dates.day = 28N; Dates.weekday = Dates.Friday}) = {Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday} @>
            test <@ Dates.nextDate ({Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday}) = {Dates.year = 2000N; Dates.month = 3N; Dates.day = 1N; Dates.weekday = Dates.Sunday} @>
            test <@ Dates.nextDate ({Dates.year = 2024N; Dates.month = 2N; Dates.day = 9N; Dates.weekday = Dates.Friday}) = {Dates.year = 2024N; Dates.month = 2N; Dates.day = 10N; Dates.weekday = Dates.Saturday} @>


        // ------------------------------------------------------------------------
        // e)

        // ------------------------------------------------------------------------
        // bonus)

        [<TestMethod>] [<Timeout(60000)>]
        member this.``bonus) Beispiele`` (): unit =
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday}) = true @>
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday}) = true @>
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Thursday}) = false @>

