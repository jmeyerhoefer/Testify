namespace GdP23.S04.A2.Template

module TestifyTests =
    open Mini
    open Testify
    open Testify.ArbitraryOperators
    open Testify.AssertOperators
    open Testify.CheckOperators


    [< TestifyClass  >]
    type TestifyTests () =
        let config = CheckConfig.defaultConfig.WithEndSize 10000
        let isValidDay (day: Nat) : bool = 0N < day && day <= 31N
        let isValidMonth (month: Nat) : bool = 0N < month && month <= 12N
        let isPositive (year: Nat) : bool = 0N < year
        let isValidDate (date: Dates.Date) : bool =
            isValidDay date.day && isValidMonth date.month && isPositive date.year
        let validDate =
             Arbitraries.from<Dates.Date>
             |> Arbitraries.filter isValidDate
        let posNat =
             Arbitraries.from<Nat>
             |> Arbitraries.filter isPositive
        let toDate (date: System.DateTime) : Dates.Date =
            {
                year = Nat.Make date.Year
                month = Nat.Make date.Month
                day = Nat.Make date.Day
                weekday =
                    match date.DayOfWeek with
                    | System.DayOfWeek.Monday -> Dates.Monday
                    | System.DayOfWeek.Tuesday -> Dates.Tuesday
                    | System.DayOfWeek.Wednesday -> Dates.Wednesday
                    | System.DayOfWeek.Thursday -> Dates.Thursday
                    | System.DayOfWeek.Friday -> Dates.Friday
                    | System.DayOfWeek.Saturday -> Dates.Saturday
                    | System.DayOfWeek.Sunday -> Dates.Sunday
                    | unknown -> failwith $"Unknown weekday: {unknown}"
            }

        // ------------------------------------------------------------------------
        // a)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert a) nextWeekday Beispiele`` () : unit =
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
        member _.``#testify-assert b) isLeapYear Beispiele`` () : unit =
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
        member _.``#testify-check b) isLeapYear Zufallstest`` () : unit =
            <@ fun (y: Nat) -> Dates.isLeapYear y @>
            ||=>? (Some config, Some posNat, None, fun y -> System.DateTime.IsLeapYear (int y))


        // ------------------------------------------------------------------------
        // c)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert c) daysInMonth Beispiele`` () : unit =
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
        member _.``#testify-check c) daysInMonth Zufallstest`` () : unit =
            let validYearAndMonth =
                posNat <.> (Arbitraries.from<Nat> |> Arbitraries.filter isValidMonth)

            <@ fun (y: Nat, m: Nat) -> Dates.daysInMonth y m @>
            ||=>? (Some config, Some validYearAndMonth, None, fun (y, m) ->
                   System.DateTime.DaysInMonth (int y, int m) |> Nat.Make)


        // ------------------------------------------------------------------------
        // d)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-assert d) nextDate Beispiele`` () : unit =
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
        member _.``??? #testify-check d) nextDate Zufallstest`` () : unit =
            <@ fun (date: Dates.Date) -> Dates.nextDate date @>
            ||=>? (Some config, Some validDate, None, fun d ->
                System.DateTime (int d.year, int d.month, int d.day)
                |> _.AddDays(1)
                |> toDate)

        // ------------------------------------------------------------------------
        // e)

        [< TestifyMethod; Timeout 1000 >]
        member _.``#testify-check e) nextDate Zufallstest`` () : unit =
            let applyNTimes f n x = Seq.init n (fun _ -> f) |> Seq.fold (fun acc fn -> fn acc) x
            <@ fun (date: Dates.Date, n: Nat) -> Dates.nextDateN date n @>
            ||=>? (Some config, Some (validDate <.> posNat), None, fun (d, n) ->
                System.DateTime (int d.year, int d.month, int d.day)
                |> _.AddDays(int n)
                |> toDate)

        // ------------------------------------------------------------------------
        // bonus)

        [< TestifyMethod; Timeout 60000 >]
        member _.``#testify-assert bonus) Beispiele`` () : unit =
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday} @>
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @>
            (!?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Thursday} @>

        [< TestifyMethod; Timeout 60000 >]
        member _.``#testify-check bonus) validateWeekday Zufallstest`` () : unit =
            let validDateIncludingWeekdate =
                validDate TODO
            <@ fun (date: Dates.Date) -> Dates.validateWeekday date @>
            |> Check.shouldBeTrueUsingWith config validDateIncludingWeekdate

