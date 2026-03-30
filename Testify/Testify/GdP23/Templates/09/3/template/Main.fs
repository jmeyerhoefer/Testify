namespace GdP23.S09.A3.Template

module Main =

    open Model
    open Helpers
    open Program

    // Schlüssel in einer Map durchnummerieren
    let assignNumbers<'A, 'B when 'A: comparison> (map: Map<'A, 'B>): Map<'A, int> =
        map |> Map.fold
            (fun (m, size) r _ -> (Map.add r size m, size + 1))
            (Map.empty, 0)
        |> fst


    // Generiere F# Code (als string) für die Akzeptorfunktionen
    let compileToFunctionString<'T when 'T: comparison> (r: Reg<'T>) (automaton: Automaton<'T>) (numbers: Map<Reg<'T>, int>): string =
        let choices = cases<'T>()
        sprintf "type Alphabet = %s\n\nlet accept (input: Alphabet list): bool =\n%s\n    accept%i input\n\n// accept [%s]"
            (choices |> List.map (sprintf "| %A") |> String.concat " ")
            (
                automaton
                |> Map.toList
                |> List.map (
                    fun (r, (transitions, isFinite)) ->
                        sprintf "accept%i (input: Alphabet list): bool = // %s\n        match input with\n        | [] -> %b\n"
                            (Map.find r numbers)
                            (formatRegex r)
                            isFinite
                        +
                        (
                            choices
                            |> List.map (
                                fun x ->
                                    let r' = Map.find x transitions
                                    sprintf "        | %A::rest -> accept%i rest // %s"
                                        x
                                        (Map.find r' numbers)
                                        (formatRegex r')
                            )
                            |> String.concat "\n"
                        )
                )
                |> (
                    fun xs ->
                        sprintf "    let rec %s\n%s"
                            xs.Head
                            (xs.Tail |> List.map (sprintf "    and %s") |> String.concat "\n")
                )
            )
            (Map.find r numbers)
            (String.concat "; " (choices |> List.map (sprintf "%A")))


    // Generiere den Aufrufgraphen für den Akzeptor (als Graphviz Beschreibung)
    let compileToGraphviz<'T when 'T: comparison> (r: Reg<'T>) (automaton: Automaton<'T>) (numbers: Map<Reg<'T>, int>): string =
        let choices = cases<'T>()
        sprintf "digraph G {\n%s\n};\n"
            (
                let xs = automaton |> Map.toList
                (sprintf "    node [shape = box, style = \"\", label = \"Start\"] start;")
                ::
                (
                    xs |> List.map (
                        fun (r, (_, isFinite)) ->
                            sprintf "    node [shape = ellipse, style = %s, label=\"%s\"] %i;"
                                (if isFinite then "filled" else "\"\"")
                                (formatRegex r)
                                (Map.find r numbers)
                    )
                )
                @
                [sprintf "    start -> %i;" (Map.find r numbers)]
                @
                (
                    xs |> List.collect (
                        fun (r, (transitions, _)) ->
                            let i = (Map.find r numbers)
                            choices |> List.map (
                                fun x ->
                                    let r' = Map.find x transitions
                                    sprintf "    %i -> %i [label = \"%A\"];" i (Map.find r' numbers) x
                            )
                    )
                )
                |> String.concat "\n"
        )

    let main args =
        let r = simplify mainRegex
        printfn "Betrachte den Ausdruck %s\n\n" (formatRegex r)
        let automaton = calculateAutomaton r
        let numbers = assignNumbers automaton
        printfn "Aufrufgraph als Graphviz Code, auf http://www.webgraphviz.com/ einfügen\n\n%s" (compileToGraphviz r automaton numbers)
        printfn "\n\nAkzeptor Funktion, im F# Interpreter einfügen:\n\n%s" (compileToFunctionString r automaton numbers)
        0

