namespace GdP23.S04.A2.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini


    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())


    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

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
            Check.One({Config.QuickThrowOnFailure with EndSize = 10000}, fun (y: Nat) ->
                if y > 0N then
                    let expected = System.DateTime.IsLeapYear(int y)
                    let actual = Dates.isLeapYear y
                    Assert.AreEqual(expected, actual)
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

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) daysInMonth Zufallstest`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 10000}, fun (y: Nat) (m: Nat) ->
                if y > 0N && m > 0N && m <= 12N then
                    let expected = System.DateTime.DaysInMonth(int y, int m)
                    let actual = Dates.daysInMonth y m
                    Assert.AreEqual(expected, (int actual))
            )
            

        // ------------------------------------------------------------------------
        // d)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) nextDate Beispiele`` (): unit =
            test <@ Dates.nextDate ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday}) = {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @>
            test <@ Dates.nextDate ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday}) = {Dates.year = 2023N; Dates.month = 11N; Dates.day = 23N; Dates.weekday = Dates.Thursday} @>
            test <@ Dates.nextDate ({Dates.year = 2000N; Dates.month = 2N; Dates.day = 28N; Dates.weekday = Dates.Friday}) = {Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday} @>
            test <@ Dates.nextDate ({Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday}) = {Dates.year = 2000N; Dates.month = 3N; Dates.day = 1N; Dates.weekday = Dates.Sunday} @>
            test <@ Dates.nextDate ({Dates.year = 2024N; Dates.month = 2N; Dates.day = 9N; Dates.weekday = Dates.Friday}) = {Dates.year = 2024N; Dates.month = 2N; Dates.day = 10N; Dates.weekday = Dates.Saturday} @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) nextDate Zufallstest`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 10000}, fun (date: Dates.Date) ->
                 if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                      let dt = System.DateTime((int date.year), (int date.month), (int date.day))
                      let expected = dt.AddDays(1)
                      let actual = Dates.nextDate date
                      Assert.AreEqual(expected.Year, (int actual.year))
                      Assert.AreEqual(expected.Month, (int actual.month))
                      Assert.AreEqual(expected.Day, (int actual.day))
                      Assert.AreEqual(actual.weekday, Dates.nextWeekday date.weekday)
            )


        // ------------------------------------------------------------------------
        // e)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``e) nextDate Zufallstest`` (): unit =
            let applyNTimes f n x = Seq.init n (fun _ -> f) |> Seq.fold (fun acc fn -> fn acc) x
            Check.One({Config.QuickThrowOnFailure with EndSize = 10000}, fun (date: Dates.Date) (n: Nat) ->
                 if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                      let dt = System.DateTime((int date.year), (int date.month), (int date.day))
                      let expected = dt.AddDays((int n))
                      let actual = Dates.nextDateN date n
                      Assert.AreEqual(expected.Year, (int actual.year))
                      Assert.AreEqual(expected.Month, (int actual.month))
                      Assert.AreEqual(expected.Day, (int actual.day))
                      Assert.AreEqual(actual.weekday, applyNTimes Dates.nextWeekday (int n) date.weekday)
            )

        // ------------------------------------------------------------------------
        // bonus)

        [<TestMethod>] [<Timeout(60000)>]
        member this.``bonus) Beispiele`` (): unit =
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday}) = true @>
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday}) = true @>
            test <@ Dates.validateWeekday ({Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Thursday}) = false @>
            
        
        [<TestMethod>] [<Timeout(60000)>]
        member this.``bonus) validateWeekday Zufallstest`` (): unit =
            Check.One({Config.QuickThrowOnFailure with EndSize = 10000}, fun (date: Dates.Date) ->
                 if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                      let dt = System.DateTime((int date.year), (int date.month), (int date.day))
                      let expectedWeekday = 
                          match dt.DayOfWeek with
                          | System.DayOfWeek.Sunday -> Dates.Sunday
                          | System.DayOfWeek.Monday -> Dates.Monday
                          | System.DayOfWeek.Tuesday -> Dates.Tuesday
                          | System.DayOfWeek.Wednesday -> Dates.Wednesday
                          | System.DayOfWeek.Thursday -> Dates.Thursday
                          | System.DayOfWeek.Friday -> Dates.Friday
                          | System.DayOfWeek.Saturday -> Dates.Saturday
                          | _ -> failwithf "Unexpected DayOfWeek value: %A" dt.DayOfWeek
                      Assert.AreEqual((expectedWeekday = date.weekday), Dates.validateWeekday date)
            )

