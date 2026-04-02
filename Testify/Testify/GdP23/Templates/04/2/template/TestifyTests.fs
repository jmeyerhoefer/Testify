namespace GdP23.S04.A2.Template

module TestifyTests =
    open Mini
    open Testify
    open Testify.ArbitraryOperators
    open Testify.AssertOperators
    open Testify.CheckOperators

    [< TestifyClass  >]
    type TestifyTests () =
        let config =
            CheckConfig.defaultConfig
                .WithEndSize(10000)
        let configFor methodName = ReplayCatalog.applyReplay methodName config
        let isValidDay (day: Nat) : bool = 0N < day && day <= 31N
        let isValidMonth (month: Nat) : bool = 0N < month && month <= 12N
        let isPositive (year: Nat) : bool = 0N < year
        let isValidDate (date: Dates.Date) : bool =
            isValidDay date.day && isValidMonth date.month && isPositive date.year
        let rawDate =
             Arbitraries.fromConfig<Dates.Date> config
        let rawNat =
             Arbitraries.fromConfig<Nat> config
        let posNat =
             rawNat
             |> Arbitraries.filter isPositive

        // ------------------------------------------------------------------------
        // a)

        [< TestifyMethod; Timeout 1000 >]
        member _.``a) nextWeekday Beispiele`` () : unit =
            <@ Dates.nextWeekday Dates.Sunday @> =? Dates.Monday
            <@ Dates.nextWeekday Dates.Monday @> =? Dates.Tuesday
            <@ Dates.nextWeekday Dates.Tuesday @> =? Dates.Wednesday
            <@ Dates.nextWeekday Dates.Wednesday @> =? Dates.Thursday
            <@ Dates.nextWeekday Dates.Thursday @> =? Dates.Friday
            <@ Dates.nextWeekday Dates.Friday @> =? Dates.Saturday
            <@ Dates.nextWeekday Dates.Saturday @> =? Dates.Sunday

        // ------------------------------------------------------------------------
        // b)
        [< TestifyMethod; Timeout 1000 >]
        member _.``b) isLeapYear Beispiele`` () : unit =
            (?) <@ Dates.isLeapYear 2000N @>
            (!?) <@ Dates.isLeapYear 2001N @>
            (!?) <@ Dates.isLeapYear 2002N @>
            (!?) <@ Dates.isLeapYear 2003N @>
            (?) <@ Dates.isLeapYear 2004N @>
            (!?) <@ Dates.isLeapYear 1900N @>
            (?) <@ Dates.isLeapYear 1600N @>
            (?) <@ Dates.isLeapYear 2008N @>
            (!?) <@ Dates.isLeapYear 1901N @>

        [< TestifyMethod; Timeout 1000 >]
        member _.``b) isLeapYear Zufallstest`` () : unit =
            <@ fun (y: Nat) -> Dates.isLeapYear y @>
            ||=>? (Some (configFor "b) isLeapYear Zufallstest"), Some posNat, None, fun y -> System.DateTime.IsLeapYear (int y))


        // ------------------------------------------------------------------------
        // c)

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) daysInMonth Beispiele`` () : unit =
            <@ Dates.daysInMonth 2000N 1N @> =? 31N
            <@ Dates.daysInMonth 2000N 2N @> =? 29N
            <@ Dates.daysInMonth 2000N 3N @> =? 31N
            <@ Dates.daysInMonth 2000N 4N @> =? 30N
            <@ Dates.daysInMonth 2000N 9N @> =? 30N
            <@ Dates.daysInMonth 2000N 10N @> =? 31N
            <@ Dates.daysInMonth 2000N 11N @> =? 30N
            <@ Dates.daysInMonth 2000N 12N @> =? 31N
            <@ Dates.daysInMonth 2001N 1N @> =? 31N
            <@ Dates.daysInMonth 2001N 2N @> =? 28N

        [< TestifyMethod; Timeout 1000 >]
        member _.``c) daysInMonth Zufallstest`` () : unit =
            Check.shouldBeTrueUsingWith
                (configFor "c) daysInMonth Zufallstest")
                (rawNat <.> rawNat)
                <@ fun (y: Nat, m: Nat) ->
                    if y > 0N && m > 0N && m <= 12N then
                        Dates.daysInMonth y m = Nat.Make (System.DateTime.DaysInMonth(int y, int m))
                    else
                        true @>


        // ------------------------------------------------------------------------
        // d)

        [< TestifyMethod; Timeout 1000 >]
        member _.``d) nextDate Beispiele`` () : unit =
            <@ Dates.nextDate { Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday} @> =?
                {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday}
            <@ Dates.nextDate {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @> =?
                {Dates.year = 2023N; Dates.month = 11N; Dates.day = 23N; Dates.weekday = Dates.Thursday}
            <@ Dates.nextDate {Dates.year = 2000N; Dates.month = 2N; Dates.day = 28N; Dates.weekday = Dates.Friday} @> =?
                {Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday}
            <@ Dates.nextDate {Dates.year = 2000N; Dates.month = 2N; Dates.day = 29N; Dates.weekday = Dates.Saturday} @> =?
                {Dates.year = 2000N; Dates.month = 3N; Dates.day = 1N; Dates.weekday = Dates.Sunday}
            <@ Dates.nextDate {Dates.year = 2024N; Dates.month = 2N; Dates.day = 9N; Dates.weekday = Dates.Friday} @> =?
                {Dates.year = 2024N; Dates.month = 2N; Dates.day = 10N; Dates.weekday = Dates.Saturday}

        // TODO
        [< TestifyMethod; Timeout 1000 >]
        member _.``d) nextDate Zufallstest`` () : unit =
            Check.shouldBeTrueUsingWith
                (configFor "d) nextDate Zufallstest")
                rawDate
                <@ fun (date: Dates.Date) ->
                    if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                        let next = System.DateTime(int date.year, int date.month, int date.day).AddDays(1)
                        let expected =
                            {
                                Dates.year = Nat.Make next.Year
                                Dates.month = Nat.Make next.Month
                                Dates.day = Nat.Make next.Day
                                Dates.weekday = Dates.nextWeekday date.weekday
                            }

                        Dates.nextDate date = expected
                    else
                        true @>

        // ------------------------------------------------------------------------
        // e)

        [< TestifyMethod; Timeout 1000 >]
        member _.``e) nextDate Zufallstest`` () : unit =
            Check.shouldBeTrueUsingWith
                (configFor "e) nextDate Zufallstest")
                (rawDate <.> rawNat)
                <@ fun (date: Dates.Date, n: Nat) ->
                    if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                        let next = System.DateTime(int date.year, int date.month, int date.day).AddDays(int n)
                        let expectedWeekday =
                            let steps = (int n) % 7
                            Seq.init steps (fun _ -> Dates.nextWeekday)
                            |> Seq.fold (fun weekday nextWeekday -> nextWeekday weekday) date.weekday

                        let expected =
                            {
                                Dates.year = Nat.Make next.Year
                                Dates.month = Nat.Make next.Month
                                Dates.day = Nat.Make next.Day
                                Dates.weekday = expectedWeekday
                            }

                        Dates.nextDateN date n = expected
                    else
                        true @>


        // ------------------------------------------------------------------------
        // bonus)

        [< TestifyMethod; Timeout 60000 >]
        member _.``bonus) Beispiele`` () : unit =
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday} @>
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @>
            (!?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Thursday} @>

        // TODO
        [< TestifyMethod; Timeout 60000 >]
        member _.``bonus) validateWeekday Zufallstest`` () : unit =
            Check.shouldBeTrueUsingWith
                (configFor "bonus) validateWeekday Zufallstest")
                rawDate
                <@ fun (date: Dates.Date) ->
                    if date.year > 0N && date.month > 0N && date.month <= 12N && date.day > 0N && date.day <= 31N then
                        let dt = System.DateTime (int date.year, int date.month, int date.day)
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

                        Dates.validateWeekday date = (expectedWeekday = date.weekday)
                    else
                        true @>

