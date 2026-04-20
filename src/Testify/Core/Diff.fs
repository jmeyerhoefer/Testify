namespace Testify


open System
open System.Collections
open System.Text.RegularExpressions
open DEdge.Diffract
open Microsoft.FSharp.Reflection


/// <summary>Controls how Testify tries to explain mismatches between expected and actual values.</summary>
type DiffMode =
    | FastOnly
    | FastThenStructural
    | StructuralOnly


/// <summary>Options for Testify diff generation and truncation.</summary>
type DiffOptions =
    {
        Mode: DiffMode
        MaxLines: int
        MaxChars: int
        StructuralPrintParams: PrintParams option
    }


/// <summary>Helpers for generating short human-readable diffs for assertion and property failures.</summary>
[<RequireQualifiedAccess>]
module Diff =
    let private structuralPrefix = "Structural diff:"
    let private wrapperExpectLinePattern =
        Regex(@"^[A-Za-z_][A-Za-z0-9_]*\s+Expect\s*=\s*.+$", RegexOptions.Compiled)
    let private actualLinePattern =
        Regex(@"^\s*Actual\s*=\s*.+$", RegexOptions.Compiled)

    /// <summary>The default diff settings used by Testify expectations.</summary>
    /// <example id="diff-defaultoptions-1">
    /// <code lang="fsharp">
    /// let options = Diff.defaultOptions
    /// </code>
    /// </example>
    let defaultOptions : DiffOptions =
        {
            Mode = FastThenStructural
            MaxLines = 12
            MaxChars = 1200
            StructuralPrintParams = None
        }

    let private defaultStructuralPrintParams =
        Differ.AssertPrintParams

    let private tryRewriteWrapperScalarDiff<'T>
        (expected: 'T)
        (actual: 'T)
        (lines: string array)
        : string array option =
        let valueType = typeof<'T>

        if
            not (FSharpType.IsUnion(valueType, true))
            || lines.Length <> 2
            || not (wrapperExpectLinePattern.IsMatch(lines[0]))
            || not (actualLinePattern.IsMatch(lines[1]))
        then
            None
        else
            let unionCases = FSharpType.GetUnionCases(valueType, true)
            if unionCases.Length <> 1 || unionCases[0].GetFields().Length <> 1 then
                None
            else
                Some
                    [|
                        $"Expect = {Render.formatValue expected}"
                        $"Actual = {Render.formatValue actual}"
                    |]

    let private sanitizeStructuralDiff<'T>
        (options: DiffOptions)
        (expected: 'T)
        (actual: 'T)
        (text: string)
        : string option =
        let trimmed =
            text.TrimEnd ()

        if System.String.IsNullOrWhiteSpace trimmed then
            None
        else
            let lines =
                trimmed.Split([| '\n' |], System.StringSplitOptions.None)
                |> Array.map (fun line -> line.TrimEnd '\r')

            let normalizedLines =
                tryRewriteWrapperScalarDiff expected actual lines
                |> Option.defaultValue lines

            let truncatedLines =
                if normalizedLines.Length > options.MaxLines then
                    Array.append
                        normalizedLines[0 .. options.MaxLines - 1]
                        [| "..." |]
                else
                    normalizedLines

            let combined = String.concat "\n" truncatedLines

            let bounded =
                if combined.Length > options.MaxChars then
                    combined.Substring(0, options.MaxChars).TrimEnd() + "..."
                else
                    combined

            Some $"{structuralPrefix}\n{bounded}"

    let private renderStructuralDiff<'T>
        (options: DiffOptions)
        (expected: 'T)
        (actual: 'T)
        : string option =
        try
            let printParams =
                options.StructuralPrintParams
                |> Option.defaultValue defaultStructuralPrintParams

            Differ.ToString(
                expected,
                actual,
                Unchecked.defaultof<IDiffer<'T>>,
                printParams
            )
            |> sanitizeStructuralDiff options expected actual
        with _ ->
            None

    let private toEnumerableArray (value: objnull) : obj array =
        match value with
        | null -> [||]
        | value -> value :?> IEnumerable |> Seq.cast<obj> |> Seq.toArray

    let private formatSequenceMismatch
        (expectedItems: 'T array)
        (actualItems: 'T array)
        : string option =
        let mismatchIndex =
            Seq.zip expectedItems actualItems
            |> Seq.tryFindIndex (fun (expectedItem, actualItem) -> expectedItem <> actualItem)

        match mismatchIndex with
        | Some index ->
            Some
                $"First mismatch at index {index}: expected \
                {Render.formatValue expectedItems[index]} but got \
                {Render.formatValue actualItems[index]}."
        | None when expectedItems.Length <> actualItems.Length ->
            Some
                $"Different lengths: expected {expectedItems.Length} item(s) \
                but got {actualItems.Length}."
        | None ->
            None

    let private describeSequenceMismatch
        (expectedItems: 'T array)
        (actualItems: 'T array)
        : string option =
        if expectedItems = actualItems then
            None
        else
            formatSequenceMismatch expectedItems actualItems
            |> Option.map (fun mismatch ->
                $"Expected {Render.formatSequencePreview expectedItems} \
                but got {Render.formatSequencePreview actualItems}. {mismatch}")

    let private formatCharLiteral
        (value: char)
        : string =
        match value with
        | '\n' -> @"'\n'"
        | '\r' -> @"'\r'"
        | '\t' -> @"'\t'"
        | '\\' -> @"'\\'"
        | '\'' -> @"'\''"
        | _ when System.Char.IsControl value -> $"U+{int value:X4}"
        | _ -> $"'{value}'"

    let private formatStringWindow
        (value: string)
        (index: int)
        : string =
        let startIndex = max 0 (index - 4)
        let endExclusive = min value.Length (index + 5)
        let snippet = value.Substring(startIndex, endExclusive - startIndex)
        let prefix = if startIndex > 0 then "…" else ""
        let suffix = if endExclusive < value.Length then "…" else ""
        Render.formatValue($"{prefix}{snippet}{suffix}")

    let private describeStringLengthMismatch
        (expected: string)
        (actual: string)
        (sharedLength: int)
        : string =
        if expected.Length + 1 = actual.Length
           && actual.StartsWith(expected, System.StringComparison.Ordinal)
           && actual[sharedLength] = '\n' then
            $"Actual has one extra trailing newline at index {sharedLength}."
        elif actual.Length + 1 = expected.Length
             && expected.StartsWith(actual, System.StringComparison.Ordinal)
             && expected[sharedLength] = '\n' then
            $"Actual is missing one trailing newline at index {sharedLength}."
        elif sharedLength < actual.Length then
            let actualContext = formatStringWindow actual sharedLength
            $"Strings match through index {max 0 (sharedLength - 1)}, but actual has extra content starting at index {sharedLength}. Actual context: {actualContext}."
        else
            let expectedContext = formatStringWindow expected sharedLength
            $"Strings match through index {max 0 (sharedLength - 1)}, but expected has extra content starting at index {sharedLength}. Expected context: {expectedContext}."

    /// <summary>Describes a string mismatch, including the first differing character when possible.</summary>
    /// <param name="expected">The expected string.</param>
    /// <param name="actual">The observed string.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when the strings differ; otherwise <c>None</c>.
    /// </returns>
    let string
        (expected: string)
        (actual: string)
        : string option =
        if expected = actual then
            None
        else
            let sharedLength = min expected.Length actual.Length

            let mismatchIndex =
                [ 0 .. sharedLength - 1 ]
                |> List.tryFind (fun index -> expected[index] <> actual[index])

            match mismatchIndex with
            | Some index ->
                let expectedContext = formatStringWindow expected index
                let actualContext = formatStringWindow actual index

                Some
                    $"Expected {Render.formatValue expected} but got \
                    {Render.formatValue actual}. First mismatch at index {index}: \
                    expected {formatCharLiteral expected[index]} but got \
                    {formatCharLiteral actual[index]}. Context near mismatch: \
                    expected {expectedContext}, actual {actualContext}."
            | None ->
                Some
                    $"Expected {Render.formatValue expected} but got \
                    {Render.formatValue actual}. \
                    {describeStringLengthMismatch expected actual sharedLength}"

    /// <summary>Describes a mismatch between two sequences.</summary>
    /// <param name="expected">The expected sequence.</param>
    /// <param name="actual">The observed sequence.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when the sequences differ; otherwise <c>None</c>.
    /// </returns>
    let seq<'T when 'T: equality>
        (expected: 'T seq)
        (actual: 'T seq)
        : string option =
        let expectedItems = Seq.toArray expected
        let actualItems = Seq.toArray actual

        describeSequenceMismatch expectedItems actualItems

    /// <summary>Describes a mismatch between two lists.</summary>
    /// <param name="expected">The expected list.</param>
    /// <param name="actual">The observed list.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when the lists differ; otherwise <c>None</c>.
    /// </returns>
    let list<'T when 'T: equality>
        (expected: 'T list)
        (actual: 'T list)
        : string option =
        seq expected actual

    /// <summary>Describes a mismatch between two arrays.</summary>
    /// <param name="expected">The expected array.</param>
    /// <param name="actual">The observed array.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when the arrays differ; otherwise <c>None</c>.
    /// </returns>
    let array<'T when 'T: equality>
        (expected: 'T array)
        (actual: 'T array)
        : string option =
        seq expected actual

    /// <summary>Describes a mismatch between two option values.</summary>
    /// <param name="expected">The expected option value.</param>
    /// <param name="actual">The observed option value.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when the options differ; otherwise <c>None</c>.
    /// </returns>
    let option<'T when 'T: equality>
        (expected: 'T option)
        (actual: 'T option)
        : string option =
        match expected, actual with
        | None, None ->
            None
        | Some expectedValue, Some actualValue when expectedValue = actualValue ->
            None
        | Some expectedValue, Some actualValue ->
            Some
                $"Expected {Render.formatOption expected} but got \
                {Render.formatOption actual}. Values differ: expected \
                {Render.formatValue expectedValue} but got \
                {Render.formatValue actualValue}."
        | _ ->
            Some
                $"Expected {Render.formatOption expected} but got \
                {Render.formatOption actual}."

    /// <summary>Attempts a lightweight mismatch description without structural diffing.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The observed value.</param>
    /// <returns>
    /// <c>Some</c> human-readable explanation when a lightweight diff strategy can explain the mismatch;
    /// otherwise <c>None</c>.
    /// </returns>
    let tryDescribeFast<'T>
        (expected: 'T)
        (actual: 'T)
        : string option =
        match box expected, box actual with
        | :? string as expectedString, (:? string as actualString) ->
            string expectedString actualString
        | _ ->
            let expectedType = typeof<'T>

            if expectedType.IsArray then
                let expectedItems = toEnumerableArray (box expected)
                let actualItems = toEnumerableArray (box actual)
                describeSequenceMismatch expectedItems actualItems
            elif
                expectedType.IsGenericType
                && expectedType.GetGenericTypeDefinition () = typedefof<option<_>>
            then
                let expectedCase, expectedFields =
                    FSharpValue.GetUnionFields (expected, expectedType)

                let actualCase, actualFields =
                    FSharpValue.GetUnionFields (actual, expectedType)

                match expectedCase.Name, actualCase.Name, expectedFields, actualFields with
                | "None", "None", _, _ ->
                    None
                | "Some", "Some", [| expectedValue |], [| actualValue |]
                    when expectedValue = actualValue ->
                    None
                | _ ->
                    Some
                        $"Expected {Render.formatValue expected} but got \
                        {Render.formatValue actual}."
            elif
                typeof<IEnumerable>.IsAssignableFrom expectedType
                && expectedType <> typeof<string>
            then
                let expectedItems = toEnumerableArray (box expected)
                let actualItems = toEnumerableArray (box actual)
                describeSequenceMismatch expectedItems actualItems
            else
                None

    /// <summary>Attempts a structural mismatch description using Diffract.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The observed value.</param>
    /// <returns>
    /// <c>Some</c> structural diff text when Diffract can explain the mismatch; otherwise <c>None</c>.
    /// </returns>
    let tryDescribeStructural<'T>
        (expected: 'T)
        (actual: 'T)
        : string option =
        renderStructuralDiff defaultOptions expected actual

    /// <summary>Attempts a mismatch description using the supplied diff options.</summary>
    /// <param name="options">The diff configuration controlling strategy and truncation.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The observed value.</param>
    /// <returns>
    /// <c>Some</c> explanation when the values differ and the chosen diff strategy can describe the mismatch;
    /// otherwise <c>None</c>.
    /// </returns>
    let tryDescribeWith<'T>
        (options: DiffOptions)
        (expected: 'T)
        (actual: 'T)
        : string option =
        if obj.Equals(box expected, box actual) then
            None
        else
            match options.Mode with
            | FastOnly ->
                tryDescribeFast expected actual
            | StructuralOnly ->
                renderStructuralDiff options expected actual
            | FastThenStructural ->
                match tryDescribeFast expected actual with
                | Some description -> Some description
                | None -> renderStructuralDiff options expected actual

    /// <summary>Attempts a mismatch description using <c>defaultOptions</c>.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The observed value.</param>
    /// <returns>
    /// <c>Some</c> explanation when the values differ and Testify can describe the mismatch;
    /// otherwise <c>None</c>.
    /// </returns>
    /// <example id="diff-trydescribe-1">
    /// <code lang="fsharp">
    /// let description =
    ///     Diff.tryDescribe [ 1; 2; 3 ] [ 1; 4; 3 ]
    /// </code>
    /// </example>
    let tryDescribe<'T>
        (expected: 'T)
        (actual: 'T)
        : string option =
        tryDescribeWith defaultOptions expected actual
