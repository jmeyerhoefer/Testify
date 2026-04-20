namespace Testify


/// <summary>High-level category of failure that Testify is reporting.</summary>
type TestifyFailureKind =
    /// <summary>A direct example-based assertion failed.</summary>
    | AssertionFailure
    /// <summary>A generated property check found a counterexample.</summary>
    | PropertyFailure
    /// <summary>A property check could not generate enough valid inputs to finish.</summary>
    | PropertyExhausted
    /// <summary>A property check encountered an unexpected runtime or infrastructure error.</summary>
    | PropertyError


/// <summary>Structured, renderer-friendly failure data used across Assert and Check reporting.</summary>
/// <remarks>
/// This is the common payload behind rendered failure text, JSON output, hint inference, and persisted
/// diagnostic artifacts.
/// </remarks>
type TestifyFailureReport =
    {
        /// <summary>The high-level failure category.</summary>
        Kind: TestifyFailureKind
        /// <summary>An optional user-provided or framework-provided label for the failing test.</summary>
        Label: string option
        /// <summary>The main one-line failure summary.</summary>
        Summary: string
        /// <summary>Explicit and inferred hints associated with the failure.</summary>
        Hints: string list
        /// <summary>The rendered tested expression when available.</summary>
        Test: string option
        /// <summary>The human-readable expectation description.</summary>
        Expectation: string option
        /// <summary>The rendered expected value or behavior description.</summary>
        Expected: string option
        /// <summary>The rendered actual value or behavior description.</summary>
        Actual: string option
        /// <summary>The raw expected value display when Testify can expose it separately.</summary>
        ExpectedValue: string option
        /// <summary>The raw actual value display when Testify can expose it separately.</summary>
        ActualValue: string option
        /// <summary>Structured metadata describing the expected observation.</summary>
        ExpectedObservedInfo: TestifyObservedInfo option
        /// <summary>Structured metadata describing the actual observation.</summary>
        ActualObservedInfo: TestifyObservedInfo option
        /// <summary>An optional explanation of why the failure occurred.</summary>
        Because: string option
        /// <summary>Additional detail text, often including nested or structured mismatch information.</summary>
        DetailsText: string option
        /// <summary>Optional diff-oriented text extracted from the failure details.</summary>
        DiffText: string option
        /// <summary>The original unshrunk property test expression when available.</summary>
        OriginalTest: string option
        /// <summary>The original unshrunk expected value text when available.</summary>
        OriginalExpected: string option
        /// <summary>The original unshrunk actual value text when available.</summary>
        OriginalActual: string option
        /// <summary>Structured metadata for the original expected observation.</summary>
        OriginalExpectedObservedInfo: TestifyObservedInfo option
        /// <summary>Structured metadata for the original actual observation.</summary>
        OriginalActualObservedInfo: TestifyObservedInfo option
        /// <summary>The final shrunk property test expression when available.</summary>
        ShrunkTest: string option
        /// <summary>The final shrunk expected value text when available.</summary>
        ShrunkExpected: string option
        /// <summary>The final shrunk actual value text when available.</summary>
        ShrunkActual: string option
        /// <summary>Structured metadata for the shrunk expected observation.</summary>
        ShrunkExpectedObservedInfo: TestifyObservedInfo option
        /// <summary>Structured metadata for the shrunk actual observation.</summary>
        ShrunkActualObservedInfo: TestifyObservedInfo option
        /// <summary>The number of generated tests run before the failure, when applicable.</summary>
        NumberOfTests: int option
        /// <summary>The number of shrink steps performed before the final counterexample, when applicable.</summary>
        NumberOfShrinks: int option
        /// <summary>The replay token for reproducing the failing property run, when available.</summary>
        Replay: string option
        /// <summary>The most relevant recovered source location for the failure.</summary>
        SourceLocation: Diagnostics.SourceLocation option
    }


/// <summary>Logical fields that the Testify renderer may include or hide based on verbosity.</summary>
type ReportField =
    /// <summary>The one-line summary field.</summary>
    | Summary
    /// <summary>The resolved hint list.</summary>
    | Hints
    /// <summary>The expectation description.</summary>
    | Expectation
    /// <summary>The rendered expected text.</summary>
    | Expected
    /// <summary>The rendered actual text.</summary>
    | Actual
    /// <summary>The raw expected value display field.</summary>
    | ExpectedValue
    /// <summary>The raw actual value display field.</summary>
    | ActualValue
    /// <summary>The explanatory because field.</summary>
    | Because
    /// <summary>The additional details field.</summary>
    | Details
    /// <summary>The diff-specific text field.</summary>
    | Diff
    /// <summary>The original unshrunk property test field.</summary>
    | OriginalTest
    /// <summary>The original unshrunk expected field.</summary>
    | OriginalExpected
    /// <summary>The original unshrunk actual field.</summary>
    | OriginalActual
    /// <summary>The shrunk property test field.</summary>
    | ShrunkTest
    /// <summary>The shrunk expected field.</summary>
    | ShrunkExpected
    /// <summary>The shrunk actual field.</summary>
    | ShrunkActual
    /// <summary>The number-of-tests metadata field.</summary>
    | NumberOfTests
    /// <summary>The number-of-shrinks metadata field.</summary>
    | NumberOfShrinks
    /// <summary>The replay-token field.</summary>
    | Replay
    /// <summary>The source-location field.</summary>
    | SourceLocation
