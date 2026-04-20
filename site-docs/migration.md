---
title: Migration and Orientation
---

# Migration and Orientation

This docs site teaches the **small runner / powerful expectation** model.

## The New Reading Rule

Read Testify like this:

- runner decides control flow
  - `result`
  - `should`
  - `resultBy`
  - `shouldBy`
- expectation decides semantics
  - equality
  - equality by projection
  - equality by projected key
  - custom relations
  - exception behavior

## The Main Naming Shift

On the assertion side:

- `Assert.result` is the non-throwing structured path
- `Assert.should` is the fail-fast path

On the property side:

- `Check.result` is the non-throwing structured path
- `Check.should` is the fail-fast path
- `Check.resultBy` / `Check.shouldBy` are the advanced custom-property-builder escape hatches

## Expectation-First Equality

Instead of growing the runner surface with many equality variants, semantics live in:

- `AssertExpectation.equalBy`
- `AssertExpectation.equalByKey`
- `AssertExpectation.equalWith`
- `CheckExpectation.equalBy`
- `CheckExpectation.equalByKey`
- `CheckExpectation.equalWith`

That keeps the runner surface small and the DSL stable.

## Operators Stay Thin

Operators are just fail-fast sugar:

- `|>?`, `=?`, `<?`, `?`, `^?` on the Assert side
- `|=>`, `|=>>`, `|?>` on the Check side

If a test needs more explanation than the operator gives you, prefer the named API instead of inventing more punctuation.

## Read The API Like This

When you see a Testify test, decode it in this order:

1. which runner is it using?
   - `Assert` or `Check`
2. is it fail-fast or inspectable?
   - `should` or `result`
3. what semantics are being applied?
   - expectation builder or operator shorthand
4. is there any extra control?
   - `config`
   - `arbitrary`
   - `resultBy` / `shouldBy`
   - global `Testify.configure`

That reading rule keeps the API small even when the behavior becomes powerful.
