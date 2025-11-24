//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// PROPERTYTESTS %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.PropertyTests


open Assertify.Checkify
open Assertify.Expressions
open Assertify.Expressions.Operators
open Assertify.Checkify.Operators
open Assertify.Types
open Assertify.Types.GenBuilder


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


module Student =
    module Lists2 =
        let collect (f: 'a -> 'a list) (xs: 'a list): 'a list =
            List.collect f (xs @ xs)


[<TestClass>]
type PropertyTests () =
    // [<TestMethod; Timeout 5000>]
    // member _.``test with |?> operator`` (): unit =
    //     let generator =
    //         gen {
    //             let! f =
    //                 FsCheck.FSharp.Gen.elements [
    //                     fun x -> [ x + 1N; x + 2N ]
    //                     fun x -> [ x + 1N; x + 2N ]
    //                     fun x -> [ x * 2N; x * 3N ]
    //                 ]
    //             let! xs = NatModifier.NatList().Generator
    //             return f, xs
    //         }
    //     <@ fun (f, xs) -> Student.Lists2.collect f xs = List.collect f xs @>
    //     |?> fun expr -> Checkify.ForAllGen (expr, generator)


    [<TestMethod; Timeout 5000>]
    member _.``test with CheckWithProperty`` (): unit =
        let generator =
            gen {
                let! f =
                    FsCheck.FSharp.Gen.elements [
                        fun x -> [ x + 1N; x + 2N ]
                        fun x -> [ x + 1N; x + 2N ]
                        fun x -> [ x * 2N; x * 3N ]
                    ]
                let! xs = NatModifier.NatList().Generator
                return f, xs
            }
        Checkify.CheckWithProperty (
            <@ fun (f, xs) -> Student.Lists2.collect f xs = List.collect f xs @>,
            fun expr -> Checkify.ForAllGen (expr, generator)
        )


    [<TestMethod; Timeout 5000>]
    member _.``test with CheckAdvanced`` (): unit =
        Checkify.CheckAdvanced (
            <@ fun f xs -> Student.Lists2.collect f xs = List.collect f xs @>,
            (fun expr ->
                fun (xs: Nat list) ->
                    let fG: Arbitrary<Nat -> List<Nat>> =
                        (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                            [ (fun x -> [ x + 1N; x + 2N ])
                              (fun x -> [ x + 1N; x + 2N ])
                              (fun x -> [ x * 2N; x * 3N ]) ] in
                    FsCheck.FSharp.Prop.forAll fG (fun f -> expr |> Expressions.applyArgs [ xs, f ])
            )
        )

    [<TestMethod; Timeout 5000>]
    member _.``test 001`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student.Lists2.collect f xs = List.collect f xs @>,
            (fun expr (xs: Nat list) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun f -> expr |> Expressions.applyArgs [ f, xs ])
            )
        )

    [<TestMethod; Timeout 5000>]
    member _.``test 002`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student.Lists2.collect f xs = List.collect f xs @>,
            (fun expr (xs: Nat list) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ (f, xs))
            )
        )

    [<TestMethod; Timeout 5000>]
    member _.``test 003`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (xs: Nat list) (n: Nat) -> Student.Lists2.collect f xs = [ 0N .. n ] @>,
            (fun expr (xs: Nat list) (n: Nat) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ ((f, xs), n))
            )
        )

    [<TestMethod; Timeout 5000>]
    member _.``test with CheckAdvanced2`` (): unit =
        let fGenerator: Arbitrary<Nat -> List<Nat>> =
            (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                [
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x * 2N; x * 3N ]
                ]
        Checkify.CheckAdvanced<Nat -> Nat list, Nat list> (
            <@ fun f xs -> Student.Lists2.collect f xs = List.collect f xs @>,
            fun expr xs ->
                FsCheck.FSharp.Prop.forAll fGenerator (fun f -> expr |> Expressions.applyArgs [ xs, f ])
        )

    [<TestMethod; Timeout 5000>]
    member _.``test with CheckAdvanced3`` (): unit =
        let fGenerator: Arbitrary<Nat -> List<Nat>> =
            (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                [
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x * 2N; x * 3N ]
                ]
        <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student.Lists2.collect f xs = List.collect f xs @>
        |?> fun expr xs -> FsCheck.FSharp.Prop.forAll fGenerator (fun f -> expr |> Expressions.applyArgs [ xs, f ])


    // [<TestMethod; Timeout 5000>]
    // member _.``test with |?> operator 2`` (): unit =
    //     let generator =
    //             gen {
    //                 let! n = NatModifier.Nat().Generator
    //                 let! xs = NatModifier.NatListOfLength n
    //                 return n, xs
    //             }
    //     <@ fun (n: Nat, xs: Nat list) -> xs.Length = (int n + 1) @>
    //     |?> fun expr ->
    //         Checkify.ForAllGenShrink (expr, generator,
    //             fun (n, xs) -> [ if n = 0N then 0N, [] else n - 1N, xs.Tail ]
    //         )


    [<TestMethod; Timeout 5000>]
    member _.``test with CheckWithProperty2`` (): unit =
        let generator =
            gen {
                let! n = NatModifier.Nat().Generator
                let! xs = NatModifier.NatListOfLength n
                return n, xs
            }
        Checkify.CheckWithProperty (
            <@ fun (n: Nat, xs: Nat list) -> xs.Length = (int n + 1) @>,
            fun expr ->
                Checkify.ForAllGenShrink (expr, generator,
                    fun (n, xs) -> [ if n = 0N then 0N, [] else n - 1N, xs.Tail ]
                )
        )


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%