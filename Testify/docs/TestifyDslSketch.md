# A Sketch of Testify as a Deeply Embedded DSL

This note gives a compact, design-oriented sketch of the testing DSL exposed by `Testify`.
It is not a full formal semantics and not a parser grammar for arbitrary F# programs.
Instead, it abstracts the public API into a few core syntactic categories that explain why the DSL is
deeply embedded:

- expectations are explicit F# values
- those values can be composed in the host language
- operators provide a readable concrete syntax for applying the embedded terms

## Assert DSL

The Assert side is expression-left: a quoted expression is checked against one reusable
`AssertExpectation`.

```text
AExpect ::= EqualTo(v)
          | NotEqualTo(v)
          | LessThan(v)
          | GreaterThan(v)
          | Satisfy(d, p)
          | Throws
          | DoesNotThrow
          | IsTrue
          | IsFalse
          | IsSome
          | IsNone
          | Not(AExpect)
          | AExpect OrElse AExpect
          | AExpect AndAlso AExpect
          | Any(AExpect*)
          | All(AExpect*)

AExp    ::= <e> |>?  AExpect
          | <e> >>?  AExpect
          | <e> =?   v
          | <e> <>?  v
          | <e> <?   v
          | <e> <=?  v
          | <e> >?   v
          | <e> >=?  v
          | <e> ^?
          | <e> ^!?
          | <e> ?
          | <e> !?
          | <e> ||?  AExpect*
          | <e> &&?  AExpect*
```

## Check DSL

The Check side describes relations between a quoted implementation and a reference implementation
over generated inputs. The deeply embedded core is the reusable `CheckExpectation` value.

```text
CExpect ::= EqualToReference
          | EqualTo(v)
          | EqualToReferenceBy(proj)
          | EqualToReferenceWith(cmp)
          | ThrowsSameExceptionType
          | SatisfyWith(d, p)
          | SatisfyObservedWith(d, p)
          | CExpect OrElse CExpect
          | CExpect AndAlso CExpect

CExp    ::= <f> |=>    r
          | <f> |=>>   r
          | <f> |=>?   (cfg, r)
          | <f> |=>??  (arb, r)
          | <f> |=>??? (CExpect, r)
          | <f> |=>>?  (CExpect, r)
          | <f> ||=>?  (cfg?, arb?, CExpect?, r)
```

## Optional Declaration Layer

At the test-hosting layer, Testify wraps MSTest declarations with attributes that trigger Testify
reporting and result persistence.

```text
TestDecl   ::= [<TestifyClass>] type T = { MethodDecl* }
MethodDecl ::= [<TestifyMethod>] member m = Body
```

## Mapping to the Current Implementation

- `AExpect` corresponds to `AssertExpectation<'T>`.
- `CExpect` corresponds to `CheckExpectation<'Args, 'Actual, 'Expected>`.
- `OrElse` and `AndAlso` correspond to `orElse` / `andAlso` and surface concretely via `<|>` and `<&>`.
- `|>?`, `|=>`, `|=>?`, and `||=>?` are the concrete-syntax operators that apply embedded expectation
  values to quotations and references.
- The DSL is therefore "deep" mainly at the expectation level; the operators are the readable facade.

## Interpretation Note

This sketch is a compact conceptual model of Testify's testing language.
It keeps the main split between the Assert side and the Check side, but it does not try to enumerate
every specialized helper such as diff-aware, string-specific, or sequence-specific expectations.
That is deliberate: the goal here is to show the underlying DSL shape clearly and briefly.
