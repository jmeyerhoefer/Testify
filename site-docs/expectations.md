---
title: Expectations and Composition
---

# Expectations and Composition

Expectations are the semantic heart of Testify.

The runner layer says:

- return me a result
- or fail immediately

The expectation layer says:

- what relation should hold

This is the core Testify design rule:

- keep runners small
- move meaning into reusable expectations

That lets the DSL stay stable while your domain vocabulary grows.

## `AssertExpectation`

`AssertExpectation<'T>` is for one observed value.

Common categories:

- fixed equality
  - `equalTo`
  - `equalToWithDiff`
  - `notEqualTo`
- projections and custom equality
  - `equalBy`
  - `equalByKey`
  - `equalWith`
- ordering and range
  - `lessThan`
  - `greaterThan`
  - `between`
- containers and structure
  - `contains`
  - `hasLength`
  - `sequenceEqual`
- booleans, options, and results
  - `isTrue`
  - `isFalse`
  - `isSome`
  - `isNone`
  - `isOk`
  - `isError`
- exceptions
  - `doesNotThrow`
  - `throwsAny`
  - `throws`
  - async variants

The beginner path is usually:

- `equalTo`
- ordering/range expectations
- string/container expectations
- then projection/custom-relation expectations once the tests become domain-shaped

## `equalBy` vs `equalByKey` vs `equalWith`

These three are worth separating clearly.

### `equalBy`

Project both the actual and expected full values, then compare the keys.

```fsharp
type Person = { Name: string; Age: int }

<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalBy (fun person -> person.Age) { Name = "Anthony"; Age = 48 }
```

### `equalByKey`

Project the actual full value, then compare the projected key to one explicit expected key.

```fsharp
<@ "Testify" @>
|>? AssertExpectation.equalByKey String.length 7
```

### `equalWith`

Keep the full values and provide your own comparison relation.

```fsharp
<@ { Name = "Tony"; Age = 48 } @>
|>? AssertExpectation.equalWith (fun a b -> a.Age = b.Age) { Name = "Anthony"; Age = 48 }
```

## `CheckExpectation`

`CheckExpectation<'Args, 'Actual, 'Expected>` is for tested code vs reference behavior.

Typical entry points:

- `equalToReference`
- `equalTo`
- `equalToReferenceWithDiff`
- `equalBy`
- `equalByKey`
- `equalWith`
- `throwsSameExceptionType`
- `satisfyWith`
- `satisfyObservedWith`

The property-side pattern is similar:

- start with `equalToReference`
- use `equalTo` for fixed expected values
- reach for `equalBy` / `equalByKey` / `equalWith` when equality itself needs to be domain-aware
- use `satisfyWith` when you want a named custom relation instead of another equality variant

## Composition

Both expectation types compose the same way:

```fsharp
let bounded =
    AssertExpectation.greaterThan 0
    <&> AssertExpectation.lessThan 10

let relaxed =
    AssertExpectation.equalTo 0
    <|> bounded
```

And on the property side:

```fsharp
let relation =
    CheckExpectation.equalToReference
    <|> CheckExpectation.throwsSameExceptionType
```

And composition is chainable, not just binary:

```fsharp
let smallNatural =
    AssertExpectation.greaterThanOrEqualTo 0
    <&> AssertExpectation.lessThan 10
    <&> AssertExpectation.notEqualTo 7

let relaxed =
    AssertExpectation.equalTo "yes"
    <|> AssertExpectation.equalTo "y"
    <|> AssertExpectation.equalTo "true"
```

For longer sets of alternatives or requirements, sequence-based helpers such as `any` and `all` are often easier to read than a long operator chain.

## Build Your Own Vocabulary

One of the best uses of expectations is to lift raw comparison logic into names that match your domain:

```fsharp
let sameAge =
    AssertExpectation.equalBy (fun person -> person.Age)

let successfulResult =
    AssertExpectation.isOk
    <&> AssertExpectation.satisfy "non-empty payload" (function Ok text -> text <> "" | Error _ -> false)
```

On the property side:

```fsharp
let sameVisibleBehavior =
    CheckExpectation.equalBy (fun user -> user.VisibleName)
    <|> CheckExpectation.throwsSameExceptionType
```

The expectation layer is where Testify becomes a language instead of only a helper library.

## Rule Of Thumb

If the same idea appears in more than one test, give it a reusable expectation name instead of repeating inline logic.
