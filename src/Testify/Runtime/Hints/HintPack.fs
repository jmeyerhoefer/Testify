namespace Testify


/// <summary>Named group of hint rules that can be enabled together in a stable order.</summary>
type TestifyHintPack =
    {
        /// <summary>The stable pack name.</summary>
        Name: string
        /// <summary>The rules that belong to the pack.</summary>
        Rules: TestifyHintRule list
    }


[<RequireQualifiedAccess>]
module TestifyHintPack =
    /// <summary>Creates a named hint pack from an ordered list of rules.</summary>
    /// <param name="name">The stable pack name.</param>
    /// <param name="rules">The rules that should be applied when the pack is enabled.</param>
    /// <returns>A hint pack that can be passed to <c>TestifyConfig.withHintPacks</c>.</returns>
    /// <seealso cref="M:Testify.TestifyConfig.withHintPacks(Microsoft.FSharp.Collections.FSharpList{Testify.TestifyHintPack},Testify.TestifyConfig)">
    /// Use <c>withHintPacks</c> to enable one or more packs globally.
    /// </seealso>
    /// <example id="hint-pack-create-1">
    /// <code lang="fsharp">
    /// let coursePack =
    ///     TestifyHintPack.create
    ///         "course"
    ///         [ TestifyHintRule.onFieldRegexPattern
    ///               "Course.TrailingSpace"
    ///               HintTextField.ActualValue
    ///               @"\s+$"
    ///               (fun _ -> "The value appears to end with trailing whitespace.") ]
    /// </code>
    /// </example>
    let create
        (name: string)
        (rules: TestifyHintRule list)
        : TestifyHintPack =
        {
            Name = name
            Rules = rules
        }
