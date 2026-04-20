namespace Testify.ApiTests

open System
open System.IO
open System.Text.Json.Nodes
open System.Xml.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.Expecto
open Testify.AssertOperators
open Testify.CheckOperators

do ()

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

let inline private alwaysTrue (_: 'Args) = true
let inline private alwaysFalse (_: 'Args) = false

[<TestClass>]
type ApiConventionTests() =
    member private _.ParseJsonObject(text: string) : JsonObject =
        JsonNode.Parse(text).AsObject()

    member private _.CaptureConsoleOut(action: unit -> 'T) : 'T * string =
        let original = Console.Out
        use writer = new StringWriter()
        Console.SetOut(writer)

        try
            let result = action ()
            result, writer.ToString()
        finally
            Console.SetOut(original)

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

    member private this.WithHintPacks(packs: TestifyHintPack list, action: unit -> unit) : unit =
        let configured =
            Testify.currentConfiguration()
            |> TestifyConfig.withHintPacks packs

        this.WithConfiguration(configured, action)

    [<TestMethod>]
    member _.``Assert check is pipe-friendly``() : unit =
        let result =
            <@ 1 + 2 @>
            |> Assert.check (AssertExpectation.equalTo 3)

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(AssertResult.Passed, result)

    [<TestMethod>]
    member _.``Check That returns Passed for matching reference behavior``() : unit =
        let result =
            Check.That(
                CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 1 @>
            )

        match result with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

    [<TestMethod>]
    member _.``Tupled equality check covers the grouped use case``() : unit =
        let result =
            Check.Equal(
                (fun (x, y) -> x + y),
                <@ fun (x, y) -> x + y @>,
                arbitrary = Arbitraries.tuple2 (Arbitraries.from<int>) (Arbitraries.from<int>)
            )

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

        StringAssert.StartsWith(ex.Message, "\n\nFailed test:")
        StringAssert.Contains(ex.Message, "Expected: 0N")
        StringAssert.Contains(ex.Message, "Actual: 1N")
        StringAssert.Contains(ex.Message, "Because: Tested code returned 1N but expected 0N.")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            ex.Message.Contains("Structural diff:", StringComparison.Ordinal))

    [<TestMethod>]
    member _.``Rendered property mismatch omits structural diff``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Should.Equal((fun x -> x), <@ fun (x: int) -> x + 1 @>)
            )

        StringAssert.StartsWith(ex.Message, "\n\nFailed property case:")
        StringAssert.Contains(ex.Message, "Because: Tested code returned 1 but the reference returned 0.")
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
        StringAssert.Contains(ex.Message, "expected 'b' but got 'd'")
        StringAssert.Contains(ex.Message, "Context near mismatch")
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            ex.Message.Contains("Structural diff:", StringComparison.Ordinal))

    [<TestMethod>]
    member _.``Rendered string length mismatch explains trailing newline``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ "abc\n" @>
                |> Assert.should (AssertExpectation.equalTo "abc")
            )

        StringAssert.Contains(ex.Message, "extra trailing newline")
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

        Should.Equal((fun x -> x + 1), expr)

    [<TestMethod>]
    member _.``Check general operator uses defaults``() : unit =
        <@ fun x -> x + 1 @>
        |=> (fun x -> x + 1)

    [<TestMethod>]
    member _.``Check composed AND expectation via operator matches named composition``() : unit =
        let viaOperator =
            Check.That(
                CheckExpectation.equalToReference <&> CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 1 @>
            )

        let viaNamed =
            Check.That(
                CheckExpectation.andAlso CheckExpectation.equalToReference CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 1 @>
            )

        match viaOperator, viaNamed with
        | CheckResult.Passed, CheckResult.Passed -> ()
        | operatorResult, namedResult ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                $"Expected both composed checks to pass but got operator={operatorResult}, named={namedResult}")

    [<TestMethod>]
    member _.``Check composed OR expectation via operator matches named composition``() : unit =
        let viaOperator =
            Check.That(
                CheckExpectation.equalToReference <|> CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 1 @>
            )

        let viaNamed =
            Check.That(
                CheckExpectation.orElse CheckExpectation.equalToReference CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 1 @>
            )

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

        Should.That(
            expectation,
            (fun x -> x + 1),
            <@ fun x -> x + 1 @>,
            config = config,
            arbitrary = arbitrary
        )

    [<TestMethod>]
    member this.``Current configuration reflects installed values and reset restores defaults``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withReportOptions {
                Verbosity = Verbosity.Quiet
                MaxValueLines = 3
                OutputFormat = OutputFormat.WallOfText
            }
            |> TestifyConfig.withOutputFormat OutputFormat.Json
            |> TestifyConfig.withHintPacks [ MiniHints.pack ]
            |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs

        this.WithConfiguration(configured, fun () ->
            let current = Testify.currentConfiguration()
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(Verbosity.Quiet, current.ReportOptions.Verbosity)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(3, current.ReportOptions.MaxValueLines)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(OutputFormat.Json, current.ReportOptions.OutputFormat)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(1, current.HintPacks.Length)
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
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
            TestifyReportOptions.Default.OutputFormat,
            reset.ReportOptions.OutputFormat)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, reset.HintRules.Length)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, reset.HintPacks.Length)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, reset.CheckConfigTransformers.Length)

    [<TestMethod>]
    member this.``Installed report options affect default rendering but explicit rendering still overrides them``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withReportOptions {
                Verbosity = Verbosity.Quiet
                MaxValueLines = 12
                OutputFormat = OutputFormat.WallOfText
            }
            |> TestifyConfig.withOutputFormat OutputFormat.Json

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
                        OutputFormat = OutputFormat.WallOfText
                    }
                    result

            let defaultJson = this.ParseJsonObject(defaultRendered)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertion", defaultJson["kind"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", defaultJson["outcome"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(defaultRendered.Contains("\"because\"", StringComparison.Ordinal))
            let defaultJson = this.ParseJsonObject(defaultRendered)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, defaultJson["hints"].AsArray().Count)
            StringAssert.Contains(explicitRendered, "Because: Tested code returned 2 but expected 3.")
        )

    [<TestMethod>]
    member _.``Current test rendering observes output format changes configured after beginTest``() : unit =
        let originalConfig = Testify.currentConfiguration()

        try
            Testify.resetConfiguration()

            TestExecution.beginTest
                "ApiConventionTests"
                "Current test rendering observes output format changes configured after beginTest"
                {
                    Verbosity = Verbosity.Normal
                    MaxValueLines = 12
                    OutputFormat = OutputFormat.WallOfText
                }

            Testify.configure (
                Testify.currentConfiguration()
                |> TestifyConfig.withOutputFormat OutputFormat.Json
            )

            let rendered =
                <@ 1 + 1 @>
                |> Assert.check (AssertExpectation.equalTo 3)
                |> Assert.toDisplayString

            let json = JsonNode.Parse(rendered).AsObject()
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertion", json["kind"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
        finally
            TestExecution.endTest () |> ignore
            Testify.configure originalConfig

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
                Check.Equal(id, <@ fun (value: ConfigOnlyValue) -> value @>)

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
                Check.That(
                    CheckExpectation.equalToReference,
                    id,
                    <@ fun (value: ConfigOnlyValue) -> value @>,
                    config = CheckConfig.defaultConfig
                )
                |> ignore
            )
            |> ignore
        )

    [<TestMethod>]
    member this.``Mini preset enables Mini checks without changing neutral defaults``() : unit =
        this.WithConfiguration(TestifyPresets.Mini.config, fun () ->
            let result =
                Check.Equal(id, <@ fun (n: Mini.Nat) -> n @>)

            match result with
            | CheckResult.Passed -> ()
            | other ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                    $"Expected Mini preset check to pass but got {other}")
        )

    [<TestMethod>]
    member _.``Should That passes for always-true bool properties``() : unit =
        Should.That(CheckExpectation.isTrue, alwaysTrue, <@ fun (value: int) -> value = value @>)

    [<TestMethod>]
    member _.``Should That fails when a generated case returns false``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Should.That(CheckExpectation.isTrue, alwaysTrue, <@ fun (_: int) -> false @>)
            )

        StringAssert.Contains(ex.Message, "Tested code returned false but expected true.")

    [<TestMethod>]
    member _.``Should That respects a custom arbitrary for bool checks``() : unit =
        let arb =
            Arbitraries.from<unit>
            |> Arbitraries.convert (fun () -> 0) (fun _ -> ())

        Should.That(
            CheckExpectation.isTrue,
            alwaysTrue,
            <@ fun value -> value = 0 @>,
            arbitrary = arb
        )

    [<TestMethod>]
    member _.``Should That accepts explicit config for bool checks``() : unit =
        let config = CheckConfig.withMaxTest 5

        Should.That(
            CheckExpectation.isTrue,
            alwaysTrue,
            <@ fun (value: int) -> value = value @>,
            config = config
        )

    [<TestMethod>]
    member _.``Check By passes for callback-built bool properties``() : unit =
        let result =
            Check.By(
                (fun verify ->
                    FsCheck.FSharp.Prop.forAll (Arbitraries.from<int>) verify),
                CheckExpectation.isTrue,
                alwaysTrue,
                <@ fun value -> value = value @>
            )

        match result with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

    [<TestMethod>]
    member _.``Should By fails with existing bool helper wording``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Should.By(
                    (fun verify ->
                        FsCheck.FSharp.Prop.forAll (Arbitraries.from<int>) verify),
                    CheckExpectation.isTrue,
                    alwaysTrue,
                    <@ fun value -> value = 0 @>
                )
            )

        StringAssert.Contains(ex.Message, "Tested code returned false but expected true.")

    [<TestMethod>]
    member _.``Check bool callback operator delegates to Should By``() : unit =
        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                <@ fun value -> value = 0 @>
                |?> (fun verify ->
                        FsCheck.FSharp.Prop.forAll (Arbitraries.from<int>) verify)
            )

        StringAssert.Contains(ex.Message, "Tested code returned false but expected true.")

    [<TestMethod>]
    member _.``Check by supports callback-built reference checks and captures failure metadata``() : unit =
        let shrinkingArbitrary =
            Arbitraries.fromGenShrink
                (FsCheck.FSharp.Gen.constant 2)
                (fun value ->
                    if value = 2 then seq { 1 } else Seq.empty)

        let result =
            Check.By(
                (fun verify ->
                    FsCheck.FSharp.Prop.forAll shrinkingArbitrary verify),
                CheckExpectation.equalToReference,
                (fun value -> value + 1),
                <@ fun value -> value @>
            )

        match result with
        | CheckResult.Failed failure ->
            StringAssert.Contains(failure.Test, "1")
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.Original.Test.Length > 0)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.NumberOfTests.IsSome)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.NumberOfShrinks.IsSome)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.Replay.IsSome)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(failure.Shrunk.IsSome)
        | other ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Failed but got {other}")

    [<TestMethod>]
    member _.``Check By with explicit config respects configured max test``() : unit =
        let result =
            Check.By(
                (fun verify ->
                    FsCheck.FSharp.Prop.forAll (Arbitraries.from<int>) verify),
                CheckExpectation.isTrue,
                alwaysTrue,
                <@ fun value -> value = value @>,
                config = CheckConfig.withMaxTest 1
            )

        match result with
        | CheckResult.Passed -> ()
        | other -> Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Passed but got {other}")

    [<TestMethod>]
    member _.``Check By reports property-builder exceptions through the existing error path``() : unit =
        let result =
            Check.By(
                (fun _ -> failwith "builder boom"),
                CheckExpectation.isTrue,
                alwaysTrue,
                <@ fun (value: int) -> value = value @>
            )

        match result with
        | CheckResult.Errored message ->
            StringAssert.Contains(message, "Check runner threw")
            StringAssert.Contains(message, "builder boom")
        | other ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Expected Errored but got {other}")

    [<TestMethod>]
    member _.``Should That mirrors false bool helper behavior``() : unit =
        Should.That(CheckExpectation.isFalse, alwaysFalse, <@ fun (_: int) -> false @>)

        let ex =
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                Should.That(CheckExpectation.isFalse, alwaysFalse, <@ fun (_: int) -> true @>)
            )

        StringAssert.Contains(ex.Message, "Tested code returned true but expected false.")

    [<TestMethod>]
    member _.``Check bool expectations return normal Check result shapes``() : unit =
        let passed =
            Check.That(CheckExpectation.isTrue, alwaysTrue, <@ fun (value: int) -> value = value @>)

        let failed =
            Check.That(CheckExpectation.isTrue, alwaysTrue, <@ fun (_: int) -> false @>)

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

        Check.Collect.add collector (Check.That(CheckExpectation.equalToReference, (fun x -> x + 1), <@ fun x -> x + 2 @>))
        |> ignore

        Check.Collect.add collector (Check.That(CheckExpectation.equalToReference, (fun x -> x + 3), <@ fun x -> x + 4 @>))
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
                    OutputFormat = OutputFormat.WallOfText
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
            Check.That(
                CheckExpectation.equalToReference,
                (fun x -> x + 2),
                <@ fun x -> x + 1 @>
            )

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
                    OutputFormat = OutputFormat.WallOfText
                }
                report

        let diagnostic =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Diagnostic
                    MaxValueLines = 12
                    OutputFormat = OutputFormat.WallOfText
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
                        OutputFormat = OutputFormat.WallOfText
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
    member this.``Missing hints are omitted in every wall-of-text verbosity mode``() : unit =
        this.WithHintRules([], fun () ->
            let report =
                {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: demo" with
                        Expected = Some "1"
                        Actual = Some "1"
                }
                |> TestifyReport.withResolvedHints

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
                            OutputFormat = OutputFormat.WallOfText
                        }
                        report

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
                    rendered.Contains("Hint:", StringComparison.Ordinal))
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
                    rendered.Contains("Hints:", StringComparison.Ordinal)))

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
                |> TestifyReport.withResolvedHints

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, report.Hints.Length))

    [<TestMethod>]
    member _.``Literal None hint placeholder is suppressed``() : unit =
        let report =
            {
                TestifyReport.create
                    AssertionFailure
                    (Some "EqualTo")
                    "[EqualTo] Failed test: sample" with
                    Hints = [ "None" ]
                    Expected = Some "expected"
                    Actual = Some "actual"
            }
            |> TestifyReport.withResolvedHints

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, report.Hints.Length)

        let rendered =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Normal
                    MaxValueLines = 12
                    OutputFormat = OutputFormat.WallOfText
                }
                report

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(
            rendered.Contains("Hint:", StringComparison.Ordinal))

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
                    |> TestifyReport.withResolvedHints

                CollectionAssert.AreEqual(
                    [| "Implementation placeholder detected" |],
                    report.Hints |> List.toArray)

                let rendered =
                    TestifyReport.renderWith
                        {
                            Verbosity = Verbosity.Normal
                            MaxValueLines = 12
                            OutputFormat = OutputFormat.WallOfText
                        }
                        report

                StringAssert.Contains(rendered, "Hint: Implementation placeholder detected"))

    [<TestMethod>]
    member this.``Custom hint rules collect ordered hints``() : unit =
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
                    |> TestifyReport.withResolvedHints

                CollectionAssert.AreEqual(
                    [| "First rule"; "Second rule" |],
                    report.Hints |> List.toArray))

    [<TestMethod>]
    member this.``Manual hints are preserved before configured rules``() : unit =
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
                            Hints = [ "Keep this hint" ]
                            Actual = Some "Exception: TODO"
                    }
                    |> TestifyReport.withResolvedHints

                CollectionAssert.AreEqual(
                    [| "Keep this hint"; "Generated rule hint" |],
                    report.Hints |> List.toArray))

    [<TestMethod>]
    member this.``Hint section is rendered after Because``() : unit =
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
                            OutputFormat = OutputFormat.WallOfText
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
    member this.``Hint packs determine ordered hint resolution and dedupe duplicates``() : unit =
        let sharedHint = TestifyHintRule.create "shared" (fun _ -> Some "Shared hint")

        let firstPack =
            TestifyHintPack.create
                "first"
                [
                    sharedHint
                    TestifyHintRule.create "first-only" (fun _ -> Some "First pack hint")
                ]

        let secondPack =
            TestifyHintPack.create
                "second"
                [
                    TestifyHintRule.create "shared-again" (fun _ -> Some "Shared hint")
                    TestifyHintRule.create "second-only" (fun _ -> Some "Second pack hint")
                ]

        this.WithHintPacks([ firstPack; secondPack ], fun () ->
            let report =
                TestifyReport.create AssertionFailure None "demo"
                |> TestifyReport.withResolvedHints

            CollectionAssert.AreEqual(
                [| "Shared hint"; "First pack hint"; "Second pack hint" |],
                report.Hints |> List.toArray))

    [<TestMethod>]
    member this.``Built-in string and property hint packs infer semantic hints``() : unit =
        this.WithHintPacks([ StringHints.pack; PropertyHints.pack ], fun () ->
            let report =
                {
                    TestifyReport.create
                        PropertyFailure
                        None
                        "demo" with
                        ExpectedValue = Some "abc"
                        ActualValue = Some "abc\n"
                        ShrunkTest = Some "[]"
                        NumberOfTests = Some 2
                }
                |> TestifyReport.withResolvedHints

            CollectionAssert.AreEqual(
                [|
                    "The values differ only in whitespace. Check spaces, blank lines, or trailing output."
                    "This looks like exactly one extra or missing newline."
                    "The smallest counterexample is an empty or minimal value. Your base case is a good place to check first."
                    "The property failed after very few generated tests. This usually points to a broad logic error."
                |],
                report.Hints |> List.toArray))

    [<TestMethod>]
    member _.``Assertion failure report carries structured observed metadata``() : unit =
        let report =
            <@ 1 + 1 @>
            |> Assert.check (AssertExpectation.equalTo 3)
            |> Assert.toFailureReport
            |> Option.defaultWith (fun () ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected a failure report.")
                Unchecked.defaultof<_>)

        match report.ExpectedObservedInfo, report.ActualObservedInfo with
        | Some expectedInfo, Some actualInfo ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("3", expectedInfo.Display |> Option.defaultValue "")
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(expectedInfo.IsException)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("2", actualInfo.Display |> Option.defaultValue "")
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(actualInfo.IsException)
        | _ ->
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected structured observed info for both expected and actual values.")

    [<TestMethod>]
    member this.``Built-in generic hint pack infers semantic exception hints``() : unit =
        this.WithHintPacks([ GenericHints.pack ], fun () ->
            let report =
                <@ 1 / 0 @>
                |> Assert.check AssertExpectation.doesNotThrow
                |> Assert.toFailureReport
                |> Option.defaultWith (fun () ->
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected a failure report.")
                    Unchecked.defaultof<_>)
                |> TestifyReport.withResolvedHints

            CollectionAssert.AreEqual(
                [|
                    "This looks like a division-by-zero failure. Check whether zero is allowed here or needs explicit handling before dividing."
                    "Your code raises an exception in a case that likely expects a handled result instead."
                |],
                report.Hints |> List.toArray))

    [<TestMethod>]
    member this.``Property hint pack detects minimal exception counterexamples``() : unit =
        this.WithHintPacks([ PropertyHints.pack ], fun () ->
            let report =
                {
                    TestifyReport.create
                        PropertyFailure
                        None
                        "demo" with
                        ShrunkTest = Some "[]"
                        ShrunkActualObservedInfo =
                            Some {
                                Display = Some "DivideByZeroException"
                                IsException = true
                                ExceptionType = Some "DivideByZeroException"
                                ExceptionMessage = Some "Attempted to divide by zero."
                            }
                }
                |> TestifyReport.withResolvedHints

            CollectionAssert.Contains(
                report.Hints |> List.toArray,
                "The smallest failing case is minimal and still throws an exception. Check the base case or empty-input handling first."))

    [<TestMethod>]
    member this.``Json rendering uses semantic keys and omits empty fields``() : unit =
        let rendered =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Normal
                    MaxValueLines = 12
                    OutputFormat = OutputFormat.Json
                }
                {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "Failed test: <@ 1 + 1 @>" with
                        Test = Some "<@ 1 + 1 @>"
                        Expected = Some "3"
                        Actual = Some "2"
                }

        let json = this.ParseJsonObject(rendered)

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertion", json["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Failed test: <@ 1 + 1 @>", json["summary"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("<@ 1 + 1 @>", json["testedExpression"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("3", json["expected"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("2", json["actual"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, json["hints"].AsArray().Count)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(json["because"])
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(json["sourceLocation"])

    [<TestMethod>]
    member this.``Json rendering includes structured observed metadata when available``() : unit =
        let rendered =
            <@ 1 / 0 @>
            |> Assert.check AssertExpectation.doesNotThrow
            |> Assert.toFailureReport
            |> Option.map (fun report ->
                TestifyReport.renderWith
                    {
                        Verbosity = Verbosity.Normal
                        MaxValueLines = 12
                        OutputFormat = OutputFormat.Json
                    }
                    report)
            |> Option.defaultWith (fun () ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected a failure report.")
                String.Empty)

        let json = this.ParseJsonObject(rendered)
        let actualObserved = json["actualObserved"].AsObject()
        let expectedObserved = json["expectedObserved"].AsObject()

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<bool>(true, actualObserved["isException"].GetValue<bool>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("DivideByZeroException", actualObserved["exceptionType"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<bool>(false, expectedObserved["isException"].GetValue<bool>())

    [<TestMethod>]
    member this.``Detailed json rendering includes nested source location and optional value fields``() : unit =
        let rendered =
            TestifyReport.renderWith
                {
                    Verbosity = Verbosity.Detailed
                    MaxValueLines = 12
                    OutputFormat = OutputFormat.Json
                }
                {
                    TestifyReport.create
                        PropertyFailure
                        (Some "EqualToReference")
                        "Failed property case: demo" with
                        Test = Some "demo 1"
                        Expectation = Some "equal to reference"
                        Expected = Some "0"
                        Actual = Some "1"
                        ExpectedValue = Some "0"
                        ActualValue = Some "1"
                        DiffText = Some "First mismatch at index 0"
                        NumberOfTests = Some 5
                        NumberOfShrinks = Some 2
                        Replay = Some "123,456"
                        SourceLocation =
                            Some {
                                FilePath = "Student.fs"
                                Line = 42
                                Column = Some 7
                                Context = Some "ignored"
                            }
                }

        let json = this.ParseJsonObject(rendered)
        let sourceLocation = json["sourceLocation"].AsObject()

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("property", json["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("0", json["expectedValue"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("1", json["actualValue"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("First mismatch at index 0", json["diff"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(5, json["numberOfTests"].GetValue<int>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, json["numberOfShrinks"].GetValue<int>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Student.fs", sourceLocation["filePath"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(42, sourceLocation["line"].GetValue<int>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(7, sourceLocation["column"].GetValue<int>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(sourceLocation["context"])

    [<TestMethod>]
    member this.``Json mode renders passing assertion and property results``() : unit =
        let options =
            {
                Verbosity = Verbosity.Normal
                MaxValueLines = 12
                OutputFormat = OutputFormat.Json
            }

        let assertionJson =
            Assert.toDisplayStringWith options AssertResult.Passed
            |> this.ParseJsonObject

        let passedCheckResult : CheckResult<int, int, int> =
            CheckResult.Passed

        let propertyJson =
            Check.ToDisplayStringWith(options, passedCheckResult)
            |> this.ParseJsonObject

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertion", assertionJson["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("passed", assertionJson["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Test passed.", assertionJson["summary"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("property", propertyJson["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("passed", propertyJson["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Property passed.", propertyJson["summary"].GetValue<string>())

    [<TestMethod>]
    member this.``Json mode renders exhausted and errored property results``() : unit =
        let options =
            {
                Verbosity = Verbosity.Normal
                MaxValueLines = 12
                OutputFormat = OutputFormat.Json
            }

        let exhausted =
            Check.ToDisplayStringWith(options, CheckResult<int, int, int>.Exhausted "Not enough generated values.")
            |> this.ParseJsonObject

        let errored =
            Check.ToDisplayStringWith(options, CheckResult<int, int, int>.Errored "Runner exploded.")
            |> this.ParseJsonObject

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("property", exhausted["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("exhausted", exhausted["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Not enough generated values.", exhausted["because"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("property", errored["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("errored", errored["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Runner exploded.", errored["because"].GetValue<string>())

    [<TestMethod>]
    member this.``Assertion collector renders aggregated json payload when configured``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withOutputFormat OutputFormat.Json

        this.WithConfiguration(configured, fun () ->
            let collector = Assert.Collect.create ()

            Assert.Collect.add collector (AssertExpectation.equalTo 1) <@ 2 @>
            |> ignore

            Assert.Collect.add collector (AssertExpectation.greaterThan 5) <@ 3 @>
            |> ignore

            let ex =
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                    Assert.Collect.assertAll collector
                )

            let json = this.ParseJsonObject(ex.Message)
            let failures = json["failures"].AsArray()

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertionCollection", json["kind"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, json["failureCount"].GetValue<int>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, failures.Count)
        )

    [<TestMethod>]
    member this.``Check collector renders aggregated json payload when configured``() : unit =
        let configured =
            TestifyConfig.defaults
            |> TestifyConfig.withOutputFormat OutputFormat.Json

        this.WithConfiguration(configured, fun () ->
            let collector : Check.Collector<int, int, int> = Check.Collect.create ()

            Check.Collect.add collector (Check.That(CheckExpectation.equalToReference, (fun x -> x + 1), <@ fun x -> x + 2 @>))
            |> ignore

            Check.Collect.add collector (Check.That(CheckExpectation.equalToReference, (fun x -> x + 3), <@ fun x -> x + 4 @>))
            |> ignore

            let ex =
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<System.Exception>(fun () ->
                    Check.Collect.assertAll collector
                )

            let json = this.ParseJsonObject(ex.Message)
            let failures = json["failures"].AsArray()

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("propertyCollection", json["kind"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, json["failureCount"].GetValue<int>())
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(2, failures.Count)
        )

    [<TestMethod>]
    member this.``TestifyExpecto prints json failure payload when configured``() : unit =
        let config =
            {
                TestifyExpectoConfig.defaults with
                    ReportOptions =
                        Some {
                            Verbosity = Verbosity.Normal
                            MaxValueLines = 12
                            OutputFormat = OutputFormat.Json
                        }
            }

        let tests =
            TestifyExpecto.testList "samples" [
                TestifyExpecto.testCase "fails" (fun () ->
                    <@ 1 + 1 @>
                    |> Assert.should (AssertExpectation.equalTo 3))
            ]

        let exitCode, output =
            this.CaptureConsoleOut(fun () ->
                TestifyExpecto.runTestsWithCLIArgs config [||] tests)

        let json =
            output.Trim()
            |> this.ParseJsonObject

        let failure = json["failure"].AsObject()

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(1, exitCode)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("test", json["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("failed", json["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("samples/fails", json["testName"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("assertion", failure["kind"].GetValue<string>())

    [<TestMethod>]
    member this.``TestifyExpecto prints json summary when configured``() : unit =
        let config =
            {
                TestifyExpectoConfig.defaults with
                    ReportOptions =
                        Some {
                            Verbosity = Verbosity.Normal
                            MaxValueLines = 12
                            OutputFormat = OutputFormat.Json
                        }
                    ShowSummary = true
            }

        let tests =
            TestifyExpecto.testList "samples" [
                TestifyExpecto.testCase "passes" (fun () ->
                    <@ 1 + 2 @>
                    |> Assert.should (AssertExpectation.equalTo 3))
            ]

        let exitCode, output =
            this.CaptureConsoleOut(fun () ->
                TestifyExpecto.runTestsWithCLIArgs config [||] tests)

        let json =
            output.Trim()
            |> this.ParseJsonObject

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, exitCode)
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("summary", json["kind"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("completed", json["outcome"].GetValue<string>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(1, json["passed"].GetValue<int>())
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, json["failed"].GetValue<int>())

    [<TestMethod>]
    member _.``Persisted XML includes Hints container``() : unit =
        let originalRoot = TestifySettings.ResultRootOverride
        let originalConfig = Testify.currentConfiguration()
        let temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))

        try
            TestifySettings.ResultRootOverride <- Some temporaryRoot
            Testify.configure TestifyConfig.defaults

            let state =
                TestExecution.createState
                    "ApiConventionTests"
                    "Persisted XML includes Hints container"
                    {
                        Verbosity = Verbosity.Normal
                        MaxValueLines = 12
                        OutputFormat = OutputFormat.WallOfText
                    }

            state.LastFailureReport <-
                Some {
                    TestifyReport.create
                        AssertionFailure
                        (Some "EqualTo")
                        "[EqualTo] Failed test: sample" with
                        Expected = Some "expected"
                        Actual = Some "Exception: TODO"
                        ExpectedObservedInfo =
                            Some {
                                Display = Some "expected"
                                IsException = false
                                ExceptionType = None
                                ExceptionMessage = None
                            }
                        ActualObservedInfo =
                            Some {
                                Display = Some "Exception: TODO"
                                IsException = true
                                ExceptionType = Some "Exception"
                                ExceptionMessage = Some "TODO"
                            }
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
                let hints = document.Root.Element(XName.Get "Hints")
                let expectedObserved = document.Root.Element(XName.Get "ExpectedObserved")
                let actualObserved = document.Root.Element(XName.Get "ActualObserved")
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(hints)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(0, hints.Elements(XName.Get "Hint") |> Seq.length)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(expectedObserved)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("false", expectedObserved.Element(XName.Get "IsException").Value)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(actualObserved)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("true", actualObserved.Element(XName.Get "IsException").Value)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("Exception", actualObserved.Element(XName.Get "ExceptionType").Value)
            | None ->
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Expected Testify to persist a result file.")
        finally
            TestifySettings.ResultRootOverride <- originalRoot
            Testify.configure originalConfig

            if Directory.Exists temporaryRoot then
                Directory.Delete(temporaryRoot, true)
