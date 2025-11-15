//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// PROPERTYTESTS %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.PropertyTests


open Assertify.Checkify
open Assertify.Checkify.Operators
open Assertify.Types
open Assertify.Types.GenBuilder


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


[<TestClass>]
type PropertyTests () =
    [<TestMethod; Timeout 5000>]
    member _.``test`` (): unit =
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
        <@ fun (f, xs) -> Student.Lists2.collect f xs = List.collect f xs @>
        |?> fun expr -> Checkify.ForAllGen (expr, generator)


    [<TestMethod; Timeout 5000>]
    member _.``test'`` (): unit =
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
    member _.``test2`` (): unit =
        let generator =
                gen {
                    let! n = NatModifier.Nat().Generator
                    let! xs = NatModifier.NatListOfLength n
                    return n, xs
                }
        <@ fun (n: Nat, xs: Nat list) -> xs.Length = (int n + 1) @>
        |?> fun expr ->
            Checkify.ForAllGenShrink (expr, generator,
                fun (n, xs) -> [ if n = 0N then 0N, [] else n - 1N, xs.Tail ]
            )


    [<TestMethod; Timeout 5000>]
    member _.``test2'`` (): unit =
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