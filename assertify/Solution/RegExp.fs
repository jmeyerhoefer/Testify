module Solution.RegExp


open Types.RegExpTypes


let accept (input: List<Alphabet>): Bool =
    let rec accept0 (input: List<Alphabet>): Bool = // B*A
        match input with
        | [] -> false
        | A::rest -> accept1 rest
        | B::rest -> accept0 rest
    and accept1 (input: List<Alphabet>): Bool = // $\epsilon$
        match input with
        | [] -> true
        | A::rest -> accept2 rest
        | B::rest -> accept2 rest
    and accept2 (input: List<Alphabet>): Bool = // $\emptyset$
        match input with
        | [] -> false
        | A::rest -> accept2 rest
        | B::rest -> accept2 rest
    accept0 input
