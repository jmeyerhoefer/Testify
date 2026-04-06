namespace Testify


/// <summary>Global default configuration that Testify uses when callers do not override behavior explicitly.</summary>
/// <remarks>
/// Configure this once during test setup, then use the normal <c>Assert</c> and <c>Check</c> APIs.
/// Explicit per-call overloads such as <c>Check.shouldWith</c> still take precedence where used.
/// </remarks>
type TestifyConfig =
    {
        ReportOptions: TestifyReportOptions
        HintRules: TestifyHintRule list
        CheckConfigTransformers: (FsCheck.Config -> FsCheck.Config) list
    }


[<RequireQualifiedAccess>]
module TestifyConfig =
    /// <summary>The neutral Testify configuration.</summary>
    let defaults : TestifyConfig =
        {
            ReportOptions = TestifyReportOptions.Default
            HintRules = []
            CheckConfigTransformers = []
        }

    /// <summary>Replaces the default report options used by Testify.</summary>
    let withReportOptions
        (options: TestifyReportOptions)
        (config: TestifyConfig)
        : TestifyConfig =
        { config with ReportOptions = options }

    /// <summary>Replaces the configured hint rules used by Testify.</summary>
    let withHints
        (rules: TestifyHintRule list)
        (config: TestifyConfig)
        : TestifyConfig =
        { config with HintRules = rules }

    /// <summary>Adds one transformer that augments the default FsCheck configuration used by Testify property checks.</summary>
    let addCheckConfigTransformer
        (transformer: FsCheck.Config -> FsCheck.Config)
        (config: TestifyConfig)
        : TestifyConfig =
        {
            config with
                CheckConfigTransformers = config.CheckConfigTransformers @ [ transformer ]
        }

