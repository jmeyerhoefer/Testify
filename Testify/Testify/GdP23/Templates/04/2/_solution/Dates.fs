module Dates
open Mini

////type)
type Weekday = | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
type Date = { year: Nat; month: Nat; day: Nat; weekday: Weekday }

////defs)
let christmas = { year = 2023N ; month = 12N ; day = 25N; weekday = Monday }
let newyear = { year = 2024N ; month = 1N ; day = 1N; weekday = Monday }

////a)
let nextWeekday (d: Weekday): Weekday =
    match d with
    | Monday -> Tuesday
    | Tuesday -> Wednesday
    | Wednesday -> Thursday
    | Thursday -> Friday
    | Friday -> Saturday
    | Saturday -> Sunday
    | Sunday -> Monday

////b)
let isLeapYear (y: Nat): Bool =
    (y % 4N = 0N) && (y % 100N <> 0N || y % 400N = 0N)

////c)
let daysInMonth (year: Nat) (month: Nat): Nat =
    if month = 2N then if isLeapYear year then 29N else 28N
    elif month = 4N || month = 6N || month = 9N || month = 11N then 30N
    else 31N

////d)
let nextDate (d: Date): Date =
    let nextDay = nextWeekday d.weekday
    let nextDayNum = d.day + 1N
    if nextDayNum > daysInMonth d.year d.month then
        if d.month = 12N then
            { year = d.year + 1N; month = 1N; day = 1N; weekday = nextDay }
        else
            { year = d.year; month = d.month + 1N; day = 1N; weekday = nextDay }
    else
        { year = d.year; month = d.month; day = nextDayNum; weekday = nextDay }

////e)
let rec nextDateN (d: Date) (n: Nat): Date =
    if n = 0N then d
    else nextDateN (nextDate d) (n - 1N)

////bonus)
let rec numberToWeekday (x: Nat): Weekday =
    if x = 0N then Sunday
    elif x = 1N then Monday
    elif x = 2N then Tuesday
    elif x = 3N then Wednesday
    elif x = 4N then Thursday
    elif x = 5N then Friday
    elif x = 6N then Saturday
    else numberToWeekday (x % 7N)

let monthFactor (d: Date): Nat =
    let m = d.month
    if m = 3N then 2N
    elif m = 4N then 5N
    elif m = 5N then 0N
    elif m = 6N then 3N
    elif m = 7N then 5N
    elif m = 8N then 1N
    elif m = 9N then 4N
    elif m = 10N then 6N
    elif m = 11N then 2N
    elif m = 12N then 4N
    elif m = 1N then 0N
    elif m = 2N then 3N
    else 0N

let firstDigitsOfYear (d: Date): Nat =
    if d.month <= 2N then (d.year - 1N)/100N else d.year/100N

let lastDigitsOfYear (d: Date): Nat =
    if d.month <= 2N then (d.year - 1N)%100N else d.year%100N

let validateWeekday (d: Date): Bool =
    let y = lastDigitsOfYear d
    let c = firstDigitsOfYear d
    let m = monthFactor d
    let actualWdAsNumber = d.day + m + y + y / 4N + c / 4N - 2N*c
    let actualWeekday = numberToWeekday (actualWdAsNumber % 7N)
    actualWeekday = d.weekday

////end)
