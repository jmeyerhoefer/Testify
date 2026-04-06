module Testify.ApiTests

open System
open System.IO
open System.Xml.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.AssertOperators
open Testify.CheckOperators

exception SampleCustomException

type SampleRecord =
    {
        Count: int
        Name: string
    }

type ConfigOnlyValue private (value: int) =
    member _.Value = value

    static member Create(value: int) =
        ConfigOnlyValue(value)

    override this.Equals(other: obj) =
        match other with
        | :? ConfigOnlyValue as value' -> this.Value = value'.Value
        | _ -> false

    override this.GetHashCode() =
        hash this.Value

type ConfigOnlyValueModifier =
    static member ConfigOnlyValue() : FsCheck.Arbitrary<ConfigOnlyValue> =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<int>
        |> FsCheck.FSharp.Arb.convert ConfigOnlyValue.Create (fun value -> value.Value)

[<TestClass>]
type ApiConventionTests() =
    member private _.WithConfiguration(config: TestifyConfig, action: unit -> unit) : unit =
        let original = Testify.currentConfiguration()

        try
            Testify.configure config
            action ()
        finally
            Testify.configure original

    member private this.WithHintRules(rules: TestifyHintRule list, action: unit -> unit) : unit =
        let configured =
            Testify.currentConfiguration()
            |> TestifyConfig.withHints rules

        this.WithConfiguration(configured, action)

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
    member _.``Exception formatter suppresses redundant default message``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(
            "SampleCustomException",
            Render.formatException SampleCustomException
        )

    [<TestMethod>]
    member _.``Exception formatter keeps meaningful message``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(
            "Exception: TODO",
            Render.formatException (Exception("TODO"))
        )

    [<TestMethod>]
    member _.``Structural diff preserves wrapper formatting for Nat``() : unit =
        let diff =
            Diff.tryDescribeStructural
                (Mini.Nat.Make 0)
                (Mini.Nat.Make 1)

        match diff with
        | Some text ->
            StringAssert.Contains(text, "Structural diff:")
            StringAssert.Contains(text, "Expect = 0N")
            StringAssert.Contains(text, "Actual = 1N")
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
                text.Contains("n Expect =", StringComparison.Ordinal))
        | None ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                "Expected a structural diff for mismatched Nat values.")

    [<TestMethod>]
    member _.``Structural diff keeps clean scalar output for booleans``() : unit =
        let diff =
            Diff.tryDescribeWith
                { Diff.defaultOptions with Mode = StructuralOnly }
                false
                true

        match diff with
        | Some text ->
            StringAssert.Contains(text, "Expect = false")
            StringAssert.Contains(text, "Actual = true")
        | None ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                "Expected a structural diff for mismatched booleans.")

    [<TestMethod>]
    member _.``Structural diff preserves nested context for records``() : unit =
        let diff =
            Diff.tryDescribeWith
                { Diff.defaultOptions with Mode = StructuralOnly }
                { Count = 0; Name = "Alice" }
                { Count = 1; Name = "Alice" }

        match diff with
        | Some text ->
            StringAssert.Contains(text, "Count")
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
                text.Contains("Expect = { Count", StringComparison.Ordinal))
        | None ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                "Expected a structural diff for mismatched records.")

    [<TestMethod>]
    member _.``Rendered assert mismatch omits structural diff``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ Mini.Nat.Make 1 @>
                |> Assert.should (AssertExpectation.equalTo (Mini.Nat.Make 0))
            )

        StringAssert.StartsWith(ex.Message, "\n[EqualTo] Failed test:")
        StringAssert.Contains(ex.Message, "Expected: be equal to 0N")
        StringAssert.Contains(ex.Message, "Actual: 1N")
        StringAssert.Contains(ex.Message, "Because: Expected 0N but got 1N.")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            ex.Message.Contains("Structural diff:", StringComparison.Ordinal))

    [<TestMethod>]
    member _.``Rendered property mismatch omits structural diff``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ fun (x: int) -> x + 1 @>
                |> Check.shouldEqual (fun x -> x)
            )

        StringAssert.StartsWith(ex.Message, "\n[EqualToReference] Failed property case:")
        StringAssert.Contains(ex.Message, "Because: Tested code returned 1 but the reference returned 0. Expected 0 but got 1.")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            ex.Message.Contains("Structural diff:", StringComparison.Ordinal))

    [<TestMethod>]
    member _.``Rendered string mismatch keeps informative fast diff``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ "adc" @>
                |> Assert.should (AssertExpectation.equalTo "abc")
            )

        StringAssert.Contains(ex.Message, "First mismatch at index 1")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            ex.Message.Contains("Structural diff:", StringComparison.Ordinal))

    [<TestMethod>]
    member _.``Structural diff text remains available internally``() : unit =
        let diff =
            Diff.tryDescribeStructural
                (Mini.Nat.Make 0)
                (Mini.Nat.Make 1)

        let extracted =
            TestifyReport.diffText diff None

        match extracted with
        | Some text ->
            StringAssert.Contains(text, "Structural diff:")
            StringAssert.Contains(text, "Expect = 0N")
            StringAssert.Contains(text, "Actual = 1N")
        | None ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                "Expected internal diff extraction to keep structural diff text.")

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
    member this.``Current configuration reflects installed values and reset restores defaults``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withReportOptions {
                Verbosity = Verbosity.Quiet
                MaxValueLines = 3
            }
            |> TestifyConfig.withHints [
                MiniHints.placeholderTodo
            ]
            |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs

        this.WithConfiguration(configured, fun () ->
            let current = Testify.currentConfiguration()
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(Verbosity.Quiet, current.ReportOptions.Verbosity)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(3, current.ReportOptions.MaxValueLines)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(1, current.HintRules.Length)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(1, current.CheckConfigTransformers.Length)
        )

        Testify.resetConfiguration()

        let reset = Testify.currentConfiguration()
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
            TestifyReportOptions.Default.Verbosity,
            reset.ReportOptions.Verbosity)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(
            TestifyReportOptions.Default.MaxValueLines,
            reset.ReportOptions.MaxValueLines)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, reset.HintRules.Length)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, reset.CheckConfigTransformers.Length)

    [<TestMethod>]
    member this.``Installed report options affect default rendering but explicit rendering still overrides them``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withReportOptions {
                Verbosity = Verbosity.Quiet
                MaxValueLines = 12
            }

        this.WithConfiguration(configured, fun () ->
            let result =
                <@ 1 + 1 @>
                |> Assert.check (AssertExpectation.equalTo 3)

            let defaultRendered = Assert.toDisplayString result
            let explicitRendered =
                Assert.toDisplayStringWith
                    {
                        Verbosity = Verbosity.Normal
                        MaxValueLines = 12
                    }
                    result

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
                defaultRendered.Contains("Because:", StringComparison.Ordinal))
            StringAssert.Contains(defaultRendered, "Hint: None")
            StringAssert.Contains(explicitRendered, "Because: Expected 3 but got 2.")
        )

    [<TestMethod>]
    member _.``Neutral default config does not include custom configured arbitraries``() : unit =
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Reflection.TargetInvocationException>(fun () ->
            CheckConfig.defaultConfig.ArbMap.ArbFor<ConfigOnlyValue>() |> ignore
        )
        |> ignore

    [<TestMethod>]
    member this.``Installed check config transformers affect default Check behavior``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.addCheckConfigTransformer (fun config ->
                config.WithArbitrary [ typeof<ConfigOnlyValueModifier> ])

        this.WithConfiguration(configured, fun () ->
            let result =
                <@ fun (value: ConfigOnlyValue) -> value @>
                |> Check.checkEqual id

            match result with
            | CheckResult.Passed -> ()
            | other ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                    $"Expected configured default check to pass but got {other}")
        )

    [<TestMethod>]
    member this.``Explicit Check config overrides installed transformers``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.addCheckConfigTransformer (fun config ->
                config.WithArbitrary [ typeof<ConfigOnlyValueModifier> ])

        this.WithConfiguration(configured, fun () ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Reflection.TargetInvocationException>(fun () ->
                <@ fun (value: ConfigOnlyValue) -> value @>
                |> Check.checkWith CheckConfig.defaultConfig CheckExpectation.equalToReference id
                |> ignore
            )
            |> ignore
        )

    [<TestMethod>]
    member this.``Mini preset enables Mini checks without changing neutral defaults``() : unit =
        this.WithConfiguration(TestifyPresets.Mini.config, fun () ->
            let result =
                <@ fun (n: Mini.Nat) -> n @>
                |> Check.checkEqual id

            match result with
            | CheckResult.Passed -> ()
            | other ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                    $"Expected Mini preset check to pass but got {other}")
        )

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
    member this.``Hint is rendered in every verbosity mode``() : unit =
        this.WithHintRules([], fun () ->
            let report =
                {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: demo" with
                        Expected = Some "be equal to 1"
                        Actual = Some "1"
                }
                |> TestifyReport.withResolvedHint

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

                StringAssert.Contains(rendered, "Hint: None"))

    [<TestMethod>]
    member this.``Default hint inference no longer applies built-in heuristics``() : unit =
        this.WithHintRules([], fun () ->
            let report =
                {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: sample" with
                        Expected = Some "expected"
                        Actual = Some "Exception: TODO"
                        Because = Some "Expression raised an exception before producing a value: Exception: TODO"
                }
                |> TestifyReport.withResolvedHint

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("None", report.Hint))

    [<TestMethod>]
    member this.``Custom hint rule can infer from report fields``() : unit =
        this.WithHintRules(
            [
                TestifyHintRule.onFieldRegexPattern
                    "todo"
                    HintTextField.Actual
                    "TODO"
                    (fun _ -> "Implementation placeholder detected")
            ],
            fun () ->
                let report =
                    {
                        TestifyReport.create
                            AssertionFailure
                            (Some "EqualTo")
                            "[EqualTo] Failed test: sample" with
                            Expected = Some "expected"
                            Actual = Some "Exception: TODO"
                            Because = Some "Expression raised an exception before producing a value: Exception: TODO"
                    }
                    |> TestifyReport.withResolvedHint

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>(
                    "Implementation placeholder detected",
                    report.Hint)

                let rendered =
                    TestifyReport.renderWith
                        {
                            Verbosity = Verbosity.Normal
                            MaxValueLines = 12
                        }
                        report

                StringAssert.Contains(rendered, "Hint: Implementation placeholder detected"))

    [<TestMethod>]
    member this.``Custom hint rules respect registration order``() : unit =
        this.WithHintRules(
            [
                TestifyHintRule.onFieldRegexPattern
                    "first"
                    HintTextField.Actual
                    "TODO"
                    (fun _ -> "First rule")
                TestifyHintRule.onFieldRegexPattern
                    "second"
                    HintTextField.Actual
                    "TODO"
                    (fun _ -> "Second rule")
            ],
            fun () ->
                let report =
                    {
                        TestifyReport.create
                            AssertionFailure
                            (Some "EqualTo")
                            "[EqualTo] Failed test: sample" with
                            Actual = Some "Exception: TODO"
                    }
                    |> TestifyReport.withResolvedHint

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("First rule", report.Hint))

    [<TestMethod>]
    member this.``Explicit hint is preserved over configured rules``() : unit =
        this.WithHintRules(
            [
                TestifyHintRule.onFieldRegexPattern
                    "todo"
                    HintTextField.Actual
                    "TODO"
                    (fun _ -> "Generated rule hint")
            ],
            fun () ->
                let report =
                    {
                        TestifyReport.create
                            AssertionFailure
                            (Some "EqualTo")
                            "[EqualTo] Failed test: sample" with
                            Hint = "Keep this hint"
                            Actual = Some "Exception: TODO"
                    }
                    |> TestifyReport.withResolvedHint

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Keep this hint", report.Hint))

    [<TestMethod>]
    member this.``Hint line is rendered after Because``() : unit =
        this.WithHintRules(
            [
                TestifyHintRule.create "exception" (fun report ->
                    match report.Actual with
                    | Some actual when actual.Contains("Div", StringComparison.Ordinal) ->
                        Some "Code throws unexpectedly"
                    | _ ->
                        None)
            ],
            fun () ->
                let report =
                    {
                        TestifyReport.create
                            PropertyFailure
                            (Some "EqualToReference")
                            "[EqualToReference] Failed property case: Zahlen.avg3 0N 0N 0N" with
                            Expected = Some "0N"
                            Actual = Some "Div"
                            Because = Some "Tested code threw Div but the reference returned 0N."
                    }

                let rendered =
                    TestifyReport.renderWith
                        {
                            Verbosity = Verbosity.Normal
                            MaxValueLines = 12
                        }
                        report

                let expectedIndex = rendered.IndexOf("Expected:", StringComparison.Ordinal)
                let actualIndex = rendered.IndexOf("Actual:", StringComparison.Ordinal)
                let becauseIndex = rendered.IndexOf("Because:", StringComparison.Ordinal)
                let hintIndex = rendered.IndexOf("Hint:", StringComparison.Ordinal)

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(expectedIndex >= 0)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(actualIndex > expectedIndex)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(becauseIndex > actualIndex)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(hintIndex > becauseIndex))

    [<TestMethod>]
    member _.``Persisted XML includes Hint element``() : unit =
        let originalRoot = TestifySettings.ResultRootOverride
        let originalConfig = Testify.currentConfiguration()
        let temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))

        try
            TestifySettings.ResultRootOverride <- Some temporaryRoot
            Testify.configure TestifyConfig.defaults

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
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("None", hint.Value)
            | None ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected Testify to persist a result file.")
        finally
            TestifySettings.ResultRootOverride <- originalRoot
            Testify.configure originalConfig

            if Directory.Exists temporaryRoot then
                Directory.Delete(temporaryRoot, true)
