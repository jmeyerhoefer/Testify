namespace Testify

open System


/// <summary>Global process-wide settings that affect Testify result persistence and rendering defaults.</summary>
type TestifySettings private () =
    static let mutable overwriteExistingResults = true
    static let mutable resultRootOverride: string option = None
    static let mutable configuration = TestifyConfig.defaults

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

    static member Configuration
        with get() = configuration
        and set value =
            configuration <-
                {
                    value with
                        ReportOptions = TestifyReportOptions.normalize value.ReportOptions
                }

    static member HintRules
        with get() = configuration.HintRules
        and set value =
            configuration <- { configuration with HintRules = value }

    static member HintPacks
        with get() = configuration.HintPacks
        and set value =
            configuration <- { configuration with HintPacks = value }

    static member DefaultReportOptions
        with get() = configuration.ReportOptions
        and set value =
            configuration <-
                {
                    configuration with
                        ReportOptions = TestifyReportOptions.normalize value
                }

    static member CheckConfigTransformers
        with get() = configuration.CheckConfigTransformers
        and set value =
            configuration <-
                {
                    configuration with
                        CheckConfigTransformers = value
                }

    static member ApplyCheckConfigTransformers
        (config: FsCheck.Config)
        : FsCheck.Config =
        TestifySettings.CheckConfigTransformers
        |> List.fold (fun current transformer -> transformer current) config
