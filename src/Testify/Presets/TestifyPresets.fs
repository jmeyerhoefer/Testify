namespace Testify


open System


/// <summary>Reusable hint rules for Mini/course-specific teaching scenarios.</summary>
[<RequireQualifiedAccess>]
module MiniHints =
    /// <summary>Suggests that a common placeholder implementation still needs to be replaced.</summary>
    let placeholderTodo : TestifyHintRule =
        TestifyHintRule.onFieldRegexPattern
            "Mini.PlaceholderTodo"
            HintTextField.Actual
            @"\bTODO\b"
            (fun _ -> "Implementation placeholder detected.")

    /// <summary>Suggests checking whether a natural-number literal needs an <c>N</c> suffix.</summary>
    let missingNatSuffix : TestifyHintRule =
        TestifyHintRule.create "Mini.MissingNatSuffix" (fun report ->
            let relevantTexts =
                [
                    report.Actual
                    report.Because
                    report.DetailsText
                ]
                |> List.choose id

            let containsNat =
                relevantTexts
                |> List.exists (fun text -> text.Contains("Nat", StringComparison.Ordinal))

            let containsInt =
                relevantTexts
                |> List.exists (fun text -> text.Contains("int", StringComparison.Ordinal))

            if containsNat && containsInt then
                Some "Check whether Nat literals need an N suffix."
            else
                None)

    /// <summary>Groups the Mini-specific hints so they can be enabled in a stable order.</summary>
    let pack : TestifyHintPack =
        TestifyHintPack.create
            "mini"
            [
                placeholderTodo
                missingNatSuffix
            ]


/// <summary>Optional ready-made Testify configurations for specific teaching contexts.</summary>
[<RequireQualifiedAccess>]
module TestifyPresets =
    /// <summary>Mini-oriented configuration helpers.</summary>
    [<RequireQualifiedAccess>]
    module Mini =
        /// <summary>A Mini-friendly Testify configuration with Mini arbitraries enabled.</summary>
        let config : TestifyConfig =
            TestifyConfig.defaults
            |> TestifyConfig.addCheckConfigTransformer CheckConfig.addMiniArbs

        /// <summary>A Mini-friendly Testify configuration with Mini arbitraries and Mini hint rules enabled.</summary>
        let configWithHints : TestifyConfig =
            config
            |> TestifyConfig.withHintPacks [ MiniHints.pack ]

        /// <summary>An explicit Mini-enabled FsCheck configuration for use with custom per-test tweaking.</summary>
        let checkConfig : FsCheck.Config =
            CheckConfig.defaultConfig
            |> CheckConfig.addMiniArbs

        /// <summary>Installs the Mini preset without optional Mini hint rules.</summary>
        let install () : unit =
            Testify.configure config

        /// <summary>Installs the Mini preset together with optional Mini hint rules.</summary>
        let installWithHints () : unit =
            Testify.configure configWithHints

