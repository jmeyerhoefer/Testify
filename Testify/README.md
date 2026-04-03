# Testify

`Testify` is a beginner-friendly F# testing library built around quotations, reusable expectations,
and property-style checks with richer failure output than a plain assertion stack trace.

Current source version: `0.1.0`

## Layout

Inside this folder:

- [`Testify/Testify.fsproj`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/Testify.fsproj)
  The library itself.
- [`Testify/Testify.ApiTests/Testify.ApiTests.fsproj`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/Testify.ApiTests/Testify.ApiTests.fsproj)
  API and output-shape tests for the library.
- [`Testify/GdP23/GdP23.fsproj`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/GdP23.fsproj)
  The thesis comparison pipeline used to replay selected student submissions.

## Build And Test

From [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify):

```powershell
dotnet build .\Testify\Testify.fsproj --no-restore
dotnet test .\Testify\Testify.ApiTests\Testify.ApiTests.fsproj --no-build --no-restore
dotnet build .\Testify\GdP23\GdP23.fsproj --no-restore
```

## Minimal Example

```fsharp
namespace Demo

open Microsoft.VisualStudio.TestTools.UnitTesting
open Testify
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

## Assert DSL

The Assert DSL is expression-left: you keep the quoted expression on the left and apply one or more
expectations on the right.

Typical imports:

```fsharp
open Testify
open Testify.AssertOperators
```

### Assert Operators

```fsharp
<@ 1 + 2 @> |>? AssertExpectation.equalTo 3          // apply one reusable expectation
<@ 5 @> >>? AssertExpectation.greaterThan 0 |> ignore // chain and keep the quotation

<@ 1 + 2 @> =? 3    // equals
<@ 1 + 2 @> <>? 4   // not equal
<@ 1 + 2 @> <? 4    // less than
<@ 1 + 2 @> <=? 3   // less than or equal
<@ 1 + 2 @> >? 2    // greater than
<@ 1 + 2 @> >=? 3   // greater than or equal

<@ failwith "boom" @> ^?   // throws
<@ 1 + 2 @> ^!?            // does not throw

<@ 1 + 2 = 3 @> ?          // boolean is true
<@ 1 + 2 = 4 @> !?         // boolean is false

<@ 5 @> ||?
    [ AssertExpectation.equalTo 4
      AssertExpectation.equalTo 5 ]                  // at least one expectation passes

<@ "Testify" @> &&?
    [ AssertExpectation.startsWith "Test"
      AssertExpectation.endsWith "fy" ]              // all expectations pass
```

### Assert Expectation Composition

```fsharp
let positive = AssertExpectation.greaterThan 0
let small = AssertExpectation.lessThan 10

let positiveAndSmall = positive <&> small
let edgeCase = AssertExpectation.equalTo 0 <|> positiveAndSmall

<@ 5 @> |>? positiveAndSmall
<@ 0 @> |>? edgeCase
```

### Common Assert Expectation Builders

```fsharp
<@ 1 + 2 @> |>? AssertExpectation.equalTo 3
<@ 1 + 2 @> |>? AssertExpectation.equalToWithDiff Diff.defaultOptions 3
<@ 1 + 2 @> |>? AssertExpectation.notEqualTo 4

<@ "Testify" @> |>? AssertExpectation.satisfy "contain y" (fun s -> s.Contains "y")
<@ 1 / 0 @> |>? AssertExpectation.satisfyObserved "throw" (function Result.Error _ -> true | _ -> false)

<@ 1 + 2 @> |>? AssertExpectation.doesNotThrow
<@ failwith "boom" @> |>? AssertExpectation.throwsAny
<@ 1 / 0 @> |>? AssertExpectation.throws<int, System.DivideByZeroException>

<@ 5 @> |>? AssertExpectation.lessThan 10
<@ 5 @> |>? AssertExpectation.lessThanOrEqualTo 5
<@ 5 @> |>? AssertExpectation.greaterThan 0
<@ 5 @> |>? AssertExpectation.greaterThanOrEqualTo 5
<@ 5 @> |>? AssertExpectation.between 0 10

<@ "Testify" @> |>? AssertExpectation.equalBy String.length 7
<@ "Testify" @> |>? AssertExpectation.equalWith (fun a b -> a.ToLower() = b.ToLower()) "testify"
<@ [1; 2; 3] @> |>? AssertExpectation.sequenceEqual [1; 2; 3]

<@ true @> |>? AssertExpectation.isTrue
<@ false @> |>? AssertExpectation.isFalse
<@ Some 3 @> |>? AssertExpectation.isSome
<@ None @> |>? AssertExpectation.isNone<int>
<@ Ok 3 @> |>? AssertExpectation.isOk<int, string>
<@ Error "boom" @> |>? AssertExpectation.isError<int, string>

<@ [1; 2; 3] @> |>? AssertExpectation.contains 2
<@ "Testify" @> |>? AssertExpectation.startsWith "Test"
<@ "Testify" @> |>? AssertExpectation.endsWith "fy"
<@ [1; 2; 3] @> |>? AssertExpectation.hasLength 3

<@ 5 @> |>? AssertExpectation.not (AssertExpectation.equalTo 0)
<@ 5 @> |>? AssertExpectation.orElse (AssertExpectation.equalTo 4) (AssertExpectation.equalTo 5)
<@ 5 @> |>? AssertExpectation.andAlso (AssertExpectation.greaterThan 0) (AssertExpectation.lessThan 10)
<@ 5 @> |>? AssertExpectation.any [ AssertExpectation.equalTo 4; AssertExpectation.equalTo 5 ]
<@ 5 @> |>? AssertExpectation.all [ AssertExpectation.greaterThan 0; AssertExpectation.lessThan 10 ]
```

## Check DSL

The Check DSL compares a quoted implementation against a reference implementation over generated
inputs. It can also express bool-returning properties directly.

Typical imports:

```fsharp
open Testify
open Testify.CheckOperators
open Testify.ArbitraryOperators
```

### Check Operators

```fsharp
let config = CheckConfig.defaultConfig
let intArb = Arbitraries.from<int>
let pairArb = intArb <.> intArb

let expectation =
    CheckExpectation.equalToReference
    <&> CheckExpectation.throwsSameExceptionType

let relaxedExpectation =
    CheckExpectation.equalToReference
    <|> CheckExpectation.throwsSameExceptionType

<@ List.rev >> List.rev @> |=> id
<@ List.rev >> List.rev @> |=>> id |> ignore

<@ List.sort @> |=>? (config, List.sort)
<@ List.sort @> |=>?? (Arbitraries.from<int list>, List.sort)
<@ List.sort @> |=>??? (CheckExpectation.equalToReference, List.sort)
<@ List.sort @> |=>>? (CheckExpectation.equalToReference, List.sort) |> ignore

<@ List.rev >> List.rev @>
||=>? (Some config, Some (Arbitraries.from<int list>), Some CheckExpectation.equalToReference, id)
```

### Named Check Helpers

The bool-returning property helpers intentionally use named functions instead of dedicated operators.

```fsharp
let config = CheckConfig.defaultConfig
let intArb = Arbitraries.from<int>
```

#### General `Check.should*`

```fsharp
<@ List.sort @>
|> Check.should CheckExpectation.equalToReference List.sort

Check.shouldWith config CheckExpectation.equalToReference List.sort <@ List.sort @>
Check.shouldUsing intArb CheckExpectation.equalToReference (fun x -> x) <@ fun x -> x @>
Check.shouldUsingWith config intArb CheckExpectation.equalToReference (fun x -> x) <@ fun x -> x @>
```

#### Bool-returning Properties

```fsharp
<@ fun x -> x = x @> |> Check.shouldBeTrue
Check.shouldBeTrueWith config <@ fun x -> x = x @>
<@ fun x -> x = x @> |> Check.shouldBeTrueUsing intArb
Check.shouldBeTrueUsingWith config intArb <@ fun x -> x = x @>

<@ fun x -> x <> x @> |> Check.shouldBeFalse
Check.shouldBeFalseWith config <@ fun x -> x <> x @>
<@ fun x -> x <> x @> |> Check.shouldBeFalseUsing intArb
Check.shouldBeFalseUsingWith config intArb <@ fun x -> x <> x @>
```

#### Equality-to-reference Shortcuts

```fsharp
<@ List.sort @> |> Check.shouldEqual List.sort
<@ List.sort @> |> Check.shouldEqualUsing (Arbitraries.from<int list>) List.sort
Check.shouldEqualWith config List.sort <@ List.sort @>
Check.shouldEqualUsingWith config (Arbitraries.from<int list>) List.sort <@ List.sort @>
```

#### Two- and Three-argument Equality Shortcuts

```fsharp
<@ fun x y -> x + y @> |> Check.shouldEqual2 (+)
Check.shouldEqual2With config (+) <@ fun x y -> x + y @>
<@ fun x y -> x + y @> |> Check.shouldEqual2Using intArb (+)
Check.shouldEqual2UsingWith config intArb (+) <@ fun x y -> x + y @>

<@ fun x y z -> x + y + z @> |> Check.shouldEqual3 (fun x y z -> x + y + z)
Check.shouldEqual3With config (fun x y z -> x + y + z) <@ fun x y z -> x + y + z @>
<@ fun x y z -> x + y + z @> |> Check.shouldEqual3Using intArb (fun x y z -> x + y + z)
Check.shouldEqual3UsingWith config intArb (fun x y z -> x + y + z) <@ fun x y z -> x + y + z @>
```

#### Projection, Diff, and Comparer Variants

```fsharp
<@ List.sort @> |> Check.shouldEqualBy List.length List.sort
<@ List.sort @> |> Check.shouldEqualUsingBy List.length (Arbitraries.from<int list>) List.sort

<@ List.sort @> |> Check.shouldEqualWithDiff Diff.defaultOptions List.sort
<@ List.sort @> |> Check.shouldEqualUsingWithDiff Diff.defaultOptions (Arbitraries.from<int list>) List.sort

<@ fun s -> s.Trim() @>
|> Check.shouldEqualUsingComparer
    (fun a b -> a.ToLowerInvariant() = b.ToLowerInvariant())
    (fun s -> s.Trim())

<@ fun s -> s.Trim() @>
|> Check.shouldEqualUsingComparerUsing
    (fun a b -> a.ToLowerInvariant() = b.ToLowerInvariant())
    (Arbitraries.from<string>)
    (fun s -> s.Trim())
```

#### Grouped / Dependent-input Checks

```fsharp
let listArb = Arbitraries.from<int list>

Check.shouldGroupedUsing
    listArb
    CheckExpectation.equalToReference
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldGroupedUsingWith
    config
    listArb
    CheckExpectation.equalToReference
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldGroupedUsingBoth
    intArb
    listArb
    CheckExpectation.equalToReference
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldGroupedUsingBothWith
    config
    intArb
    listArb
    CheckExpectation.equalToReference
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldGroupedDependingOn
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    CheckExpectation.equalToReference
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldGroupedDependingOnWith
    config
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    CheckExpectation.equalToReference
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldGroupedDependingOnUsing
    intArb
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    CheckExpectation.equalToReference
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldGroupedDependingOnUsingWith
    config
    intArb
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    CheckExpectation.equalToReference
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>
```

#### Equality Grouped Variants

```fsharp
Check.shouldEqualGroupedUsing
    listArb
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldEqualGroupedUsingWith
    config
    listArb
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldEqualGroupedUsingBoth
    intArb
    listArb
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldEqualGroupedUsingBothWith
    config
    intArb
    listArb
    (fun x xs -> List.replicate x xs)
    <@ fun x xs -> List.replicate x xs @>

Check.shouldEqualGroupedDependingOn
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldEqualGroupedDependingOnWith
    config
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldEqualGroupedDependingOnUsing
    intArb
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>

Check.shouldEqualGroupedDependingOnUsingWith
    config
    intArb
    (fun x -> Arbitraries.filter (fun y -> y >= x) intArb)
    (fun x y -> x <= y)
    <@ fun x y -> x <= y @>
```

## Choosing Between Operators And Named Functions

- Use Assert operators when you are checking one quoted expression directly.
- Use Check operators when you are comparing a quoted implementation against a reference function.
- Use named `Check.shouldBeTrue*` / `Check.shouldBeFalse*` helpers for bool-returning properties.
- Prefer named helpers over inventing more symbolic operators for bool properties; the current API is
  intentionally explicit there.

## GdP23 Notes

The `GdP23` project in this folder is the thesis comparison pipeline. It materializes selected
submission snapshots, runs the original and Testify-based test suites inside Docker, and produces local
comparison artifacts for analysis. The generated exports under `GdP23/DockerResults/` are kept locally
but are not versioned in git anymore.

## Related Docs

- [`docs/TestifyDslSketch.tex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.tex)
- [`docs/TestifyDslSketch.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.md)
