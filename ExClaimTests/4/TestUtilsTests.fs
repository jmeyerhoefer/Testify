module Calculus.TestUtilsTests


open Calculus.TestUtils.Parser
open Calculus.TestUtils.Simplifier
open Calculus.TestUtils.Normalizer
open Microsoft.VisualStudio.TestTools.UnitTesting


//=============================================================================================================================================================================
// ParserTests
//=============================================================================================================================================================================


[<TestClass>]
type Tests () =
    let equation1: string list * Function =
        [
            "(1 + x) * 2"
            "(1 + x) * 2 * 1"
            "(1 + (x)) * 2"
            "((1 + (x + 0)) * 2)"
            "((1 + x)) * (2 * 1 + 0)"
            "((1 + x) * 2)"
            "((x + 1) * 2)"
            "(2 * (1 + x))"
            "(2 * (x + 1))"
            "(2 * (x + (1)))"
        ],
        Mul (Add (Const 1N, Id), Const 2N)


    let equation2: string list * Function =
        [
            "(x ^ 2) o ((x + 1) ^ 3)"
            "((x ^ 2) o ((x + 1 + 0) ^ 3))"
            "((x) ^ 2) o (((x + 1)) ^ 3)"
            "((x + 0) ^ 2) o (((x + 0 + 1)) ^ 3)"
            "(x ^ 2) o ((x + 1) ^ 3)"
            "(x ^ 2) o ((1 + x) ^ 3)"
            "(x ^ 2) o ((1 + (x)) ^ 3)"
        ],
        Comp (Pow (Id, 2N), Pow (Add (Id, Const 1N), 3N))


    let equation3: string list * Function =
        [
            "((x * (x + 2)) ^ 2) o ((x + 1) ^ 3)"
            "((x * (2 + x)) ^ 2) o ((x + 1) ^ 3)"
            "((x * (x + 2)) ^ 2) o ((1 + x) ^ 3)"
            "((x * (2 + x)) ^ 2) o ((1 + x) ^ 3)"
            "(((2 + x) * x) ^ 2) o ((1 + x + 0) ^ 3)"
            "(((2 + x) * x) ^ 2) o ((x + 1) ^ 3)"
            "((((2 + x)) * x) ^ 2) o (((x) + 1 * 1) ^ 3)"
        ],
        Comp (
            Pow (Mul (Id, Add (Id, Const 2N)), 2N),
            Pow (Add (Id, Const 1N), 3N)
        )

    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 1 - all variants parse equal`` () : unit =
        // let (strVariants: string list), _ = equation1
        // let results: ParserResult list = strVariants |> List.map parse
        // Assert.IsTrue (results |> List.forall (fun r -> match r with | ParserSuccess _ -> true | _ -> false))
        // // Assert.IsTrue (results |> List.forall _.IsParserSuccess)
        // let results: Function list = results |> List.map (fun r -> match r with | ParserSuccess f -> f | _ -> failwith "PANIC")
        // Assert.IsLessThanOrEqualTo (1, results |> List.map (simplify >> normalize) |> Set.ofList |> Set.count)

    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 1 - all variants equal expected AST`` () : unit =
    //     let (strVariants: string list), (f: Function) = equation2
    //     Assert.IsTrue (
    //         strVariants
    //         |> List.map parse
    //         |> List.forall (fun (result: ParserResult) ->
    //             match result with
    //             | ParserSuccess (g: Function) -> g = f
    //             | _ -> false
    //         )
    //     )

    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 2 - all variants parse equal`` () : unit =
    //     Assert.IsLessThanOrEqualTo (1, equation2.StringVariants |> (List.map (parse >> simplify >> normalize) >> Set.ofList >> Set.count))
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 2 - all variants equal expected AST`` () : unit =
    //     Assert.IsTrue (
    //         equation2.StringVariants
    //         |> List.map (parse >> simplify >> normalize)
    //         |> List.forall ((=) equation2.Function)
    //     )
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 3 - all variants parse equal`` () : unit =
    //     Assert.IsLessThanOrEqualTo (1, equation3.StringVariants |> (List.map (parse >> simplify >> normalize) >> Set.ofList >> Set.count))
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Equation 3 - all variants equal expected AST`` () : unit =
    //     Assert.IsTrue (
    //         equation3.StringVariants
    //         |> List.map (parse >> simplify >> normalize)
    //         |> List.forall ((=) equation3.Function)
    //     )
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Precedence: * binds tighter than +`` (): unit =
    //     let ast: Function = parse "1 + 2 * 3"
    //     Assert.AreEqual<Function> (Add (Const 1N, Mul (Const 2N, Const 3N)), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Precedence: ^ binds tighter than *`` () =
    //     let ast: Function = parse "2 * x ^ 3"
    //     Assert.AreEqual<Function> (Mul (Const 2N, Pow (Id, 3N)), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Composition lowest precedence`` (): unit =
    //     // x + 1 o x + 2  should be (x+1) o (x+2) if 'o' has lowest precedence
    //     let ast: Function = parse "x + 1 o x + 2"
    //     Assert.AreEqual<Function> (Comp (Add (Id, Const 1N), Add (Id, Const 2N)), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Associativity: + is left-associative`` (): unit =
    //     let ast: Function = parse "1 + 2 + 3"
    //     Assert.AreEqual<Function> (Add (Add (Const 1N, Const 2N), Const 3N), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Associativity: * is left-associative`` () =
    //     let ast: Function = parse "2 * 3 * 4"
    //     Assert.AreEqual<Function> (Mul (Mul (Const 2N, Const 3N), Const 4N), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Associativity: o is left-associative`` () =
    //     let ast: Function = parse "x o x o x"
    //     Assert.AreEqual<Function> (Comp (Comp (Id, Id), Id), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Parentheses in Exponent are allowed`` (): unit =
    //     let ast: Function = parse "x ^ (1)"
    //     Assert.AreEqual<Function> (Pow (Id, 1N), ast)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Commutativity: Addition`` (): unit =
    //     let ast1: Function = parse "x + 1"
    //     let ast2: Function = parse "1 + x"
    //     Assert.AreEqual<Function> (ast1 |> normalize, ast2 |> normalize)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``Commutativity: Multiplication`` (): unit =
    //     let ast1: Function = parse "x * 1"
    //     let ast2: Function = parse "1 * x"
    //     Assert.AreEqual<Function> (ast1 |> normalize, ast2 |> normalize)
    //
    // [<TestMethod; Timeout 1000>]
    // member _.``No Commutativity: Composition`` (): unit =
    //     let ast1: Function = parse "x o (x + 1)"
    //     let ast2: Function = parse "(x + 1) o x"
    //     Assert.AreNotEqual<Function> (ast1 |> normalize, ast2 |> normalize)