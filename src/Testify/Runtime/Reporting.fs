namespace Testify


open System
open System.Text.Json
open System.Text.Json.Nodes


[<RequireQualifiedAccess>]
module TestifyReport =
    let private jsonRenderOptions =
        JsonSerializerOptions(WriteIndented = true)

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

    let private normalizeTextCandidate
        (value: string option)
        : string option =
        value
        |> Option.bind (fun text ->
            if String.IsNullOrWhiteSpace text then
                None
            else
                Some text)

    let inferHints
        (report: TestifyFailureReport)
        : string list =
        HintInference.inferHints report

    let withResolvedHints
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        HintInference.withResolvedHints report

    let withInferredHints
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        withResolvedHints report

    // This is the single edit point for what each verbosity contains.
    let rec fieldsForVerbosity
        (verbosity: Verbosity)
        : Set<ReportField> =
        match verbosity with
        | Verbosity.Quiet -> set [
            Summary
            Hints
            Expected
            Actual ]
        | Verbosity.Normal -> set [
            Summary
            Hints
            Expected
            Actual
            Because ]
        | Verbosity.Detailed -> set [
            Summary
            Hints
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
            Hints
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

    let private visibleText
        (fields: Set<ReportField>)
        (field: ReportField)
        (maxLines: int)
        (value: string option)
        : string option =
        if hasField fields field then
            value
            |> normalizeTextCandidate
            |> Option.map (truncateTextLines maxLines)
        else
            None

    let private visibleHints
        (fields: Set<ReportField>)
        (maxLines: int)
        (hints: string list)
        : string list =
        if hasField fields Hints then
            hints
            |> HintInference.normalizeHints
            |> List.map (truncateTextLines maxLines)
        else
            []

    let private visibleInt
        (fields: Set<ReportField>)
        (field: ReportField)
        (value: int option)
        : int option =
        if hasField fields field then
            value
        else
            None

    let private visibleSourceLocation
        (fields: Set<ReportField>)
        (location: Diagnostics.SourceLocation option)
        : Diagnostics.SourceLocation option =
        if hasField fields SourceLocation then
            location
        else
            None

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
            Hints = []
            Test = None
            Expectation = None
            Expected = None
            Actual = None
            ExpectedValue = None
            ActualValue = None
            ExpectedObservedInfo = None
            ActualObservedInfo = None
            Because = None
            DetailsText = None
            DiffText = None
            OriginalTest = None
            OriginalExpected = None
            OriginalActual = None
            OriginalExpectedObservedInfo = None
            OriginalActualObservedInfo = None
            ShrunkTest = None
            ShrunkExpected = None
            ShrunkActual = None
            ShrunkExpectedObservedInfo = None
            ShrunkActualObservedInfo = None
            NumberOfTests = None
            NumberOfShrinks = None
            Replay = None
            SourceLocation = None
        }

    let private setJsonTextProperty
        (name: string)
        (value: string option)
        (target: JsonObject)
        : unit =
        match value with
        | Some text -> target[name] <- JsonValue.Create(text)
        | None -> ()

    let private setJsonIntProperty
        (name: string)
        (value: int option)
        (target: JsonObject)
        : unit =
        match value with
        | Some count -> target[name] <- JsonValue.Create(count)
        | None -> ()

    let private setJsonStringArrayProperty
        (name: string)
        (values: string list)
        (target: JsonObject)
        : unit =
        let array = JsonArray()

        values
        |> List.iter (fun value -> array.Add(JsonValue.Create(value)))

        target[name] <- array

    let private setJsonObservedInfoProperty
        (name: string)
        (value: TestifyObservedInfo option)
        (target: JsonObject)
        : unit =
        match value with
        | None -> ()
        | Some info ->
            let observed = JsonObject()
            observed["isException"] <- JsonValue.Create(info.IsException)
            setJsonTextProperty "display" (normalizeTextCandidate info.Display) observed
            setJsonTextProperty "exceptionType" (normalizeTextCandidate info.ExceptionType) observed
            setJsonTextProperty "exceptionMessage" (normalizeTextCandidate info.ExceptionMessage) observed
            target[name] <- observed

    let private renderJsonNode
        (node: JsonNode)
        : string =
        node.ToJsonString(jsonRenderOptions)

    let private reportKindText
        (kind: TestifyFailureKind)
        : string =
        match kind with
        | AssertionFailure -> "assertion"
        | PropertyFailure
        | PropertyExhausted
        | PropertyError -> "property"

    let private reportOutcomeText
        (kind: TestifyFailureKind)
        : string =
        match kind with
        | AssertionFailure
        | PropertyFailure -> "failed"
        | PropertyExhausted -> "exhausted"
        | PropertyError -> "errored"

    let toJsonObjectWith
        (options: TestifyReportOptions)
        (report: TestifyFailureReport)
        : JsonObject =
        let report = withResolvedHints report
        let resolvedOptions = TestifyReportOptions.normalize options
        let fields = resolveFields resolvedOptions
        let json = JsonObject()

        json["kind"] <- JsonValue.Create(reportKindText report.Kind)
        json["outcome"] <- JsonValue.Create(reportOutcomeText report.Kind)

        setJsonTextProperty "summary" (visibleText fields Summary resolvedOptions.MaxValueLines (Some report.Summary)) json
        setJsonTextProperty "label" (normalizeTextCandidate report.Label) json
        setJsonTextProperty "testedExpression" (normalizeTextCandidate report.Test |> Option.map (truncateTextLines resolvedOptions.MaxValueLines)) json
        setJsonTextProperty "expectation" (visibleText fields Expectation resolvedOptions.MaxValueLines report.Expectation) json
        setJsonTextProperty "expected" (visibleText fields Expected resolvedOptions.MaxValueLines report.Expected) json
        setJsonTextProperty "actual" (visibleText fields Actual resolvedOptions.MaxValueLines report.Actual) json
        setJsonTextProperty "expectedValue" (visibleText fields ExpectedValue resolvedOptions.MaxValueLines report.ExpectedValue) json
        setJsonTextProperty "actualValue" (visibleText fields ActualValue resolvedOptions.MaxValueLines report.ActualValue) json
        setJsonObservedInfoProperty
            "expectedObserved"
            (if hasField fields Expected then report.ExpectedObservedInfo else None)
            json
        setJsonObservedInfoProperty
            "actualObserved"
            (if hasField fields Actual then report.ActualObservedInfo else None)
            json
        setJsonTextProperty "because" (visibleText fields Because resolvedOptions.MaxValueLines report.Because) json
        setJsonTextProperty "details" (visibleText fields Details resolvedOptions.MaxValueLines report.DetailsText) json
        setJsonTextProperty "diff" (visibleText fields Diff resolvedOptions.MaxValueLines report.DiffText) json
        setJsonTextProperty "originalTestedExpression" (visibleText fields OriginalTest resolvedOptions.MaxValueLines report.OriginalTest) json
        setJsonTextProperty "originalExpected" (visibleText fields OriginalExpected resolvedOptions.MaxValueLines report.OriginalExpected) json
        setJsonTextProperty "originalActual" (visibleText fields OriginalActual resolvedOptions.MaxValueLines report.OriginalActual) json
        setJsonObservedInfoProperty
            "originalExpectedObserved"
            (if hasField fields OriginalExpected then report.OriginalExpectedObservedInfo else None)
            json
        setJsonObservedInfoProperty
            "originalActualObserved"
            (if hasField fields OriginalActual then report.OriginalActualObservedInfo else None)
            json
        setJsonTextProperty "shrunkTestedExpression" (visibleText fields ShrunkTest resolvedOptions.MaxValueLines report.ShrunkTest) json
        setJsonTextProperty "shrunkExpected" (visibleText fields ShrunkExpected resolvedOptions.MaxValueLines report.ShrunkExpected) json
        setJsonTextProperty "shrunkActual" (visibleText fields ShrunkActual resolvedOptions.MaxValueLines report.ShrunkActual) json
        setJsonObservedInfoProperty
            "shrunkExpectedObserved"
            (if hasField fields ShrunkExpected then report.ShrunkExpectedObservedInfo else None)
            json
        setJsonObservedInfoProperty
            "shrunkActualObserved"
            (if hasField fields ShrunkActual then report.ShrunkActualObservedInfo else None)
            json
        setJsonIntProperty "numberOfTests" (visibleInt fields NumberOfTests report.NumberOfTests) json
        setJsonIntProperty "numberOfShrinks" (visibleInt fields NumberOfShrinks report.NumberOfShrinks) json
        setJsonTextProperty "replay" (visibleText fields Replay resolvedOptions.MaxValueLines report.Replay) json

        if hasField fields Hints then
            setJsonStringArrayProperty "hints" (visibleHints fields resolvedOptions.MaxValueLines report.Hints) json

        match visibleSourceLocation fields report.SourceLocation with
        | Some location ->
            let sourceLocation = JsonObject()

            sourceLocation["filePath"] <- JsonValue.Create(location.FilePath)
            sourceLocation["line"] <- JsonValue.Create(location.Line)

            match location.Column with
            | Some column -> sourceLocation["column"] <- JsonValue.Create(column)
            | None -> ()

            json["sourceLocation"] <- sourceLocation
        | None -> ()

        json

    let private renderWallOfTextWith
        (options: TestifyReportOptions)
        (report: TestifyFailureReport)
        : string =
        let report = withResolvedHints report
        let resolvedOptions = TestifyReportOptions.normalize options
        let fields = resolveFields resolvedOptions

        let appendMaybeLines
            (items: string list)
            (lines: string list)
            : string list =
            match items with
            | [] -> lines
            | values -> lines @ values

        let appendLabeledValue
            (label: string)
            (value: string option)
            (lines: string list)
            : string list =
            match value with
            | Some text -> lines @ [ $"{label}: {text}" ]
            | None -> lines

        let valueFirstLines =
            let expectation = visibleText fields Expectation resolvedOptions.MaxValueLines report.Expectation
            let expected = visibleText fields Expected resolvedOptions.MaxValueLines report.Expected
            let actual = visibleText fields Actual resolvedOptions.MaxValueLines report.Actual
            let expectedValue = visibleText fields ExpectedValue resolvedOptions.MaxValueLines report.ExpectedValue
            let actualValue = visibleText fields ActualValue resolvedOptions.MaxValueLines report.ActualValue

            match expectedValue, actualValue with
            | Some _, Some _ ->
                []
                |> appendLabeledValue "Expectation" expectation
                |> appendLabeledValue "Expected value" expectedValue
                |> appendLabeledValue "Actual value" actualValue
            | _ ->
                []
                |> appendLabeledValue "Expectation" expectation
                |> appendLabeledValue "Expected" expected
                |> appendLabeledValue "Actual" actual

        let becauseLines =
            []
            |> appendLabeledValue "Because" (visibleText fields Because resolvedOptions.MaxValueLines report.Because)

        let hintLines =
            match visibleHints fields resolvedOptions.MaxValueLines report.Hints with
            | [] -> []
            | [ hint ] -> [ $"Hint: {hint}" ]
            | hints ->
                "Hints:"
                :: (hints |> List.mapi (fun index hint -> $"{index + 1}. {hint}"))

        let detailLines =
            []
            |> appendLabeledValue "Details" (visibleText fields Details resolvedOptions.MaxValueLines report.DetailsText)

        let originalLines =
            []
            |> appendLabeledValue "Original test" (visibleText fields OriginalTest resolvedOptions.MaxValueLines report.OriginalTest)
            |> appendLabeledValue "Original expected" (visibleText fields OriginalExpected resolvedOptions.MaxValueLines report.OriginalExpected)
            |> appendLabeledValue "Original actual" (visibleText fields OriginalActual resolvedOptions.MaxValueLines report.OriginalActual)

        let shrunkLines =
            []
            |> appendLabeledValue "Shrunk test" (visibleText fields ShrunkTest resolvedOptions.MaxValueLines report.ShrunkTest)
            |> appendLabeledValue "Shrunk expected" (visibleText fields ShrunkExpected resolvedOptions.MaxValueLines report.ShrunkExpected)
            |> appendLabeledValue "Shrunk actual" (visibleText fields ShrunkActual resolvedOptions.MaxValueLines report.ShrunkActual)

        let metadataLines =
            []
            |> appendLabeledValue "Tests" (visibleInt fields NumberOfTests report.NumberOfTests |> Option.map string)
            |> appendLabeledValue "Shrinks" (visibleInt fields NumberOfShrinks report.NumberOfShrinks |> Option.map string)
            |> appendLabeledValue "Replay" (visibleText fields Replay resolvedOptions.MaxValueLines report.Replay)

        let locationLines =
            match visibleSourceLocation fields report.SourceLocation with
            | Some location -> Render.splitLines (Diagnostics.formatLocation location)
            | None -> []

        let summary =
            visibleText fields Summary resolvedOptions.MaxValueLines (Some report.Summary)
            |> Option.defaultValue report.Summary

        [
            summary
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

    let renderStatusWith
        (options: TestifyReportOptions)
        (kind: string)
        (outcome: string)
        (summary: string)
        : string =
        let resolvedOptions = TestifyReportOptions.normalize options

        match resolvedOptions.OutputFormat with
        | OutputFormat.WallOfText ->
            summary
        | OutputFormat.Json ->
            let json = JsonObject()
            json["kind"] <- JsonValue.Create(kind)
            json["outcome"] <- JsonValue.Create(outcome)
            json["summary"] <- JsonValue.Create(summary)
            renderJsonNode json
        | _ ->
            summary

    let renderCollectionWith
        (options: TestifyReportOptions)
        (kind: string)
        (summary: string)
        (reports: TestifyFailureReport list)
        : string =
        let resolvedOptions = TestifyReportOptions.normalize options

        match resolvedOptions.OutputFormat with
        | OutputFormat.WallOfText ->
            let renderedFailures =
                reports
                |> List.map (renderWallOfTextWith resolvedOptions)
                |> String.concat "\n\n---\n\n"

            if String.IsNullOrWhiteSpace renderedFailures then
                summary
            else
                $"{summary}\n\n{renderedFailures}"
        | OutputFormat.Json ->
            let json = JsonObject()
            let failures = JsonArray()

            json["kind"] <- JsonValue.Create(kind)
            json["outcome"] <- JsonValue.Create("failed")
            json["summary"] <- JsonValue.Create(summary)
            json["failureCount"] <- JsonValue.Create(reports.Length)

            for report in reports do
                failures.Add(toJsonObjectWith resolvedOptions report)

            json["failures"] <- failures
            renderJsonNode json
        | _ ->
            let renderedFailures =
                reports
                |> List.map (renderWallOfTextWith resolvedOptions)
                |> String.concat "\n\n---\n\n"

            if String.IsNullOrWhiteSpace renderedFailures then
                summary
            else
                $"{summary}\n\n{renderedFailures}"

    let renderWith
        (options: TestifyReportOptions)
        (report: TestifyFailureReport)
        : string =
        let resolvedOptions = TestifyReportOptions.normalize options

        match resolvedOptions.OutputFormat with
        | OutputFormat.WallOfText -> renderWallOfTextWith resolvedOptions report
        | OutputFormat.Json -> toJsonObjectWith resolvedOptions report |> renderJsonNode
        | _ -> renderWallOfTextWith resolvedOptions report
