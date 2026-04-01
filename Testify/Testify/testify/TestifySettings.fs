namespace Testify

open System


/// <summary>Controls how much detail Testify includes in rendered reports.</summary>
type Verbosity =
    | Default = -1
    | Quiet = 0
    | Normal = 1
    | Detailed = 2
    | Diagnostic = 3


/// <summary>Options that influence how Testify renders assertion and property failures.</summary>
type TestifyReportOptions =
    {
        Verbosity: Verbosity
        IncludeCodeContext: bool
        MaxValueLines: int
    }
    static member Default =
        {
            Verbosity = Verbosity.Normal
            IncludeCodeContext = false
            MaxValueLines = 12
        }


/// <summary>Helpers for normalizing report options against Testify defaults.</summary>
[<RequireQualifiedAccess>]
module TestifyReportOptions =
    let private defaults =
        TestifyReportOptions.Default

    /// <summary>Replaces placeholder values such as <c>Verbosity.Default</c> with concrete defaults.</summary>
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
        }


/// <summary>Global process-wide settings that affect Testify result persistence and rendering defaults.</summary>
type TestifySettings private () =
    static let mutable overwriteExistingResults = true
    static let mutable resultRootOverride: string option = None

    static member OverwriteExistingResults
        with get() =
            match Environment.GetEnvironmentVariable("TESTIFY_OVERWRITE_EXISTING_RESULTS") with
            | null
            | "" -> overwriteExistingResults
            | value ->
                match Boolean.TryParse value with
                | true, parsed -> parsed
                | _ -> overwriteExistingResults
        and set value =
            overwriteExistingResults <- value

    static member ResultRootOverride
        with get() =
            match resultRootOverride with
            | Some _ as value -> value
            | None ->
                match Environment.GetEnvironmentVariable("TESTIFY_RESULT_ROOT") with
                | null
                | "" -> None
                | value -> Some value
        and set value =
            resultRootOverride <- value

    static member val DefaultReportOptions = TestifyReportOptions.Default with get, set
