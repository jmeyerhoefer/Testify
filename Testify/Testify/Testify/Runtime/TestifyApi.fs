namespace Testify


/// <summary>Public facade for installing and inspecting Testify's global default configuration.</summary>
/// <remarks>
/// <para>
/// Configure Testify once during test setup, then use the ordinary <c>Assert</c> and <c>Check</c>
/// APIs. Explicit per-call overloads still override these defaults.
/// </para>
/// <para>
/// Example:
/// <code lang="fsharp">
/// Testify.configure (
///     TestifyConfig.defaults
///     |> TestifyConfig.withHints [ MiniHints.placeholderTodo ]
///     |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs
/// )
/// </code>
/// </para>
/// </remarks>
[<AbstractClass; Sealed>]
type Testify private () =
    /// <summary>Installs the supplied global Testify configuration.</summary>
    static member configure
        (config: TestifyConfig)
        : unit =
        TestifySettings.Configuration <- config

    /// <summary>Returns the currently installed global Testify configuration.</summary>
    static member currentConfiguration() : TestifyConfig =
        TestifySettings.Configuration

    /// <summary>Restores Testify's neutral default configuration.</summary>
    static member resetConfiguration() : unit =
        TestifySettings.Configuration <- TestifyConfig.defaults

