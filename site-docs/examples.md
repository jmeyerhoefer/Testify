---
title: Cookbook Examples
---

# Cookbook Examples

This page is a concentrated set of “show me the shape” examples.

If you want the most persuasive examples first, jump to [Power Showcase](power-showcase.html).

## Basics

### Direct Equality Assertion

```fsharp
<@ List.length [1; 2; 3] @> =? 3
```

### Assertion With A Reusable Expectation

```fsharp
let positiveSmall =
    AssertExpectation.greaterThan 0
    <&> AssertExpectation.lessThan 10

<@ 5 @> |>? positiveSmall
```

## Domain Modeling

```fsharp
type Person = { Name: string; Age: int }

### Compare Domain Objects By One Field

<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalBy (fun person -> person.Age) { Name = "Anthony"; Age = 48 }
```

### Compare To One Projected Key

```fsharp
<@ "Testify" @>
|>? AssertExpectation.equalByKey String.length 7
```

### Compare With A Custom Relation

```fsharp
<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalWith (fun a b -> a.Age = b.Age) { Name = "Anthony"; Age = 48 }
```

## Property Power

### Simple Property Check Against A Reference

```fsharp
<@ List.rev >> List.rev @> |=> id
```

### Property Check With A Custom Arbitrary

```fsharp
let pairArb =
    Arbitraries.tuple2
        (Arbitraries.from<int>)
        (Arbitraries.from<int>)

Check.should(
    CheckExpectation.equalToReference,
    (fun (a, b) -> a + b),
    <@ fun (a, b) -> a + b @>,
    arbitrary = pairArb)
```

### Custom Generator Then Arbitrary

```fsharp
let fixedLengthLists =
    Generators.listOfLength 5 (Generators.from<int>)
    |> Arbitraries.fromGen

Check.should(
    CheckExpectation.equalToReference,
    List.length,
    <@ List.length @>,
    arbitrary = fixedLengthLists)
```

### Advanced Dependent Property

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

### Inspect A Property Failure Without Throwing

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        (fun x -> x + 1),
        <@ fun x -> x + 2 @>)

match result with
| Passed -> printfn "ok"
| _ -> printfn "%s" (Check.toDisplayString result)
```

### Replay A Failing Property Run

```fsharp
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

## Hints And Global Configuration

### Turn On Built-In Hint Packs

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
)
```

### Add A Custom Hint Rule

```fsharp
let trailingSpaceHint =
    TestifyHintRule.onFieldRegexPattern
        "Course.TrailingSpace"
        HintTextField.ActualValue
        @"\s+$"
        (fun _ -> "The actual value appears to end with trailing whitespace.")

let customHints =
    TestifyHintPack.create "course" [ trailingSpaceHint ]

Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks (BuiltInHintPacks.beginner @ [ customHints ])
)
```

## Integrations

### Expecto

```fsharp
open Expecto
open Testify.Expecto

[<Tests>]
let tests =
    TestifyExpecto.testList "demo" [
        TestifyExpecto.testCase "projection equality" (fun () ->
            <@ { Name = "Tony"; Age = 48 } @>
            |>? AssertExpectation.equalBy (fun person -> person.Age) { Name = "Anthony"; Age = 48 })
    ]
```
