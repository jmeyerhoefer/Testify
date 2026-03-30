namespace GdP23.S04.A2.Template

module Dates =
    open Mini

    ////type)
    type Weekday = | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    type Date = { year: Nat; month: Nat; day: Nat; weekday: Weekday }

    ////defs)
    let christmas = { year = 2023N ; month = 12N ; day = 25N; weekday = Monday }
    let newyear = { year = 2024N ; month = 1N ; day = 1N; weekday = Monday }

    ////a)
    let nextWeekday (d: Weekday): Weekday =
        failwith "TODO"

    ////b)
    let isLeapYear (y: Nat): Bool =
        failwith "TODO"

    ////c)
    let daysInMonth (year: Nat) (month: Nat): Nat =
        failwith "TODO"

    ////d)
    let nextDate (d: Date): Date =
        failwith "TODO"

    ////e)
    let rec nextDateN (d: Date) (n: Nat): Date =
        failwith "TODO"

    ////bonus)
    let validateWeekday (d: Date): Bool =
        failwith "TODO"

    ////end)

