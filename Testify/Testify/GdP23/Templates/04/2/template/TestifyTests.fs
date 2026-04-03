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
        let isPositive (year: Nat) : bool = 0N < year
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

        // ------------------------------------------------------------------------
        // e)

        // ------------------------------------------------------------------------
        // bonus)

        [< TestifyMethod; Timeout 60000 >]
        member _.``bonus) Beispiele`` () : unit =
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Tuesday} @>
            (?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 22N; Dates.weekday = Dates.Wednesday} @>
            (!?) <@ Dates.validateWeekday {Dates.year = 2023N; Dates.month = 11N; Dates.day = 21N; Dates.weekday = Dates.Thursday} @>

