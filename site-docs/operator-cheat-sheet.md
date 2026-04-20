---
title: Operator Cheat Sheet
---

# Operator Cheat Sheet

Operators in Testify are deliberately thin:

- they are fail-fast sugar over `should`
- they do not replace `result`
- they are best when they keep the test clearer than the named API

They are the surface syntax of the Testify DSL, not a separate engine. If you want the full model behind that statement, read [DSL and Mental Model](dsl-and-mental-model.html) first or come back to it after this page.

If an operator stops being readable, switch back to the named API.

## Assert Operators

<table>
  <thead>
    <tr>
      <th>Operator</th>
      <th>Meaning</th>
      <th>Named Equivalent</th>
      <th>Returns</th>
      <th>Best Used When</th>
    </tr>
  </thead>
  <tbody>
    <tr><td><code>|&gt;?</code></td><td>Apply one reusable expectation</td><td><code>Assert.should expectation expr</code></td><td><code>unit</code></td><td>You already have an expectation value</td></tr>
    <tr><td><code>&gt;&gt;?</code></td><td>Apply one expectation and keep the quotation</td><td><code>Assert.should expectation expr</code> plus chaining</td><td>the original quotation</td><td>You want left-to-right chained checks</td></tr>
    <tr><td><code>=?</code></td><td>Equality assertion</td><td><code>Assert.should (AssertExpectation.equalTo value) expr</code></td><td><code>unit</code></td><td>Simple direct equality</td></tr>
    <tr><td><code>&lt;&gt;?</code></td><td>Non-equality assertion</td><td><code>Assert.should (AssertExpectation.notEqualTo value) expr</code></td><td><code>unit</code></td><td>Simple direct inequality</td></tr>
    <tr><td><code>&lt;?</code></td><td>Less-than assertion</td><td><code>Assert.should (AssertExpectation.lessThan value) expr</code></td><td><code>unit</code></td><td>Simple comparison</td></tr>
    <tr><td><code>&lt;=?</code></td><td>Less-than-or-equal assertion</td><td><code>Assert.should (AssertExpectation.lessThanOrEqualTo value) expr</code></td><td><code>unit</code></td><td>Simple comparison</td></tr>
    <tr><td><code>&gt;?</code></td><td>Greater-than assertion</td><td><code>Assert.should (AssertExpectation.greaterThan value) expr</code></td><td><code>unit</code></td><td>Simple comparison</td></tr>
    <tr><td><code>&gt;=?</code></td><td>Greater-than-or-equal assertion</td><td><code>Assert.should (AssertExpectation.greaterThanOrEqualTo value) expr</code></td><td><code>unit</code></td><td>Simple comparison</td></tr>
    <tr><td><code>^?</code></td><td>The quoted expression should throw</td><td><code>Assert.should AssertExpectation.throwsAny expr</code></td><td><code>unit</code></td><td>Exception-oriented tests</td></tr>
    <tr><td><code>^!?</code></td><td>The quoted expression should not throw</td><td><code>Assert.should AssertExpectation.doesNotThrow expr</code></td><td><code>unit</code></td><td>Making exception-freedom explicit</td></tr>
    <tr><td><code>?</code></td><td>Boolean expression should be true</td><td><code>Assert.should AssertExpectation.isTrue expr</code></td><td><code>unit</code></td><td>Short direct boolean tests</td></tr>
    <tr><td><code>!?</code></td><td>Boolean expression should be false</td><td><code>Assert.should AssertExpectation.isFalse expr</code></td><td><code>unit</code></td><td>Short direct boolean tests</td></tr>
    <tr><td><code>||?</code></td><td>Any expectation from a sequence may pass</td><td><code>Assert.should (AssertExpectation.any expectations) expr</code></td><td><code>unit</code></td><td>You have many alternatives</td></tr>
    <tr><td><code>&amp;&amp;?</code></td><td>All expectations from a sequence must pass</td><td><code>Assert.should (AssertExpectation.all expectations) expr</code></td><td><code>unit</code></td><td>You have many required expectations</td></tr>
  </tbody>
</table>

## Check Operators

<table>
  <thead>
    <tr>
      <th>Operator</th>
      <th>Meaning</th>
      <th>Named Equivalent</th>
      <th>Returns</th>
      <th>Best Used When</th>
    </tr>
  </thead>
  <tbody>
    <tr><td><code>|=&gt;</code></td><td>Default fail-fast equality check against a reference</td><td><code>Check.should(CheckExpectation.equalToReference, reference, expr)</code></td><td><code>unit</code></td><td>The standard reference-style property test</td></tr>
    <tr><td><code>|=&gt;&gt;</code></td><td>Same as <code>|=&gt;</code>, but keep the quotation</td><td><code>Check.should(...)</code> plus chaining</td><td>the original quotation</td><td>You want chain-friendly property syntax</td></tr>
    <tr><td><code>|?&gt;</code></td><td>Callback-built fail-fast property</td><td><code>Check.shouldBy(buildProperty, CheckExpectation.isTrue, (fun _ -&gt; true), expr)</code></td><td><code>unit</code></td><td>You need custom quantification or dependent generation</td></tr>
  </tbody>
</table>

## Composition Operators

The composition operators are shared expectation-level building blocks.

<table>
  <thead>
    <tr>
      <th>Operator</th>
      <th>Meaning</th>
      <th>Named Equivalent</th>
      <th>Notes</th>
    </tr>
  </thead>
  <tbody>
    <tr><td><code>&lt;|&gt;</code></td><td>Logical OR of expectations</td><td><code>orElse</code></td><td>Chainable; use <code>any</code> for longer alternative lists</td></tr>
    <tr><td><code>&lt;&amp;&gt;</code></td><td>Logical AND of expectations</td><td><code>andAlso</code></td><td>Chainable; use <code>all</code> for longer required lists</td></tr>
  </tbody>
</table>

## Chainable Composition Examples

Short OR-chain:

```fsharp
let relaxedYes =
    AssertExpectation.equalTo "yes"
    <|> AssertExpectation.equalTo "y"
    <|> AssertExpectation.equalTo "true"
```

Short AND-chain:

```fsharp
let bounded =
    AssertExpectation.greaterThanOrEqualTo 0
    <&> AssertExpectation.lessThan 10
    <&> AssertExpectation.notEqualTo 7
```

On the property side:

```fsharp
let forgivingRelation =
    CheckExpectation.equalToReference
    <|> CheckExpectation.throwsSameExceptionType
    <|> CheckExpectation.equalByKey String.length 5
```

## When To Prefer Named APIs

Prefer named APIs over operators when:

- you need `result` instead of fail-fast behavior
- the symbolic form hides too much meaning
- the property needs `config` or `arbitrary`
- the expectation deserves a meaningful name of its own

Operators are there to reduce ceremony, not to hide the structure of the test.
