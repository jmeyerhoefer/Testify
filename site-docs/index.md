---
title: Testify
---

# Testify

`Testify` is a quotation-first F# testing library for readable tests, reusable expectation vocabularies, and property-style checks against trusted reference implementations.

> [!WARNING]
> This documentation site is a work in progress. The public API and examples are evolving, and not every page or scenario has been fully verified and battle-tested yet.

This public site is designed to do three jobs at once:

- get you productive quickly
- show you where Testify becomes unusually powerful
- give you a real API reference when you need exact signatures

## Start Here

- [Getting Started](getting-started.html)
- [DSL and Mental Model](dsl-and-mental-model.html)
- [Assertions](assertions.html)
- [Property Checks](properties.html)
- [Expectations and Composition](expectations.html)
- [Configuration, Arbitraries, and Generators](configuration.html)
- [Global Configuration](global-configuration.html)
- [Hints and Feedback](hints.html)
- [Results, Rendering, and Failure Reports](results-reporting.html)
- [Operator Cheat Sheet](operator-cheat-sheet.html)
- [Integrations](integrations.html)
- [Cookbook Examples](examples.html)
- [Power Showcase](power-showcase.html)
- [Migration and Orientation](migration.html)
- [API Reference](reference/index.html)

## Mental Model

Think in two layers:

- runner layer
  - `Assert.result` / `Assert.should`
  - `Check.result` / `Check.should`
  - `Check.resultBy` / `Check.shouldBy`
- expectation layer
  - `AssertExpectation<'T>`
  - `CheckExpectation<'Args, 'Actual, 'Expected>`

The runner decides **how** you want the check to behave:

- return a structured result
- fail fast immediately
- use a custom property builder in the advanced `By` case

The expectation decides **what** should hold:

- equality
- equality by projection
- equality by projected key
- custom comparison relations
- exception behavior
- composed relations

If you want the fuller conceptual story behind this split, continue with [DSL and Mental Model](dsl-and-mental-model.html).

## Why Testify Exists

Testify is meant for cases where plain `assert true/false` or minimal xUnit-style checks stop being satisfying:

- you want the original quoted expression in failure output
- you want reusable semantics with meaningful names
- you want to compare student or candidate code against a reference implementation
- you want property testing without losing readable diagnostics
- you want failures to teach, not only fail

## What Makes It Special

Testify becomes interesting when several features meet:

- **quotations**
  - the original tested code can show up in diagnostics instead of only the final boolean outcome
- **expectation-first semantics**
  - `equalBy`, `equalByKey`, `equalWith`, and composed expectations let you name domain intent instead of repeating inline logic
- **reference-based property checks**
  - `Check.should` and `|=>` let you compare your implementation against a trusted oracle over many generated inputs
- **hints and feedback**
  - failures can carry inferred or configured hints instead of only raw mismatch text
- **configurable input spaces**
  - `CheckConfig`, `Arbitraries`, `Generators`, and `shouldBy` let you shape the property run instead of accepting the default input space blindly

## Typical Paths

If you are new to Testify:

- start with [Getting Started](getting-started.html)
- read [DSL and Mental Model](dsl-and-mental-model.html) once the runner/expectation split looks unfamiliar
- learn `Assert.should` plus `|>?` and `=?`
- then learn `Check.should` plus `|=>`

If you already know why you are here:

- jump to [Hints and Feedback](hints.html) to see the teaching/diagnostics side
- jump to [Configuration, Arbitraries, and Generators](configuration.html) for property control
- jump to [Power Showcase](power-showcase.html) for the “you can do that?” examples

## The Public Split

The public surface is intentionally strict:

- runner APIs stay small
  - `Assert.result` / `Assert.should`
  - `Check.result` / `Check.should`
  - `Check.resultBy` / `Check.shouldBy`
- expressive power lives in expectations and configuration
  - `AssertExpectation`
  - `CheckExpectation`
  - `CheckConfig`
  - `Arbitraries`
  - `Generators`
  - `Testify.configure`

## Visual Specs

Prompt-ready SVG specs live in `assets/`:

- `assets/testify-mental-model.svg`
- `assets/result-vs-should.svg`
- `assets/property-check-pipeline.svg`

These are intentionally simple prompt/spec assets rather than polished final artwork.
