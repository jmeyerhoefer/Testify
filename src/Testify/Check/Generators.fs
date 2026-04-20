namespace Testify


/// <summary>Helpers for building FsCheck generators used by Testify property checks.</summary>
[<RequireQualifiedAccess>]
module Generators =
    /// <summary>Looks up the generator for a type from the supplied configuration.</summary>
    /// <param name="config">The configuration whose arbitrary map should provide the generator.</param>
    /// <returns>The resolved generator for <c>'T</c> from <paramref name="config" />.</returns>
    let fromConfig<'T> (config: FsCheck.Config) : FsCheck.Gen<'T> =
        config.ArbMap.ArbFor<'T>().Generator

    /// <summary>Looks up the generator for a type from the neutral <c>CheckConfig.defaultConfig</c>.</summary>
    /// <returns>The resolved generator for <c>'T</c> from <c>CheckConfig.defaultConfig</c>.</returns>
    /// <example id="generators-from-1">
    /// <code lang="fsharp">
    /// let intGen = Generators.from&lt;int&gt;
    /// </code>
    /// </example>
    let from<'T> : FsCheck.Gen<'T> =
        fromConfig<'T> CheckConfig.defaultConfig

    /// <summary>Extracts the generator portion of an arbitrary.</summary>
    /// <param name="arbitrary">The arbitrary whose generator should be reused.</param>
    /// <returns>The generator component of <paramref name="arbitrary" />.</returns>
    let inline fromArb (arbitrary: FsCheck.Arbitrary<'T>) : FsCheck.Gen<'T> =
        arbitrary.Generator

    /// <summary>Creates a generator that chooses from a fixed sequence of values.</summary>
    /// <param name="values">The candidate values that the generator may choose from.</param>
    /// <returns>A generator that yields one value from <paramref name="values" /> on each run.</returns>
    /// <example id="generators-elements-1">
    /// <code lang="fsharp">
    /// let weekendGen = Generators.elements [ "Sat"; "Sun" ]
    /// </code>
    /// </example>
    let elements (values: seq<'T>) : FsCheck.Gen<'T> =
        FsCheck.FSharp.Gen.elements values

    /// <summary>Maps generated values through the supplied projection.</summary>
    /// <param name="mapping">The projection applied to each generated value.</param>
    /// <param name="generator">The source generator.</param>
    /// <returns>A generator that maps each produced value through <paramref name="mapping" />.</returns>
    let inline map
        (mapping: 'T -> 'U)
        (generator: FsCheck.Gen<'T>)
        : FsCheck.Gen<'U> =
        FsCheck.FSharp.Gen.map mapping generator

    /// <summary>Combines two generators into a generator of pairs.</summary>
    /// <param name="generator1">The generator for the first tuple component.</param>
    /// <param name="generator2">The generator for the second tuple component.</param>
    /// <returns>A generator that produces pairs by sampling both input generators.</returns>
    /// <example id="generators-tuple2-1">
    /// <code lang="fsharp">
    /// let pairGen =
    ///     Generators.tuple2
    ///         Generators.from&lt;int&gt;
    ///         Generators.from&lt;string&gt;
    /// </code>
    /// </example>
    let tuple2
        (generator1: FsCheck.Gen<'T1>)
        (generator2: FsCheck.Gen<'T2>)
        : FsCheck.Gen<'T1 * 'T2> =
        FsCheck.FSharp.Gen.map2
            (fun value1 value2 -> value1, value2)
            generator1
            generator2

    /// <summary>Combines three generators into a generator of triples.</summary>
    /// <param name="generator1">The generator for the first tuple component.</param>
    /// <param name="generator2">The generator for the second tuple component.</param>
    /// <param name="generator3">The generator for the third tuple component.</param>
    /// <returns>A generator that produces triples by sampling all three input generators.</returns>
    let tuple3
        (generator1: FsCheck.Gen<'T1>)
        (generator2: FsCheck.Gen<'T2>)
        (generator3: FsCheck.Gen<'T3>)
        : FsCheck.Gen<'T1 * 'T2 * 'T3> =
        FsCheck.FSharp.Gen.map3
            (fun value1 value2 value3 -> value1, value2, value3)
            generator1
            generator2
            generator3

    let listOf<'T> : FsCheck.Gen<'T list> =
        from<'T list>

    let listOfLength<'T>
        (length: int)
        : FsCheck.Gen<'T list> =
        FsCheck.FSharp.Gen.listOfLength length from<'T>

    let arrayOf<'T> : FsCheck.Gen<'T array> =
        from<'T array>

    let seqOf<'T> : FsCheck.Gen<'T seq> =
        from<'T seq>

    /// <summary>Creates a list generator from an explicit arbitrary.</summary>
    /// <param name="arbitrary">The arbitrary whose generator should populate the produced lists.</param>
    /// <returns>A generator that creates lists using <paramref name="arbitrary" />.</returns>
    let listOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T list> =
        FsCheck.FSharp.Gen.listOf arbitrary.Generator

    /// <summary>Creates a fixed-length list generator from an explicit arbitrary.</summary>
    /// <param name="length">The required list length.</param>
    /// <param name="arbitrary">The arbitrary whose generator should populate the produced lists.</param>
    /// <returns>A generator that creates lists of exactly <paramref name="length" /> items.</returns>
    /// <example id="generators-listoflengthwith-1">
    /// <code lang="fsharp">
    /// let wordGen =
    ///     Generators.listOfLengthWith
    ///         5
    ///         (Arbitraries.from&lt;char&gt;)
    /// </code>
    /// </example>
    let listOfLengthWith
        (length: int)
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T list> =
        FsCheck.FSharp.Gen.listOfLength length arbitrary.Generator

    /// <summary>Creates an array generator from an explicit arbitrary.</summary>
    /// <param name="arbitrary">The arbitrary whose generator should populate the produced arrays.</param>
    /// <returns>A generator that creates arrays using <paramref name="arbitrary" />.</returns>
    let arrayOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T array> =
        FsCheck.FSharp.Gen.arrayOf arbitrary.Generator

    /// <summary>Creates a sequence generator from an explicit arbitrary.</summary>
    /// <param name="arbitrary">The arbitrary whose generator should populate the produced sequences.</param>
    /// <returns>A generator that creates sequences using <paramref name="arbitrary" />.</returns>
    let seqOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T seq> =
        listOfWith arbitrary
        |> map Seq.ofList
