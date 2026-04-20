namespace Testify


/// <summary>Helpers for assembling custom FsCheck arbitraries used by Testify checks.</summary>
[<RequireQualifiedAccess>]
module Arbitraries =
    let private shrinkPair
        (arbitrary1: FsCheck.Arbitrary<'T1>)
        (arbitrary2: FsCheck.Arbitrary<'T2>)
        ((value1, value2): 'T1 * 'T2)
        : seq<'T1 * 'T2> =
        seq {
            for shrunkValue1 in arbitrary1.Shrinker value1 do
                yield shrunkValue1, value2

            for shrunkValue2 in arbitrary2.Shrinker value2 do
                yield value1, shrunkValue2
        }

    let private shrinkTriple
        (arbitrary1: FsCheck.Arbitrary<'T1>)
        (arbitrary2: FsCheck.Arbitrary<'T2>)
        (arbitrary3: FsCheck.Arbitrary<'T3>)
        ((value1, value2, value3): 'T1 * 'T2 * 'T3)
        : seq<'T1 * 'T2 * 'T3> =
        seq {
            for shrunkValue1 in arbitrary1.Shrinker value1 do
                yield shrunkValue1, value2, value3

            for shrunkValue2 in arbitrary2.Shrinker value2 do
                yield value1, shrunkValue2, value3

            for shrunkValue3 in arbitrary3.Shrinker value3 do
                yield value1, value2, shrunkValue3
        }

    /// <summary>Looks up the arbitrary for a type from the supplied configuration.</summary>
    /// <param name="config">The configuration whose arbitrary map should be queried.</param>
    /// <returns>The resolved arbitrary for <c>'T</c> from <paramref name="config" />.</returns>
    let fromConfig<'T> (config: FsCheck.Config) : FsCheck.Arbitrary<'T> =
        config.ArbMap.ArbFor<'T> ()

    /// <summary>Looks up the arbitrary for a type from the neutral <c>CheckConfig.defaultConfig</c>.</summary>
    /// <returns>The resolved arbitrary for <c>'T</c> from <c>CheckConfig.defaultConfig</c>.</returns>
    /// <example id="arbitraries-from-1">
    /// <code lang="fsharp">
    /// let intArb = Arbitraries.from&lt;int&gt;
    /// </code>
    /// </example>
    let from<'T> : FsCheck.Arbitrary<'T> =
        fromConfig<'T> CheckConfig.defaultConfig

    /// <summary>Creates an arbitrary from a generator without a custom shrinker.</summary>
    /// <param name="generator">The generator that should produce values.</param>
    /// <returns>An arbitrary that uses <paramref name="generator" /> and FsCheck's default empty shrinker.</returns>
    /// <example id="arbitraries-fromgen-1">
    /// <code lang="fsharp">
    /// let digitArb =
    ///     Arbitraries.fromGen (Generators.elements [ '0' .. '9' ])
    /// </code>
    /// </example>
    let fromGen (generator: FsCheck.Gen<'T>) : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.fromGen generator

    /// <summary>Creates an arbitrary from a generator and an explicit shrinker.</summary>
    /// <param name="generator">The generator that should produce values.</param>
    /// <param name="shrinker">The shrinker used to reduce failing counterexamples.</param>
    /// <returns>An arbitrary that uses the supplied generator and shrinker.</returns>
    let fromGenShrink
        (generator: FsCheck.Gen<'T>)
        (shrinker: 'T -> seq<'T>)
        : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.fromGenShrink (generator, shrinker)

    /// <summary>Maps an arbitrary from one type to another using reversible conversion functions.</summary>
    /// <param name="toValue">Converts source arbitrary values into the target representation.</param>
    /// <param name="fromValue">Converts target values back to the source representation for shrinking.</param>
    /// <param name="arbitrary">The source arbitrary.</param>
    /// <returns>A mapped arbitrary for the target type.</returns>
    let convert
        (toValue: 'T1 -> 'T2)
        (fromValue: 'T2 -> 'T1)
        (arbitrary: FsCheck.Arbitrary<'T1>)
        : FsCheck.Arbitrary<'T2> =
        FsCheck.FSharp.Arb.convert toValue fromValue arbitrary

    /// <summary>Filters the generated values of an arbitrary with the supplied predicate.</summary>
    /// <param name="predicate">The predicate that candidate values must satisfy.</param>
    /// <param name="arbitrary">The arbitrary to filter.</param>
    /// <returns>An arbitrary that only keeps values satisfying <paramref name="predicate" />.</returns>
    let filter
        (predicate: 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.filter predicate arbitrary

    /// <summary>Builds an arbitrary that generates and shrinks pairs from two arbitraries.</summary>
    /// <param name="arbitrary1">The arbitrary for the first tuple component.</param>
    /// <param name="arbitrary2">The arbitrary for the second tuple component.</param>
    /// <returns>An arbitrary that generates and shrinks pairs.</returns>
    /// <example id="arbitraries-tuple2-1">
    /// <code lang="fsharp">
    /// let pairArb =
    ///     Arbitraries.tuple2
    ///         (Arbitraries.from&lt;int&gt;)
    ///         (Arbitraries.from&lt;string&gt;)
    /// </code>
    /// </example>
    let tuple2
        (arbitrary1: FsCheck.Arbitrary<'T1>)
        (arbitrary2: FsCheck.Arbitrary<'T2>)
        : FsCheck.Arbitrary<'T1 * 'T2> =
        fromGenShrink
            (Generators.tuple2 arbitrary1.Generator arbitrary2.Generator)
            (shrinkPair arbitrary1 arbitrary2)

    /// <summary>Builds an arbitrary that generates and shrinks triples from three arbitraries.</summary>
    /// <param name="arbitrary1">The arbitrary for the first tuple component.</param>
    /// <param name="arbitrary2">The arbitrary for the second tuple component.</param>
    /// <param name="arbitrary3">The arbitrary for the third tuple component.</param>
    /// <returns>An arbitrary that generates and shrinks triples.</returns>
    let tuple3
        (arbitrary1: FsCheck.Arbitrary<'T1>)
        (arbitrary2: FsCheck.Arbitrary<'T2>)
        (arbitrary3: FsCheck.Arbitrary<'T3>)
        : FsCheck.Arbitrary<'T1 * 'T2 * 'T3> =
        fromGenShrink
            (
                Generators.tuple3
                    arbitrary1.Generator
                    arbitrary2.Generator
                    arbitrary3.Generator
            )
            (shrinkTriple arbitrary1 arbitrary2 arbitrary3)

    let pairOf<'T1, 'T2> : FsCheck.Arbitrary<'T1 * 'T2> =
        tuple2 from<'T1> from<'T2>

    let tripleOf<'T1, 'T2, 'T3> : FsCheck.Arbitrary<'T1 * 'T2 * 'T3> =
        tuple3 from<'T1> from<'T2> from<'T3>

    let listOf<'T> : FsCheck.Arbitrary<'T list> =
        from<'T list>

    let arrayOf<'T> : FsCheck.Arbitrary<'T array> =
        from<'T array>

    let seqOf<'T> : FsCheck.Arbitrary<'T seq> =
        from<'T seq>


/// <summary>Operator helpers for composing arbitraries.</summary>
module ArbitraryOperators =
    /// <summary>Combines two arbitraries into an arbitrary of pairs.</summary>
    let inline (<.>) arb1 arb2 = Arbitraries.tuple2 arb1 arb2
