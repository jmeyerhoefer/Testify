namespace GdP23.S06.A4.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open Mini
    open Types

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

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
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        // ------------------------------------------------------------------------
        // a)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Smarter Konstruktor`` (): unit =
            test <@ preprocess [Turtle.right 10.0] = [L 350.0] @>

        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) substF`` (): unit =
            let trafo = fun (len: Double) -> [F len; L 45.0; F len]
            test <@ preprocess (Turtle.substF trafo []) = [] @>
            test <@ preprocess (Turtle.substF trafo [D]) = [D] @>
            test <@ preprocess (Turtle.substF trafo [L 90.0]) = [L 90.0] @>
            test <@ preprocess (Turtle.substF trafo [F 1.0]) = [F 1.0; L 45.0; F 1.0] @>
            let ex = [D; L 45.0; F 1.0; L -90.0; F 1.0; L 45.0]
            test <@ preprocess (Turtle.substF trafo ex) = [D; L 45.0; F 1.0; L 45.0; F 1.0; L 270.0; F 1.0; L 45.0; F 1.0; L 45.0] @>

        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Levy-C-Kurve 1 Iteration`` (): unit =
            let expected = [D; L 45.0; F 1.0; L 270.0; F 1.0; L 45.0]
            test <@ preprocess (Turtle.substF Turtle.levyTransform (Turtle.levyStart (sqrt 2.0))) = expected @>

        // ------------------------------------------------------------------------
        // d)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) Kochflocke Transformation angewendet auf Gerade`` (): unit =
            let expected = [D; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0]
            test <@ preprocess (Turtle.substF Turtle.kochflockeTransform [D; F 3.0]) = expected @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) Kochflocke 1 Iteration`` (): unit =
            let expected = [D; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0; L 240.0; F 1.0; L 60.0; F 1.0]
            test <@ preprocess (Turtle.substF Turtle.kochflockeTransform (Turtle.kochflockeStart 3.0)) = expected @>

        // ------------------------------------------------------------------------
        // e)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``e) Penta Plexity Transformation angewendet auf Gerade`` (): unit =
            let phi = (1.0 + sqrt 5.0) / 2.0
            let expected = [D; F 1.0; L 72.0; F 1.0; L 72.0; F 1.0; L 180.0; F 1.0; L 324.0; F 1.0; L 72.0; F 1.0]
            test <@ preprocess (Turtle.substF Turtle.pentaplexityTransform [D; F (phi*phi)]) = expected @>
