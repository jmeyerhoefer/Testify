---
title: Property Checks
---

# Property Checks

This page covers the generated-input side of Testify:

- `Check`
- `CheckExpectation`
- `CheckOperators`

Use this layer when you want to compare a tested implementation against a trusted reference over many generated inputs.

## Why This Style Is Powerful

`Check` is not just “FsCheck with another name.”

Its sweet spot is:

- you already trust one implementation
- you want to compare another implementation against it over many generated inputs
- you still want readable diagnostics, shrinking, replay, and structured failure output

That makes it especially useful for:

- student/reference comparisons
- regression checks after refactoring
- “these two implementations should behave the same” testing
- algebraic or round-trip properties when one side can act as the oracle

## `Check.result`

`Check.result` is the structured, non-throwing property runner.

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        (fun xs -> xs |> List.rev |> List.rev),
        <@ List.rev >> List.rev @>)
```

Optional named parameters:

- `config`
  - an `FsCheck.Config`
  - use `CheckConfig.defaultConfig`, `CheckConfig.thorough`, `CheckConfig.withMaxTest`, `CheckConfig.withEndSize`, or replay helpers
- `arbitrary`
  - an `FsCheck.Arbitrary<'Args>`
  - use `Arbitraries.from`, `Arbitraries.fromGen`, tuple helpers, filtered arbitraries, or mapped arbitraries

```fsharp
let configured =
    Check.result(
        CheckExpectation.equalToReference,
        (fun x -> x),
        <@ fun x -> x @>,
        config = CheckConfig.withMaxTest 25,
        arbitrary = Arbitraries.from<int>)
```

Use `result` when you want to:

- inspect `Passed` / `Failed` / `Exhausted` / `Errored`
- render the failure yourself
- persist or replay the failing case
- collect several property results before failing

## `Check.should`

`Check.should` uses the same engine, but raises immediately on failure.

```fsharp
Check.should(
    CheckExpectation.equalToReference,
    List.sort,
    <@ List.sort @>)
```

Use `should` when:

- the test should fail immediately
- the operator DSL reads well
- you do not need to inspect the result first

## `Check.resultBy` and `Check.shouldBy`

Use the `By` variants when one plain `Arbitrary<'Args>` is not enough and you want to build the surrounding property yourself.

```fsharp
Check.shouldBy(
    (fun verify ->
        FsCheck.Prop.forAll Arbitraries.from<int> (fun n ->
            let length = abs n
            let arb = Arbitraries.fromGen (FsCheck.Gen.listOfLength length FsCheck.Arb.generate<int>)
            FsCheck.Prop.forAll arb (fun xs ->
                verify (length, xs)))),
    CheckExpectation.isTrue,
    (fun _ -> true),
    <@ fun (expectedLength, xs) -> List.length xs = expectedLength @>)
```

Important rule:

- `resultBy` / `shouldBy` take `config`
- they do **not** take `arbitrary`
- the property builder owns quantification

## Decision Guide

Start with the simplest thing that expresses the property:

### Use `Check.result` / `Check.should`

When:

- one generated `'Args` value is enough
- the default input space is already fine
- you just want a reference-style comparison

### Add `arbitrary = ...`

When:

- you still have one generated `'Args`
- but you need a shaped input space
- for example: tuples, filtered values, fixed-length collections, or domain-specific values

### Move to `resultBy` / `shouldBy`

When:

- generation depends on earlier generated values
- nested `Prop.forAll` is the natural shape
- one flat `Arbitrary<'Args>` would hide the real property structure

## Operators

The Check operator layer is intentionally small:

```fsharp
open Testify.CheckOperators

<@ List.rev >> List.rev @> |=> id
<@ List.rev >> List.rev @> |=>> id |> ignore
```

Advanced callback-built bool property:

```fsharp
<@ fun (n, xs) -> List.length xs = n @>
|?> (fun verify ->
        FsCheck.Prop.forAll Arbitraries.from<int> (fun n ->
            let length = abs n
            let arb = Arbitraries.fromGen (FsCheck.Gen.listOfLength length FsCheck.Arb.generate<int>)
            FsCheck.Prop.forAll arb (fun xs ->
                verify (length, xs))))
```

Operators are deliberately thin:

- `|=>` is the “default equality against a reference” shorthand
- `|=>>` is the chain-friendly version
- `|?>` is the advanced callback-built property shorthand

If you need custom config, a custom arbitrary, or a more explicit explanation, prefer the named `Check` API.

## Common Expectation Shapes

Start with:

- `CheckExpectation.equalToReference`
- `CheckExpectation.equalTo`
- `CheckExpectation.throwsSameExceptionType`

Then reach for:

- `CheckExpectation.equalBy`
- `CheckExpectation.equalByKey`
- `CheckExpectation.equalWith`
- `CheckExpectation.satisfyWith`
- `CheckExpectation.satisfyObservedWith`

## Shrinking, Replay, and Debugging

When a property fails, Testify preserves more than a boolean false:

- the failure can shrink to a smaller counterexample
- the result can be rendered with `Check.toDisplayString`
- the replay token can be recovered from `CheckFailure.TryGetReplayConfig()`

Typical replay flow:

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        (fun x -> x + 1),
        <@ fun x -> x + 2 @>)

match result with
| Failed failure ->
    match failure.TryGetReplayConfig() with
    | Some replayConfig ->
        let replayed =
            Check.result(
                CheckExpectation.equalToReference,
                (fun x -> x + 1),
                <@ fun x -> x + 2 @>,
                config = replayConfig)
        printfn "%s" (Check.toDisplayString replayed)
    | None ->
        ()
| _ ->
    ()
```

## When This Style Shines Most

Property checks are especially useful for:

- trusted-reference comparisons
- round-trip properties
- shrinking counterexamples to small failing cases
- controlled custom input spaces
- debugging with replay configs from `CheckFailure.TryGetReplayConfig()`

For exact configuration helpers and the full callback contract, continue with [Configuration, Arbitraries, and Generators](configuration.html).
