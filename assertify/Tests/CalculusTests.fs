module Tests.CalculusTests

open Assertify.Types
open Assertify.Assertify.Operators
open Assertify.Checkify
open Types.CalculusTypes


let ex = [
    ( Student.Calculus.constant 1N, Constant 1N, (fun (x: Nat) -> 1N), (fun (x: Nat) -> 0N), "1N")
    ( Student.Calculus.id (), Id, (fun (x: Nat) -> x), (fun (x: Nat) -> 1N), "x")
    ( Student.Calculus.add (Student.Calculus.id (), Student.Calculus.constant 2N), Add (Id, Constant 2N), (fun (x: Nat) -> x + 2N), (fun (x: Nat) -> 1N), "(x + 2N)")
    ( Student.Calculus.mul (Student.Calculus.id (), Student.Calculus.constant 2N), Mul (Id, Constant 2N), (fun (x: Nat) -> x * 2N), (fun (x: Nat) -> 2N), "x * 2N")
    ( Student.Calculus.add (Student.Calculus.mul (Student.Calculus.id (), Student.Calculus.id ()), Student.Calculus.mul (Student.Calculus.constant 2N, Student.Calculus.id ())),
        Add (Mul (Id, Id), Mul (Constant 2N, Id)), (fun (x: Nat) -> x ** 2N + 2N * x), (fun (x: Nat) -> 2N * x + 2N), "(x * x + 2N * x)" )
    (Student.Calculus.mul (Student.Calculus.comp (Student.Calculus.pow (Student.Calculus.id (), 2N), Student.Calculus.add (Student.Calculus.id (), Student.Calculus.constant 3N)), Student.Calculus.add (Student.Calculus.id (), Student.Calculus.constant 4N) ),
        Mul (Comp (Pow (Id, 2N), Add (Id, Constant 3N)), Add (Id, Constant 4N)), (fun (x: Nat) -> (x + 3N) ** 2N *(x + 4N)), (fun (x: Nat) -> 2N * (x + 3N) * (x + 4N) + (x + 3N) ** 2N), "(x ^ 2N o (x + 3N)) * (x + 4N)" )
    ( Student.Calculus.mul (Student.Calculus.pow ((Student.Calculus.mul (Student.Calculus.id (), Student.Calculus.constant 3N)), 2N), Student.Calculus.add (Student.Calculus.id (), Student.Calculus.constant 4N) ),
        Mul (Pow (Mul (Id, Constant 3N), 2N), Add (Id, Constant 4N)), (fun (x: Nat) -> (x * 3N) ** 2N *(x + 4N)), (fun (x: Nat) -> (2N * 3N * x * 3N) * (x + 4N) + (x * 3N) ** 2N), "x * 3N ^ 2N * (x + 4N)" )
  ]

let trim = String.filter (fun x -> not (x = ' ' || x = '(' || x = ')' ))
let charnum = fun chr str -> String.filter (fun x -> x = chr) str |> String.length

let rec toOO (f: Function): IFunction =
    match f with
    | Constant n    -> Student.Calculus.constant n
    | Id            -> Student.Calculus.id ()
    | Add (f1, f2)  -> Student.Calculus.add (toOO f1, toOO f2)
    | Mul (f1, f2)  -> Student.Calculus.mul (toOO f1, toOO f2)
    | Pow (f1, n)   -> Student.Calculus.pow (toOO f1, n)
    | Comp (f1, f2) -> Student.Calculus.comp (toOO f1, toOO f2)

let rec testToString (f: Function): String =
    match f with
    | Constant n    -> show n
    | Id            -> "x"
    | Add (f1, f2)  -> "(" + testToString f1 + " + " + testToString f2 + ")"
    | Mul (f1, f2)  -> "(" + testToString f1 + " * " + testToString f2 + ")"
    | Pow (f1, n)   -> testToString f1 + " ^ " + show n
    | Comp (f1, f2) -> "(" + testToString f1 + " o " + testToString f2 + ")"

let rec testApply (f: Function) (x: Nat): Nat =
    match f with
    | Constant n    -> n
    | Id            -> x
    | Add (f1, f2)  -> testApply f1 x + testApply f2 x
    | Mul (f1, f2)  -> testApply f1 x * testApply f2 x
    | Pow (f1, n)   -> testApply f1 x ** n
    | Comp (f1, f2) -> testApply f1 (testApply f2 x)

let rec testDerive (f: Function): Function =
    match f with
    | Constant n    -> Constant 0N
    | Id            -> Constant 1N
    | Add (f1, f2)  -> Add (testDerive f1, testDerive f2)
    | Mul (f1, f2)  -> Add (Mul (testDerive f1, f2), Mul (f1, testDerive f2))
    | Pow (f1, n)   -> Mul (Mul (Constant n, Pow (f1, n - 1N)), testDerive f1)
    | Comp (f1, f2) -> Mul (Comp (testDerive f1, f2), testDerive f2)

[<TestClass>]
type CalculusTests () =
    // TODO: Test this and replace inner "<@@>; <@@>" with "... && ..."
    [<TestMethod; Timeout 10000>]
    member _.``OO: ToString Beispiele`` (): unit =
        Checkify.Check
            <@ fun (n: Nat) ->
                for func, _, _, _, expected in ex do
                    <@ trim (func.ToString ()) = trim expected @> -?> $"expected %A{expected} but got %A{func.ToString ()} (whitespaces and braces are ignored in _ test)"
                    <@ charnum ')' (func.ToString ()) = charnum '(' (func.ToString ()) @> -?> $"number of opening and closing braces does not match in %A{func.ToString ()}." @>

    [<TestMethod; Timeout 10000>]
    member _.``OO: ToString Zufall`` (): unit =
        Checkify.Check
            <@ fun (fct: Function) (n: Nat) ->
                let fctOO = toOO fct
                <@ trim (testToString fct) = trim (fctOO.ToString ()) @> -?> $"expected %A{testToString fct} but got %A{fctOO.ToString ()} (whitespaces and braces are ignored in _ test)"
                <@ charnum '(' (fctOO.ToString ()) = charnum ')' (fctOO.ToString ()) @> -?> $"number of opening and closing braces does not match in %A{fctOO.ToString ()}." @>

    [<TestMethod; Timeout 10000>]
    member _.``OO: Apply Beispiele`` (): unit =
        Checkify.Check <@ fun (n: Nat) -> for func, _, f, _, _ in ex do (?) <@ func.Apply n = f n @> @>

    [<TestMethod; Timeout 10000>]
    member _.``OO: Apply Zufall`` (): unit =
        Checkify.Check <@ fun (fct: Function) (n: Nat) -> (toOO fct).Apply n = testApply fct n @>

    [<TestMethod; Timeout 10000>]
    member _.``OO: Derive Beispiele`` (): unit =
        Checkify.Check
            <@ fun (n: Nat) ->
                for func, _, _, f', _ in ex do
                    <@ func.Derive().Apply n = f' n @> -?> $"From f(x)=%s{func.ToString ()} you derived f'(x)=%s{func.Derive().ToString ()} which is not correct at %A{n}" @>

    [<TestMethod; Timeout 10000>]
    member _.``OO: Derive Zufall`` (): unit =
        Checkify.Check
            <@ fun (fct: Function) (n: Nat) ->
                <@ (toOO fct).Derive().Apply n = testApply (testDerive fct) n @> -?> $"From f(x)=%s{(toOO fct).ToString ()} you derived f'(x)=%s{(testDerive fct).ToString ()} which is not correct at %A{n}" @>

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: ToString Beispiele`` (): unit =
        Checkify.Check
            <@ fun (n: Nat) ->
                for _, func, _, _, expected in ex do
                    <@ trim (Student.Calculus.toString func) = trim expected @> -?> $"expected %A{expected} but got %A{func.ToString ()} (whitespaces and braces are ignored in _ test)"
                    <@ charnum '(' (Student.Calculus.toString func) = charnum ')' (Student.Calculus.toString func) @> -?> $"number of opening and closing braces does not match in %A{func.ToString ()}."
                @>

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: ToString Zufall`` (): unit =
        Checkify.Check
            <@ fun (fct: Function) (n: Nat) ->
                <@ trim (Student.Calculus.toString fct) = trim (testToString fct) @> -?> $"expected %A{testToString fct} but got %A{Student.Calculus.toString fct} (whitespaces and braces are ignored in _ test)"
                <@ charnum '(' (Student.Calculus.toString fct) = charnum ')' (Student.Calculus.toString fct) @> -?> $"number of opening and closing braces does not match in %A{testToString fct}."
                @>

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: Apply Beispiele`` (): unit =
        Checkify.Check (
            <@ fun (n: Nat) -> for _, func, f, _, _ in ex do (?) <@ Student.Calculus.apply func n = f n @> @> // TODO: Inner (?) <@ ... @> needed ?
        )

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: Apply Zufall`` (): unit =
        Checkify.Check <@ fun (fct: Function) (n: Nat) -> Student.Calculus.apply fct n = testApply fct n @>

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: Derive Beispiele`` (): unit =
        Checkify.Check
            <@ fun (n: Nat) ->
                for _, func, _, f', _ in ex do
                    let expected = f' n
                    let func_deriv = Student.Calculus.derive func
                    let actual = Student.Calculus.apply func_deriv  n
                    <@ actual = expected @> -?> $"From f(x)=%s{Student.Calculus.toString func} you derived f'(x)=%s{Student.Calculus.toString func_deriv} which is not correct at %A{n}" @>

    [<TestMethod; Timeout 10000>]
    member _.``Funktional: Derive Zufall`` (): unit =
        Checkify.Check
            <@ fun (fct: Function) (n: Nat) ->
                let expectedFct = testDerive fct
                let actualFct = Student.Calculus.derive fct
                let expected = testApply expectedFct n
                let actual = Student.Calculus.apply actualFct n
                <@ actual = expected @> -?> $"From f(x)=%s{Student.Calculus.toString fct} you derived f'(x)=%s{Student.Calculus.toString actualFct} which is not correct at %A{n}" @>
