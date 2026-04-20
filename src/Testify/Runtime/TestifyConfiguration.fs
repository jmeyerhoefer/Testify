namespace Testify


/// <summary>Global default configuration that Testify uses when callers do not override behavior explicitly.</summary>
/// <remarks>
/// Configure this once during test setup, then use the normal <c>Assert</c> and <c>Check</c>
/// APIs. Explicit per-call overrides still take precedence where used.
/// </remarks>
type TestifyConfig =
    {
        ReportOptions: TestifyReportOptions
        HintRules: TestifyHintRule list
        HintPacks: TestifyHintPack list
        CheckConfigTransformers: (FsCheck.Config -> FsCheck.Config) list
    }


[<RequireQualifiedAccess>]
module TestifyConfig =
    /// <summary>The neutral Testify configuration.</summary>
    /// <returns>
    /// A configuration with default report options, no hint rules, no hint packs, and no property-config
    /// transformers.
    /// </returns>
    /// <example id="testify-config-defaults-1">
    /// <code lang="fsharp">
    /// let config = TestifyConfig.defaults
    /// </code>
    /// </example>
    let defaults : TestifyConfig =
        {
            ReportOptions = TestifyReportOptions.Default
            HintRules = []
            HintPacks = []
            CheckConfigTransformers = []
        }

    /// <summary>Replaces the default report options used by Testify.</summary>
    /// <param name="options">The new report options to install into the configuration.</param>
    /// <param name="config">The existing Testify configuration.</param>
    /// <returns>A copy of <paramref name="config" /> that uses <paramref name="options" />.</returns>
    /// <seealso cref="M:Testify.TestifyConfig.withOutputFormat(Testify.OutputFormat,Testify.TestifyConfig)">
    /// Use <c>withOutputFormat</c> when only the output format should change.
    /// </seealso>
    let withReportOptions
        (options: TestifyReportOptions)
        (config: TestifyConfig)
        : TestifyConfig =
        { config with ReportOptions = options }

    /// <summary>Replaces the configured output format used by Testify rendering.</summary>
    /// <param name="outputFormat">The new rendered output format, such as <c>WallOfText</c> or <c>Json</c>.</param>
    /// <param name="config">The existing Testify configuration.</param>
    /// <returns>A copy of <paramref name="config" /> with its report output format replaced.</returns>
    /// <example id="testify-config-output-format-1">
    /// <code lang="fsharp">
    /// let config =
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.withOutputFormat OutputFormat.Json
    /// </code>
    /// </example>
    let withOutputFormat
        (outputFormat: OutputFormat)
        (config: TestifyConfig)
        : TestifyConfig =
        {
            config with
                ReportOptions =
                    {
                        config.ReportOptions with
                            OutputFormat = outputFormat
                    }
        }

    /// <summary>Replaces the configured hint rules used by Testify.</summary>
    /// <param name="rules">The explicit hint rules that should replace the current list.</param>
    /// <param name="config">The existing Testify configuration.</param>
    /// <returns>A copy of <paramref name="config" /> with its hint rules replaced.</returns>
    /// <example id="testify-config-with-hints-1">
    /// <code lang="fsharp">
    /// let whitespaceHint =
    ///     TestifyHintRule.onFieldRegexPattern
    ///         "Whitespace.Trailing"
    ///         HintTextField.ActualValue
    ///         @"\s+$"
    ///         (fun _ -> "The value appears to end with trailing whitespace.")
    ///
    /// let config =
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.withHints [ whitespaceHint ]
    /// </code>
    /// </example>
    let withHints
        (rules: TestifyHintRule list)
        (config: TestifyConfig)
        : TestifyConfig =
        { config with HintRules = rules }

    /// <summary>Replaces the configured hint packs used by Testify.</summary>
    /// <param name="packs">The hint packs that should replace the current list.</param>
    /// <param name="config">The existing Testify configuration.</param>
    /// <returns>A copy of <paramref name="config" /> with its hint packs replaced.</returns>
    /// <example id="testify-config-with-hintpacks-1">
    /// <code lang="fsharp">
    /// let config =
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.withHintPacks BuiltInHintPacks.beginner
    /// </code>
    /// </example>
    let withHintPacks
        (packs: TestifyHintPack list)
        (config: TestifyConfig)
        : TestifyConfig =
        { config with HintPacks = packs }

    /// <summary>Adds one transformer that augments the default FsCheck configuration used by Testify property checks.</summary>
    /// <param name="transformer">The transformer that should adjust the default FsCheck configuration.</param>
    /// <param name="config">The existing Testify configuration.</param>
    /// <returns>A copy of <paramref name="config" /> with the transformer appended.</returns>
    /// <example id="testify-config-transformer-1">
    /// <code lang="fsharp">
    /// let config =
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
    /// </code>
    /// </example>
    let addCheckConfigTransformer
        (transformer: FsCheck.Config -> FsCheck.Config)
        (config: TestifyConfig)
        : TestifyConfig =
        {
            config with
                CheckConfigTransformers = config.CheckConfigTransformers @ [ transformer ]
        }

