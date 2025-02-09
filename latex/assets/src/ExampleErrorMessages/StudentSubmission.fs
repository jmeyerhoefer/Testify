module StudentSubmission


////digitSumStudentBegin)
let rec digitSum (n: Nat): Nat =
    if n = 0N then
        1N
    else
        (n % 10N) + digitSum (n / 10N) ////digitSumStudentEnd)


////sortedDigitsStudentBegin)
let rec sortedDigits (n: Nat): bool =
    if n = 0N then
        true
    else
        let frontSorted: bool = sortedDigits (n / 10N)
        let lastDigit: Nat = n % 10N
        let secondLastDigit: Nat = (n / 10N) % 10N
        frontSorted && secondLastDigit < lastDigit ////sortedDigitsStudentEnd)
