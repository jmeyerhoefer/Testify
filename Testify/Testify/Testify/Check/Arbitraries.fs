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
    let fromConfig<'T> (config: FsCheck.Config) : FsCheck.Arbitrary<'T> =
        config.ArbMap.ArbFor<'T> ()

    /// <summary>Looks up the arbitrary for a type from <c>CheckConfig.defaultConfig</c>.</summary>
    let from<'T> : FsCheck.Arbitrary<'T> =
        fromConfig<'T> CheckConfig.defaultConfig

    /// <summary>Creates an arbitrary from a generator without a custom shrinker.</summary>
    let fromGen (generator: FsCheck.Gen<'T>) : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.fromGen generator

    /// <summary>Creates an arbitrary from a generator and an explicit shrinker.</summary>
    let fromGenShrink
        (generator: FsCheck.Gen<'T>)
        (shrinker: 'T -> seq<'T>)
        : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.fromGenShrink (generator, shrinker)

    /// <summary>Maps an arbitrary from one type to another using reversible conversion functions.</summary>
    let convert
        (toValue: 'T1 -> 'T2)
        (fromValue: 'T2 -> 'T1)
        (arbitrary: FsCheck.Arbitrary<'T1>)
        : FsCheck.Arbitrary<'T2> =
        FsCheck.FSharp.Arb.convert toValue fromValue arbitrary

    /// <summary>Filters the generated values of an arbitrary with the supplied predicate.</summary>
    let filter
        (predicate: 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Arbitrary<'T> =
        FsCheck.FSharp.Arb.filter predicate arbitrary

    /// <summary>Builds an arbitrary that generates and shrinks pairs from two arbitraries.</summary>
    let tuple2
        (arbitrary1: FsCheck.Arbitrary<'T1>)
        (arbitrary2: FsCheck.Arbitrary<'T2>)
        : FsCheck.Arbitrary<'T1 * 'T2> =
        fromGenShrink
            (Generators.tuple2 arbitrary1.Generator arbitrary2.Generator)
            (shrinkPair arbitrary1 arbitrary2)

    /// <summary>Builds an arbitrary that generates and shrinks triples from three arbitraries.</summary>
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
