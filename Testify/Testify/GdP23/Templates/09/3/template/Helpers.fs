namespace GdP23.S09.A3.Template

module Helpers =

    open Microsoft.FSharp.Reflection
    open Mini
    open Model

    // Funktion die alle Varianten eines Variantentyps als Liste zurückgibt.
    // Beispiel:
    // type Foo = | X | Y
    // cases<Foo>() ergibt [X; Y]
    let cases<'T>() =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Seq.map (fun x -> FSharpValue.MakeUnion(x, [||]) :?> 'T)
        |> List.ofSeq


    // Vereinfache einen gegebenen regulären Ausdruck:
    // Leere Sprache in Sequenz -> Leere Sprache
    // Leeres Wort in Sequenz -> Nur der andere Teil
    // Leere Sprache in Alternative -> Nur der andere Teil
    // Gleiche Alternativen -> Nur einml
    // Zweimal Wiederholung -> Einmal Wiederholung
    let rec simplify<'T when 'T: comparison> (r: Reg<'T>): Reg<'T> =
        match r with
        | Cat (r1, r2) ->
            match (simplify r1, simplify r2) with
            | (Empty, _) | (_, Empty) -> Empty
            | (Eps, r) | (r, Eps) -> r
            | (r1, r2) -> Cat (r1, r2)
        | Alt (r1, r2) ->
            match (simplify r1, simplify r2) with
            | (Empty, r) | (r, Empty) -> r
            | (r, r') when r = r' -> r
            | (r1, r2) -> Alt (r1, r2)
        | Rep r ->
            match simplify r with
            | Empty -> Eps
            | Rep r | r -> Rep r
        | r -> r


    // Formatiere einen regulären Ausdruck
    // Setze dabei nur die nötigen Klammern.
    // Präzedenzregeln: Eps = Sym = Empty > Rep > Cat > Alt
    let formatRegex<'T> (r: Reg<'T>): String =
        let rec f (r: Reg<'T>) (prec: Int): String =
            let (s, prec') =
                match r with
                | Eps -> ("\u03b5", 0)
                | Sym a -> (sprintf "%A" a, 0)
                | Empty -> ("\u2205", 0)
                | Rep r -> (sprintf "%s*" (f r 0), 1)
                | Cat (r1, r2) -> ((f r1 2) + (f r2 2), 2)
                | Alt (r1, r2) -> (sprintf "%s|%s" (f r1 3) (f r2 3), 3)
            if prec' <= prec then s
            else (sprintf "(%s)" s)
        f r 3

