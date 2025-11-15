module Tests.Lists2Tests


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators


// Ugly duplication, should import reference solution as separate module
let rec intersperse<'a> (sep: 'a) (xs: List<'a>): List<'a> =
    match xs with
    | [] -> []
    | one & [_] -> one
    | x :: xs -> x :: sep :: intersperse sep xs

let rec runs<'a when 'a: comparison> (xs: List<'a>): List<List<'a>> =
    match xs with
    | [] -> []
    | x::xs -> match runs xs with
               | rest & ((current & (y :: _)) :: ys) ->
                   if x <= y then (x :: current) :: ys else [x] :: rest
               | rest -> [x] :: rest
// end ugly duplication


let ex = [2N; 4N; 3N; 4N; 2N; 1N]

type ArbMod =
    inherit NatModifier
    static member Func (): Arbitrary<Nat -> List<Nat>> =
        (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
            [
                (fun x -> [ x + 1N; x + 2N ])
                (fun x -> [ x + 1N; x + 2N ])
                (fun x -> [ x * 2N; x * 3N ])
            ]


[<TestClass>]
type Lists2Tests () =
    let config = DefaultConfig.WithArbitrary [typeof<ArbMod>]
    // a)

    [<TestMethod; Timeout 1000>]
    member _.``a) minAndMax Beispiele`` (): unit =
        (?) <@ Student.Lists2.minAndMax [] = None @>
        (?) <@ Student.Lists2.minAndMax [1N;2N;3N;4N] = Some(1N, 4N) @>
        (?) <@ Student.Lists2.minAndMax [4N;3N;2N;1N] = Some(1N, 4N) @>
        (?) <@ Student.Lists2.minAndMax [4N;7N;1N;1N] = Some(1N, 7N) @>
        (?) <@ Student.Lists2.minAndMax ex = Some(1N, 4N) @>

    [<TestMethod; Timeout 5000>]
    member _.``a) minAndMax Zufallstest`` (): unit =
        Checkify.Check
            <@ fun (xs: List<Nat>) ->
                Student.Lists2.minAndMax xs =
                    List.fold (fun acc x ->
                        match acc with
                        | None -> Some (x, x)
                        | Some (low, high) -> Some (min x low, max x high)
                    ) None xs @>

    [<TestMethod; Timeout 5000>]
    member _.``a) minAndMax Zufallstest2`` (): unit =
        let solution (xs: Nat list) =
            xs
            |> List.fold (fun acc x ->
                match acc with
                | None -> Some (x, x)
                | Some (low, high) -> Some (min x low, max x high)
            ) None
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists2.minAndMax xs = solution xs @>

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) map Beispiele`` (): unit =
        (?) <@ Student.Lists2.map (fun x -> x + 1N) [] = [] @>
        (?) <@ Student.Lists2.map (fun x -> x + 1N) [1N;2N;3N] = [2N;3N;4N] @>
        (?) <@ Student.Lists2.map (fun x -> x * 2N) [1N;2N;3N] = [2N;4N;6N] @>
        (?) <@ Student.Lists2.map (fun x -> x * x) [4N;7N;1N;1N] = [16N;49N;1N;1N] @>
        (?) <@ Student.Lists2.map (fun x -> x + 1N) ex = [3N;5N;4N;5N;3N;2N] @>

    [<TestMethod; Timeout 5000>]
    member _.``b) map Zufallstest`` (): unit =
        Checkify.Check <@ fun (p: Nat -> Bool) (xs: List<Nat>) -> Student.Lists2.map p xs = List.map p xs @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) duplicate Beispiele`` (): unit =
        (?) <@ Student.Lists2.duplicate [] = [] @>
        (?) <@ Student.Lists2.duplicate [1N;2N;3N] = [1N;1N;2N;2N;3N;3N] @>
        (?) <@ Student.Lists2.duplicate [4N;7N;1N;1N] = [4N;4N;7N;7N;1N;1N;1N;1N] @>

    [<TestMethod; Timeout 5000>]
    member _.``c) duplicate Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists2.duplicate xs = List.foldBack (fun x acc -> x :: x :: acc) xs [] @>

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``d) collect Beispiele`` (): unit =
        (?) <@ Student.Lists2.collect (fun x -> [x + 1N; x + 2N]) [] = [] @>
        (?) <@ Student.Lists2.collect (fun x -> [x + 1N; x + 2N]) [1N; 2N; 3N] = [2N; 3N; 3N; 4N; 4N; 5N] @>
        (?) <@ Student.Lists2.collect (fun x -> [x * 2N; x * 3N]) [4N; 7N; 1N; 1N] = [8N; 12N; 14N; 21N; 2N; 3N; 2N; 3N] @>

    [<TestMethod; Timeout 5000>]
    member _.``d) collect Zufallstest``() : unit =
        // let fG: Arbitrary<Nat -> List<Nat>> =
        //     (FsCheck.FSharp.Arb.fromGen << FsCheck.FSharp.Gen.elements)
        //         [
        //             (fun x -> [ x + 1N; x + 2N ])
        //             (fun x -> [ x + 1N; x + 2N ])
        //             (fun x -> [ x * 2N; x * 3N ])
        //         ]
        // Checkify.Check <@ fun (xs: List<Nat>) -> FsCheck.FSharp.Prop.forAll fG (fun f -> Student.Lists2.collect f xs = List.collect f xs) @>
        // TODO: Add option of Prop.forAll
        // TODO: Add option to pass optional message like in this case: What is f? Or optimize 'toReadable'
        // let f = fun x -> [ x + 1N; x + 2N ]
        Checkify.Check (
            <@ fun (f: Nat -> Nat list) (xs: List<Nat>) -> Student.Lists2.collect f xs = List.collect f xs @>,
            config
        )

//     [<TestMethod; Timeout 5000>]
//     member _.``d) collect Zufallstest``() : unit =
//         Checkify.Check
//             (  fun (foo : Expr<bool> -> bool) ->
//         (fun (xs: List<Nat>) ->
//             let fG: Arbitrary<Nat -> List<Nat>> =
//                 (Arb.fromGen << Gen.elements)
//                     [ (fun x -> [ x + 1N; x + 2N ])
//                       (fun x -> [ x + 1N; x + 2N ])
//                       (fun x -> [ x * 2N; x * 3N ]) ] in
//                                   Prop.forAll fG (fun f -> foo .eval f g)
//                                   ,
//         <@ fun (f: Nat -> Nat list) (xs: List<Nat>) -> Student.Lists2.collect f xs = List.collect f xs @>
//   )
// )

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``e) intersperse Beispiele`` (): unit =
        (?) <@ Student.Lists2.intersperse 0N [] = [] @>
        (?) <@ Student.Lists2.intersperse 0N [2N] = [2N] @>
        (?) <@ Student.Lists2.intersperse 0N [1N; 2N; 3N; 4N] = [1N; 0N; 2N; 0N; 3N; 0N; 4N] @>

    [<TestMethod; Timeout 5000>]
    member _.``e) intersperse Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) (x: Nat) -> Student.Lists2.intersperse x xs = intersperse x xs @>

    // ------------------------------------------------------------------------
    // f)

    [<TestMethod; Timeout 1000>]
    member _.``f) runs Beispiele`` (): unit =
        (?) <@ Student.Lists2.runs [] = []  @>
        (?) <@ Student.Lists2.runs [1N; 2N; 3N] = [[1N; 2N; 3N]] @>
        (?) <@ Student.Lists2.runs [3N; 2N; 1N] = [[3N]; [2N]; [1N]] @>
        (?) <@ Student.Lists2.runs [4N; 7N; 1N; 1N] = [[4N; 7N]; [1N; 1N]] @>

    [<TestMethod; Timeout 5000>]
    member _.``f) runs Zufallstest`` (): unit =
        Checkify.Check <@ fun (xs: List<Nat>) -> Student.Lists2.runs xs = runs xs @>