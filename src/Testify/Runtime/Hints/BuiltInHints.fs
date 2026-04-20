namespace Testify

open System


[<AutoOpen>]
module private BuiltInHintHelpers =
    let tryGetComparableTextPairs
        (report: TestifyFailureReport)
        : (string * string) list =
        [
            report.ExpectedValue, report.ActualValue
            report.Expected, report.Actual
        ]
        |> List.choose (function
            | Some expected, Some actual -> Some(expected, actual)
            | _ -> None)

    let normalizeWhitespace
        (value: string)
        : string =
        value.Trim()

    let normalizeLineEndings
        (value: string)
        : string =
        value.Replace("\r\n", "\n")

    let tokenizeCollectionLike
        (value: string)
        : string array =
        value
            .Replace("[", String.Empty)
            .Replace("]", String.Empty)
            .Replace("(", String.Empty)
            .Replace(")", String.Empty)
            .Split([| ';'; ',' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun token -> token.Trim())
        |> Array.filter (String.IsNullOrWhiteSpace >> not)

    let containsAny
        (needles: string list)
        (text: string)
        : bool =
        needles
        |> List.exists (fun needle -> text.Contains(needle, StringComparison.OrdinalIgnoreCase))

    let isMinimalCaseText
        (value: string)
        : bool =
        let trimmed = value.Trim()

        trimmed = "[]"
        || trimmed = "\"\""
        || trimmed = "None"
        || trimmed = "0"


/// <summary>Broad, reusable hint rules for common runtime and mismatch scenarios.</summary>
[<RequireQualifiedAccess>]
module GenericHints =
    /// <summary>Hint rule for null-reference failures.</summary>
    let nullReferenceException : TestifyHintRule =
        TestifyHintRule.create "Generic.NullReferenceException" (fun report ->
            match report.ActualObservedInfo with
            | Some info when info.ExceptionType = Some "NullReferenceException" ->
                Some "This looks like a null-reference failure. Check initialization, empty/base cases, and whether a value is present before accessing it."
            | _ ->
                None)

    let divideByZeroException : TestifyHintRule =
        TestifyHintRule.create "Generic.DivideByZeroException" (fun report ->
            match report.ActualObservedInfo with
            | Some info when info.ExceptionType = Some "DivideByZeroException" ->
                Some "This looks like a division-by-zero failure. Check whether zero is allowed here or needs explicit handling before dividing."
            | _ ->
                None)

    let exceptionInsteadOfResult : TestifyHintRule =
        TestifyHintRule.create "Generic.ExceptionInsteadOfResult" (fun report ->
            match report.ActualObservedInfo, report.ExpectedObservedInfo with
            | Some actualInfo, Some expectedInfo
                when actualInfo.IsException && not expectedInfo.IsException ->
                Some "Your code raises an exception in a case that likely expects a handled result instead."
            | _ ->
                let relevantTexts =
                    [
                        Some report.Summary
                        report.Actual
                        report.ActualValue
                        report.Because
                        report.DetailsText
                    ]
                    |> List.choose id

                if relevantTexts |> List.exists (containsAny [ "exception"; "threw"; "system." ]) then
                    Some "Your code appears to raise an exception in a case that may need explicit handling."
                else
                    None)

    let unexpectedExceptionTypeMismatch : TestifyHintRule =
        TestifyHintRule.create "Generic.UnexpectedExceptionTypeMismatch" (fun report ->
            match report.ActualObservedInfo, report.ExpectedObservedInfo with
            | Some actualInfo, Some expectedInfo
                when actualInfo.IsException
                     && expectedInfo.IsException
                     && actualInfo.ExceptionType <> expectedInfo.ExceptionType ->
                Some "The code throws an exception, but not the expected kind. Check which exceptional case the assignment or test actually requires."
            | _ ->
                None)

    let sameItemsDifferentOrder : TestifyHintRule =
        TestifyHintRule.create "Generic.SameItemsDifferentOrder" (fun report ->
            tryGetComparableTextPairs report
            |> List.tryPick (fun (expected, actual) ->
                let expectedTokens = tokenizeCollectionLike expected |> Array.sort
                let actualTokens = tokenizeCollectionLike actual |> Array.sort

                if expected <> actual
                   && expectedTokens.Length > 1
                   && expectedTokens = actualTokens then
                    Some "The same elements appear to be present but in a different order."
                else
                    None))

    /// <summary>Pack containing the generic built-in hints.</summary>
    let pack : TestifyHintPack =
        TestifyHintPack.create
            "generic"
            [
                nullReferenceException
                divideByZeroException
                exceptionInsteadOfResult
                unexpectedExceptionTypeMismatch
                sameItemsDifferentOrder
            ]


/// <summary>Built-in hint rules focused on string formatting and textual comparison mistakes.</summary>
[<RequireQualifiedAccess>]
module StringHints =
    let whitespaceOnlyMismatch : TestifyHintRule =
        TestifyHintRule.create "String.WhitespaceOnlyMismatch" (fun report ->
            tryGetComparableTextPairs report
            |> List.tryPick (fun (expected, actual) ->
                if expected <> actual
                   && normalizeWhitespace expected = normalizeWhitespace actual then
                    Some "The values differ only in whitespace. Check spaces, blank lines, or trailing output."
                else
                    None))

    let caseNormalization : TestifyHintRule =
        TestifyHintRule.create "String.CaseNormalization" (fun report ->
            tryGetComparableTextPairs report
            |> List.tryPick (fun (expected, actual) ->
                if expected <> actual
                   && String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase) then
                    Some "This looks like a case-sensitivity issue. You may need to normalize case before comparing."
                else
                    None))

    let extraNewline : TestifyHintRule =
        TestifyHintRule.create "String.ExtraNewline" (fun report ->
            tryGetComparableTextPairs report
            |> List.tryPick (fun (expected, actual) ->
                let normalizedExpected = normalizeLineEndings expected
                let normalizedActual = normalizeLineEndings actual

                if normalizedExpected + "\n" = normalizedActual
                   || normalizedExpected = normalizedActual + "\n" then
                    Some "This looks like exactly one extra or missing newline."
                else
                    None))

    let pack : TestifyHintPack =
        TestifyHintPack.create
            "string"
            [
                whitespaceOnlyMismatch
                caseNormalization
                extraNewline
            ]


/// <summary>Built-in hint rules focused on shrinking behavior and property-test diagnostics.</summary>
[<RequireQualifiedAccess>]
module PropertyHints =
    let shrunkToEmpty : TestifyHintRule =
        TestifyHintRule.onFieldRegexPattern
            "Property.ShrunkToEmpty"
            HintTextField.ShrunkTest
            @"(?i)(\[\]|""""|None|\b0\b)"
            (fun _ -> "The smallest counterexample is an empty or minimal value. Your base case is a good place to check first.")

    let shrunkToSingleton : TestifyHintRule =
        TestifyHintRule.onFieldRegexPattern
            "Property.ShrunkToSingleton"
            HintTextField.ShrunkTest
            @"(?i)(\[[^\];]+\]|Some\([^\)]+\))"
            (fun _ -> "The failure survives on a one-element case, so the smallest non-empty case is likely implemented incorrectly.")

    let failsQuickly : TestifyHintRule =
        TestifyHintRule.create "Property.FailsQuickly" (fun report ->
            match report.NumberOfTests with
            | Some tests when tests <= 5 ->
                Some "The property failed after very few generated tests. This usually points to a broad logic error."
            | _ ->
                None)

    let manyShrinks : TestifyHintRule =
        TestifyHintRule.create "Property.ManyShrinks" (fun report ->
            match report.NumberOfShrinks with
            | Some shrinks when shrinks >= 20 ->
                Some "The property shrank substantially before reporting the failure. Focus on the final shrunk counterexample first."
            | _ ->
                None)

    let shrunkToExceptionOnMinimalCase : TestifyHintRule =
        TestifyHintRule.create "Property.ShrunkToExceptionOnMinimalCase" (fun report ->
            match report.ShrunkTest, report.ShrunkActualObservedInfo with
            | Some shrunkTest, Some actualInfo
                when actualInfo.IsException && isMinimalCaseText shrunkTest ->
                Some "The smallest failing case is minimal and still throws an exception. Check the base case or empty-input handling first."
            | _ ->
                None)

    let pack : TestifyHintPack =
        TestifyHintPack.create
            "property"
            [
                shrunkToEmpty
                shrunkToSingleton
                shrunkToExceptionOnMinimalCase
                failsQuickly
                manyShrinks
            ]


/// <summary>Curated groups of built-in hint packs for easy enablement.</summary>
[<RequireQualifiedAccess>]
module BuiltInHintPacks =
    /// <summary>
    /// Beginner-friendly built-in packs covering generic runtime issues, string mismatches, and common
    /// property-testing diagnostics.
    /// </summary>
    /// <example id="builtin-hint-packs-beginner-1">
    /// <code lang="fsharp">
    /// Testify.configure (
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
    /// )
    /// </code>
    /// </example>
    let beginner : TestifyHintPack list =
        [
            GenericHints.pack
            StringHints.pack
            PropertyHints.pack
        ]
