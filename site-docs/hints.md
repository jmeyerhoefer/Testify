---
title: Hints and Feedback
---

# Hints and Feedback

Hints are one of the places where Testify stops being “just another assertion library.”

A failure can do more than report a mismatch. It can also suggest a likely cause, point the reader toward a debugging direction, and explain recurring mistake patterns in a way that is useful for:

- teaching
- onboarding
- diagnostics
- assignment feedback
- domain-specific test suites

## What A Hint Is

A hint is a short explanatory message attached to a `TestifyFailureReport`.

Hints can come from:

- explicit hints already present on the report
- `HintInference`, which runs configured hint rules
- configured hint packs that group rules together

By the time you render with `Assert.toDisplayString` or `Check.toDisplayString`, Testify can merge:

- the core mismatch report
- diff text
- inferred hints
- configured domain hints

That is why hinting feels integrated into reporting rather than bolted on afterward.

## Two Ways To Author Hints

There are two main authoring styles.

### 1. Field-mapped regex hints

Use `TestifyHintRule.onFieldRegexPattern` when the rule can be expressed as:

- inspect one text field from the report
- run a regex
- build a hint if it matches

Example:

```fsharp
let trailingWhitespace =
    TestifyHintRule.onFieldRegexPattern
        "String.TrailingWhitespace"
        HintTextField.ActualValue
        @"\s+$"
        (fun _ -> "The value appears to end with trailing whitespace.")
```

This is the most direct answer to “what is mappable?” in the hint system: the selected `HintTextField` values are the text surfaces you can map from when you use the regex-based helpers.

### 2. Full-report rules

Use `TestifyHintRule.create` when the hint depends on structured facts rather than one text field.

Example:

```fsharp
let failsQuickly =
    TestifyHintRule.create "Property.FailsQuickly" (fun report ->
        match report.NumberOfTests with
        | Some tests when tests <= 5 ->
            Some "The property failed after very few generated tests. This usually points to a broad logic error."
        | _ ->
            None)
```

This form is stronger than field mapping because it can inspect the whole `TestifyFailureReport`, including:

- observed exception metadata
- shrink counts
- replay data
- original vs shrunk property text
- any of the optional rendered text fields

## What Is Mappable?

When you use `HintTextField`, these are the report text surfaces that regex rules can inspect.

### Core assertion text

| Field | Meaning |
| --- | --- |
| `Summary` | The top-level summary of the failure |
| `Test` | The rendered tested expression |
| `Expectation` | The expectation description |

### Rendered values and explanation text

| Field | Meaning |
| --- | --- |
| `Expected` | The rendered expected text |
| `Actual` | The rendered actual text |
| `ExpectedValue` | The raw expected value display text |
| `ActualValue` | The raw actual value display text |
| `Because` | The explanation text |
| `Details` | The details section text |
| `Diff` | The diff text extracted into the report |

### Property-check lifecycle text

| Field | Meaning |
| --- | --- |
| `OriginalTest` | The unshrunk property test text |
| `OriginalExpected` | The unshrunk expected text |
| `OriginalActual` | The unshrunk actual text |
| `ShrunkTest` | The final shrunk property test text |
| `ShrunkExpected` | The final shrunk expected text |
| `ShrunkActual` | The final shrunk actual text |
| `Replay` | The replay token text |

Important practical detail:

- not every field is present on every failure
- assertion-style failures and property-style failures populate different subsets
- that is why field-based rules naturally decline by returning no hint when the field is missing

## Built-In Hint Packs By Specialty

Testify ships ready-made hint groups that cover different kinds of failures.

### `GenericHints`

These focus on broad recurring problems:

- null references
- divide-by-zero failures
- exception-vs-result mismatches
- unexpected exception type mismatches
- “same items, different order” sequence issues

### `StringHints`

These focus on textual mismatches where the raw expected/actual values are often not enough:

- whitespace-only mismatch
- case-normalization issues
- exactly one extra or missing newline

### `PropertyHints`

These focus on property-testing behavior:

- shrinking to empty or singleton cases
- failures that happen very quickly
- many-shrink scenarios
- minimal failing cases that still throw

### `MiniHints`

These are course/Mini-oriented teaching hints from the presets layer, for example:

- placeholder `TODO` implementations
- natural-number literal suffix hints

### `BuiltInHintPacks`

These are curated combinations of packs. The most convenient entry point is:

```fsharp
BuiltInHintPacks.beginner
```

which combines:

- `GenericHints.pack`
- `StringHints.pack`
- `PropertyHints.pack`

## What `HintInference` Does

`HintInference` is the bridge between raw failure data and user-facing guidance.

Conceptually it:

1. starts from the `TestifyFailureReport`
2. runs configured `TestifyHintRule`s
3. runs rules from configured `TestifyHintPack`s
4. normalizes and de-duplicates the resulting hint texts

That is why hints become part of the rendered failure instead of feeling like a separate post-processing step.

## String Mismatch Diagnostics: Hints And Diffs

String diagnostics are a good place to see the difference between:

- a hint
- a diff-style explanation

Hints answer:

- “what class of mistake does this resemble?”

Diffs answer:

- “where do the values diverge?”

### Raw string explanation with `Diff`

You can ask `Diff` for a direct explanation:

```fsharp
let explanation =
    Diff.tryDescribe "MiniLib" "MiniLib "
```

For strings, this can describe:

- the first differing character
- context around the mismatch
- extra or missing trailing newline situations

For more complex values, the `Diff` helpers can also fall back to Diffract-backed structural diffing through `Diff.tryDescribeWith` and the diff-enabled expectations.

### Diff-oriented assertion output

You can also use a diff-aware expectation:

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalToWithDiff Diff.defaultOptions "MiniLib")
        <@ "MiniLib " @>
```

This is useful when you want the failure report itself to carry a more explicit mismatch explanation.

### Add string hints on top

Now layer in built-in string hints:

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
)
```

At that point, a rendered failure can combine:

- diff-style text showing where the mismatch occurs
- string-oriented hints suggesting likely causes such as whitespace-only mismatch, case mismatch, or an extra newline

That combination is much stronger than a plain equality failure.

## Before / After

Without hinting, this kind of assertion gives you only the mismatch and whatever built-in explanation the expectation provides:

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalToWithDiff Diff.defaultOptions "MiniLib")
        <@ "MiniLib " @>
```

With string-focused hints enabled, the same failure can become more informative:

- raw mismatch
  - expected `"MiniLib"`
  - actual `"MiniLib "`
- diff-style explanation
  - there is extra trailing content or a first mismatch near the end
- hinted interpretation
  - this may differ only in whitespace or a trailing newline

That is the difference between “I see the test failed” and “I already have a credible debugging hypothesis.”

## Custom Hint Pack

Here is a minimal custom rule based on one report field and a regex:

```fsharp
let trailingSpaceHint =
    TestifyHintRule.onFieldRegexPattern
        "Course.TrailingSpace"
        HintTextField.ActualValue
        @"\s+$"
        (fun _ -> "The actual value appears to end with trailing whitespace.")
```

Turn that rule into a pack:

```fsharp
let courseHints =
    TestifyHintPack.create
        "course"
        [ trailingSpaceHint ]
```

And install it:

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks (BuiltInHintPacks.beginner @ [ courseHints ])
)
```

## Custom Domain Example

Suppose a course or codebase has a common mistake: people accidentally leave a placeholder `TODO` implementation in place.

You can encode that directly:

```fsharp
let todoHint =
    TestifyHintRule.onFieldRegexPattern
        "Course.TodoPlaceholder"
        HintTextField.Actual
        @"\bTODO\b"
        (fun _ -> "Implementation placeholder detected. Replace the TODO with the real logic.")

let courseHints =
    TestifyHintPack.create "course" [ todoHint ]
```

If you need more than text matching, move to `TestifyHintRule.create` and inspect the whole report.

## Good Uses For Hints

Hints are best when they:

- point to likely causes, not guaranteed truths
- are short and actionable
- capture recurring failure patterns
- help readers debug faster without replacing the actual failure data

Hints are less useful when they:

- merely restate the failure
- overfit one tiny test
- sound certain when they are only heuristics

## Relationship To Rendering

Hints become visible through the normal rendering pipeline:

- `Assert.toDisplayString`
- `Check.toDisplayString`
- `toFailureReport`
- collectors and aggregated failures

So the hint system is part of Testify’s reporting story, not a subsystem the reader has to wire manually for every single test.

For the result/rendering side of that pipeline, continue with [Results, Rendering, and Failure Reports](results-reporting.html).
