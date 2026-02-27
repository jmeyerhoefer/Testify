module Tests.TernaryTests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.TernaryTypes



//TODO: Add to arbitrary modifier???
let rec removeLeadingZs (ns: List<Ternary>): List<Ternary> =
    match ns with
    | [] -> []
    | m::ms ->
        let rest = removeLeadingZs ms
        match rest with
        | [] -> if m = Z then [] else m::rest
        | _ -> m::rest


type ArbitraryModifier =
    inherit NatModifier

    // TODO: Does this work? 
    static member TernaryList (): Arbitrary<Ternary list> =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<Ternary list>
        |> FsCheck.FSharp.Arb.mapFilter removeLeadingZs (fun _ -> true)


[<TestClass>]
type TernaryTests () =
    let one   = [P]
    let two   = [M;P]
    let three = [Z;P]
    let minusone   = [M]
    let minustwo   = [P;M]
    let minusthree = [Z;M]

    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``a) bedeutung Beispiele`` (): unit =
        (?) <@ Student.Ternary.bedeutung one   = 1 @>
        (?) <@ Student.Ternary.bedeutung two   = 2 @>
        (?) <@ Student.Ternary.bedeutung three = 3 @>

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) zCons Beispiele`` (): unit =
        (?) <@ Student.Ternary.zCons [] = [] @>
        (?) <@ Student.Ternary.zCons [P] = [Z;P] @>
        (?) <@ Student.Ternary.zCons [P;Z;M] = [Z;P;Z;M] @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) inc Beispiele`` (): unit =
        (?) <@ Student.Ternary.inc [] = [P] @>
        (?) <@ Student.Ternary.inc [M] = [] @>
        (?) <@ Student.Ternary.inc [P] = [M;P] @>
        (?) <@ Student.Ternary.inc [M;P] = [Z;P] @>


    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``d) dec Beispiele`` (): unit =
        (?) <@ Student.Ternary.dec [] = [M] @>
        (?) <@ Student.Ternary.dec [M] = [P;M] @>
        (?) <@ Student.Ternary.dec [P] = [] @>
        (?) <@ Student.Ternary.dec [M;P] = [P] @>

    // ------------------------------------------------------------------------
    // c) + d)

    [<TestMethod; Timeout 5000>]
    member _.``c) + d) dec (inc n) = n Zufall`` (): unit =
        Checkify.Check (
            <@ fun (n: List<Ternary>) -> Student.Ternary.dec (Student.Ternary.inc n) = removeLeadingZs n @>,
            defaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``e) fromInt Beispiele`` (): unit =
        (?) <@ Student.Ternary.fromInt  1 = one   @>
        (?) <@ Student.Ternary.fromInt  2 = two   @>
        (?) <@ Student.Ternary.fromInt  3 = three @>
        (?) <@ Student.Ternary.fromInt -1 = [M]   @>

    // ------------------------------------------------------------------------
    // a) + e)

    [<TestMethod; Timeout 5000>]
    member _.``a) + e) bedeutung (fromInt n) = n Zufall`` (): unit =
        Checkify.Check (
            <@ fun (n: Int) -> Student.Ternary.bedeutung (Student.Ternary.fromInt n) = n @>,
            defaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // f)

    [<TestMethod; Timeout 1000>]
    member _.``f) add Beispiele`` (): unit =
        (?) <@ Student.Ternary.add one one = two @>
        (?) <@ Student.Ternary.add minustwo two = [] @>
        (?) <@ Student.Ternary.add minusthree two = minusone @>
        (?) <@ Student.Ternary.add three minusone = two @>

    [<TestMethod; Timeout 5000>]
    member _.``f) add Zufall (setzt voraus, dass fromInt funktioniert)`` (): unit =
        Checkify.Check (
            <@ fun (m: Int) (n: Int) -> Student.Ternary.add (Student.Ternary.fromInt m) (Student.Ternary.fromInt n) = Solution.Ternary.fromInt (m+n) @>,
            defaultConfig.WithEndSize 1000
        )

    [<TestMethod; Timeout 5000>]
    member _.``f) add kommutativ Zufall`` (): unit =
        Checkify.Check (
            <@ fun (m: List<Ternary>) (n: List<Ternary>) ->
                Student.Ternary.add (removeLeadingZs m) (removeLeadingZs n) = Solution.Ternary.add (removeLeadingZs n) (removeLeadingZs m) @>,
            defaultConfig.WithEndSize 1000
        )

    [<TestMethod; Timeout 5000>]
    member _.``f) add assoziativ Zufall`` (): unit =
        // TODO: find easier solution to removeLeadingZs problem. (let m = removeLeadingZs m) destroys output of expression.
        Checkify.Check (
            <@ fun (m: List<Ternary>) (n: List<Ternary>) (o: List<Ternary>) ->
                Student.Ternary.add (removeLeadingZs m) (Student.Ternary.add (removeLeadingZs n) (removeLeadingZs o)) =
                    Solution.Ternary.add (Solution.Ternary.add (removeLeadingZs m) (removeLeadingZs n)) (removeLeadingZs o) @>,
            defaultConfig.WithEndSize 1000
        )

    // ------------------------------------------------------------------------
    // g)

    [<TestMethod; Timeout 1000>]
    member _.``g) negative Beispiele`` (): unit =
        (?) <@ Student.Ternary.negative [] = [] @>
        (?) <@ Student.Ternary.negative one = minusone @>
        (?) <@ Student.Ternary.negative two = minustwo @>
        (?) <@ Student.Ternary.negative minusthree = three @>

    [<TestMethod; Timeout 5000>]
    member _.``g) negative (negative n) = n Zufall`` (): unit =
        Checkify.Check (
            <@ fun (n: List<Ternary>) -> Student.Ternary.negative (Student.Ternary.negative n) = removeLeadingZs n @>,
            defaultConfig.WithEndSize 1000
        )

    [<TestMethod; Timeout 5000>]
    member _.``g) add n (negative n) = [] Zufall (setzt voraus, dass add funktioniert)`` (): unit =
        Checkify.Check (
            <@ fun (n: List<Ternary>) -> Student.Ternary.add (removeLeadingZs n) (Student.Ternary.negative (removeLeadingZs n)) = [] @>,
            defaultConfig.WithEndSize 1000
        )
