---
title: Global Configuration
---

# Global Configuration

This page is about the suite-wide defaults that Testify uses when individual calls do not override them.

Conceptually, this is the part of the DSL that shapes the environment around checks rather than the checks themselves.

The main entry points are:

- `Testify.configure`
- `Testify.currentConfiguration()`
- `Testify.resetConfiguration()`
- `TestifyConfig`

## Why Global Configuration Exists

Global configuration is useful when you want one stable default setup for a whole test suite:

- report verbosity and output format
- default hint packs or explicit hint rules
- default FsCheck transformations applied to property checks

This keeps ordinary `Assert` and `Check` calls small while still letting the suite behave consistently.

## The Core API

### Install a suite-wide default

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
    |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
)
```

### Inspect the current configuration

```fsharp
let active = Testify.currentConfiguration()
```

### Reset back to neutral defaults

```fsharp
Testify.resetConfiguration()
```

## What `TestifyConfig` Controls

`TestifyConfig` is the global container for:

- `ReportOptions`
  - rendering verbosity, line limits, output format
- `HintRules`
  - explicit rules applied directly
- `HintPacks`
  - grouped reusable hint rules
- `CheckConfigTransformers`
  - FsCheck-config transformations applied to default property runs

Useful helpers include:

- `TestifyConfig.defaults`
- `TestifyConfig.withReportOptions`
- `TestifyConfig.withOutputFormat`
- `TestifyConfig.withHints`
- `TestifyConfig.withHintPacks`
- `TestifyConfig.addCheckConfigTransformer`

## Precedence: Local Beats Global

Use this rule:

- global configuration supplies defaults
- local runner arguments override the defaults for that call

That means:

- `Assert.result` and `Assert.should` use the global report options unless you render differently afterward
- `Check.result(..., config = ...)` overrides the default property-run config for that one check
- `Check.result(..., arbitrary = ...)` overrides the default arbitrary resolution for that one check

Global config is for broad suite behavior, not for replacing precise local control.

## Good Default Setup

This is a good “make the whole suite friendlier” setup:

```fsharp
let suiteDefaults =
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
    |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs

Testify.configure suiteDefaults
```

This does three useful things:

- enables beginner-friendly built-in hints
- keeps direct tests unchanged
- makes property checks pick up extra Mini/course-specific arbitraries by default

## Report Formatting Defaults

Global report formatting is a good fit for:

- choosing `WallOfText` vs `Json`
- choosing a suite-wide verbosity level
- capping rendered value size in a predictable way

Example:

```fsharp
let reportingDefaults =
    TestifyConfig.defaults
    |> TestifyConfig.withOutputFormat OutputFormat.Json

Testify.configure reportingDefaults
```

## Hint Defaults

Global hints are often better than per-test hints because they act like teaching rules for the whole suite.

Example:

```fsharp
let configWithHints =
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner

Testify.configure configWithHints
```

If you need domain-specific hints too:

```fsharp
let domainRule =
    TestifyHintRule.create "Course.RecursionMissing" (fun report ->
        if report.Actual |> Option.exists (fun text -> text.Contains "while") then
            Some "This task may expect a recursive solution rather than an imperative loop."
        else
            None)

let domainPack =
    TestifyHintPack.create "course" [ domainRule ]

Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks (BuiltInHintPacks.beginner @ [ domainPack ])
)
```

## When To Prefer Local Overrides

Prefer local runner arguments when:

- one property needs a custom arbitrary
- one property needs replay or a special max-test count
- one test should stay special instead of changing suite behavior

Prefer global configuration when:

- the whole suite should render similarly
- the same hint packs should always be available
- the same default property transformers should apply everywhere

## Relationship To Local Property Configuration

This page is about suite-wide defaults.

For per-check shaping of property runs with `CheckConfig`, `Arbitraries`, `Generators`, and `resultBy` / `shouldBy`, continue with [Configuration, Arbitraries, and Generators](configuration.html).
