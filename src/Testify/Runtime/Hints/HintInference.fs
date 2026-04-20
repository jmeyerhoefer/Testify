namespace Testify

open System
open System.Collections.Generic


[<RequireQualifiedAccess>]
module HintInference =
    /// <summary>Normalizes one optional hint candidate by trimming and dropping empty placeholder values.</summary>
    /// <param name="hint">The optional hint candidate.</param>
    /// <returns>A cleaned hint or <c>None</c> when the candidate should be ignored.</returns>
    let normalizeHintCandidate
        (hint: string option)
        : string option =
        hint
        |> Option.bind (fun value ->
            let trimmed = value.Trim()

            if String.IsNullOrWhiteSpace trimmed || String.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase) then
                None
            else
                Some trimmed)

    /// <summary>Removes duplicate hints while keeping the first occurrence order.</summary>
    /// <param name="hints">The candidate hints.</param>
    /// <returns>The de-duplicated hints in first-seen order.</returns>
    let dedupePreservingOrder
        (hints: string list)
        : string list =
        let seen = HashSet<string>(StringComparer.Ordinal)

        hints
        |> List.filter (fun hint -> seen.Add hint)

    let normalizeHints
        (hints: string list)
        : string list =
        hints
        |> List.choose (fun hint -> normalizeHintCandidate (Some hint))
        |> dedupePreservingOrder

    let private inferFromRules
        (rules: TestifyHintRule list)
        (report: TestifyFailureReport)
        : string list =
        rules
        |> List.choose (fun rule ->
            rule.TryInfer report
            |> normalizeHintCandidate)

    /// <summary>Infers hints from the currently configured hint rules and hint packs.</summary>
    /// <param name="report">The failure report to inspect.</param>
    /// <returns>The resolved inferred hints in stable order.</returns>
    /// <remarks>
    /// This uses the active globally configured <c>HintRules</c> and <c>HintPacks</c>.
    /// </remarks>
    /// <seealso cref="M:Testify.Testify.configure(Testify.TestifyConfig)">
    /// Global configuration determines which hint rules and packs participate here.
    /// </seealso>
    /// <example id="hint-inference-1">
    /// <code lang="fsharp">
    /// let hints = HintInference.inferHints report
    /// </code>
    /// </example>
    let inferHints
        (report: TestifyFailureReport)
        : string list =
        let configuredHints =
            inferFromRules TestifySettings.HintRules report

        let packedHints =
            TestifySettings.HintPacks
            |> List.collect (fun pack -> inferFromRules pack.Rules report)

        configuredHints @ packedHints
        |> dedupePreservingOrder

    /// <summary>Adds normalized explicit and inferred hints back onto a failure report.</summary>
    /// <param name="report">The failure report to enrich.</param>
    /// <returns>A copy of <paramref name="report" /> whose <c>Hints</c> field contains resolved hints.</returns>
    let withResolvedHints
        (report: TestifyFailureReport)
        : TestifyFailureReport =
        let manualHints = normalizeHints report.Hints
        let inferredHints = inferHints report

        {
            report with
                Hints = dedupePreservingOrder (manualHints @ inferredHints)
        }
