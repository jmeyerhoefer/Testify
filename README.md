# Testify

`Testify` is a beginner-friendly F# testing library built around quotations, reusable expectations, and property-style checks with readable failure output.

> [!WARNING]
> Testify and these docs are still under active development. Treat the current public surface as the intended direction, not as a claim that every edge case is already fully verified and battle-tested.

The public API is intentionally split into:

- `Assert` for one quoted expression right now
- `AssertExpectation` for reusable example-based semantics
- `Check` for generated property checks against a reference implementation
- `CheckExpectation` for reusable property relations
- `AssertOperators` and `CheckOperators` for concise fail-fast DSL syntax

## Docs Site

Testify now ships with a local FsDocs site in `site-docs/`.

From the repository root:

```powershell
.\build-docs.ps1
```

That will:

- restore the pinned local FsDocs tool
- build the library and XML documentation
- verify the main API-doc coverage rules
- generate the site into `output/docs/`

For local live preview:

```powershell
.\watch-docs.ps1
```

## Build And Test

From the repository root:

```powershell
dotnet build .\src\Testify\Testify.fsproj --no-restore
dotnet test .\tests\Testify.ApiTests\Testify.ApiTests.fsproj --no-build --no-restore
dotnet build .\samples\Testify.Expecto.Sample\Testify.Expecto.Sample.fsproj --no-restore
```

## Minimal Example

```fsharp
namespace Demo

open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
open Testify.MSTest
open Testify.AssertOperators
open Testify.CheckOperators

module Math =
    let add x y = x + y

[<TestifyClass>]
type DemoTests() =

    [<TestifyMethod>]
    member _.``example assertion`` () =
        <@ Math.add 1 2 @> =? 3

    [<TestifyMethod>]
    member _.``example property`` () =
        <@ List.rev >> List.rev @> |=> id
```

## Where To Start

- Read the generated site landing page under `output/docs/` after running `.\build-docs.ps1`
- Use `Assert.should` or `=?` for direct examples
- Use `Assert.result` when you want to inspect or render failures yourself
- Use `Check.should` or `|=>` for fail-fast property checks against a trusted reference
- Use `Check.result` when you want a structured `CheckResult`
- Reach for `Check.resultBy` / `Check.shouldBy` when you need nested or dependent `FsCheck` quantification
