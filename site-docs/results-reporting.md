---
title: Results, Rendering, and Failure Reports
---

# Results, Rendering, and Failure Reports

One of Testify’s main design goals is that non-throwing checks should still give you something useful.

This is where the `result` half of the API pays off.

If the named API split feels arbitrary, read [DSL and Mental Model](dsl-and-mental-model.html) alongside this page. Reporting is where the value of `result` becomes most obvious.

## `AssertResult`

`Assert.result` returns:

- `Passed`
- `Failed of AssertFailure`

`AssertFailure` carries:

- the stable expectation label
- the tested expression or explicit label
- rendered expected/actual values
- observed exception/value metadata
- optional explanation text
- optional structured nested details
- source location when Testify can recover one

That means `Assert.result` is not just “no exception.” It is a real diagnostic object.

## `CheckResult`

`Check.result` and `Check.resultBy` return:

- `Passed`
- `Failed of CheckFailure`
- `Exhausted of string`
- `Errored of string`

That means property-style checks distinguish:

- a genuine counterexample
- exhaustion in the generator space
- infrastructure/runtime problems

`Exhausted` and `Errored` matter because property-style failures are not all the same. Sometimes the issue is the tested behavior; sometimes it is the input model or property setup.

## Rendering

Both layers expose the same reporting pattern:

- `toFailureReport`
- `toDisplayStringWith`
- `toDisplayString`
- `assertPassed`

Example:

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalTo 3)
        <@ 1 + 1 @>

let text = Assert.toDisplayString result
```

And on the property side:

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        (fun x -> x + 1),
        <@ fun x -> x + 2 @>)

let text = Check.toDisplayString result
```

Use the rendering API when you want:

- a terminal-friendly failure message
- JSON output for tooling or downstream consumers
- richer output than a raw exception string

`TestifyReportOptions` controls:

- verbosity
- maximum rendered value lines
- output format such as wall-of-text vs JSON

## Replay

When a property check fails, `CheckFailure.TryGetReplayConfig()` tries to rebuild an `FsCheck.Config` that can replay the failing run.

That is useful when you want to:

- debug the exact failing case locally
- preserve a shrink/replay token from CI
- re-run a counterexample during investigation

Typical workflow:

1. run `Check.result`
2. match on `Failed failure`
3. call `failure.TryGetReplayConfig()`
4. rerun the same property with `config = replayConfig`

That makes debugging much less guessy than “try to reproduce it somehow.”

## Hints In Reports

Hints are part of the rendered failure experience, not a disconnected subsystem.

When hint packs or hint rules are configured:

- Testify collects explicit hints already on the report
- `HintInference` adds inferred hints from the failure shape
- `Assert.toDisplayString` and `Check.toDisplayString` render the resolved hints together with the failure

That means a failure can say more than “expected X but got Y.” It can point to a likely cause.

Minimal example:

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

A configured string-hint pack can turn that from a plain mismatch into a more useful “this may differ only in whitespace” diagnostic.

## Diffs And Hints Work Together

Hints and diffs solve different problems:

- diff text tries to explain where values diverge
- hints try to explain what kind of mistake the divergence resembles

Example:

```fsharp
let result =
    Assert.result
        (AssertExpectation.equalToWithDiff Diff.defaultOptions "MiniLib")
        <@ "MiniLib " @>
```

In a setup with beginner-friendly hint packs enabled, one rendered failure can contain:

- expected/actual values
- diff text saying where the mismatch occurs
- a string hint suggesting that the problem may be trailing whitespace or an extra newline

That layered explanation is one of the reasons Testify failures can feel more helpful than plain assertion messages.

## Failure Anatomy

At a high level, a failure report can include:

- summary
- expectation text
- expected/actual display
- observed exception/value metadata
- details and diff text
- shrunk counterexample data
- replay information
- hints
- source location

That is also why `HintTextField` has so many mapping targets: hint rules can inspect summary text, value text, diff text, and shrink/replay-related text without needing a separate reporting format.

If you want the fully structured version instead of the rendered string, use `toFailureReport`.

## Collectors

Both `Assert` and `Check` expose result collectors:

- `Collect.create`
- `Collect.add`
- `Collect.toResultList`
- `Collect.assertAll`

These are useful when you want multiple independent failures to show up together instead of stopping at the first one.

Collectors are especially useful when:

- you want a small batch of independent assertions
- you want to render all failures together at the end
- you are validating several facets of one object and do not want the first failure to hide the rest
