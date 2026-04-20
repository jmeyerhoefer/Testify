---
title: DSL and Mental Model
---

# DSL and Mental Model

Testify is not just a list of helpers. It is a small embedded testing DSL.

That matters because it explains why the API is split the way it is:

- quotations capture the code you want to talk about
- runners control how a check is executed
- expectations define what counts as success
- operators are fail-fast surface syntax over the same model
- hints, rendering, and configuration shape how failures are explained

## What “DSL” Means Here

In Testify, one test usually has this structure:

1. a quoted expression or function
2. a runner
3. an expectation
4. optional configuration, hints, or custom input generation

That gives Testify three syntax layers for the same underlying check.

## One Idea, Three Syntax Layers

### Assert

The most explicit named form:

```fsharp
Assert.should
    (AssertExpectation.equalTo 4)
    <@ 2 + 2 @>
```

The expectation-application DSL:

```fsharp
<@ 2 + 2 @> |>? AssertExpectation.equalTo 4
```

The shortest shorthand:

```fsharp
<@ 2 + 2 @> =? 4
```

All three mean the same thing.

### Check

Named API:

```fsharp
Check.should(
    CheckExpectation.equalToReference,
    id,
    <@ List.rev >> List.rev @>)
```

Operator DSL:

```fsharp
<@ List.rev >> List.rev @> |=> id
```

Again, the operator is not a second engine. It is just a compact syntax layer over `should`.

## Why Quotations Matter

Quotations are why Testify can do more than “expected true, got false.”

With `<@ ... @>`, Testify can retain the tested code shape and include it in:

- rendered failure output
- structured reports
- hints and diff text
- teaching-oriented diagnostics

That is why Testify asks for quotations even when the underlying semantic check is simple.

## Runner vs Expectation

This is the central split in Testify’s design.

The runner answers:

- should I return a structured result?
- should I fail immediately?
- should I use a custom property builder?

The expectation answers:

- what relation should hold?

So:

- `Assert.result` / `Assert.should`
- `Check.result` / `Check.should`
- `Check.resultBy` / `Check.shouldBy`

control execution,

while:

- `AssertExpectation.equalTo`
- `AssertExpectation.equalBy`
- `AssertExpectation.equalByKey`
- `AssertExpectation.equalWith`
- `CheckExpectation.equalToReference`
- `CheckExpectation.equalBy`
- `CheckExpectation.satisfyWith`

control meaning.

This split is what keeps the runner APIs small while still letting the library grow in expressive power.

## `result` vs `should`

This is the most important behavioral split in the public API.

Use `should` when you want:

- fail-fast tests
- compact operator-driven syntax
- normal direct test authoring

Use `result` when you want:

- inspectable structured outcomes
- custom rendering
- collectors and aggregation
- replay/debug workflows
- tooling around failures

Operators are always fail-fast syntax over `should`, never over `result`.

## Property Checks As “Generated Differential Testing”

`Check` is easiest to understand as a generated comparison pipeline:

1. generate input values
2. run the quoted tested function
3. run the reference function or fixed oracle
4. apply a `CheckExpectation`
5. shrink failing cases
6. render a structured failure report

That is why `Check` feels more powerful than a plain boolean property. It combines:

- generated inputs
- a trusted oracle
- readable diagnostics
- shrinking and replay

## Where Hints And Config Fit

Hints and configuration are not side systems. They are second-order parts of the DSL.

- configuration shapes how the run behaves
- arbitraries and generators shape which worlds are explored
- hints shape how failures are explained
- rendering turns the structured result into a report you can read or consume

So the full mental model is:

- quotations say what code is under test
- runners say how to run
- expectations say what success means
- configuration shapes the run
- hints and rendering shape the explanation

## When To Stay Named And When To Use Operators

Prefer named APIs when:

- you need `result`
- the operator spelling hides too much meaning
- the property needs `config` or `arbitrary`
- the expectation deserves a domain name

Prefer operators when:

- the meaning stays obvious at a glance
- the test is fail-fast
- the symbolic form is genuinely clearer than the expanded version

For the full symbolic reference, continue with [Operator Cheat Sheet](operator-cheat-sheet.html).
