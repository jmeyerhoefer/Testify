module Testify.ApiTests

open System
open System.IO
open System.Xml.Linq
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
    member _.``Check composed AND expectation via operator matches named composition``() : unit =
        let viaOperator =
            <@ fun x -> x + 1 @>
            |> Check.check (CheckExpectation.equalToReference <&> CheckExpectation.equalToReference) (fun x -> x + 1)

        let viaNamed =
            <@ fun x -> x + 1 @>
            |> Check.check (CheckExpectation.andAlso CheckExpectation.equalToReference CheckExpectation.equalToReference) (fun x -> x + 1)

        match viaOperator, viaNamed with
        | CheckResult.Passed, CheckResult.Passed -> ()
        | operatorResult, namedResult ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                $"Expected both composed checks to pass but got operator={operatorResult}, named={namedResult}")

    [<TestMethod>]
    member _.``Check composed OR expectation via operator matches named composition``() : unit =
        let viaOperator =
            <@ fun x -> x + 1 @>
            |> Check.check (CheckExpectation.equalToReference <|> CheckExpectation.equalToReference) (fun x -> x + 1)

        let viaNamed =
            <@ fun x -> x + 1 @>
            |> Check.check (CheckExpectation.orElse CheckExpectation.equalToReference CheckExpectation.equalToReference) (fun x -> x + 1)

        match viaOperator, viaNamed with
        | CheckResult.Passed, CheckResult.Passed -> ()
        | operatorResult, namedResult ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                $"Expected both composed checks to pass but got operator={operatorResult}, named={namedResult}")

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

    [<TestMethod>]
    member _.``Pure assertion failures no longer infer a source location``() : unit =
        let result =
            <@ 1 + 1 @>
            |> Assert.check (AssertExpectation.equalTo 3)

        match result with
        | AssertResult.Failed failure ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.SourceLocation.IsNone)
        | other ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Failed but got {other}")

        let rendered =
            Assert.toDisplayStringWith
                {
                    Verbosity = Verbosity.Diagnostic
                    MaxValueLines = 12
                }
                result

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(rendered.Contains("Location:"))
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(rendered.Contains("Code:"))

    [<TestMethod>]
    member _.``Diagnostics still extract source locations from stack frames with file info``() : unit =
        let frame =
            System.Diagnostics.StackFrame("Student.fs", 42, 7)

        let location =
            Diagnostics.tryFrameToSourceLocation frame

        match location with
        | Some resolved ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Student.fs", resolved.FilePath)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(42, resolved.Line)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(Some 7, resolved.Column)

            let formatted = Diagnostics.formatLocation resolved
            let lines = formatted.Split(Environment.NewLine)

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(3, lines.Length)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Location: Student.fs", lines[0])
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Line: 42", lines[1])
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Approximate character: 7", lines[2])
        | None ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected source location data from a stack frame with file info.")

    [<TestMethod>]
    member _.``Pure property failures no longer infer a source location``() : unit =
        let result =
            <@ fun x -> x + 1 @>
            |> Check.check CheckExpectation.equalToReference (fun x -> x + 2)

        match result with
        | CheckResult.Failed failure ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.SourceLocation.IsNone)
        | other ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Failed but got {other}")

    [<TestMethod>]
    member _.``Diagnostic verbosity matches detailed without code context``() : unit =
        let report =
            {
                TestifyReport.create
                    AssertionFailure
                    (Some "EqualTo")
                    "[EqualTo] Failed test: <@ 1 + 1 @>" with
                    Expectation = Some "equalTo 3"
                    Expected = Some "equalTo 3"
                    Actual = Some "2"
                    SourceLocation =
                        Some {
                            FilePath = "Sample.fs"
                            Line = 12
                            Column = Some 4
                            Context = None
                        }
            }

        let detailed =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Detailed
                    MaxValueLines = 12
                }
                report

        let diagnostic =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Diagnostic
                    MaxValueLines = 12
                }
                report

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(detailed, diagnostic)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(diagnostic.Contains("Code:"))
        StringAssert.Contains(diagnostic, "Location: Sample.fs")
        StringAssert.Contains(diagnostic, "Line: 12")
        StringAssert.Contains(diagnostic, "Approximate character: 4")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(diagnostic.Contains("Sample.fs:12:4"))

    [<TestMethod>]
    member _.``Persisted XML uses readable source location text``() : unit =
        let originalRoot = TestifySettings.ResultRootOverride
        let temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))

        try
            TestifySettings.ResultRootOverride <- Some temporaryRoot

            let state =
                TestExecution.createState
                    "ApiConventionTests"
                    "Persisted XML omits removed code context elements"
                    {
                        Verbosity = Verbosity.Diagnostic
                        MaxValueLines = 12
                    }

            let location: Diagnostics.SourceLocation =
                {
                    FilePath = Path.Combine(temporaryRoot, "Student.fs")
                    Line = 42
                    Column = Some 7
                    Context = None
                }

            state.FirstTestedSourceLocation <- Some location
            state.LastFailureReport <-
                Some {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: sample" with
                        Expected = Some "expected"
                        Actual = Some "actual"
                        SourceLocation = Some location
                }

            let result = TestResult()
            result.DisplayName <- state.TestName
            result.Outcome <- UnitTestOutcome.Failed
            result.TestFailureException <- Exception("boom")

            let persisted =
                TestResults.writeResults (Some state) [| result |]

            match persisted with
            | Some persistedResult ->
                let document = XDocument.Load(persistedResult.FilePath)
                let testedSourceLocation = document.Root.Element(XName.Get "TestedSourceLocation")
                let failureSourceLocation = document.Root.Element(XName.Get "FailureSourceLocation")

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(document.Root.Element(XName.Get "IncludeCodeContext"))
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(document.Root.Element(XName.Get "CodeContext"))
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(document.Root.Element(XName.Get "TestMethodSourceLocation"))
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(testedSourceLocation)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(failureSourceLocation)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(
                    "Location: Student.fs\nLine: 42\nApproximate character: 7",
                    testedSourceLocation.Value.Replace("\r\n", "\n"))
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(
                    "Location: Student.fs\nLine: 42\nApproximate character: 7",
                    failureSourceLocation.Value.Replace("\r\n", "\n"))
            | None ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected Testify to persist a result file.")
        finally
            TestifySettings.ResultRootOverride <- originalRoot

            if Directory.Exists temporaryRoot then
                Directory.Delete(temporaryRoot, true)

    [<TestMethod>]
    member _.``Hint is rendered in every verbosity mode``() : unit =
        let report =
            {
                TestifyReport.create
                    AssertionFailure
                    (Some "EqualTo")
                    "[EqualTo] Failed test: demo" with
                    Expected = Some "be equal to 1"
                    Actual = Some "1"
            }
            |> TestifyReport.withInferredHint

        let verbosities =
            [ Verbosity.Quiet
              Verbosity.Normal
              Verbosity.Detailed
              Verbosity.Diagnostic ]

        for verbosity in verbosities do
            let rendered =
                TestifyReport.renderWith
                    {
                        Verbosity = verbosity
                        MaxValueLines = 12
                    }
                    report

            StringAssert.Contains(rendered, "Hint: None")

    [<TestMethod>]
    member _.``Hint infers missing Nat suffix from code context``() : unit =
        let location : Diagnostics.SourceLocation =
            {
                FilePath = "Zahlen.fs"
                Line = 12
                Column = None
                Context =
                    Some
                        """
>   12: let result : Nat = 6
    13: result
"""
            }

        let report =
            {
                TestifyReport.create
                    AssertionFailure
                    (Some "EqualTo")
                    "[EqualTo] Failed test: result" with
                    Expected = Some "be equal to 6N"
                    Actual = Some "6N"
                    SourceLocation = Some location
            }
            |> TestifyReport.withInferredHint

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Forgot N suffix", report.Hint)

        let rendered =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Normal
                    MaxValueLines = 12
                }
                report

        StringAssert.Contains(rendered, "Hint: Forgot N suffix")

    [<TestMethod>]
    member _.``Hint does not infer missing Nat suffix from Nat-flavored values alone``() : unit =
        let report =
            {
                TestifyReport.create
                    AssertionFailure
                    (Some "EqualTo")
                    "[EqualTo] Failed test: Peano.mult3 1N" with
                    Expected = Some "be equal to 3N"
                    Actual = Some "0N"
                    Because = Some "Expected 3N but got 0N."
            }
            |> TestifyReport.withInferredHint

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("None", report.Hint)

    [<TestMethod>]
    member _.``Persisted XML includes Hint element``() : unit =
        let originalRoot = TestifySettings.ResultRootOverride
        let temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))

        try
            TestifySettings.ResultRootOverride <- Some temporaryRoot

            let state =
                TestExecution.createState
                    "ApiConventionTests"
                    "Persisted XML includes Hint element"
                    {
                        Verbosity = Verbosity.Normal
                        MaxValueLines = 12
                    }

            state.LastFailureReport <-
                Some {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: sample" with
                        Expected = Some "expected"
                        Actual = Some "Exception: TODO"
                        Because = Some "Expression raised an exception before producing a value: Exception: TODO"
                }

            let result = TestResult()
            result.DisplayName <- state.TestName
            result.Outcome <- UnitTestOutcome.Failed
            result.TestFailureException <- Exception("boom")

            let persisted =
                TestResults.writeResults (Some state) [| result |]

            match persisted with
            | Some persistedResult ->
                let document = XDocument.Load(persistedResult.FilePath)
                let hint = document.Root.Element(XName.Get "Hint")
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(hint)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Replace TODO placeholder", hint.Value)
            | None ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected Testify to persist a result file.")
        finally
            TestifySettings.ResultRootOverride <- originalRoot

            if Directory.Exists temporaryRoot then
                Directory.Delete(temporaryRoot, true)
