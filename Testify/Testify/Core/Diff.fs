namespace Testify


open System.Collections
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

    /// <summary>The default diff settings used by Testify expectations.</summary>
    let defaultOptions : DiffOptions =
        {
            Mode = FastThenStructural
            MaxLines = 12
            MaxChars = 1200
            StructuralPrintParams = None
        }

    let private defaultStructuralPrintParams =
        Differ.AssertPrintParams

    let private sanitizeStructuralDiff
        (options: DiffOptions)
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

            let truncatedLines =
                if lines.Length > options.MaxLines then
                    Array.append
                        lines[0 .. options.MaxLines - 1]
                        [| "..." |]
                else
                    lines

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
            |> sanitizeStructuralDiff options
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

    /// <summary>Describes a string mismatch, including the first differing character when possible.</summary>
    let string
        (expected: string)
        (actual: string)
        : string option =
        if expected = actual then
            None
        else
            formatSequenceMismatch (expected.ToCharArray ()) (actual.ToCharArray ())
            |> Option.map (fun mismatch ->
                $"Expected {Render.formatValue expected} but got \
                {Render.formatValue actual}. {mismatch}")

    /// <summary>Describes a mismatch between two sequences.</summary>
    let seq<'T when 'T: equality>
        (expected: 'T seq)
        (actual: 'T seq)
        : string option =
        let expectedItems = Seq.toArray expected
        let actualItems = Seq.toArray actual

        describeSequenceMismatch expectedItems actualItems

    /// <summary>Describes a mismatch between two lists.</summary>
    let list<'T when 'T: equality>
        (expected: 'T list)
        (actual: 'T list)
        : string option =
        seq expected actual

    /// <summary>Describes a mismatch between two arrays.</summary>
    let array<'T when 'T: equality>
        (expected: 'T array)
        (actual: 'T array)
        : string option =
        seq expected actual

    /// <summary>Describes a mismatch between two option values.</summary>
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
    let tryDescribeStructural<'T>
        (expected: 'T)
        (actual: 'T)
        : string option =
        renderStructuralDiff defaultOptions expected actual

    /// <summary>Attempts a mismatch description using the supplied diff options.</summary>
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
    let tryDescribe<'T>
        (expected: 'T)
        (actual: 'T)
        : string option =
        tryDescribeWith defaultOptions expected actual
