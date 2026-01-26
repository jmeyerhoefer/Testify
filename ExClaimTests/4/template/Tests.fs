module Calculus.Tests


open System
open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators
open Assertify.Checkify.Operators


open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck


open Calculus.Types
open Calculus.Calculus
open Calculus.TestUtils
open Calculus.TestUtils.Parser
open Calculus.TestUtils.Simplifier


//=============================================================================================================================================================================
// CalculusTests
//=============================================================================================================================================================================


[<TestClass>]
type Tests () =
    let exampleFunctions: (Function * IFunction) list =
        [
            Const 5N,                                                               constant 5N
            Id,                                                                     id ()
            Add (Id, Const 1N),                                                     add (id (), constant 1N)
            Mul (Const 2N, Id),                                                     mul (constant 2N, id ())
            Pow (Id, 3N),                                                           pow (id (), 3N)

            Mul (Add (Id, Const 1N), Const 2N),                                     mul (add (id (), constant 1N), constant 2N)
            Add (Pow (Id, 2N), Const 1N),                                           add (pow (id (), 2N), constant 1N)
            Add (Pow (Id, 2N), Mul (Const 2N, Id)),                                 add (pow (id (), 2N), mul (constant 2N, id ()))
            Add (Add (Id, Const 1N), Add (Id, Const 2N)),                           add (add (id (), constant 1N), add (id (), constant 2N))
            Mul (Mul (Id, Const 2N), Add (Id, Const 3N)),                           mul (mul (id (), constant 2N), add (id (), constant 3N))

            Pow (Add (Id, Const 1N), 2N),                                           pow (add (id (), constant 1N), 2N)
            Pow (Mul (Const 2N, Id), 3N),                                           pow (mul (constant 2N, id ()), 3N)
            Add (Pow (Id, 2N), Pow (Id, 3N)),                                       add (pow (id (), 2N), pow (id (), 3N))
            Mul (Pow (Id, 2N), Add (Id, Const 1N)),                                 mul (pow (id (), 2N), add (id (), constant 1N))
            Mul (Pow (Add (Id, Const 1N), 2N), Const 3N),                           mul (pow (add (id (), constant 1N), 2N), constant 3N)

            Comp (Add (Id, Const 1N), Id),                                          comp (add (id (), constant 1N), id ())
            Comp (Pow (Id, 2N), Add (Id, Const 1N)),                                comp (pow (id (), 2N), add (id (), constant 1N))
            Comp (Mul (Const 2N, Id), Add (Id, Const 3N)),                          comp (mul (constant 2N, id ()), add (id (), constant 3N))
            Comp (Add (Id, Const 1N), Comp (Pow (Id, 2N), Add (Id, Const 2N))),     comp (add (id (), constant 1N), comp (pow (id (), 2N), add (id (), constant 2N)))
            Comp (Pow (Id, 2N), Const 3N),                                          comp (pow (id (), 2N), constant 3N)
        ]


    let exampleValues: Nat list =
        let rnd: Random = Random 42069 // produce the same random values every time
        [ for _ in 0 .. 20 -> rnd.Next 100 |> Nat.Make ]


    //================================================================================================================================================
    // FUNCTIONAL
    //================================================================================================================================================
    // Functional: toString Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: toString Beispiele`` (): unit =
        for f, _  in exampleFunctions do
            Assert.AreEqual<Function> (
                parse (toString f), // expected
                f                   // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: toString Beispiele`` (): unit =
        for f, _  in exampleFunctions do
            try (?) <@ parse (Calculus.toString f) = f @> with  // test     `actualAST = expectedAST`
            | _ -> (?) <@ Calculus.toString f = toString f @>   // fallback `actualToString = expectedToString`


    //----------------------------------------------------------------------------------------------
    // Functional: toString Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: toString Zufallstest`` (): unit =
        Check.One (defaultConfig, fun (f: Function) ->
            Assert.AreEqual<Function> (
                parse (toString f),         // expected
                parse (Calculus.toString f) // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: toString Zufallstest`` (): unit =
        try (!?) <@ fun (f: Function) -> parse (Calculus.toString f) = f @> with  // test     `actualAST = expectedAST`
        | _ -> (!?) <@ fun (f: Function) -> Calculus.toString f = toString f @>   // fallback `actualToString = expectedToString`


    //----------------------------------------------------------------------------------------------
    // Functional: apply Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: apply Beispiele`` (): unit =
        for (f, _), x in List.allPairs exampleFunctions exampleValues do
            Assert.AreEqual<Nat> (
                apply f x,          // expected
                Calculus.apply f x  // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: apply Beispiele`` (): unit =
        for (f, _), x in List.allPairs exampleFunctions exampleValues do
            (?) <@ Calculus.apply f x = apply f x @>


    //----------------------------------------------------------------------------------------------
    // Functional: apply Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: apply Zufallstest`` (): unit =
        Check.One (defaultConfig, fun (f: Function) (x: Nat) ->
            Assert.AreEqual<Nat> (
                apply f x,          // expected
                Calculus.apply f x  // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: apply Zufallstest`` (): unit =
        (!?) <@ fun (f: Function) (x: Nat) -> Calculus.apply f x = apply f x @>


    //----------------------------------------------------------------------------------------------
    // Functional: derive Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: derive Beispiele`` (): unit =
        for f, _ in exampleFunctions do
            Assert.AreEqual<Function> (
                simplify (derive f),            // expected
                simplify (Calculus.derive f)    // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: derive Beispiele`` (): unit =
        for f, _ in exampleFunctions do
            (?) <@ simplify (Calculus.derive f) = simplify (derive f) @>


    //----------------------------------------------------------------------------------------------
    // Functional: derive Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Functional: derive Zufallstest`` (): unit =
        Check.One (defaultConfig, fun (f: Function) ->
            Assert.AreEqual<Function> (
                simplify (derive f),            // expected
                simplify (Calculus.derive f)    // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Functional: derive Zufallstest`` (): unit =
        (!?) <@ fun (f: Function) -> simplify (Calculus.derive f) = simplify (derive f) @>


    //================================================================================================================================================
    // OBJECT ORIENTED
    //================================================================================================================================================
    // Object Oriented: toString Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: toString Beispiele`` (): unit =
        for f, f' in exampleFunctions do
            Assert.AreEqual<Function> (
                parse (toString f),     // expected
                parse (f'.ToString ())  // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: toString Beispiele`` (): unit =
        for f, f'  in exampleFunctions do
            try (?) <@ parse (f'.ToString ()) = f @> with   // test     `actualAST = expectedAST`
            | _ -> (?) <@ f'.ToString () = toString f @>    // fallback `actualToString = expectedToString`


    //----------------------------------------------------------------------------------------------
    // Object Oriented: toString Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: toString Zufallstest`` (): unit =
        Check.One (defaultConfig, fun (f: Function) ->
            Assert.AreEqual<Function> (
                f,                                  // expected
                parse ((toIFunction f).ToString ()) // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: toString Zufallstest`` (): unit =
        try (!?) <@ fun (f: Function) -> parse ((toIFunction f).ToString ()) = f @> with  // test     `actualAST = expectedAST`
        | _ -> (!?) <@ fun (f: Function) -> (toIFunction f).ToString () = toString f @>   // fallback `actualToString = expectedToString`


    //----------------------------------------------------------------------------------------------
    // Object Oriented: apply Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: apply Beispiele`` (): unit =
        for (f, f'), x in List.allPairs exampleFunctions exampleValues do
            Assert.AreEqual<Nat> (
                apply f x, // expected
                f'.Apply x // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: apply Beispiele`` (): unit =
        for (f, f'), x in List.allPairs exampleFunctions exampleValues do
            (?) <@ apply f x = f'.Apply x @>


    //----------------------------------------------------------------------------------------------
    // Object Oriented: apply Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: apply Zufallstest`` (): unit =
        Check.One (defaultConfig, fun (f: Function) (x: Nat) ->
            Assert.AreEqual<Nat> (
                apply f x,              // expected
                (toIFunction f).Apply x // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: apply Zufallstest`` (): unit =
        (!?) <@ fun (f: Function) (x: Nat) -> (toIFunction f).Apply x = apply f x @>


    //----------------------------------------------------------------------------------------------
    // Object Oriented: derive Beispiele
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: derive Beispiele`` (): unit =
        for (f, f'), x in List.allPairs exampleFunctions exampleValues do
            Assert.AreEqual<Nat> (
                apply (derive f) x, // expected
                f'.Derive().Apply x // actual
            )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: derive Beispiele`` (): unit =
        for (f, f'), x in List.allPairs exampleFunctions exampleValues do
            (?) <@ f'.Derive().Apply x = apply (derive f) x @>


    //----------------------------------------------------------------------------------------------
    // Object Oriented: derive Zufallstest
    //----------------------------------------------------------------------------------------------


    // Original Test
    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: derive Zufallstest`` (): unit =
        Check.One (defaultConfig.WithEndSize 100, fun (f: Function) (x: Nat) ->
            Assert.AreEqual<Nat> (
                apply (derive f) x,                 // expected
                (toIFunction f).Derive().Apply x    // actual
            )
        )


    // Assertify Test
    [<TestMethod; Timeout 1000>]
    member _.``#assertify Object Oriented: derive Zufallstest`` (): unit =
        (!?) <@ fun (f: Function) (x: Nat) -> (toIFunction f).Derive().Apply x = apply (derive f) x @>


//=============================================================================================================================================================================
// EOF ========================================================================================================================================================================
//=============================================================================================================================================================================