//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// TYPES %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.Types


// FsCheck Modules
module Arb = FsCheck.FSharp.Arb
module ArbMap = FsCheck.FSharp.ArbMap
module Gen = FsCheck.FSharp.Gen
module GenBuilder = FsCheck.FSharp.GenBuilder
module Prop = FsCheck.FSharp.Prop


// FsCheck Types
type Arbitrary<'a> = FsCheck.Arbitrary<'a>
type Check = FsCheck.Check
type Config = FsCheck.Config
type Gen<'a> = FsCheck.Gen<'a>
type GenBuilder = FsCheck.FSharp.GenBuilder.GenBuilder
type IRunner = FsCheck.IRunner
type TestResult = FsCheck.TestResult
type Property = FsCheck.Property


/// <summary>TODO</summary>
module GenBuilder =
    /// <summary>TODO</summary>
    let gen: GenBuilder = GenBuilder.gen


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
type SmallNat = SmallNat of Nat


/// <summary>TODO</summary>
type NatModifier =
    /// <summary>
    /// Returns Arbitrary for <c>Nat</c>
    /// </summary>
    static member Nat (): Arbitrary<Nat> =
        ArbMap.defaults
        |> ArbMap.arbitrary<FsCheck.NonNegativeInt>
        |> Arb.convert (int >> Nat.Make) (int >> FsCheck.NonNegativeInt)


    /// <summary>
    /// Returns Arbitrary for <c>Nat list</c>
    /// </summary>
    static member NatList (): Arbitrary<Nat list> = NatModifier.Nat () |> Arb.list


    /// <summary>
    /// Returns Arbitrary for <c>Nat list</c> of length <c>n</c>
    /// </summary>
    static member NatListOfLength (n: Nat): Gen<Nat list> = NatModifier.Nat().Generator |> Gen.listOfLength (int n)


    /// <summary>
    /// Returns Arbitrary for <c>Nat array</c>
    /// </summary>
    static member NatArray (): Arbitrary<Nat array> = NatModifier.Nat () |> Arb.array


    /// <summary>
    /// Returns Arbitrary for <c><![CDATA[Set<Nat>]]></c>
    /// </summary>
    static member NatSet (): Arbitrary<Set<Nat>> = NatModifier.Nat () |> Arb.set


    /// <summary>
    /// Returns Arbitrary for <c>Nat option</c>
    /// </summary>
    static member NatOption (): Arbitrary<Nat option> = NatModifier.Nat () |> Arb.option


    /// <summary>
    /// Returns Arbitrary for <c>SmallNat</c>
    /// </summary>
    static member SmallNat (): Arbitrary<SmallNat> =
        ArbMap.defaults
        |> ArbMap.arbitrary<Nat>
        |> Arb.filter (fun (n: Nat) -> n < 7N)
        |> Arb.convert SmallNat (fun (SmallNat (n: Nat)) -> n)


/// <summary>TODO</summary>
module Configurations =
    /// <summary>TODO</summary>
    let DefaultConfig: Config = Config.QuickThrowOnFailure.WithArbitrary [ typeof<NatModifier> ]


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute</c> class.</summary>
type TestClassAttribute () = inherit Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute ()


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute</c> class.</summary>
type TestMethodAttribute () = inherit Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute ()


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute</c> class.</summary>
/// <param name="timeout">The timeout of a unit test.</param>
type TimeoutAttribute (timeout: int) =
    inherit System.Attribute ()


    /// <summary>The timeout of a unit test.</summary>
    member _.Timeout: int = timeout


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%