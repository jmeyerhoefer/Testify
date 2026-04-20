namespace Testify


/// <summary>Public facade for installing and inspecting Testify's global default configuration.</summary>
/// <remarks>
/// <para>
/// Configure Testify once during test setup, then use the ordinary <c>Assert</c> and <c>Check</c>
/// APIs. Explicit per-call overloads still override these defaults.
/// </para>
/// <para>
/// Hints are opt-in. Start from <c>TestifyConfig.defaults</c> for a neutral configuration, then
/// add <c>withHints</c> or <c>withHintPacks</c> only when a test suite intentionally wants them.
/// </para>
/// <para>
/// Example:
/// <code lang="fsharp">
/// Testify.configure (
///     TestifyConfig.defaults
///     |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
/// )
///
/// Testify.configure (
///     TestifyConfig.defaults
///     |> TestifyConfig.withHintPacks [ MiniHints.pack ]
///     |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
/// )
/// </code>
/// </para>
/// </remarks>
[<AbstractClass; Sealed>]
type Testify private () =
    /// <summary>Installs the supplied global Testify configuration.</summary>
    /// <param name="config">
    /// The configuration to install globally for subsequent <c>Assert</c> and <c>Check</c> calls that
    /// do not override behavior explicitly.
    /// </param>
    /// <seealso cref="M:Testify.Testify.currentConfiguration">
    /// Inspect the currently installed defaults without changing them.
    /// </seealso>
    /// <example id="testify-configure-1">
    /// <code lang="fsharp">
    /// Testify.configure (
    ///     TestifyConfig.defaults
    ///     |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
    /// )
    /// </code>
    /// </example>
    static member configure
        (config: TestifyConfig)
        : unit =
        TestifySettings.Configuration <- config

    /// <summary>Returns the currently installed global Testify configuration.</summary>
    /// <returns>The active global Testify configuration.</returns>
    /// <seealso cref="M:Testify.Testify.configure(Testify.TestifyConfig)">
    /// Install a new global configuration before running tests.
    /// </seealso>
    /// <example id="testify-currentconfiguration-1">
    /// <code lang="fsharp">
    /// let config = Testify.currentConfiguration()
    /// </code>
    /// </example>
    static member currentConfiguration() : TestifyConfig =
        TestifySettings.Configuration

    /// <summary>Restores Testify's neutral default configuration.</summary>
    /// <seealso cref="M:Testify.Testify.configure(Testify.TestifyConfig)">
    /// Use <c>configure</c> when you want a custom global setup instead of the neutral defaults.
    /// </seealso>
    /// <example id="testify-resetconfiguration-1">
    /// <code lang="fsharp">
    /// Testify.resetConfiguration()
    /// </code>
    /// </example>
    static member resetConfiguration() : unit =
        TestifySettings.Configuration <- TestifyConfig.defaults

