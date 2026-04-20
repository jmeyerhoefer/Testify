---
title: Integrations
---

# Integrations

Testify is not tied to one test framework.

The current library ships focused integration helpers for:

- MSTest
- Expecto

The integrations exist so you can keep Testify’s richer failure model instead of flattening everything to a generic assertion string.

## MSTest

Use:

- `Testify.MSTest`
- `TestifyClass`
- `TestifyMethod`

Example:

```fsharp
open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.MSTest
open Testify.AssertOperators

[<TestifyClass>]
type DemoTests() =
    [<TestifyMethod>]
    member _.``addition`` () =
        <@ 1 + 2 @> =? 3
```

MSTest is a good fit when:

- your project already uses MSTest attributes and discovery
- you want Testify-style failure rendering inside an established Visual Studio workflow
- you want attribute-based test organization

## Expecto

Use:

- `Testify.Expecto`
- `TestifyExpecto.testCase`
- `TestifyExpecto.testList`

Example:

```fsharp
open Expecto
open Testify
open Testify.Expecto
open Testify.AssertOperators

[<Tests>]
let tests =
    TestifyExpecto.testList "samples" [
        TestifyExpecto.testCase "addition" (fun () ->
            <@ 1 + 2 @> =? 3)
    ]
```

Expecto is a good fit when:

- your test suite already prefers functional composition over attributes
- you want `testList` and `testCase` style structuring
- you want Testify’s diagnostics without giving up Expecto’s style

## Which One Should You Choose?

Choose MSTest when:

- the host environment already expects MSTest
- attribute-based tests are the norm

Choose Expecto when:

- you want compositional test trees
- the project is already Expecto-first

Choose based on the host test framework, not because Testify itself changes meaning between them. The core `Assert`, `Check`, expectation, operator, hint, and reporting concepts stay the same.

## Why The Integrations Exist

The integrations mainly do two things:

- fit Testify’s failure model into the host framework cleanly
- preserve the richer rendered failure output instead of collapsing everything to a plain assertion message

Use the API reference for the exact attribute/helper signatures.
