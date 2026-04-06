namespace Testify

open System
open System.Text.RegularExpressions


type TestifyFailureKind =
    | AssertionFailure
    | PropertyFailure
    | PropertyExhausted
    | PropertyError


type TestifyFailureReport =
    {
        Kind: TestifyFailureKind
        Label: string option
        Summary: string
        Hint: string
        Test: string option
        Expectation: string option
        Expected: string option
        Actual: string option
        ExpectedValue: string option
        ActualValue: string option
        Because: string option
        DetailsText: string option
        DiffText: string option
        OriginalTest: string option
        OriginalExpected: string option
        OriginalActual: string option
        ShrunkTest: string option
        ShrunkExpected: string option
        ShrunkActual: string option
        NumberOfTests: int option
        NumberOfShrinks: int option
        Replay: string option
        SourceLocation: Diagnostics.SourceLocation option
    }


type ReportField =
    | Summary
    | Hint
    | Expectation
    | Expected
    | Actual
    | ExpectedValue
    | ActualValue
    | Because
    | Details
    | Diff
    | OriginalTest
    | OriginalExpected
    | OriginalActual
    | ShrunkTest
    | ShrunkExpected
    | ShrunkActual
    | NumberOfTests
    | NumberOfShrinks
    | Replay
    | SourceLocation


[<RequireQualifiedAccess>]
type HintTextField =
    | Summary
    | Test
    | Expectation
    | Expected
    | Actual
    | ExpectedValue
    | ActualValue
    | Because
    | Details
    | Diff
    | OriginalTest
    | OriginalExpected
    | OriginalActual
    | ShrunkTest
    | ShrunkExpected
    | ShrunkActual
    | Replay


type TestifyHintRule =
    {
        Name: string
        TryInfer: TestifyFailureReport -> string option
    }


[<RequireQualifiedAccess>]
module TestifyHintRule =
    let create
        (name: string)
        (tryInfer: TestifyFailureReport -> string option)
        : TestifyHintRule =
        {
            Name = name
            TryInfer = tryInfer
        }

    let private tryGetField
        (field: HintTextField)
        (report: TestifyFailureReport)
        : string option =
        match field with
        | HintTextField.Summary -> Some report.Summary
        | HintTextField.Test -> report.Test
        | HintTextField.Expectation -> report.Expectation
        | HintTextField.Expected -> report.Expected
        | HintTextField.Actual -> report.Actual
        | HintTextField.ExpectedValue -> report.ExpectedValue
        | HintTextField.ActualValue -> report.ActualValue
        | HintTextField.Because -> report.Because
        | HintTextField.Details -> report.DetailsText
        | HintTextField.Diff -> report.DiffText
        | HintTextField.OriginalTest -> report.OriginalTest
        | HintTextField.OriginalExpected -> report.OriginalExpected
        | HintTextField.OriginalActual -> report.OriginalActual
        | HintTextField.ShrunkTest -> report.ShrunkTest
        | HintTextField.ShrunkExpected -> report.ShrunkExpected
        | HintTextField.ShrunkActual -> report.ShrunkActual
        | HintTextField.Replay -> report.Replay

    let onFieldRegex
        (name: string)
        (field: HintTextField)
        (pattern: Regex)
        (buildHint: Match -> string)
        : TestifyHintRule =
        create name (fun report ->
            match tryGetField field report with
            | Some text ->
                let patternMatch = pattern.Match text

                if patternMatch.Success then
                    Some (buildHint patternMatch)
                else
                    None
            | None ->
                None)

    let onFieldRegexPattern
        (name: string)
        (field: HintTextField)
        (pattern: string)
        (buildHint: Match -> string)
        : TestifyHintRule =
        let regex = Regex(pattern, RegexOptions.Compiled)
        onFieldRegex name field regex buildHint
