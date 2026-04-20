namespace Testify


open System
open System.Globalization
open System.Text.RegularExpressions


/// <summary>Helpers for building FsCheck configurations used by <c>Testify.Check</c>.</summary>
/// <remarks>
/// Start with <c>defaultConfig</c> or <c>thorough</c>, then refine the FsCheck run with helpers such
/// as <c>withMaxTest</c>, <c>withEndSize</c>, or replay-specific helpers.
/// </remarks>
[<RequireQualifiedAccess>]
module CheckConfig =
    let private replayPattern =
        Regex("^Rnd=(\d+),(\d+); Size=(None|\d+)$", RegexOptions.Compiled)

    let private parseUInt64Invariant (text: string) : uint64 =
        UInt64.Parse (text, CultureInfo.InvariantCulture)

    let private parseIntInvariant (text: string) : int =
        Int32.Parse (text, CultureInfo.InvariantCulture)

    let private nonNegativeIntArbitrary =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<FsCheck.NonNegativeInt>

    /// <summary>Registers a generator for <c>Mini.Nat</c> based on FsCheck non-negative integers.</summary>
    type NatModifier =
        static member Nat () : FsCheck.Arbitrary<Mini.Nat> =
            nonNegativeIntArbitrary
            |> FsCheck.FSharp.Arb.convert
                (int >> Mini.Nat.Make)
                (int >> FsCheck.NonNegativeInt)

    /// <summary>A tiny natural-number wrapper useful for compact demos and targeted generators.</summary>
    type SmallNat =
        SmallNat of Mini.Nat

    /// <summary>Registers a generator for <c>SmallNat</c> values smaller than seven.</summary>
    type SmallNatModifier =
        static member SmallNat () : FsCheck.Arbitrary<SmallNat> =
            nonNegativeIntArbitrary
            |> FsCheck.FSharp.Arb.filter
                (fun (FsCheck.NonNegativeInt n) -> n < 7)
            |> FsCheck.FSharp.Arb.convert
                (fun (FsCheck.NonNegativeInt n) -> SmallNat (Mini.Nat.Make n))
                (fun (SmallNat n) -> FsCheck.NonNegativeInt (int n))

    /// <summary>Adds Testify-specific arbitraries such as <c>Mini.Nat</c> and <c>SmallNat</c> to a config.</summary>
    /// <param name="config">The base FsCheck configuration to enrich.</param>
    /// <returns>A new configuration whose arbitrary map also includes Testify's demo-specific natural-number helpers.</returns>
    let inline addMiniArbs (config: FsCheck.Config) : FsCheck.Config =
        config.WithArbitrary [
            typeof<NatModifier>
            typeof<SmallNatModifier>
        ]

    /// <summary>The neutral FsCheck configuration used by Testify property checks before any installed transformers are applied.</summary>
    /// <example id="check-config-default-1">
    /// <code lang="fsharp">
    /// let config = CheckConfig.defaultConfig
    /// </code>
    /// </example>
    let defaultConfig : FsCheck.Config =
        FsCheck.Config.Quick.WithName "Testify Check Config"

    /// <summary>Looks up the arbitrary for a type from the supplied configuration.</summary>
    /// <param name="config">The configuration whose arbitrary map should be queried.</param>
    /// <returns>The resolved arbitrary for <c>'T</c> from <paramref name="config" />.</returns>
    let fromConfig<'T> (config: FsCheck.Config) : FsCheck.Arbitrary<'T> =
        config.ArbMap.ArbFor<'T> ()

    /// <summary>Looks up the arbitrary for a type from the neutral <c>defaultConfig</c>.</summary>
    /// <returns>The resolved arbitrary for <c>'T</c> from <c>CheckConfig.defaultConfig</c>.</returns>
    let from<'T> : FsCheck.Arbitrary<'T> =
        fromConfig<'T> defaultConfig

    /// <summary>A slower but more exhaustive neutral configuration with a larger test count.</summary>
    /// <example id="check-config-thorough-1">
    /// <code lang="fsharp">
    /// let config = CheckConfig.thorough
    /// </code>
    /// </example>
    let thorough : FsCheck.Config =
        defaultConfig.WithMaxTest 500

    /// <summary>Creates a neutral configuration that replays a previously recorded FsCheck run.</summary>
    /// <param name="replay">The recorded FsCheck replay token to embed into a new configuration.</param>
    /// <returns>A configuration that reuses the supplied replay information.</returns>
    let withReplay (replay: FsCheck.Replay) : FsCheck.Config =
        defaultConfig.WithReplay (Some replay)

    /// <summary>Parses a replay token emitted by Testify into an FsCheck replay value.</summary>
    /// <param name="text">The replay token string, typically copied from a failing <c>CheckFailure</c>.</param>
    /// <returns>
    /// <c>Some</c> replay value when <paramref name="text" /> matches Testify's replay format;
    /// otherwise <c>None</c>.
    /// </returns>
    let tryParseReplay (text: string) : FsCheck.Replay option =
        let replayMatch = replayPattern.Match text

        if replayMatch.Success then
            let seed = parseUInt64Invariant replayMatch.Groups[1].Value
            let gamma = parseUInt64Invariant replayMatch.Groups[2].Value
            let sizeText = replayMatch.Groups[3].Value

            let replaySize =
                if sizeText = "None" then
                    None
                else
                    Some (parseIntInvariant sizeText)

            Some {
                Rnd = FsCheck.Rnd(seed, gamma)
                Size = replaySize
            }
        else
            None

    /// <summary>Parses a replay token and returns a replay-enabled configuration when successful.</summary>
    /// <param name="text">The replay token string emitted by a previous failing property check.</param>
    /// <returns>
    /// <c>Some</c> replay-enabled configuration when parsing succeeds; otherwise <c>None</c>.
    /// </returns>
    /// <example id="check-config-replay-1">
    /// <code lang="fsharp">
    /// let config =
    ///     CheckConfig.withReplayString "Rnd=1,2; Size=None"
    /// </code>
    /// </example>
    let withReplayString (text: string) : FsCheck.Config option =
        tryParseReplay text
        |> Option.map withReplay

    /// <summary>Creates a neutral configuration with the supplied maximum number of tests.</summary>
    /// <param name="count">The maximum number of generated test cases FsCheck should try.</param>
    /// <returns>A new configuration with the supplied test-count limit.</returns>
    /// <example id="check-config-maxtest-1">
    /// <code lang="fsharp">
    /// let config = CheckConfig.withMaxTest 25
    /// </code>
    /// </example>
    let withMaxTest (count: int) : FsCheck.Config =
        defaultConfig.WithMaxTest count

    /// <summary>Creates a neutral configuration with the supplied maximum generated size.</summary>
    /// <param name="size">The maximum generated size that FsCheck should use.</param>
    /// <returns>A new configuration with the supplied end-size limit.</returns>
    let withEndSize (size: int) : FsCheck.Config =
        defaultConfig.WithEndSize size
