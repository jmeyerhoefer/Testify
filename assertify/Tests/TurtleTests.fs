module Tests.TurtleTests


open Assertify.Types
open Assertify.Assertify.Operators
open Types.TurtleTypes


let inline (%!) a b = (a % b + b) % b


let rec preprocess (p: Program): Program =
    match p with
    | [] -> []
    | D::ps ->
        match preprocess ps with
        | [] -> [D]
        | D::qs -> D::qs
        | qs -> D::qs
    | (F len)::ps ->
        match preprocess ps with
        | [] -> [F len]
        | (F len2)::qs -> (F (len+len2))::qs
        | qs -> (F len)::qs
    | (L ang)::ps ->
        let ang' = ang %! 360.0
        match preprocess ps with
        | [] -> [L ang']
        | (L ang2)::qs -> (L ((ang + ang2) %! 360.0))::qs
        | qs -> (L ang')::qs


[<TestClass>]
type TurtleTests () =
    // a)

    [<TestMethod; Timeout 1000>]
    member this.``a) Smarter Konstruktor`` (): unit =
        (?) <@ preprocess [Student.Turtle.right 10.0] = [L 350.0] @>

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout 1000>]
    member _.``b) substF`` (): unit =
        let trafo = fun (len: Double) -> [F len; L 45.0; F len]
        (?) <@ preprocess (Student.Turtle.substF trafo []) = [] @>
        (?) <@ preprocess (Student.Turtle.substF trafo [D]) = [D] @>
        (?) <@ preprocess (Student.Turtle.substF trafo [L 90.0]) = [L 90.0] @>
        (?) <@ preprocess (Student.Turtle.substF trafo [F 1.0]) = [F 1.0; L 45.0; F 1.0] @>
        let ex = [D; L 45.0; F 1.0; L -90.0; F 1.0; L 45.0]
        (?) <@ preprocess (Student.Turtle.substF trafo ex) = [D; L 45.0; F 1.0; L 45.0; F 1.0; L 270.0; F 1.0; L 45.0; F 1.0; L 45.0] @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout 1000>]
    member _.``c) Levy-C-Kurve 1 Iteration`` (): unit =
        let expected = [D; L 45.0; F 1.0; L 270.0; F 1.0; L 45.0]
        (?) <@ preprocess (Student.Turtle.substF Student.Turtle.levyTransform (Student.Turtle.levyStart (sqrt 2.0))) = expected @>

    // ------------------------------------------------------------------------
    // d)

    [<TestMethod; Timeout 1000>]
    member _.``d) Kochflocke Transformation angewendet auf Gerade`` (): unit =
        let expected = [D; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0]
        (?) <@ preprocess (Student.Turtle.substF Student.Turtle.kochflockeTransform [D; F 3.0]) = expected @>

    [<TestMethod; Timeout 1000>]
    member _.``d) Kochflocke 1 Iteration`` (): unit =
        let expected = [D; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0]
        (?) <@ preprocess (Student.Turtle.substF Student.Turtle.kochflockeTransform (Student.Turtle.kochflockeStart 3.0)) = expected @>

    // ------------------------------------------------------------------------
    // e)

    [<TestMethod; Timeout 1000>]
    member _.``e) Penta Plexity Transformation angewendet auf Gerade`` (): unit =
        let phi = (1.0 + sqrt 5.0) / 2.0
        let expected = [D; F 1.0; L 72.0; F 1.0; L 72.0; F 1.0; L 180.0; F 1.0; L 324.0; F 1.0; L 72.0; F 1.0]
        (?) <@ preprocess (Student.Turtle.substF Student.Turtle.pentaplexityTransform [D; F (phi*phi)]) = expected @>