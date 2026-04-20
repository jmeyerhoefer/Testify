namespace Testify


/// <summary>Controls how much detail Testify includes in rendered reports.</summary>
type Verbosity =
    /// <summary>Use the library default verbosity.</summary>
    | Default = -1
    /// <summary>Show only the smallest useful set of report fields.</summary>
    | Quiet = 0
    /// <summary>Show the normal report surface for most tests.</summary>
    | Normal = 1
    /// <summary>Show a richer failure report including details and shrunk information.</summary>
    | Detailed = 2
    /// <summary>Show the most debugging-oriented failure report.</summary>
    | Diagnostic = 3

/// <summary>Controls which textual representation Testify emits for rendered results.</summary>
type OutputFormat =
    /// <summary>Human-readable multi-line text rendering.</summary>
    | WallOfText = 0
    /// <summary>Structured JSON rendering suitable for tools and downstream consumers.</summary>
    | Json = 1

/// <summary>Options that influence how Testify renders assertion and property failures.</summary>
type TestifyReportOptions =
    {
        /// <summary>The verbosity level used to choose which report fields are visible.</summary>
        Verbosity: Verbosity
        /// <summary>The maximum number of lines shown for rendered multi-line values before truncation.</summary>
        MaxValueLines: int
        /// <summary>The output format used for rendering.</summary>
        OutputFormat: OutputFormat
    }
    static member Default =
        {
            Verbosity = Verbosity.Normal
            MaxValueLines = 12
            OutputFormat = OutputFormat.WallOfText
        }


/// <summary>Helpers for normalizing report options against Testify defaults.</summary>
[<RequireQualifiedAccess>]
module TestifyReportOptions =
    let private defaults =
        TestifyReportOptions.Default

    /// <summary>Replaces placeholder values such as <c>Verbosity.Default</c> with concrete defaults.</summary>
    /// <param name="options">The report options that may still contain placeholder values.</param>
    /// <returns>A normalized copy of <paramref name="options" /> with concrete verbosity and line-limit values.</returns>
    let normalize
        (options: TestifyReportOptions)
        : TestifyReportOptions =
        let verbosity =
            match options.Verbosity with
            | Verbosity.Default -> defaults.Verbosity
            | value -> value

        let maxValueLines =
            if options.MaxValueLines > 0 then
                options.MaxValueLines
            else
                defaults.MaxValueLines

        {
            options with
                Verbosity = verbosity
                MaxValueLines = maxValueLines
                OutputFormat = options.OutputFormat
        }

