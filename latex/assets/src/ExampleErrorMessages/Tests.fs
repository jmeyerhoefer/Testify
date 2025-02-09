module Tests


open Assertify
open FsCheck
open StudentSubmission
open Microsoft.VisualStudio.TestTools.UnitTesting
open Swensen.Unquote


////digitSumSolutionBegin)
let rec digitSumSolution (n: Nat): Nat =
    if n = 0N then
        0N
    else
        (n % 10N) + digitSumSolution (n / 10N) ////digitSumSolutionEnd)


let private digitSum' (n: Nat): Nat =
    n.ToString ()
    |> Seq.sumBy (fun (chr: char) -> int chr - int '0')
    |> Nat.Make


////sortedDigitsSolutionBegin)
let rec sortedDigitsSolution (n: Nat): bool =
    if n = 0N then
        true
    else
        let frontSorted: bool = sortedDigitsSolution (n / 10N)
        let lastDigit: Nat = n % 10N
        let secondLastDigit: Nat = (n / 10N) % 10N
        frontSorted && secondLastDigit <= lastDigit ////sortedDigitsSolutionEnd)


let private sortedDigits' (n: Nat): bool =
    n.ToString ()
    |> Seq.map (fun (chr: char) -> int chr - int '0')
    |> Seq.pairwise
    |> Seq.forall (fun (a: int, b: int) -> a <= b)


type ArbitraryModifiers =
    static member Nat (): Arbitrary<Nat> =
        Arb.from<bigint>
        |> Arb.filter (fun i -> i >= 0I)
        |> Arb.convert Nat.Make _.ToBigInteger()


[<TestClass>]
type Tests () =
    do Arb.register<ArbitraryModifiers> () |> ignore

    ////digitSumExamplesBegin)
    [<TestMethod>] [<Timeout(1000)>]
    member _.``a) digitSum Examples`` (): unit =
        test <@ digitSum 123N  = 6N @>
        test <@ digitSum 1234N = 10N @>
        test <@ digitSum 42N   = 6N @>
        test <@ digitSum 105N  = 6N @>
        test <@ digitSum 0N    = 0N @>
        test <@ digitSum 4711N = 13N @> ////digitSumExamplesEnd)

    ////digitSumRandomBegin)
    [<TestMethod>] [<Timeout(5000)>]
    member _.``a) digitSum Random`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with EndSize = 1000 },
            fun (n: Nat) -> Assert.AreEqual<Nat> (
                digitSumSolution n, digitSum n
            )
        ) ////digitSumRandomEnd)

    ////sortedDigitsExamplesBegin)
    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) sortedDigits Examples`` (): unit =
        test <@ sortedDigits 0N    = true @>
        test <@ sortedDigits 5N    = true @>
        test <@ sortedDigits 159N  = true @>
        test <@ sortedDigits 1111N = true @>
        test <@ sortedDigits 42N   = false @>
        test <@ sortedDigits 543N  = false @>
        test <@ sortedDigits 1101N = false @> ////sortedDigitsExamplesEnd)

    ////sortedDigitsRandomBegin)
    [<TestMethod>] [<Timeout(5000)>]
    member _.``b) sortedDigits Random`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with EndSize = 1000 },
            fun (n: Nat) -> Assert.AreEqual<bool> (
                sortedDigitsSolution n, sortedDigits n
            )
        ) ////sortedDigitsRandomEnd)

    ////digitSumExamplesWithBegin)
    [<TestMethod>] [<Timeout(1000)>]
    member _.``a) digitSum Examples (with)`` (): unit =
        (?) <@ digitSum 123N  = 6N @>
        (?) <@ digitSum 1234N = 10N @>
        (?) <@ digitSum 42N   = 6N @>
        (?) <@ digitSum 105N  = 6N @>
        (?) <@ digitSum 0N    = 0N @>
        (?) <@ digitSum 4711N = 13N @> ////digitSumExamplesWithEnd)

    ////digitSumRandomWithBegin)
    [<TestMethod>] [<Timeout(5000)>]
    member _.``a) digitSum Random (with)`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with EndSize = 1000 },
            fun (n: Nat) -> (?) <@ digitSum n = digitSumSolution n @>
        ) ////digitSumRandomWithEnd)

    ////sortedDigitsExamplesWithBegin)
    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) sortedDigits Examples (with)`` (): unit =
        (?) <@ sortedDigits 0N    = true @>
        (?) <@ sortedDigits 5N    = true @>
        (?) <@ sortedDigits 159N  = true @>
        (?) <@ sortedDigits 1111N = true @>
        (?) <@ sortedDigits 42N   = false @>
        (?) <@ sortedDigits 543N  = false @>
        (?) <@ sortedDigits 1101N = false @> ////sortedDigitsExamplesWithEnd)

    ////sortedDigitsRandomWithBegin)
    [<TestMethod>] [<Timeout(5000)>]
    member _.``b) sortedDigits Random (with)`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with EndSize = 1000 },
            fun (n: Nat) -> (?) <@ sortedDigits n = sortedDigitsSolution n @>
        ) ////sortedDigitsRandomWithEnd)
