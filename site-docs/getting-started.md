---
title: Getting Started
---

# Getting Started

This page is the shortest path from “I just opened Testify” to “I understand the model.”

> [!WARNING]
> Testify and these docs are still under active development. Treat the examples here as the current intended direction, not as a claim that every surface is fully finished or thoroughly validated.

## Build The Library

From the repository root:

```powershell
dotnet build .\src\Testify\Testify.fsproj --no-restore
```

That produces:

- the compiled library assembly
- the XML docs consumed by FsDocs

## Build The Documentation Site

```powershell
.\build-docs.ps1
```

That will:

- restore the pinned local `fsdocs-tool`
- build the library
- run the API-doc coverage check
- generate the site into `output/docs/`

For live preview:

```powershell
.\watch-docs.ps1
```

`watch-docs.ps1` keeps rebuilding and serving the site as files change, but it no longer auto-opens the docs in a browser.

## Three-Minute Tour

### 1. Direct fail-fast assertion

```fsharp
open Testify
open Testify.AssertOperators

<@ 1 + 2 @> =? 3
```

This is the short, fail-fast path:

- `=?` is operator sugar
- it uses the same engine as `Assert.should`
- it raises immediately if the assertion fails

### 2. Inspect the result instead of throwing

When you want control instead of immediate failure, switch to `result`:

```fsharp
open Testify

let result =
    Assert.result
        (AssertExpectation.equalTo 3)
        <@ 1 + 2 @>

let rendered = Assert.toDisplayString result
```

This is the first big Testify design rule:

- `result` returns a structured outcome
- `should` fails fast

### 2.5. One Idea, Three Syntax Layers

The same assertion can be written in three equivalent ways:

```fsharp
Assert.should
    (AssertExpectation.equalTo 3)
    <@ 1 + 2 @>
```

```fsharp
<@ 1 + 2 @> |>? AssertExpectation.equalTo 3
```

```fsharp
<@ 1 + 2 @> =? 3
```

That is the core Testify DSL pattern:

- the named API is the most explicit
- the expectation-application form is lighter
- the shorthand operator is the tersest fail-fast surface syntax

### 3. Property check against a trusted reference

```fsharp
open Testify
open Testify.CheckOperators

<@ List.rev >> List.rev @> |=> id
```

This means:

- generate inputs
- run the quoted tested function
- run the reference function
- compare both with `CheckExpectation.equalToReference`

If you want the structured result instead:

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        id,
        <@ List.rev >> List.rev @>)
```

### 4. One glimpse of the “teaching power”

Testify can do more than tell you that something failed. It can also attach configured or inferred hints.

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
)

let result =
    Assert.result
        (AssertExpectation.equalTo "MiniLib")
        <@ "MiniLib " @>

let rendered = Assert.toDisplayString result
```

With hint packs enabled, the rendered output can point out likely causes such as whitespace-only mismatches instead of only showing raw expected/actual text.

## Why Quotations?

The `<@ ... @>` syntax is what lets Testify keep the tested code visible in diagnostics.

Without quotations, a library usually sees only the final value or boolean result. With quotations, Testify can retain and render:

- the tested expression itself
- structured expected/actual displays
- diff text
- richer failure reports
- hint-driven guidance

That is why Testify feels more explanatory than plain boolean asserts.

## When To Choose `result` vs `should`

Use `result` when you want to:

- inspect the result object
- render or persist the failure yourself
- collect several independent results before failing

Use `should` when you want to:

- fail immediately
- write direct, compact tests
- drive the operator DSL

## What To Learn Next

If you only learn four things first, make them these:

1. `Assert.should` or `=?` for direct checks
2. `Check.should` or `|=>` for reference-based property checks
3. `AssertExpectation.equalBy`, `equalByKey`, and `equalWith` for domain semantics
4. `Check.result` when you want rendering, replay, or collection instead of fail-fast behavior

## Where To Go Next

- [Assertions](assertions.html) for `Assert`, `AssertExpectation`, and `AssertOperators`
- [DSL and Mental Model](dsl-and-mental-model.html) for the conceptual structure behind the syntax
- [Property Checks](properties.html) for `Check`, `CheckExpectation`, and `CheckOperators`
- [Configuration, Arbitraries, and Generators](configuration.html) for custom input spaces
- [Hints and Feedback](hints.html) for the diagnostic/teaching layer
- [Power Showcase](power-showcase.html) for the fast “why this is cool” tour
