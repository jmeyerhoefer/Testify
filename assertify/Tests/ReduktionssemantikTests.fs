module Tests.ReduktionssemantikTests


open Assertify.Types
open Assertify.Checkify
open Assertify.Assertify.Operators
open Types.ReduktionssemantikTypes


type SimpleRegEx<'a> =
    | SEps
    | SLit of 'a
    | SCat of SimpleRegEx<'a> * SimpleRegEx<'a>
    | SEmpty
    | SOr of SimpleRegEx<'a> * SimpleRegEx<'a>
    | SStar of SimpleRegEx<'a>

let rec toRegEx<'a>(r: SimpleRegEx<'a>): RegEx<'a> =
    match r with
    | SEmpty -> Empty
    | SEps -> Eps
    | SLit a -> Lit a
    | SCat (r1, r2) -> Cat (toRegEx r1, toRegEx r2)
    | SOr (r1, r2) -> Or (toRegEx r1, toRegEx r2)
    | SStar r1 -> Star (toRegEx r1)

let rec isWord<'a>(r: RegEx<'a>): Option<List<'a>> =
    match r with
    | Eps -> Some []
    | Lit a -> Some [a]
    | Cat (r1, r2) -> 
        match isWord r1, isWord r2 with
        | Some l1, Some l2 -> Some (l1 @ l2)
        | _ -> None
    | _ -> None

let rec reduceStep<'a>(r: RegEx<'a>): List<RegEx<'a>> =
    match r with
    | Cat (r, Eps) | Cat (Eps, r) -> [r]
    | Cat (r1, r2) ->
        List.map (fun r1' -> Cat (r1', r2)) (reduceStep r1) @
        List.map (fun r2' -> Cat (r1, r2')) (reduceStep r2)
    | Or (r1, r2) -> [r1; r2]
    | Star r -> [Eps; Cat (r, Star r)]
    | _ -> []

let rec reduce<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<RegEx<'a>> =
    if n = 0N then [r]
    else r :: List.collect reduceStep (reduce r (n - 1N)) |> List.distinct

let rec words<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<List<'a>> =
    reduce r n |> List.choose isWord |> List.distinct

let rec generates<'a when 'a: equality>(r: RegEx<'a>) (word: List<'a>) (n: Nat): Bool =
    words r n |> List.contains word

type SmallNat = SmallNat of Nat

type ArbitraryModifiers =
    inherit NatModifier

    static member SmallNat (): Arbitrary<SmallNat> =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<Nat>
        |> FsCheck.FSharp.Arb.filter (fun (n: Nat) -> n < 7N)
        |> FsCheck.FSharp.Arb.convert SmallNat (fun (SmallNat n) -> n)


[<TestClass>]
type ReduktionssemantikTests () =
    let config: Config =
        Config
            .QuickThrowOnFailure
            .WithArbitrary [typeof<ArbitraryModifiers>]

    let ex1 = Cat (Lit 'a', Lit 'b')
    let ex2 = Cat (Lit 'a', Eps)
    let ex3 = Cat (Eps, Lit 'b')
    let ex4 = Or (Lit 'a', Lit 'b')
    let ex5 = Star (Lit 'a')

    [<TestMethod; Timeout 10000>]
    member _.``isWord Beispiele``() : Unit =
        (?) <@ Student.Reduktionssemantik.isWord Empty = None @>
        (?) <@ Student.Reduktionssemantik.isWord Eps = Some [] @>
        (?) <@ Student.Reduktionssemantik.isWord (Lit 'a') = Some ['a'] @>
        (?) <@ Student.Reduktionssemantik.isWord ex1 = Some ['a'; 'b'] @>
        (?) <@ Student.Reduktionssemantik.isWord ex2 = Some ['a'] @>
        (?) <@ Student.Reduktionssemantik.isWord ex5 = None @>
        (?) <@ Student.Reduktionssemantik.isWord ex4 = None @>

    [<TestMethod; Timeout 10000>]
    member _.``isWord Zufall``() : Unit =
        Checkify.Check (
            <@ fun (r: SimpleRegEx<char>) -> Student.Reduktionssemantik.isWord (toRegEx r) = isWord (toRegEx r) @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout 10000>]
    member _.``reduceStep Beispiele``() : Unit =
        (?) <@ Student.Reduktionssemantik.reduceStep Empty = [] @>
        (?) <@ Student.Reduktionssemantik.reduceStep Eps = [] @>
        (?) <@ Student.Reduktionssemantik.reduceStep (Lit 'a') = [] @>
        (?) <@ Student.Reduktionssemantik.reduceStep ex1 = [] @>
        (?) <@ Student.Reduktionssemantik.reduceStep ex2 = [Lit 'a'] @>
        (?) <@ Student.Reduktionssemantik.reduceStep ex3 = [Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduceStep ex4) = Set.ofList [Lit 'a'; Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduceStep ex5) = Set.ofList [Cat (Lit 'a', ex5); Eps] @>

    [<TestMethod; Timeout 10000>]
    member _.``reduceStep Zufall``() : Unit =
        Checkify.Check (
            <@ fun (r: SimpleRegEx<char>) -> Set.ofList (Student.Reduktionssemantik.reduceStep (toRegEx r)) = Set.ofList (reduceStep (toRegEx r)) @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout 10000>]
    member _.``reduce Beispiele``() : Unit =
        (?) <@ Student.Reduktionssemantik.reduce Empty 0N = [Empty] @>
        (?) <@ Student.Reduktionssemantik.reduce Eps 0N = [Eps] @>
        (?) <@ Student.Reduktionssemantik.reduce (Lit 'a') 0N = [Lit 'a'] @>
        (?) <@ Student.Reduktionssemantik.reduce ex2 0N = [ex2] @>
        (?) <@ Student.Reduktionssemantik.reduce ex4 0N = [ex4] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex4 1N) = Set.ofList [ex4; Lit 'a'; Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex4 2N) = Set.ofList [ex4; Lit 'a'; Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex3 1N) = Set.ofList [ex3; Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex1 1N) = Set.ofList [ex1] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex4 1N) = Set.ofList [ex4; Lit 'a'; Lit 'b'] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex5 1N) = Set.ofList [ex5; Eps; Cat (Lit 'a', ex5)] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce ex5 2N) = Set.ofList [ex5; Eps; Cat (Lit 'a', ex5); Cat (Lit 'a', Cat (Lit 'a', ex5)); ex2] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.reduce (Cat (ex4, Lit 'c')) 2N) = Set.ofList [Cat (ex4, Lit 'c'); Cat (Lit 'a', Lit 'c'); Cat (Lit 'b', Lit 'c')] @>

    [<TestMethod; Timeout 10000>]
    member _.``reduce Zufall``() : Unit =
        Checkify.Check (
            <@ fun (r: SimpleRegEx<char>, SmallNat n: SmallNat) -> Set.ofList (Student.Reduktionssemantik.reduce (toRegEx r) n) = Set.ofList (reduce (toRegEx r) n) @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout 10000>]
    member _.``words Beispiele``() : Unit =
        (?) <@ Set.ofList (Student.Reduktionssemantik.words Empty 10N) = Set.ofList [] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words Eps 0N) = Set.ofList [[]] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words (Lit 'a') 0N) = Set.ofList [['a']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words ex2 1N) = Set.ofList [['a']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words ex4 1N) = Set.ofList [['a']; ['b']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words ex5 1N) = Set.ofList [[]] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words ex5 2N) = Set.ofList [[]; ['a']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words ex5 3N) = Set.ofList [[]; ['a']; ['a'; 'a']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words (Cat (ex4, Lit 'c')) 1N) = Set.ofList [['a'; 'c']; ['b'; 'c']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words (Star ex4) 2N) = Set.ofList [[]] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words (Star ex4) 3N) = Set.ofList [[]; ['a']; ['b']] @>
        (?) <@ Set.ofList (Student.Reduktionssemantik.words (Star ex4) 5N) = Set.ofList [[]; ['a']; ['b']; ['a'; 'a']; ['a'; 'b']; ['b'; 'a']; ['b'; 'b']] @>

    [<TestMethod; Timeout 10000>]
    member _.``words Zufall``() : Unit =
        Checkify.Check (
            <@ fun (r: SimpleRegEx<char>, SmallNat n: SmallNat) -> Set.ofList (Student.Reduktionssemantik.words (toRegEx r) n) = Set.ofList (words (toRegEx r) n) @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout 10000>]
    member _.``generates Beispiele``() : Unit =
        (?) <@ Student.Reduktionssemantik.generates Empty [] 0N |> not @>
        (?) <@ Student.Reduktionssemantik.generates Eps [] 0N @>
        (?) <@ Student.Reduktionssemantik.generates (Lit 'a') ['a'] 0N @>
        (?) <@ Student.Reduktionssemantik.generates (Lit 'a') ['b'] 0N |> not @>
        (?) <@ Student.Reduktionssemantik.generates ex2 ['a'] 1N @>
        (?) <@ Student.Reduktionssemantik.generates ex2 ['b'] 1N |> not @>
        (?) <@ Student.Reduktionssemantik.generates ex4 ['a'] 0N |> not @>
        (?) <@ Student.Reduktionssemantik.generates ex4 ['a'] 1N @>
        (?) <@ Student.Reduktionssemantik.generates ex4 ['b'] 1N @>
        (?) <@ Student.Reduktionssemantik.generates ex4 ['c'] 1N |> not @>
        (?) <@ Student.Reduktionssemantik.generates (Cat (ex4, Lit 'c')) ['a'; 'c'] 1N @>
        (?) <@ Student.Reduktionssemantik.generates (Star ex4) ['a'; 'a'] 5N @>
        (?) <@ Student.Reduktionssemantik.generates ex5 [] 1N @>
        (?) <@ Student.Reduktionssemantik.generates ex5 ['a'] 3N @>
        (?) <@ Student.Reduktionssemantik.generates ex5 ['a'; 'a'] 3N @>
        (?) <@ Student.Reduktionssemantik.generates ex5 ['a'; 'a'] 5N @>
        (?) <@ Student.Reduktionssemantik.generates (Star ex4) ['a'] 2N |> not @>
        (?) <@ Student.Reduktionssemantik.generates (Star ex4) ['a'] 3N @>
        (?) <@ Student.Reduktionssemantik.generates (Star ex4) ['a'; 'b'] 5N @>

    [<TestMethod; Timeout 10000>]
    member _.``generates Zufall``() : Unit =
        Checkify.Check (
            <@ fun (r: SimpleRegEx<char>, word: List<char>, SmallNat n: SmallNat) -> Student.Reduktionssemantik.generates (toRegEx r) word n = generates (toRegEx r) word n @>,
            config.WithMaxTest 1000
        )
