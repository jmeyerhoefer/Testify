namespace Testify


open System


[<RequireQualifiedAccess>]
module TestifyReport =
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

    let private normalizeHintCandidate
        (hint: string option)
        : string option =
        hint
        |> Option.bind (fun value ->
            if String.IsNullOrWhiteSpace value
               || String.Equals(value, "None", StringComparison.OrdinalIgnoreCase) then
                None
            else
                Some value)

    let inferHint
        (report: TestifyFailureReport)
        : string =
        let inferred =
            TestifySettings.HintRules
            |> List.tryPick (fun rule ->
                rule.TryInfer report
                |> normalizeHintCandidate)

        inferred
        |> Option.defaultValue "None"

    let withResolvedHint
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        let normalizedHint =
            normalizeHintCandidate (Some report.Hint)
            |> Option.defaultWith (fun () -> inferHint report)

        { report with Hint = normalizedHint }

    let withInferredHint
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        withResolvedHint report

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
        let report = withResolvedHint report
        let resolvedOptions = TestifyReportOptions.normalize options
        let fields = resolveFields resolvedOptions

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
        |> appendMaybeLines valueFirstLines
        |> appendMaybeLines detailLines
        |> appendMaybeLines becauseLines
        |> appendMaybeLines hintLines
        |> appendMaybeLines originalLines
        |> appendMaybeLines shrunkLines
        |> appendMaybeLines locationLines
        |> appendMaybeLines metadataLines
        |> String.concat "\n"
