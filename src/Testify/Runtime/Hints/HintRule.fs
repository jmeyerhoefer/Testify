namespace Testify

open System.Text.RegularExpressions


/// <summary>Selectable text fields from a failure report that hint rules can inspect.</summary>
[<RequireQualifiedAccess>]
type HintTextField =
    /// <summary>The top-level summary text.</summary>
    | Summary
    /// <summary>The rendered tested expression.</summary>
    | Test
    /// <summary>The expectation description.</summary>
    | Expectation
    /// <summary>The rendered expected text.</summary>
    | Expected
    /// <summary>The rendered actual text.</summary>
    | Actual
    /// <summary>The raw expected value display text.</summary>
    | ExpectedValue
    /// <summary>The raw actual value display text.</summary>
    | ActualValue
    /// <summary>The explanation/because text.</summary>
    | Because
    /// <summary>The details text.</summary>
    | Details
    /// <summary>The diff text.</summary>
    | Diff
    /// <summary>The original unshrunk property test text.</summary>
    | OriginalTest
    /// <summary>The original unshrunk expected text.</summary>
    | OriginalExpected
    /// <summary>The original unshrunk actual text.</summary>
    | OriginalActual
    /// <summary>The final shrunk property test text.</summary>
    | ShrunkTest
    /// <summary>The final shrunk expected text.</summary>
    | ShrunkExpected
    /// <summary>The final shrunk actual text.</summary>
    | ShrunkActual
    /// <summary>The replay token text.</summary>
    | Replay


/// <summary>One rule that tries to infer a hint from a structured failure report.</summary>
/// <remarks>
/// The rule may decline to produce a hint by returning <c>None</c>.
/// </remarks>
type TestifyHintRule =
    {
        /// <summary>The stable rule name.</summary>
        Name: string
        /// <summary>The inference callback that tries to produce one hint from a failure report.</summary>
        TryInfer: TestifyFailureReport -> string option
    }


/// <summary>Helpers for building reusable hint rules.</summary>
[<RequireQualifiedAccess>]
module TestifyHintRule =
    /// <summary>Creates a hint rule from an arbitrary inference callback.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="tryInfer">The callback that inspects the failure report and optionally returns a hint.</param>
    /// <returns>A hint rule that can be enabled directly or added to a hint pack.</returns>
    /// <seealso cref="M:Testify.TestifyHintPack.create(System.String,Microsoft.FSharp.Collections.FSharpList{Testify.TestifyHintRule})">
    /// Use <c>TestifyHintPack.create</c> to group several rules together.
    /// </seealso>
    /// <example id="hint-rule-create-1">
    /// <code lang="fsharp">
    /// let exceptionHint =
    ///     TestifyHintRule.create "Course.Exception" (fun report ->
    ///         match report.ActualObservedInfo with
    ///         | Some info when info.IsException -> Some "Your code raises an exception here."
    ///         | _ -> None)
    /// </code>
    /// </example>
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

    /// <summary>Creates a regex-based hint rule against one selected report field.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="field">The report field whose text should be inspected.</param>
    /// <param name="pattern">The compiled regex used to detect the hint condition.</param>
    /// <param name="buildHint">Builds the final hint text from the successful regex match.</param>
    /// <returns>A hint rule that only fires when the selected field matches the regex.</returns>
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

    /// <summary>Creates a regex-based hint rule from a regex pattern string.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="field">The report field whose text should be inspected.</param>
    /// <param name="pattern">The regex pattern string used to build a compiled regex.</param>
    /// <param name="buildHint">Builds the final hint text from the successful regex match.</param>
    /// <returns>A hint rule that only fires when the selected field matches the compiled regex.</returns>
    /// <seealso cref="M:Testify.TestifyHintRule.onFieldRegex(System.String,Testify.HintTextField,System.Text.RegularExpressions.Regex,Microsoft.FSharp.Core.FSharpFunc{System.Text.RegularExpressions.Match,System.String})">
    /// Use <c>onFieldRegex</c> if you already have a compiled regex instance.
    /// </seealso>
    /// <example id="hint-rule-regex-1">
    /// <code lang="fsharp">
    /// let trailingWhitespace =
    ///     TestifyHintRule.onFieldRegexPattern
    ///         "String.TrailingWhitespace"
    ///         HintTextField.ActualValue
    ///         @"\s+$"
    ///         (fun _ -> "The value appears to end with trailing whitespace.")
    /// </code>
    /// </example>
    let onFieldRegexPattern
        (name: string)
        (field: HintTextField)
        (pattern: string)
        (buildHint: Match -> string)
        : TestifyHintRule =
        let regex = Regex(pattern, RegexOptions.Compiled)
        onFieldRegex name field regex buildHint
