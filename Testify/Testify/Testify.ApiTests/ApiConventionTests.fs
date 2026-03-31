module Testify.ApiTests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.AssertOperators
open Testify.CheckOperators

[<TestClass>]
type ApiConventionTests() =
    [<TestMethod>]
    member _.``Assert check is pipe-friendly``() : unit =
        let result =
            <@ 1 + 2 @>
            |> Assert.check (AssertExpectation.equalTo 3)

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(AssertResult.Passed, result)

    [<TestMethod>]
    member _.``Check check is pipe-friendly``() : unit =
        let result =
            <@ fun x -> x + 1 @>
            |> Check.check CheckExpectation.equalToReference (fun x -> x + 1)

        match result with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

    [<TestMethod>]
    member _.``Grouped equality check uses corrected argument order``() : unit =
        let result =
            <@ fun x y -> x + y @>
            |> Check.checkEqualGroupedUsing Arbitraries.from<int> (fun x y -> x + y)

        match result with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

    [<TestMethod>]
    member _.``Assert tap operator returns expression for chaining``() : unit =
        let expr =
            <@ 5 @>
            >>? AssertExpectation.greaterThan 0
            >>? AssertExpectation.lessThan 10

        expr |> Assert.should (AssertExpectation.equalTo 5)

    [<TestMethod>]
    member _.``Assert tap operator fails fast``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
            <@ 5 @>
            >>? AssertExpectation.equalTo 4
            |> ignore
        )
        |> ignore

    [<TestMethod>]
    member _.``Assert direct operator applies composed OR expectation``() : unit =
        <@ 5 @>
        |>? (AssertExpectation.equalTo 4 <|> AssertExpectation.equalTo 5)

    [<TestMethod>]
    member _.``Assert direct operator applies composed AND expectation``() : unit =
        <@ 5 @>
        |>? (AssertExpectation.greaterThan 0 <&> AssertExpectation.lessThan 10)

    [<TestMethod>]
    member _.``Assert any operator passes when one expectation succeeds``() : unit =
        <@ 5 @>
        ||? [ AssertExpectation.equalTo 4
              AssertExpectation.equalTo 5 ]

    [<TestMethod>]
    member _.``Assert any operator fails when all expectations fail``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
            <@ 5 @>
            ||? [ AssertExpectation.equalTo 3
                  AssertExpectation.equalTo 4 ]
        )
        |> ignore

    [<TestMethod>]
    member _.``Assert all operator passes when all expectations succeed``() : unit =
        <@ 5 @>
        &&? [ AssertExpectation.greaterThan 0
              AssertExpectation.lessThan 10 ]

    [<TestMethod>]
    member _.``Assert all operator fails when one expectation fails``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
            <@ 5 @>
            &&? [ AssertExpectation.greaterThan 0
                  AssertExpectation.equalTo 4 ]
        )
        |> ignore

    [<TestMethod>]
    member _.``Check tap operators return expression for chaining``() : unit =
        let expr =
            <@ fun x -> x + 1 @>
            |=>> (fun x -> x + 1)
            |=>>? (CheckExpectation.equalToReference, fun x -> x + 1)

        expr |> Check.shouldEqual (fun x -> x + 1)

    [<TestMethod>]
    member _.``Check general operator uses defaults``() : unit =
        <@ fun x -> x + 1 @>
        ||=>? (None, None, None, fun x -> x + 1)

    [<TestMethod>]
    member _.``Check general operator accepts explicit options``() : unit =
        let config = CheckConfig.withMaxTest 25
        let arbitrary = Arbitraries.from<int>
        let expectation = CheckExpectation.equalToReferenceWithDiff Diff.defaultOptions

        <@ fun x -> x + 1 @>
        ||=>? (Some config, Some arbitrary, Some expectation, fun x -> x + 1)

    [<TestMethod>]
    member _.``Check shouldBeTrue passes for always-true bool properties``() : unit =
        <@ fun (value: int) -> value = value @>
        |> Check.shouldBeTrue

    [<TestMethod>]
    member _.``Check shouldBeTrue fails when a generated case returns false``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ fun (_: int) -> false @>
                |> Check.shouldBeTrue
            )

        StringAssert.Contains(ex.Message, "Expected true but got false.")

    [<TestMethod>]
    member _.``Check shouldBeTrueUsing respects a custom arbitrary``() : unit =
        let arb =
            Arbitraries.from<unit>
            |> Arbitraries.convert (fun () -> 0) (fun _ -> ())

        <@ fun value -> value = 0 @>
        |> Check.shouldBeTrueUsing arb

    [<TestMethod>]
    member _.``Check shouldBeTrueWith accepts explicit config``() : unit =
        let config = CheckConfig.withMaxTest 5

        <@ fun (value: int) -> value = value @>
        |> Check.shouldBeTrueWith config

    [<TestMethod>]
    member _.``Check shouldBeFalse mirrors bool helper behavior``() : unit =
        <@ fun (_: int) -> false @>
        |> Check.shouldBeFalse

        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ fun (_: int) -> true @>
                |> Check.shouldBeFalse
            )

        StringAssert.Contains(ex.Message, "Expected false but got true.")

    [<TestMethod>]
    member _.``Check bool helpers return normal Check result shapes``() : unit =
        let passed =
            <@ fun (value: int) -> value = value @>
            |> Check.checkBeTrue

        let failed =
            <@ fun (_: int) -> false @>
            |> Check.checkBeTrue

        match passed with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

        match failed with
        | CheckResult.Failed _ -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Failed but got {other}")

    [<TestMethod>]
    member _.``Assert collector preserves results and aggregates failures``() : unit =
        let collector = Assert.Collect.create ()

        Assert.Collect.add collector (AssertExpectation.equalTo 1) <@ 2 @>
        |> ignore

        Assert.Collect.add collector (AssertExpectation.greaterThan 5) <@ 3 @>
        |> ignore

        let results = Assert.Collect.toResultList collector

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, results.Length)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(results |> List.forall (function | AssertResult.Failed _ -> true | _ -> false))

        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Assert.Collect.assertAll collector
            )

        StringAssert.Contains(ex.Message, "Collected 2 assertion failure(s).")

    [<TestMethod>]
    member _.``Check collector preserves results and aggregates failures``() : unit =
        let collector : Check.Collector<int, int, int> = Check.Collect.create ()

        Check.Collect.add collector CheckExpectation.equalToReference (fun x -> x + 1) <@ fun x -> x + 2 @>
        |> ignore

        Check.Collect.add collector CheckExpectation.equalToReference (fun x -> x + 3) <@ fun x -> x + 4 @>
        |> ignore

        let results = Check.Collect.toResultList collector

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, results.Length)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(results |> List.forall (function | CheckResult.Failed _ -> true | _ -> false))

        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Check.Collect.assertAll collector
            )

        StringAssert.Contains(ex.Message, "Collected 2 property failure(s).")
