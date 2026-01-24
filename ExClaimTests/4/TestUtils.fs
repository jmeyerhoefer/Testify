module Calculus.TestUtils


open Calculus.Calculus


//=============================================================================================================================================================================
// PARSER
//=============================================================================================================================================================================


(*
    - TestUtils: Parser, Simplifier, Normalizer
    - TestsUtilsTests: Tests für Parser, etc.
    - Tests.fs: Tests für Calculus
*)


/// <summary>Convert a <c>Function</c> to a <c>IFunction</c>.</summary>
let rec toIFunction (f: Function): IFunction =
    match f with
    | Const c -> constant c
    | Id -> id ()
    | Add (g, h) -> add (toIFunction g, toIFunction h)
    | Mul (g, h) -> mul (toIFunction g, toIFunction h)
    | Pow (g, n) -> pow (toIFunction g, n)
    | Comp (g, h) -> comp (toIFunction g, toIFunction h)


/// <summary>Apply a given value to the function.</summary>
let rec apply (f: Function) (x: Nat): Nat =
    match f with
    | Const c -> c
    | Id -> x
    | Add (g, h) -> (apply g x) + (apply h x)
    | Mul (g, h) -> (apply g x) * (apply h x)
    | Pow (g, n) -> Nat.Pow (apply g x, n)
    | Comp (g, h) -> apply g (apply h x)


/// <summary>Derive the function.</summary>
let rec derive (f: Function): Function =
    match f with
    | Const _ -> Const 0N
    | Id -> Const 1N
    | Add (g, h) -> Add (derive g, derive h)
    | Mul (g, h) -> Add (Mul (derive g, h), Mul (g, derive h))
    | Pow (g, n) -> Mul (Mul (Const n, Pow (g, n - 1N)), derive g)
    | Comp (g, h) -> Mul (Comp (derive g, h), derive h)


let toString (f: Function): string =
    match f with
    | Const n -> show n
    | Id -> "x"
    | Add (g, h) -> $"({toString g} + {toString h})"
    | Mul (g, h) -> $"({toString g} * {toString h})"
    | Pow (g, n) -> $"%s{toString g} ^ %s{show n}"
    | Comp (g, h) -> $"(%s{toString g} o %s{toString h})"


/// <summary>Parser for equations. It is forgiving regarding parentheses.</summary>
module Parser =
    /// <summary>The parseable tokens.</summary>
    type Token =
        | LParen        // '('
        | RParen        // ')'
        | Plus          // '+'
        | Times         // '*'
        | Caret         // '^'
        | CompOp        // 'o'
        | X             // 'x'
        | N of Nat      // [0-9]
        | EOS           // end of stream


        override this.ToString (): string =
            match this with
            | LParen -> "("
            | RParen -> ")"
            | Plus -> "+"
            | Times -> "*"
            | Caret -> "^"
            | CompOp -> "o"
            | X -> "x"
            | N nat -> show nat
            | EOS -> String.Empty


    /// <summary>FIFO</summary>
    type TokenStream =
        { mutable Tokens: Token list }


        /// <summary>Peek at the first token of the <c>TokenStream</c>.</summary>
        member this.Peek (): Token =
            match this.Tokens with
            | [] -> EOS
            | t :: _ -> t


        /// <summary>Pop the first token of the <c>TokenStream</c>.</summary>
        member this.Pop (): Token * TokenStream =
            match this.Tokens with
            | [] -> EOS, this
            | t :: rest -> t, { Tokens = rest }


        /// <summary>Tries to pop an expected <c>Token</c>.</summary>
        member this.Expect (token: Token): TokenStream =
            match this.Pop () with
            | t, rest when t = token -> rest
            | _, t -> failwith $"Expected {token} but got {t}"


    /// <summary>Convert a <c>string</c> to a <c>TokenStream</c>.</summary>
    let tokenize (s: string): TokenStream =
        let n: int = s.Length
        let rec loop (i: int) (acc: Token list): Token list =
            if i >= n then
                List.rev (EOS :: acc)
            else
                let c: char = s[i]
                if Char.IsWhiteSpace c then
                    loop (i + 1) acc
                else
                    match c with
                    | '(' -> loop (i + 1) (LParen :: acc)
                    | ')' -> loop (i + 1) (RParen :: acc)
                    | '+' -> loop (i + 1) (Plus :: acc)
                    | '^' -> loop (i + 1) (Caret :: acc)
                    | '*' -> loop (i + 1) (Times :: acc)
                    | 'o' -> loop (i + 1) (CompOp :: acc)
                    | 'x' -> loop (i + 1) (X :: acc)
                    | _ when Char.IsDigit c ->
                        let mutable j: int = i
                        let mutable value: int = 0
                        while j < n && Char.IsDigit s[j] do
                            value <- value * 10 + int (s[j] - '0')
                            j <- j + 1
                        loop j (N (Nat.Make value) :: acc)
                    | _ -> failwith $"Unexpected character '%c{c}' at position %d{i}"
        { Tokens = loop 0 [] }


    type ParserResult =
        | ParserSuccess of Function
        | ParserFailure of Token

        override this.ToString (): string =
            match this with
            | ParserSuccess (f: Function) -> f.ToString ()
            | ParserFailure (t: Token) -> t.ToString ()

    /// <summary>Parses a given <c>string</c> and returns the according <c>Function</c> if possible.</summary>
    let parse (s: string): Function =
        /// <summary>Parse a composition-term.</summary>
        let rec parseComp (ts: TokenStream): TokenStream * Function =
            let (ts: TokenStream), (left: Function) = parseAdd ts
            let rec loop (ts: TokenStream) (left: Function): TokenStream * Function =
                match ts.Peek () with
                | CompOp -> let (ts: TokenStream), (right: Function) = parseAdd (ts.Expect CompOp) in loop ts (Comp (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse an addition-term.</summary>
        and parseAdd (ts: TokenStream): TokenStream * Function=
            let (ts: TokenStream), (left: Function) = parseMul ts
            let rec loop (ts: TokenStream) (left: Function): TokenStream * Function =
                match ts.Peek () with
                | Plus -> let (ts: TokenStream), (right: Function) = parseMul (ts.Expect Plus) in loop ts (Add (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse a multiplication-term.</summary>
        and parseMul (ts: TokenStream): TokenStream * Function =
            let (ts: TokenStream), (left: Function) = parsePow ts
            let rec loop (ts: TokenStream) (left: Function): TokenStream * Function =
                match ts.Peek () with
                | Times -> let (ts: TokenStream), (right: Function) = parsePow (ts.Expect Times) in loop ts (Mul (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse an exponential-term.</summary>
        and parsePow (ts: TokenStream): TokenStream * Function =
            let (ts: TokenStream), (baseExpr: Function) = parseAtom ts
            match ts.Peek () with
            | Caret ->
                let ts: TokenStream = ts.Expect Caret
                let (ts: TokenStream), (n: Nat) = parseNatAtom ts
                ts, Pow (baseExpr, n)
            | _ -> ts, baseExpr

        /// <summary>Parse an atomic-term.</summary>
        and parseAtom (ts: TokenStream): TokenStream * Function =
            match ts.Peek () with
            | N n -> let _, (ts: TokenStream) = ts.Pop () in ts, Const n
            | X -> let _, (ts: TokenStream) = ts.Pop () in ts, Id
            | LParen ->
                let (ts: TokenStream), (e: Function) = parseComp (ts.Expect LParen)
                let ts: TokenStream = ts.Expect RParen
                ts, e
            | t -> failwith $"Unexpected token {t}"

        /// <summary>Parse a Nat-term.</summary>
        and parseNatAtom (ts: TokenStream): TokenStream * Nat =
            match ts.Peek () with
            | N n -> let _, (ts: TokenStream) = ts.Pop () in ts, n
            | LParen ->
                let ts: TokenStream = ts.Expect LParen
                match ts.Peek () with
                | N n -> let _, (ts: TokenStream) = ts.Pop () in ts.Expect RParen, n
                | _ -> failwith "Exponent must be integer"
            | _ -> failwith "Exponent must be integer"

        let (ts: TokenStream), (expr: Function) = s |> (tokenize >> parseComp)
        match ts.Peek () with
        | EOS -> expr
        | t -> failwith $"%s{t.ToString ()}"


/// <summary>Simplifier for <c>Function</c>.</summary>
module Simplifier =
    /// <summary>Simplifies a given <c>Function</c> as much as possible.</summary>
    let rec simplify (f: Function): Function =
        match f with
        | Const c -> Const c
        | Id -> Id
        | Add (g, h) ->
            match simplify g, simplify h with
            | Const c, other | other, Const c when c = 0N -> other
            | Const a, Const b -> Const (a + b)
            | g', h' -> Add (g', h')
        | Mul (g, h) ->
            match simplify g, simplify h with
            | Const c, _ | _, Const c when c = 0N -> Const 0N
            | Const c, other | other, Const c when c = 1N -> other
            | Const a, Const b -> Const (a * b)
            | Pow (g', a), Pow (h', b) when g' = h' -> Pow (g', a + b)
            | g', h' -> Mul (g', h')
        | Pow (g, n) ->
            match simplify g, n with
            | _, n when n = 0N -> Const 1N
            | g', n when n = 1N -> g'
            | Const n, c when n = 0N && c > 0N -> Const 0N
            | Const n, _ when n = 1N -> Const 1N
            | Const c, _ -> Const (Nat.Pow (c, n))
            | g', _ -> Pow (g', n)
        | Comp (g, h) ->
            match simplify g, simplify h with
            | Const c, _ -> Const c
            | Id, h' -> h'
            | g', Const c -> Const (apply g' c)
            | g', h' -> Comp (g', h')


/// <summary>Normalizer for <c>Function</c>. Allows commutativity.</summary>
module Normalizer =
    open Simplifier


    /// <summary>Normalizes the order of a given <c>Function</c>.</summary>
    let normalize (f: Function): Function =
        /// <summary>Order of operation.</summary>
        let rec exprKey (e: Function): Nat * string =
            match e with
            | Const c -> 0N, string c
            | Id -> 1N, "x"
            | Pow (g, n)  -> let _, k = exprKey g in 2N, k + "^" + string n
            | Mul (a, b)  -> let (_, ka), (_, kb) = exprKey a, exprKey b in 3N, ka + "*" + kb
            | Add (a, b)  -> let (_, ka), (_, kb) = exprKey a, exprKey b in 4N, ka + "+" + kb
            | Comp (a, b) -> let (_, ka), (_, kb) = exprKey a, exprKey b in 5N, ka + "o" + kb

        /// <summary>Collect all </summary>
        let rec collectAdd: Function -> Function list = function | Add (g, h) -> collectAdd g @ collectAdd h | e -> [ e ]
        let rec collectMul: Function -> Function list = function | Mul (g, h) -> collectMul g @ collectMul h | e -> [ e ]
        let reduceAdd (xs: Function list): Function = xs |> List.reduce (fun a b -> Add(a, b))
        let reduceMul (xs: Function list): Function = xs |> List.reduce (fun a b -> Mul(a, b))

        /// <summary>Root for recursion.</summary>
        let rec normalizeExpr (f: Function): Function =
            match f with
            | Add _ as a -> normalizeAdd a
            | Mul _ as m -> normalizeMul m
            | Pow (g, n) -> Pow (normalizeExpr g, n)
            | Comp (g, h) -> Comp (normalizeExpr g, normalizeExpr h)
            | _ -> f

        /// <summary>Normalize an addition-term.</summary>
        and normalizeAdd (f: Function): Function =
            match f with
            | Add _ ->
                let terms: Function list =
                    collectAdd f
                    |> List.map (simplify >> normalizeExpr)
                    |> List.filter ((<>) (Const 0N))

                let constSum: Nat =
                    terms
                    |> List.choose (function Const c -> Some c | _ -> None)
                    |> List.sum

                let others: Function list =
                    terms
                    |> List.filter (function Const _ -> false | _ -> true)
                    |> List.sortBy exprKey

                match constSum, others with
                | n, [] when n = 0N -> Const 0N
                | c, [] -> Const c
                | n, xs when n = 0N -> reduceAdd xs
                | c, xs -> reduceAdd (xs @ [Const c])
            | _ -> failwith $"Unexpected Expression in normalizeAdd: {f}"

        /// <summary>Normalize a multiplication-term.</summary>
        and normalizeMul (f: Function): Function =
            match f with
            | Mul _ ->
                let factors: Function list = collectMul f |> List.map (simplify >> normalizeExpr)

                if factors |> List.exists ((=) (Const 0N)) then
                    Const 0N
                else
                    let constProd: Nat =
                        factors
                        |> List.choose (function Const c -> Some c | _ -> None)
                        |> List.fold (*) 1N

                    let others: Function list =
                        factors
                        |> List.filter (function Const _ -> false | _ -> true)
                        |> List.sortBy exprKey

                    match constProd, others with
                    | n, [] when n = 1N -> Const 1N
                    | c, [] -> Const c
                    | n, xs when n = 1N -> reduceMul xs
                    | c, xs -> reduceMul (Const c :: xs)
            | _ -> failwith $"Unexpected Expression in normalizeAdd: {f}"

        normalizeExpr f