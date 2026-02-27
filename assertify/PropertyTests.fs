//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// PROPERTYTESTS %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.PropertyTests


open Assertify.Checkify
open Assertify.Expressions.Operators
open Assertify.Checkify.Operators
open Assertify.Types
open Assertify.Types.GenBuilder


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


module Student2 =
    module Lists2 =
        let collect (f: 'a -> 'a list) (xs: 'a list): 'a list =
            List.collect f (xs @ xs)

        let map (f: 'a -> 'b) (xs: 'a list): 'b list =
            List.map f (xs @ xs)


[<TestClass>]
type PropertyTests () =
    [<TestMethod; Timeout 5000>]
    member _.``test 001`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student2.Lists2.collect f xs = List.collect f xs @>,
            (fun expr (xs: Nat list) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun f -> expr @@ [ f; xs ])
            )
        )


    [<TestMethod; Timeout 5000>]
    member _.``test 002`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student2.Lists2.collect f xs = List.collect f xs @>,
            (fun expr (xs: Nat list) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ [ f; xs ])
            )
        )


    [<TestMethod; Timeout 5000>]
    member _.``test 003`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (n: Nat) (xs: Nat list) -> Student2.Lists2.collect f xs = [ 0N .. n ] @>,
            (fun expr (n: Nat) (xs: Nat list) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ [ f; xs; n ])
            )
        )


    [<TestMethod; Timeout 5000>]
    member _.``test 004`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (n: Nat) (xs: Nat list) (m: int) -> Student2.Lists2.collect f xs = [ 0N .. (n + Nat.Make m) ] @>,
            (fun expr (n: Nat) (xs: Nat list) (m: int) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ [ f; n; xs; m ])
            )
        )


    [<TestMethod; Timeout 5000>]
    member _.``test 005`` (): unit =
        Checkify.CheckTest (
            <@ fun (f: Nat -> Nat list) (n: Nat) (xs: Nat list) (m: int) (s: string) -> Student2.Lists2.collect f xs = [ 0N .. (n + Nat.Make m + try Nat.Make s with _ -> 0N) ] @>,
            (fun expr (n: Nat) (xs: Nat list) (m: int) (s: string) ->
                let fG: Arbitrary<Nat -> List<Nat>> =
                    (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                        [ (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x + 1N; x + 2N ])
                          (fun x -> [ x * 2N; x * 3N ]) ] in
                FsCheck.FSharp.Prop.forAll fG (fun (f: Nat -> Nat list) -> expr @@ [ f; n; xs; m; s ])
            )
        )


    [<TestMethod; Timeout 5000>]
    member _.``CheckBoolean`` (): unit =
        Checkify.CheckBoolean <@ fun (f: Nat -> bool, xs: Nat array) ->
            [
                Array.toList (Student.ArrayMap.map f xs) = Array.toList (Array.map f xs)
                Array.toList xs = Array.toList (Array.copy xs)
            ]
        @>


    [<TestMethod; Timeout 5000>]
    member _.``test with CheckTest via (|?>) operator`` (): unit =
        let fG: Arbitrary<Nat -> List<Nat>> =
            (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
                [
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x + 1N; x + 2N ]
                    fun x -> [ x * 2N; x * 3N ]
                ]
        <@ fun (f: Nat -> Nat list) (xs: Nat list) -> Student2.Lists2.collect f xs = List.collect f xs @>
        |?> fun expr (xs: Nat list) -> FsCheck.FSharp.Prop.forAll fG (fun f -> expr @@ [ f; xs ])


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