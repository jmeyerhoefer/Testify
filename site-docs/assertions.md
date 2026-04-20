---
title: Assertions
---

# Assertions

This page covers the example-based half of Testify:

- `Assert`
- `AssertExpectation`
- `AssertOperators`

Use this layer when you already know the concrete expression you want to show in the test.

## Why Not Just Use Plain Asserts?

Plain asserts are fine when:

- the expression is trivial
- the failure message can stay generic
- you do not need reusable semantics

Testify becomes more interesting when you want:

- the quoted expression in failure output
- reusable expectations like `equalBy` and `equalWith`
- structured results you can render or collect
- richer failure text than a plain “expected X but got Y”

## `Assert.result`

`Assert.result` evaluates the quoted expression and gives you an `AssertResult`.

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalTo "MiniLib")
        <@ "Mini" + "Lib" @>
```

Use it when you want to:

- inspect the result
- render failure text yourself
- collect several results before deciding to fail

Example failure-inspection flow:

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalTo 3)
        <@ 1 + 1 @>

let rendered = Assert.toDisplayString result
let report = Assert.toFailureReport result
```

## `Assert.should`

`Assert.should` uses the same assertion pipeline, but raises immediately when the assertion fails.

```fsharp
Assert.should
    (AssertExpectation.equalTo "MiniLib")
    <@ "Mini" + "Lib" @>
```

Use `should` when:

- you want direct, fail-fast tests
- the operator DSL is clearer than a named pipeline
- you do not need to inspect the result first

## Operators

For direct tests, the operator layer is usually the most pleasant path:

```fsharp
open Testify.AssertOperators

<@ 1 + 2 @> |>? AssertExpectation.equalTo 3
<@ 1 + 2 @> =? 3
<@ 5 @> <? 10
<@ 5 @> >=? 5
<@ 1 + 2 = 3 @> ?
<@ 1 / 0 @> ^?
```

Useful reading rule:

- `|>?` applies one reusable expectation
- `>>?` applies one expectation and returns the quotation so you can keep chaining
- symbolic operators like `=?` and `<?` are shorthand over common expectations

If a symbolic form stops being readable, switch back to the named API. Testify does not require you to stay in operators once the test needs more explanation.

## What A Better Failure Looks Like

The real value of quotations shows up on failure:

- the test expression can be rendered directly
- the expectation can keep its own descriptive label
- structured actual/expected metadata can be preserved
- rendering can include hints, details, and source-location information

That is why `Assert.result` is worth having even when `Assert.should` is the more common path.

## Collecting Several Assertion Results

If you want to run several independent checks and fail once at the end:

```fsharp
let collector = Assert.Collect.create ()

Assert.result (AssertExpectation.equalTo 3) <@ 1 + 2 @>
|> Assert.Collect.add collector
|> ignore

Assert.result (AssertExpectation.startsWith "Mini") <@ "MiniLib" @>
|> Assert.Collect.add collector
|> ignore

Assert.Collect.assertAll collector
```

## Good First Expectation Builders

Start with:

- `AssertExpectation.equalTo`
- `AssertExpectation.notEqualTo`
- `AssertExpectation.greaterThan`
- `AssertExpectation.startsWith`
- `AssertExpectation.endsWith`
- `AssertExpectation.isTrue`
- `AssertExpectation.throws`

Then move on to:

- `AssertExpectation.equalBy`
- `AssertExpectation.equalByKey`
- `AssertExpectation.equalWith`
- `AssertExpectation.andAlso`
- `AssertExpectation.orElse`

## Build A Vocabulary, Not Just Tests

A good Testify codebase usually stops repeating raw comparison logic and starts naming expectations:

```fsharp
let sameAge =
    AssertExpectation.equalBy (fun person -> person.Age)

let visibleUser =
    AssertExpectation.isSome
    <&> AssertExpectation.satisfy "active user" (Option.exists (fun user -> user.IsActive))
```

That is usually where Assert becomes more valuable than “just another assert helper.”

For the exact signatures and full method-by-method reference, jump to the [API Reference](reference/index.html).
