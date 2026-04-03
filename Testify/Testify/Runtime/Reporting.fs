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
module TestifyReport =
    let private todoPattern =
        Regex(@"\bTODO\b", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let private bareIntPattern =
        Regex(@"(?<![\w])\d+(?![\wN])", RegexOptions.Compiled)

    let private suffixedNatPattern =
        Regex(@"\b\d+N\b", RegexOptions.Compiled)

    let private natWordPattern =
        Regex(@"\bNat\b", RegexOptions.Compiled)

    let private numericLiteralNPattern =
        Regex(@"\bNumericLiteralN\b", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let private contextPrefixPattern =
        Regex(@"^[> ]\s*\d+:\s*", RegexOptions.Compiled)

    let private weekdayPattern =
        Regex(@"weekday\s*=\s*([A-Za-z]+)", RegexOptions.Compiled)

    let private truncateTextLines
        (maxLines: int)
        (text: string)
        : string =
        let normalizedMaxLines = max 1 maxLines
        let lines = Render.splitLines text

        match lines with
        | [] -> text
        | _ when lines.Length <= normalizedMaxLines -> text
        | _ ->
            lines
            |> List.truncate normalizedMaxLines
            |> fun kept -> kept @ [ "..." ]
            |> String.concat "\n"

    let private appendLabeledValue
        (label: string)
        (value: string option)
        (maxLines: int)
        (lines: string list)
        : string list =
        match value with
        | Some text when not (String.IsNullOrWhiteSpace text) ->
            lines @ [ $"{label}: {truncateTextLines maxLines text}" ]
        | _ ->
            lines

    let private appendMaybeLines
        (items: string list)
        (lines: string list)
        : string list =
        match items with
        | [] -> lines
        | values -> lines @ values

    let private containsOrdinalIgnoreCase
        (needle: string)
        (value: string)
        : bool =
        value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0

    let private cleanCodeContext
        (context: string)
        : string =
        context
        |> Render.splitLines
        |> List.map (fun line -> contextPrefixPattern.Replace(line, ""))
        |> String.concat "\n"

    let private reportTexts
        (report: TestifyFailureReport)
        : string list =
        [
            Some report.Summary
            report.Test
            report.Expectation
            report.Expected
            report.Actual
            report.ExpectedValue
            report.ActualValue
            report.Because
            report.DetailsText
            report.DiffText
            report.OriginalTest
            report.OriginalExpected
            report.OriginalActual
            report.ShrunkTest
            report.ShrunkExpected
            report.ShrunkActual
            report.SourceLocation
            |> Option.bind (fun location -> location.Context)
            |> Option.map cleanCodeContext
        ]
        |> List.choose id

    let private natHintFragments
        (report: TestifyFailureReport)
        : string list =
        [
            report.Test
            report.Expectation
            report.Expected
            report.Actual
            report.ExpectedValue
            report.ActualValue
            report.SourceLocation
            |> Option.bind (fun location -> location.Context)
            |> Option.map cleanCodeContext
        ]
        |> List.choose id

    let private hasNatSignal
        (text: string)
        : bool =
        numericLiteralNPattern.IsMatch text
        || natWordPattern.IsMatch text
        || suffixedNatPattern.IsMatch text

    let private hasNearbyNatLiteralPattern
        (text: string)
        : bool =
        let lines = Render.splitLines text

        let windows =
            if List.isEmpty lines then
                [ text ]
            else
                lines
                |> List.mapi (fun index _ ->
                    let startIndex = max 0 (index - 1)
                    let endIndex = min (lines.Length - 1) (index + 1)

                    [ startIndex .. endIndex ]
                    |> List.map (fun lineIndex -> lines[lineIndex])
                    |> String.concat " ")

        windows
        |> List.exists (fun fragment ->
            bareIntPattern.IsMatch fragment
            && hasNatSignal fragment)

    let private tryInferNatSuffixHint
        (report: TestifyFailureReport)
        : string option =
        let fragments = natHintFragments report

        if fragments |> List.exists hasNearbyNatLiteralPattern then
            Some "Forgot N suffix"
        else
            None

    let private tryInferWeekdayHint
        (texts: string list)
        : string option =
        let weekdays =
            texts
            |> List.collect (fun text ->
                weekdayPattern.Matches(text)
                |> Seq.cast<Match>
                |> Seq.choose (fun captured ->
                    if captured.Success && captured.Groups.Count > 1 then
                        Some captured.Groups[1].Value
                    else
                        None)
                |> Seq.toList)
            |> List.distinct

        if weekdays.Length >= 2 then
            Some "Check weekday logic"
        else
            None

    let inferHint
        (report: TestifyFailureReport)
        : string =
        let texts = reportTexts report

        if texts |> List.exists todoPattern.IsMatch then
            "Replace TODO placeholder"
        elif texts |> List.exists (containsOrdinalIgnoreCase "Expected true but got false") then
            "Check boolean condition"
        elif texts |> List.exists (containsOrdinalIgnoreCase "Expected false but got true") then
            "Check boolean condition"
        else
            match tryInferWeekdayHint texts with
            | Some hint -> hint
            | None when texts |> List.exists (containsOrdinalIgnoreCase "Expression raised an exception before producing a value") ->
                "Code throws unexpectedly"
            | None when texts |> List.exists (containsOrdinalIgnoreCase "Tested code threw") ->
                "Code throws unexpectedly"
            | None ->
                match tryInferNatSuffixHint report with
                | Some hint -> hint
                | None when texts |> List.exists (containsOrdinalIgnoreCase "reference returned") ->
                    "Logic differs from reference"
                | None ->
                    "None"

    let withInferredHint
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        let normalizedHint =
            if String.IsNullOrWhiteSpace report.Hint then
                inferHint report
            elif String.Equals(report.Hint, "None", StringComparison.OrdinalIgnoreCase) then
                inferHint report
            else
                report.Hint

        { report with Hint = normalizedHint }

    // This is the single edit point for what each verbosity contains.
    let rec fieldsForVerbosity
        (verbosity: Verbosity)
        : Set<ReportField> =
        match verbosity with
        | Verbosity.Quiet -> set [
            Summary
            Hint
            Expected
            Actual ]
        | Verbosity.Normal -> set [
            Summary
            Hint
            Expected
            Actual
            Because ]
        | Verbosity.Detailed -> set [
            Summary
            Hint
            Expectation
            Expected
            Actual
            ExpectedValue
            ActualValue
            Because
            Details
            Diff
            OriginalTest
            OriginalExpected
            OriginalActual
            ShrunkTest
            ShrunkExpected
            ShrunkActual
            NumberOfTests
            NumberOfShrinks
            Replay
            SourceLocation ]
        | Verbosity.Diagnostic -> set [
            Summary
            Hint
            Expectation
            Expected
            Actual
            ExpectedValue
            ActualValue
            Because
            Details
            Diff
            OriginalTest
            OriginalExpected
            OriginalActual
            ShrunkTest
            ShrunkExpected
            ShrunkActual
            NumberOfTests
            NumberOfShrinks
            Replay
            SourceLocation ]
        | _ -> fieldsForVerbosity TestifyReportOptions.Default.Verbosity

    let private hasField
        (fields: Set<ReportField>)
        (field: ReportField)
        : bool =
        fields.Contains field

    let private resolveFields
        (options: TestifyReportOptions)
        : Set<ReportField> =
        fieldsForVerbosity options.Verbosity

    let private appendFieldValue
        (fields: Set<ReportField>)
        (field: ReportField)
        (label: string)
        (value: string option)
        (maxLines: int)
        (lines: string list)
        : string list =
        if hasField fields field then
            appendLabeledValue label value maxLines lines
        else
            lines

    let private formatSourceLocation
        (fields: Set<ReportField>)
        (location: Diagnostics.SourceLocation)
        : string list =
        [
            if hasField fields SourceLocation then
                yield! Render.splitLines (Diagnostics.formatLocation location)
        ]

    let tryExtractDiffText
        (texts: string option list)
        : string option =
        texts
        |> List.choose id
        |> List.tryFind (fun text ->
            not (String.IsNullOrWhiteSpace text)
            && (
                text.Contains("Structural diff:", StringComparison.Ordinal)
                || text.Contains("First mismatch", StringComparison.Ordinal)
            ))

    let detailsText
        (details: FailureDetails option)
        : string option =
        details
        |> Option.map Render.formatFailureDetails

    let diffText
        (because: string option)
        (details: string option)
        : string option =
        tryExtractDiffText [ because; details ]

    let create
        (kind: TestifyFailureKind)
        (label: string option)
        (summary: string)
        : TestifyFailureReport =
        {
            Kind = kind
            Label = label
            Summary = summary
            Hint = "None"
            Test = None
            Expectation = None
            Expected = None
            Actual = None
            ExpectedValue = None
            ActualValue = None
            Because = None
            DetailsText = None
            DiffText = None
            OriginalTest = None
            OriginalExpected = None
            OriginalActual = None
            ShrunkTest = None
            ShrunkExpected = None
            ShrunkActual = None
            NumberOfTests = None
            NumberOfShrinks = None
            Replay = None
            SourceLocation = None
        }

    let renderWith
        (options: TestifyReportOptions)
        (report: TestifyFailureReport)
        : string =
        let report = withInferredHint report
        let resolvedOptions = TestifyReportOptions.normalize options
        let fields = resolveFields resolvedOptions

        let distinctDiff =
            match report.DiffText with
            | Some diff when report.Because = Some diff -> None
            | Some diff when report.DetailsText = Some diff -> None
            | value -> value

        let valueFirstLines =
            match report.ExpectedValue, report.ActualValue with
            | Some expectedValue, Some actualValue
                when hasField fields ExpectedValue
                     && hasField fields ActualValue ->
                []
                |> appendFieldValue fields Expectation "Expectation" report.Expectation resolvedOptions.MaxValueLines
                |> appendFieldValue fields ExpectedValue "Expected value" (Some expectedValue) resolvedOptions.MaxValueLines
                |> appendFieldValue fields ActualValue "Actual value" (Some actualValue) resolvedOptions.MaxValueLines
            | _ ->
                []
                |> appendFieldValue fields Expectation "Expectation" report.Expectation resolvedOptions.MaxValueLines
                |> appendFieldValue fields Expected "Expected" report.Expected resolvedOptions.MaxValueLines
                |> appendFieldValue fields Actual "Actual" report.Actual resolvedOptions.MaxValueLines

        let becauseLines =
            []
            |> appendFieldValue fields Because "Because" report.Because resolvedOptions.MaxValueLines

        let hintLines =
            []
            |> appendFieldValue fields Hint "Hint" (Some report.Hint) resolvedOptions.MaxValueLines

        let detailLines =
            []
            |> appendFieldValue fields Details "Details" report.DetailsText resolvedOptions.MaxValueLines
            |> appendFieldValue fields Diff "Diff" distinctDiff resolvedOptions.MaxValueLines

        let originalLines =
            []
            |> appendFieldValue fields OriginalTest "Original test" report.OriginalTest resolvedOptions.MaxValueLines
            |> appendFieldValue fields OriginalExpected "Original expected" report.OriginalExpected resolvedOptions.MaxValueLines
            |> appendFieldValue fields OriginalActual "Original actual" report.OriginalActual resolvedOptions.MaxValueLines

        let shrunkLines =
            []
            |> appendFieldValue fields ShrunkTest "Shrunk test" report.ShrunkTest resolvedOptions.MaxValueLines
            |> appendFieldValue fields ShrunkExpected "Shrunk expected" report.ShrunkExpected resolvedOptions.MaxValueLines
            |> appendFieldValue fields ShrunkActual "Shrunk actual" report.ShrunkActual resolvedOptions.MaxValueLines

        let metadataLines =
            []
            |> appendFieldValue fields NumberOfTests "Tests" (report.NumberOfTests |> Option.map string) resolvedOptions.MaxValueLines
            |> appendFieldValue fields NumberOfShrinks "Shrinks" (report.NumberOfShrinks |> Option.map string) resolvedOptions.MaxValueLines
            |> appendFieldValue fields Replay "Replay" report.Replay resolvedOptions.MaxValueLines

        let locationLines =
            match report.SourceLocation with
            | Some location when hasField fields SourceLocation ->
                formatSourceLocation fields location
            | _ ->
                []

        [
            report.Summary
        ]
        |> appendMaybeLines hintLines
        |> appendMaybeLines valueFirstLines
        |> appendMaybeLines detailLines
        |> appendMaybeLines becauseLines
        |> appendMaybeLines originalLines
        |> appendMaybeLines shrunkLines
        |> appendMaybeLines locationLines
        |> appendMaybeLines metadataLines
        |> String.concat "\n"
