module Leibniz
open Mini

////a)
let rec quersumme (n: Nat): Nat =
    if n = 0N then 0N
    else (n % 10N) + quersumme (n / 10N)

////b)
let rec sortedDigits (n: Nat): Bool =
    if n = 0N then true
    else
        let firstDigitsSorted = sortedDigits (n / 10N)
        let lastDigit = n % 10N
        let secondLastDigit = (n / 10N) % 10N
        lastDigit >= secondLastDigit && firstDigitsSorted

////end)
