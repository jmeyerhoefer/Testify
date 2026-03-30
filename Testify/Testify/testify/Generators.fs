namespace MiniLib.Testify


/// <summary>Helpers for building FsCheck generators used by Testify property checks.</summary>
[<RequireQualifiedAccess>]
module Generators =
    /// <summary>Looks up the generator for a type from the supplied configuration.</summary>
    let fromConfig<'T> (config: FsCheck.Config) : FsCheck.Gen<'T> =
        config.ArbMap.ArbFor<'T>().Generator

    /// <summary>Looks up the generator for a type from <c>CheckConfig.defaultConfig</c>.</summary>
    let from<'T> : FsCheck.Gen<'T> =
        fromConfig<'T> CheckConfig.defaultConfig

    /// <summary>Extracts the generator portion of an arbitrary.</summary>
    let inline fromArb (arbitrary: FsCheck.Arbitrary<'T>) : FsCheck.Gen<'T> =
        arbitrary.Generator

    /// <summary>Creates a generator that chooses from a fixed sequence of values.</summary>
    let elements (values: seq<'T>) : FsCheck.Gen<'T> =
        FsCheck.FSharp.Gen.elements values

    /// <summary>Maps generated values through the supplied projection.</summary>
    let inline map
        (mapping: 'T -> 'U)
        (generator: FsCheck.Gen<'T>)
        : FsCheck.Gen<'U> =
        FsCheck.FSharp.Gen.map mapping generator

    /// <summary>Combines two generators into a generator of pairs.</summary>
    let tuple2
        (generator1: FsCheck.Gen<'T1>)
        (generator2: FsCheck.Gen<'T2>)
        : FsCheck.Gen<'T1 * 'T2> =
        FsCheck.FSharp.Gen.map2
            (fun value1 value2 -> value1, value2)
            generator1
            generator2

    /// <summary>Combines three generators into a generator of triples.</summary>
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
    let listOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T list> =
        FsCheck.FSharp.Gen.listOf arbitrary.Generator

    /// <summary>Creates a fixed-length list generator from an explicit arbitrary.</summary>
    let listOfLengthWith
        (length: int)
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T list> =
        FsCheck.FSharp.Gen.listOfLength length arbitrary.Generator

    /// <summary>Creates an array generator from an explicit arbitrary.</summary>
    let arrayOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T array> =
        FsCheck.FSharp.Gen.arrayOf arbitrary.Generator

    /// <summary>Creates a sequence generator from an explicit arbitrary.</summary>
    let seqOfWith
        (arbitrary: FsCheck.Arbitrary<'T>)
        : FsCheck.Gen<'T seq> =
        listOfWith arbitrary
        |> map Seq.ofList
