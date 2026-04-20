---
title: Configuration, Arbitraries, and Generators
---

# Configuration, Arbitraries, and Generators

This page is about the configurable part of property testing in Testify:

- `CheckConfig`
- `Arbitraries`
- `Generators`
- the optional `config` and `arbitrary` parameters on `Check.result` and `Check.should`

Think of property configuration in layers:

- `CheckConfig` shapes the FsCheck run
- `Arbitraries` shape generated values plus shrinking
- `Generators` shape raw generation before you turn it into an arbitrary
- `resultBy` / `shouldBy` let you take over the property structure itself

## `CheckConfig`

`CheckConfig` is the place to shape the FsCheck run itself.

Useful entry points include:

- `defaultConfig`
- `thorough`
- `withMaxTest`
- `withEndSize`
- `withReplay`
- `withReplayString`

Example:

```fsharp
let config =
    CheckConfig.defaultConfig
    |> CheckConfig.withMaxTest 25
    |> CheckConfig.withEndSize 20
```

The most useful knobs are:

- `withMaxTest`
  - increase or reduce how many generated cases you want
- `withEndSize`
  - shape how large generated values may become over the run
- `withReplay` / `withReplayString`
  - reproduce a previously failing run
- `thorough`
  - start from a more exhaustive preset when the default run feels too shallow

Use replay helpers when you want to re-run a previously reported counterexample without guessing the failing case.

## `Arbitraries`

`Arbitraries` builds `FsCheck.Arbitrary<'T>` values:

- `from<'T>`
- `fromConfig<'T>`
- `fromGen`
- `fromGenShrink`
- tuple helpers
- mapped and filtered arbitraries

Example:

```fsharp
let pairArb =
    Arbitraries.tuple2
        (Arbitraries.from<int>)
        (Arbitraries.from<int>)
```

Then:

```fsharp
Check.result(
    CheckExpectation.equalToReference,
    (fun (a, b) -> a + b),
    <@ fun (a, b) -> a + b @>,
    arbitrary = pairArb)
```

Reach for `Arbitraries` when the property still has one natural `'Args`, but the default generator space is too weak or too noisy.

Typical cases:

- tuples or records
- values with custom shrinking
- filtered domains
- domain-specific wrappers or course-specific types

## `Generators`

`Generators` builds raw `FsCheck.Gen<'T>` values when you want lower-level control before turning them into arbitraries.

Typical use cases:

- fixed-length sequences
- controlled ranges
- custom structured values
- composing tuple/list/array generators

Example:

```fsharp
let lengthControlled =
    Generators.listOfLength
        5
        (Generators.from<int>)
```

and then:

```fsharp
let arbitrary =
    Arbitraries.fromGen lengthControlled
```

Use `Generators` when:

- you care about generation more than shrinking first
- you want precise structural control
- you want to compose smaller generators into larger ones before turning the result into an `Arbitrary<'T>`

Good rule of thumb:

- if you already have a good `Gen<'T>`, wrap it with `Arbitraries.fromGen`
- if you need generation **and** custom shrinking, move to `Arbitraries.fromGenShrink`

## Choosing Between `arbitrary` and `resultBy`

Use `arbitrary = ...` when:

- one generated `Args` value is enough
- you just need a better input space

Use `resultBy` / `shouldBy` when:

- your generators depend on earlier generated values
- you need nested `Prop.forAll`
- the natural property shape is more complex than “one arbitrary input”

## Practical Recipes

### Run fewer, smaller tests while iterating

```fsharp
let quickConfig =
    CheckConfig.defaultConfig
    |> CheckConfig.withMaxTest 20
    |> CheckConfig.withEndSize 10
```

### Replay a failing run from stored output

```fsharp
let replayConfig =
    CheckConfig.defaultConfig
    |> CheckConfig.withReplayString "Rnd=(123,456); Size=17"
```

### Shape a property around one well-defined input model

```fsharp
let personArb =
    Generators.elements [ "Tony"; "Pepper"; "Rhodey" ]
    |> Generators.map (fun name -> { Name = name; Age = 48 })
    |> Arbitraries.fromGen
```

### Use `shouldBy` when generation depends on earlier values

```fsharp
Check.shouldBy(
    (fun verify ->
        FsCheck.Prop.forAll Arbitraries.from<int> (fun n ->
            let length = abs n
            let xsArb =
                Arbitraries.fromGen (
                    FsCheck.Gen.listOfLength length FsCheck.Arb.generate<int>
                )

            FsCheck.Prop.forAll xsArb (fun xs ->
                verify (length, xs)))),
    CheckExpectation.isTrue,
    (fun _ -> true),
    <@ fun (length, xs) -> List.length xs = length @>)
```

## Available Option Sources

When you see `config` and `arbitrary` in the API:

- `config` means `FsCheck.Config`
- `arbitrary` means `FsCheck.Arbitrary<'Args>`

Expected helper sources:

- `CheckConfig`
- `Arbitraries`
- `Generators`
- plain FsCheck APIs when you want to drop to the underlying library directly

## Local vs Global Configuration

This page is about **per-check** property configuration.

For suite-wide defaults such as report formatting, hint packs, and default check-config transformations, continue with [Global Configuration](global-configuration.html).
