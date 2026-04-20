---
title: Power Showcase
---

# Power Showcase

This page is the short answer to:

> “Why would I reach for Testify instead of just writing plain asserts?”

## 1. Compare Domain Objects By Meaning, Not Raw Equality

```fsharp
type Person = { Name: string; Age: int }

<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalBy (fun person -> person.Age) { Name = "Anthony"; Age = 48 }
```

This is stronger than “assert equal” because the test says what counts as equal in this domain.

## 2. Compare Against A Derived Key

```fsharp
<@ "Testify" @>
|>? AssertExpectation.equalByKey String.length 7
```

This is ideal when the important expectation is not the whole value, but a projected fact about it.

## 3. Keep Full Values, But Supply Your Own Relation

```fsharp
<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalWith (fun a b -> a.Age = b.Age) { Name = "Anthony"; Age = 48 }
```

This is useful when projection is too weak and raw equality is too strong.

## 4. Reference-Based Property Check

```fsharp
<@ List.rev >> List.rev @> |=> id
```

This is one of Testify’s signature moves:

- use generated inputs
- compare tested behavior to a trusted reference
- keep shrinking, replay, and rendered failure output

## 5. Shape The Input Space

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

This is where property testing stops being “whatever the default generator gives me” and starts becoming intentional.

## 6. Dependent Property With Custom Quantification

```fsharp
Check.shouldBy(
    (fun verify ->
        FsCheck.Prop.forAll Arbitraries.from<int> (fun n ->
            let length = abs n
            let arb =
                Arbitraries.fromGen (
                    FsCheck.Gen.listOfLength length FsCheck.Arb.generate<int>
                )

            FsCheck.Prop.forAll arb (fun xs ->
                verify (length, xs)))),
    CheckExpectation.isTrue,
    (fun _ -> true),
    <@ fun (expectedLength, xs) -> List.length xs = expectedLength @>)
```

This is the “plain arbitrary is not enough” escape hatch, and it keeps the property readable instead of forcing specialized `Check2` / `Check3` style overloads.

## 7. Inspect A Failure Instead Of Throwing

```fsharp
let result =
    Check.result(
        CheckExpectation.equalToReference,
        (fun x -> x + 1),
        <@ fun x -> x + 2 @>)

let rendered = Check.toDisplayString result
let report = Check.toFailureReport result
```

This is why the `result / should` split matters:

- `should` is great for direct tests
- `result` is great for tooling, aggregation, replay, and custom reporting

## 8. Turn Failures Into Guidance Instead Of Mere Mismatches

```fsharp
Testify.configure (
    TestifyConfig.defaults
    |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
)

let result =
    Assert.result
        (AssertExpectation.equalToWithDiff Diff.defaultOptions "MiniLib")
        <@ "MiniLib " @>
```

With hint packs enabled, the rendered failure can combine:

- diff-style explanation of where the string diverges
- a likely-cause hint such as “this may differ only in whitespace”

That is a much more teacher-friendly and debugger-friendly output shape than a plain equality failure.

## 9. Replay A Counterexample

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

That is a much nicer debugging loop than trying to rediscover the failing input manually.

## The Point

The power of Testify is not one single feature.

It is the combination of:

- quotation-aware diagnostics
- small runners with inspectable results
- strong expectation vocabulary
- reference-based property checks
- shaped input spaces
- hint-driven feedback
- replayable failures
