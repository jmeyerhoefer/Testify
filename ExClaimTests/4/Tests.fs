module Calculus.Tests


open Assertify.Types.Configurations
open Assertify.Checkify
open Assertify.Assertify.Operators


open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck


open Calculus.Types
open Calculus.Calculus


/// <summary>Sample <c>Function</c> type.</summary>
type FunctionExpr =
    | ConstExpr of Nat
    | VarExpr
    | AddExpr   of FunctionExpr * FunctionExpr
    | MulExpr   of FunctionExpr * FunctionExpr
    | PowExpr   of FunctionExpr * Nat
    | CompExpr  of FunctionExpr * FunctionExpr

    /// <summary>Convert a <c>Function</c> to a <c>FunctionExpr</c>.</summary>
    static member FromFunction (f: Function): FunctionExpr =
        let rec toFunctionExpr: Function -> FunctionExpr = function
            | Const c -> ConstExpr c
            | Id -> VarExpr
            | Add (g, h) -> AddExpr (toFunctionExpr g, toFunctionExpr h)
            | Mul (g, h) -> MulExpr (toFunctionExpr g, toFunctionExpr h)
            | Pow (g, n) -> PowExpr (toFunctionExpr g, n)
            | Comp (g, h) -> CompExpr (toFunctionExpr g, toFunctionExpr h)
        f |> toFunctionExpr

    /// <summary>Apply a given value to the function.</summary>
    member self.Apply (x: Nat): Nat =
        match self with
        | ConstExpr c -> c
        | VarExpr -> x
        | AddExpr (g, h) -> (g.Apply x) + (h.Apply x)
        | MulExpr (g, h) -> (g.Apply x) * (h.Apply x)
        | PowExpr (g, n) -> Nat.Pow (g.Apply x, n)
        | CompExpr (g, h) -> g.Apply (h.Apply x)

    /// <summary>Derive the function.</summary>
    member self.Derive (): FunctionExpr =
        match self with
        | ConstExpr _ -> ConstExpr 0N
        | VarExpr -> ConstExpr 1N
        | AddExpr (g, h) -> AddExpr (g.Derive (), h.Derive ())
        | MulExpr (g, h) -> AddExpr (MulExpr (g.Derive (), h), MulExpr (g, h.Derive ()))
        | PowExpr (g, n) -> MulExpr (MulExpr (ConstExpr n, PowExpr (g, n - 1N)), g.Derive ())
        | CompExpr (g, h) -> MulExpr (CompExpr (g.Derive (), h), h.Derive ())

    override self.ToString (): string =
        match self with
        | ConstExpr n -> show n
        | VarExpr -> "x"
        | AddExpr (g, h) -> $"({g.ToString ()} + {h.ToString ()})"
        | MulExpr (g, h) -> $"({g.ToString ()} * {h.ToString ()})"
        | PowExpr (g, n) -> $"%s{g.ToString ()} ^ %s{show n}"
        | CompExpr (g, h) -> $"(%s{g.ToString ()} o %s{h.ToString ()})"


/// <summary>Convert a <c>Function</c> to a <c>IFunction</c>.</summary>
let rec toIFunction (f: Function): IFunction =
    match f with
    | Const c -> constant c
    | Id -> id ()
    | Add (g, h) -> add (toIFunction g, toIFunction h)
    | Mul (g, h) -> mul (toIFunction g, toIFunction h)
    | Pow (g, n) -> pow (toIFunction g, n)
    | Comp (g, h) -> comp (toIFunction g, toIFunction h)


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
        | N of Nat    // [0-9]
        | EOS           // end of stream


    /// <summary>FIFO</summary>
    type TokenStream =
        { mutable Tokens: Token list }

        /// <summary>Peek at the first token of the <c>TokenStream</c>.</summary>
        member self.Peek (): Token =
            match self.Tokens with
            | [] -> EOS
            | t :: _ -> t

        /// <summary>Pop the first token of the <c>TokenStream</c>.</summary>
        member self.Pop (): Token * TokenStream =
            match self.Tokens with
            | [] -> EOS, self
            | t :: rest -> t, { Tokens = rest }

        /// <summary>Tries to pop an expected <c>Token</c>.</summary>
        member self.Expect (token: Token): TokenStream =
            match self.Pop () with
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


    /// <summary>Parses a given <c>string</c> and returns the according <c>FunctionExpr</c> if possible.</summary>
    let parse (s: string): FunctionExpr =
        /// <summary>Parse a composition-term.</summary>
        let rec parseComp (ts: TokenStream): TokenStream * FunctionExpr =
            let (ts: TokenStream), (left: FunctionExpr) = parseAdd ts
            let rec loop (ts: TokenStream) (left: FunctionExpr): TokenStream * FunctionExpr =
                match ts.Peek () with
                | CompOp -> let (ts: TokenStream), (right: FunctionExpr) = parseAdd (ts.Expect CompOp) in loop ts (CompExpr (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse an addition-term.</summary>
        and parseAdd (ts: TokenStream): TokenStream * FunctionExpr =
            let (ts: TokenStream), (left: FunctionExpr) = parseMul ts
            let rec loop (ts: TokenStream) (left: FunctionExpr): TokenStream * FunctionExpr =
                match ts.Peek () with
                | Plus -> let (ts: TokenStream), (right: FunctionExpr) = parseMul (ts.Expect Plus) in loop ts (AddExpr (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse a multiplication-term.</summary>
        and parseMul (ts: TokenStream): TokenStream * FunctionExpr =
            let (ts: TokenStream), (left: FunctionExpr) = parsePow ts
            let rec loop (ts: TokenStream) (left: FunctionExpr): TokenStream * FunctionExpr =
                match ts.Peek () with
                | Times -> let (ts: TokenStream), (right: FunctionExpr) = parsePow (ts.Expect Times) in loop ts (MulExpr (left, right))
                | _ -> ts, left
            loop ts left

        /// <summary>Parse an exponential-term.</summary>
        and parsePow (ts: TokenStream): TokenStream * FunctionExpr =
            let (ts: TokenStream), (baseExpr: FunctionExpr) = parseAtom ts
            match ts.Peek () with
            | Caret ->
                let ts: TokenStream = ts.Expect Caret
                let (ts: TokenStream), (n: Nat) = parseNatAtom ts
                ts, PowExpr (baseExpr, n)
            | _ -> ts, baseExpr

        /// <summary>Parse an atomic-term.</summary>
        and parseAtom (ts: TokenStream): TokenStream * FunctionExpr =
            match ts.Peek () with
            | N n -> let _, (ts: TokenStream) = ts.Pop () in ts, ConstExpr n
            | X -> let _, (ts: TokenStream) = ts.Pop () in ts, VarExpr
            | LParen ->
                let (ts: TokenStream), (e: FunctionExpr) = parseComp (ts.Expect LParen)
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

        let (ts: TokenStream), (expr: FunctionExpr) = s |> (tokenize >> parseComp)
        match ts.Peek () with
        | EOS -> expr
        | t -> failwith $"Unexpected trailing token: {t}"


/// <summary>Simplifier for <c>FunctionExpr</c>.</summary>
module Simplifier =
    /// <summary>Simplifies a given <c>FunctionExpr</c> as much as possible.</summary>
    let rec simplify (f: FunctionExpr): FunctionExpr =
        match f with
        | ConstExpr c -> ConstExpr c
        | VarExpr -> VarExpr
        | AddExpr (g, h) ->
            match simplify g, simplify h with
            | ConstExpr c, other | other, ConstExpr c when c = 0N -> other
            | ConstExpr a, ConstExpr b -> ConstExpr (a + b)
            | g', h' -> AddExpr (g', h')
        | MulExpr (g, h) ->
            match simplify g, simplify h with
            | ConstExpr c, _ | _, ConstExpr c when c = 0N -> ConstExpr 0N
            | ConstExpr c, other | other, ConstExpr c when c = 1N -> other
            | ConstExpr a, ConstExpr b -> ConstExpr (a * b)
            | PowExpr (g', a), PowExpr (h', b) when g' = h' -> PowExpr (g', a + b)
            | g', h' -> MulExpr (g', h')
        | PowExpr (g, n) ->
            match simplify g, n with
            | _, n when n = 0N -> ConstExpr 1N
            | g', n when n = 1N -> g'
            | ConstExpr n, c when n = 0N && c > 0N -> ConstExpr 0N
            | ConstExpr n, _ when n = 1N -> ConstExpr 1N
            | ConstExpr c, _ -> ConstExpr (Nat.Pow (c, n))
            | g', _ -> PowExpr (g', n)
        | CompExpr (g, h) ->
            match simplify g, simplify h with
            | ConstExpr c, _ -> ConstExpr c
            | VarExpr, h' -> h'
            | g', ConstExpr c -> ConstExpr (g'.Apply c)
            | g', h' -> CompExpr (g', h')


/// <summary>Normalizer for <c>FunctionExpr</c>. Allows commutativity.</summary>
module Normalizer =
    open Simplifier

    /// <summary>Normalizes the order of a given <c>FunctionExpr</c>.</summary>
    let normalize (f: FunctionExpr): FunctionExpr =
        /// <summary>Order of operation.</summary>
        let rec exprKey (e: FunctionExpr): Nat * string =
            match e with
            | ConstExpr c -> 0N, string c
            | VarExpr -> 1N, "x"
            | PowExpr (g, n)  -> let _, k = exprKey g in 2N, k + "^" + string n
            | MulExpr (a, b)  -> let (_, ka), (_, kb) = exprKey a, exprKey b in 3N, ka + "*" + kb
            | AddExpr (a, b)  -> let (_, ka), (_, kb) = exprKey a, exprKey b in 4N, ka + "+" + kb
            | CompExpr (a, b) -> let (_, ka), (_, kb) = exprKey a, exprKey b in 5N, ka + "o" + kb

        /// <summary>Collect all </summary>
        let rec collectAdd: FunctionExpr -> FunctionExpr list = function | AddExpr (g, h) -> collectAdd g @ collectAdd h | e -> [ e ]
        let rec collectMul: FunctionExpr -> FunctionExpr list = function | MulExpr (g, h) -> collectMul g @ collectMul h | e -> [ e ]
        let reduceAdd (xs: FunctionExpr list): FunctionExpr = xs |> List.reduce (fun a b -> AddExpr(a, b))
        let reduceMul (xs: FunctionExpr list): FunctionExpr = xs |> List.reduce (fun a b -> MulExpr(a, b))

        /// <summary>Root for recursion.</summary>
        let rec normalizeExpr (f: FunctionExpr): FunctionExpr =
            match f with
            | AddExpr _ as a -> normalizeAdd a
            | MulExpr _ as m -> normalizeMul m
            | PowExpr (g, n) -> PowExpr (normalizeExpr g, n)
            | CompExpr (g, h) -> CompExpr (normalizeExpr g, normalizeExpr h)
            | _ -> f

        /// <summary>Normalize an addition-term.</summary>
        and normalizeAdd (f: FunctionExpr): FunctionExpr =
            match f with
            | AddExpr _ ->
                let terms: FunctionExpr list =
                    collectAdd f
                    |> List.map (simplify >> normalizeExpr)
                    |> List.filter ((<>) (ConstExpr 0N))

                let constSum: Nat =
                    terms
                    |> List.choose (function ConstExpr c -> Some c | _ -> None)
                    |> List.sum

                let others: FunctionExpr list =
                    terms
                    |> List.filter (function ConstExpr _ -> false | _ -> true)
                    |> List.sortBy exprKey

                match constSum, others with
                | n, [] when n = 0N -> ConstExpr 0N
                | c, [] -> ConstExpr c
                | n, xs when n = 0N -> reduceAdd xs
                | c, xs -> reduceAdd (xs @ [ConstExpr c])
            | _ -> failwith $"Unexpected Expression in normalizeAdd: {f}"

        /// <summary>Normalize a multiplication-term.</summary>
        and normalizeMul (f: FunctionExpr): FunctionExpr =
            match f with
            | MulExpr _ ->
                let factors: FunctionExpr list = collectMul f |> List.map (simplify >> normalizeExpr)

                if factors |> List.exists ((=) (ConstExpr 0N)) then
                    ConstExpr 0N
                else
                    let constProd: Nat =
                        factors
                        |> List.choose (function ConstExpr c -> Some c | _ -> None)
                        |> List.fold (*) 1N

                    let others: FunctionExpr list =
                        factors
                        |> List.filter (function ConstExpr _ -> false | _ -> true)
                        |> List.sortBy exprKey

                    match constProd, others with
                    | n, [] when n = 1N -> ConstExpr 1N
                    | c, [] -> ConstExpr c
                    | n, xs when n = 1N -> reduceMul xs
                    | c, xs -> reduceMul (ConstExpr c :: xs)
            | _ -> failwith $"Unexpected Expression in normalizeAdd: {f}"

        normalizeExpr f


//==================================================================================================
// ParserTests
//==================================================================================================


module ParserTests =
    open Parser; open Simplifier; open Normalizer

    type Equation =
        {
            StringVariants: string list
            Function: FunctionExpr
        }

    [<TestClass>]
    type Tests () =
        let equation1: Equation =
            {
                StringVariants =
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
                    ]
                Function = MulExpr (AddExpr (ConstExpr 1N, VarExpr), ConstExpr 2N)
            }

        let equation2: Equation =
            {
                StringVariants = 
                    [
                        "(x ^ 2) o ((x + 1) ^ 3)"
                        "((x ^ 2) o ((x + 1 + 0) ^ 3))"
                        "((x) ^ 2) o (((x + 1)) ^ 3)"
                        "((x + 0) ^ 2) o (((x + 0 + 1)) ^ 3)"
                        "(x ^ 2) o ((x + 1) ^ 3)"
                        "(x ^ 2) o ((1 + x) ^ 3)"
                        "(x ^ 2) o ((1 + (x)) ^ 3)"
                    ]
                Function = CompExpr (PowExpr (VarExpr, 2N), PowExpr (AddExpr (VarExpr, ConstExpr 1N), 3N))
            }

        let equation3: Equation =
            {
                StringVariants = 
                    [
                        "((x * (x + 2)) ^ 2) o ((x + 1) ^ 3)"
                        "((x * (2 + x)) ^ 2) o ((x + 1) ^ 3)"
                        "((x * (x + 2)) ^ 2) o ((1 + x) ^ 3)"
                        "((x * (2 + x)) ^ 2) o ((1 + x) ^ 3)"
                        "(((2 + x) * x) ^ 2) o ((1 + x + 0) ^ 3)"
                        "(((2 + x) * x) ^ 2) o ((x + 1) ^ 3)"
                        "((((2 + x)) * x) ^ 2) o (((x) + 1 * 1) ^ 3)"
                    ]
                Function = CompExpr (
                    PowExpr (MulExpr (VarExpr, AddExpr (VarExpr, ConstExpr 2N)), 2N),
                    PowExpr (AddExpr (VarExpr, ConstExpr 1N), 3N)
                )
            }

        [<TestMethod; Timeout 1000>]
        member _.``Equation 1 - all variants parse equal`` () : unit =
            Assert.IsLessThanOrEqualTo (1, equation1.StringVariants |> (List.map (parse >> simplify >> normalize) >> Set.ofList >> Set.count))

        [<TestMethod; Timeout 1000>]
        member _.``Equation 1 - all variants equal expected AST`` () : unit =
            Assert.IsTrue (
                equation1.StringVariants
                |> List.map (parse >> simplify >> normalize)
                |> List.forall ((=) (equation1.Function |> normalize))
            )

        [<TestMethod; Timeout 1000>]
        member _.``Equation 2 - all variants parse equal`` () : unit =
            Assert.IsLessThanOrEqualTo (1, equation2.StringVariants |> (List.map (parse >> simplify >> normalize) >> Set.ofList >> Set.count))

        [<TestMethod; Timeout 1000>]
        member _.``Equation 2 - all variants equal expected AST`` () : unit =
            Assert.IsTrue (
                equation2.StringVariants
                |> List.map (parse >> simplify >> normalize)
                |> List.forall ((=) equation2.Function)
            )

        [<TestMethod; Timeout 1000>]
        member _.``Equation 3 - all variants parse equal`` () : unit =
            Assert.IsLessThanOrEqualTo (1, equation3.StringVariants |> (List.map (parse >> simplify >> normalize) >> Set.ofList >> Set.count))

        [<TestMethod; Timeout 1000>]
        member _.``Equation 3 - all variants equal expected AST`` () : unit =
            Assert.IsTrue (
                equation3.StringVariants
                |> List.map (parse >> simplify >> normalize)
                |> List.forall ((=) equation3.Function)
            )

        [<TestMethod; Timeout 1000>]
        member _.``Precedence: * binds tighter than +`` (): unit =
            let ast: FunctionExpr = parse "1 + 2 * 3"
            Assert.AreEqual<FunctionExpr> (AddExpr (ConstExpr 1N, MulExpr (ConstExpr 2N, ConstExpr 3N)), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Precedence: ^ binds tighter than *`` () =
            let ast: FunctionExpr = parse "2 * x ^ 3"
            Assert.AreEqual<FunctionExpr> (MulExpr (ConstExpr 2N, PowExpr (VarExpr, 3N)), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Composition lowest precedence`` (): unit =
            // x + 1 o x + 2  should be (x+1) o (x+2) if 'o' has lowest precedence
            let ast: FunctionExpr = parse "x + 1 o x + 2"
            Assert.AreEqual<FunctionExpr> (CompExpr (AddExpr (VarExpr, ConstExpr 1N), AddExpr (VarExpr, ConstExpr 2N)), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Associativity: + is left-associative`` (): unit =
            let ast: FunctionExpr = parse "1 + 2 + 3"
            Assert.AreEqual<FunctionExpr> (AddExpr (AddExpr (ConstExpr 1N, ConstExpr 2N), ConstExpr 3N), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Associativity: * is left-associative`` () =
            let ast: FunctionExpr = parse "2 * 3 * 4"
            Assert.AreEqual<FunctionExpr> (MulExpr (MulExpr (ConstExpr 2N, ConstExpr 3N), ConstExpr 4N), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Associativity: o is left-associative`` () =
            let ast: FunctionExpr = parse "x o x o x"
            Assert.AreEqual<FunctionExpr> (CompExpr (CompExpr (VarExpr, VarExpr), VarExpr), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Parentheses in Exponent are allowed`` (): unit =
            let ast: FunctionExpr = parse "x ^ (1)"
            Assert.AreEqual<FunctionExpr> (PowExpr (VarExpr, 1N), ast)

        [<TestMethod; Timeout 1000>]
        member _.``Commutativity: Addition`` (): unit =
            let ast1: FunctionExpr = parse "x + 1"
            let ast2: FunctionExpr = parse "1 + x"
            Assert.AreEqual<FunctionExpr> (ast1 |> normalize, ast2 |> normalize)

        [<TestMethod; Timeout 1000>]
        member _.``Commutativity: Multiplication`` (): unit =
            let ast1: FunctionExpr = parse "x * 1"
            let ast2: FunctionExpr = parse "1 * x"
            Assert.AreEqual<FunctionExpr> (ast1 |> normalize, ast2 |> normalize)

        [<TestMethod; Timeout 1000>]
        member _.``No Commutativity: Composition`` (): unit =
            let ast1: FunctionExpr = parse "x o (x + 1)"
            let ast2: FunctionExpr = parse "(x + 1) o x"
            Assert.AreNotEqual<FunctionExpr> (ast1 |> normalize, ast2 |> normalize)


//==================================================================================================
// CalculusTests
//==================================================================================================


[<TestClass>]
type Tests () =

    let config: Config = defaultConfig.WithEndSize 1000

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


    let exampleValues: Nat list = [ 4N; 8N; 15N; 16N; 32N; 42N ]


    //=========================================================================
    // FUNCTIONAL
    //=========================================================================


    [<TestMethod; Timeout 1000>]
    member _.``Functional: toString Beispiele`` (): unit =
        for f, _  in exampleFunctions do
            (?) <@ toString f = (FunctionExpr.FromFunction f).ToString () @>

    [<TestMethod; Timeout 1000>]
    member _.``Functional: toString ZufallsTest`` (): unit =
        Checkify.Check <@ fun (f: Function) -> toString f = (FunctionExpr.FromFunction f).ToString () @>

    [<TestMethod; Timeout 1000>]
    member _.``Functional: apply Beispiele`` (): unit =
        for (f, _), x in List.allPairs exampleFunctions exampleValues do
            (?) <@ apply f x = (FunctionExpr.FromFunction f).Apply x @>

    [<TestMethod; Timeout 1000>]
    member _.``Functional: apply ZufallsTest`` (): unit =
        Checkify.Check <@ fun (f: Function) (x: Nat) -> apply f x = (FunctionExpr.FromFunction f).Apply x @>

    [<TestMethod; Timeout 1000>]
    member _.``Functional: derive Beispiele`` (): unit =
        for f, _ in exampleFunctions do
            (?) <@ toString (derive f) = (FunctionExpr.FromFunction f).Derive().ToString() @>

    [<TestMethod; Timeout 1000>]
    member _.``Functional: derive ZufallsTest`` (): unit =
        Checkify.Check (<@ fun (f: Function) -> toString (derive f) = (FunctionExpr.FromFunction f).Derive().ToString() @>, config)


    //=========================================================================
    // OBJECT ORIENTED
    //=========================================================================


    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: toString Beispiele`` (): unit =
        for f, f'  in exampleFunctions do
            (?) <@ f'.ToString () = (FunctionExpr.FromFunction f).ToString () @>

    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: toString ZufallsTest`` (): unit =
        Checkify.Check <@ fun (f: Function) -> (toIFunction f).ToString () = (FunctionExpr.FromFunction f).ToString () @>

    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: apply Beispiele`` (): unit =
        for (f, f'), x in List.allPairs exampleFunctions exampleValues do
            (?) <@ f'.Apply x = (FunctionExpr.FromFunction f).Apply x @>

    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: apply ZufallsTest`` (): unit =
        Checkify.Check <@ fun (f: Function) (x: Nat) -> (toIFunction f).Apply x = (FunctionExpr.FromFunction f).Apply x @>

    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: derive Beispiele`` (): unit =
        for f, f' in exampleFunctions do
            (?) <@ f'.Derive().ToString() = (FunctionExpr.FromFunction f).Derive().ToString() @>

    [<TestMethod; Timeout 1000>]
    member _.``Object Oriented: derive ZufallsTest`` (): unit =
        Checkify.Check (<@ fun (f: Function) -> (toIFunction f).Derive().ToString() = (FunctionExpr.FromFunction f).Derive().ToString() @>, config)