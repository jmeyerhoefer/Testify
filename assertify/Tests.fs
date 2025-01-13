module Tests


open Mini
open Types


open Assertify
open FsCheck
open Microsoft.VisualStudio.TestTools.UnitTesting
open Swensen.Unquote


type SimpleRegExp<'a> =
    | SEps
    | SLit of 'a
    | SCat of SimpleRegExp<'a> * SimpleRegExp<'a>
    | SEmpty
    | SOr of SimpleRegExp<'a> * SimpleRegExp<'a>
    | SStar of SimpleRegExp<'a>


let rec private toRegExp<'a> (r: SimpleRegExp<'a>): RegExp<'a> =
    match r with
    | SEmpty -> Empty
    | SEps -> Eps
    | SLit a -> Lit a
    | SCat (r1, r2) -> Cat (toRegExp r1, toRegExp r2)
    | SOr (r1, r2) -> Or (toRegExp r1, toRegExp r2)
    | SStar r1 -> Star (toRegExp r1)


let rec private isWord<'a> (r: RegExp<'a>): 'a list option =
    match r with
    | Eps -> Some []
    | Lit a -> Some [ a ]
    | Cat (r1, r2) -> 
        match isWord r1, isWord r2 with
        | Some l1, Some l2 -> Some (l1 @ l2)
        | _ -> None
    | _ -> None


let rec private reduceStep<'a> (r: RegExp<'a>): RegExp<'a> list =
    match r with
    | Cat (r, Eps)
    | Cat (Eps, r) -> [ r ]
    | Cat (r1, r2) ->
        (reduceStep r1 |> List.map (fun (r: RegExp<'a>) -> Cat (r, r2)))
        @ (reduceStep r2 |> List.map (fun (r: RegExp<'a>) -> Cat (r1, r)))
    | Or (r1, r2) -> [ r1; r2 ]
    | Star r -> [ Eps; Cat (r, Star r) ]
    | _ -> []


let rec private reduce<'a when 'a: equality> (r: RegExp<'a>) (n: Nat): RegExp<'a> list =
    if n = 0N then
        [ r ]
    else
        let rest: RegExp<'a> list =
            reduce r (n - 1N)
            |> List.collect reduceStep
            |> List.distinct
        r :: rest


let rec private words<'a when 'a: equality> (r: RegExp<'a>) (n: Nat): 'a list list =
    reduce r n
    |> List.choose isWord
    |> List.distinct


let rec private generates<'a when 'a: equality> (r: RegExp<'a>) (word: 'a list) (n: Nat): Bool =
    words r n |> List.contains word


type SmallNat = SmallNat of Nat


type ArbitraryModifiers =
    static member Nat (): Arbitrary<Nat> =
        Arb.from<NonNegativeInt>
        |> Arb.convert
            (fun (i: NonNegativeInt) -> Nat.Make (int i))
            (fun (n: Nat) -> NonNegativeInt (int n))

    static member SmallNat (): Arbitrary<SmallNat> =
        Arb.from<Nat>
        |> Arb.filter (fun (n: Nat) -> n < 7N)
        |> Arb.convert SmallNat (fun (SmallNat n) -> n)


[<TestClass>]
type Tests () =
    do Arb.register<ArbitraryModifiers> () |> ignore

    let ex1: RegExp<char> = Cat (Lit 'a', Lit 'b')
    let ex2: RegExp<char> = Cat (Lit 'a', Eps)
    let ex3: RegExp<char> = Cat (Eps, Lit 'b')
    let ex4: RegExp<char> = Or (Lit 'a', Lit 'b')
    let ex5: RegExp<char> = Star (Lit 'a')

    [<TestMethod>] [<Timeout(10000)>]
    member _.``isWord Beispiele`` (): unit =
        Assertify.Test <@ Reduktionssemantik.isWord Empty = None @>
        Assertify.Test <@ Reduktionssemantik.isWord Eps = Some [] @>
        Assertify.Test <@ Reduktionssemantik.isWord (Lit 'a') = Some ['a'] @>
        Assertify.Test <@ Reduktionssemantik.isWord ex1 = Some ['a'; 'b'] @>
        Assertify.Test <@ Reduktionssemantik.isWord ex2 = Some ['a'] @>
        Assertify.Test <@ Reduktionssemantik.isWord ex5 = None @>
        Assertify.Test <@ Reduktionssemantik.isWord ex4 = None @>
    
    [<TestMethod>] [<Timeout(10000)>]
    member _.``isWord Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>) ->
                let r: RegExp<char> = toRegExp r
                Assertify.AreEqual (
                    <@ Reduktionssemantik.isWord r @>,
                    isWord r,
                    Reduktionssemantik.isWord r
                )
        )

    [<TestMethod>] [<Timeout(10000)>]
    member self.``reduceStep Beispiele`` (): unit =
        Assertify.Test <@ Reduktionssemantik.reduceStep Empty = [] @>
        Assertify.Test <@ Reduktionssemantik.reduceStep Eps = [] @>
        Assertify.Test <@ Reduktionssemantik.reduceStep (Lit 'a') = [] @>
        Assertify.Test <@ Reduktionssemantik.reduceStep ex1 = [] @>
        Assertify.Test <@ Reduktionssemantik.reduceStep ex2 = [Lit 'a'] @>
        Assertify.Test <@ Reduktionssemantik.reduceStep ex3 = [Lit 'b'] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduceStep ex4) = Set.ofList [Lit 'a'; Lit 'b'] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduceStep ex5) = Set.ofList [Cat (Lit 'a', ex5); Eps] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduceStep Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>) ->
                let r: RegExp<char> = toRegExp r
                Assertify.AreEqual (
                    <@ Set.ofList (Reduktionssemantik.reduceStep r) @>,
                    Set.ofList (reduceStep r),
                    Set.ofList (Reduktionssemantik.reduceStep r)
                )
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduce Beispiele`` (): unit =
        Assertify.Test <@ Reduktionssemantik.reduce Empty 0N = [ Empty ] @>
        Assertify.Test <@ Reduktionssemantik.reduce Eps 0N = [ Eps ] @>
        Assertify.Test <@ Reduktionssemantik.reduce (Lit 'a') 0N = [ Lit 'a' ] @>
        Assertify.Test <@ Reduktionssemantik.reduce ex2 0N = [ ex2 ] @>
        Assertify.Test <@ Reduktionssemantik.reduce ex4 0N = [ ex4 ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex4 1N) = Set.ofList [ ex4; Lit 'a'; Lit 'b' ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex4 2N) = Set.ofList [ ex4; Lit 'a'; Lit 'b' ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex3 1N) = Set.ofList [ ex3; Lit 'b' ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex1 1N) = Set.ofList [ ex1 ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex5 1N) = Set.ofList [ ex5; Eps; Cat (Lit 'a', ex5) ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce ex5 2N) = Set.ofList [ ex5; Eps; Cat (Lit 'a', ex5); Cat (Lit 'a', Cat (Lit 'a', ex5)); ex2 ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.reduce (Cat (ex4, Lit 'c')) 2N) = Set.ofList [ Cat (ex4, Lit 'c'); Cat (Lit 'a', Lit 'c'); Cat (Lit 'b', Lit 'c') ] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduce Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                Assertify.AreEqual (
                    <@ Set.ofList (Reduktionssemantik.reduce r n) @>,
                    Set.ofList (reduce r n),
                    Set.ofList (Reduktionssemantik.reduce r n)
                )
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``words Beispiele`` (): unit =
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words Empty 10N) = Set.ofList [] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words Eps 0N) = Set.ofList [ [] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words (Lit 'a') 0N) = Set.ofList [ ['a'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words ex2 1N) = Set.ofList [ ['a'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words ex4 1N) = Set.ofList [ ['a']; ['b'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words ex5 1N) = Set.ofList [ [] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words ex5 2N) = Set.ofList [ []; ['a'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words ex5 3N) = Set.ofList [ []; ['a']; ['a'; 'a'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words (Cat (ex4, Lit 'c')) 1N) = Set.ofList [ ['a'; 'c']; ['b'; 'c'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words (Star ex4) 2N) = Set.ofList [ [] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words (Star ex4) 3N) = Set.ofList [ []; ['a']; ['b'] ] @>
        Assertify.Test <@ Set.ofList (Reduktionssemantik.words (Star ex4) 5N) = Set.ofList [ []; ['a']; ['b']; ['a'; 'a']; ['a'; 'b']; ['b'; 'a']; ['b'; 'b'] ] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``words Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                Assertify.AreEqual (
                    <@ Set.ofList (Reduktionssemantik.words r n) @>,
                    Set.ofList (words r n),
                    Set.ofList (Reduktionssemantik.words r n)
                )
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``generates Beispiele`` (): unit =
        Assertify.Test <@ Reduktionssemantik.generates Empty [] 0N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates Eps [] 0N @>
        Assertify.Test <@ Reduktionssemantik.generates (Lit 'a') [ 'a' ] 0N @>
        Assertify.Test <@ Reduktionssemantik.generates (Lit 'a') [ 'b' ] 0N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates ex2 [ 'a' ] 1N @>
        Assertify.Test <@ Reduktionssemantik.generates ex2 [ 'b' ] 1N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates ex4 [ 'a' ] 0N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates ex4 [ 'a' ] 1N @>
        Assertify.Test <@ Reduktionssemantik.generates ex4 [ 'b' ] 1N @>
        Assertify.Test <@ Reduktionssemantik.generates ex4 [ 'c' ] 1N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates (Cat (ex4, Lit 'c')) [ 'a'; 'c' ] 1N @>
        Assertify.Test <@ Reduktionssemantik.generates (Star ex4) [ 'a'; 'a' ] 5N @>
        Assertify.Test <@ Reduktionssemantik.generates ex5 [] 1N @>
        Assertify.Test <@ Reduktionssemantik.generates ex5 [ 'a' ] 3N @>
        Assertify.Test <@ Reduktionssemantik.generates ex5 [ 'a'; 'a' ] 3N @>
        Assertify.Test <@ Reduktionssemantik.generates ex5 [ 'a'; 'a' ] 5N @>
        Assertify.Test <@ Reduktionssemantik.generates (Star ex4) [ 'a' ] 2N |> not @>
        Assertify.Test <@ Reduktionssemantik.generates (Star ex4) [ 'a' ] 3N @>
        Assertify.Test <@ Reduktionssemantik.generates (Star ex4) [ 'a'; 'b' ] 5N @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``generates Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, word: char list, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                Assertify.AreEqual (
                    <@ Reduktionssemantik.generates r word n @>,
                    generates r word n,
                    Reduktionssemantik.generates r word n
                )
        )

    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsTrue Beispiele 1`` (): unit =
    //     Assertify.IsTrue <@ 1N = 1N @>
    //     Assertify.IsTrue <@ 1N = 2N @>
    //
    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsTrue Beispiele 2`` (): unit =
    //     let f (n: Nat): bool = n > 10N
    //     Assertify.IsTrue <@ f 11N @>
    //     Assertify.IsTrue <@ f 9N @>
    //
    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsTrue Zufall`` (): unit =
    //     Check.One (
    //         { Config.QuickThrowOnFailure with MaxTest = 1000 },
    //         fun (r: SimpleRegExp<char>, word: char list, SmallNat n: SmallNat) ->
    //             let r: RegExp<char> = toRegExp r
    //             Assertify.IsTrue <@ Reduktionssemantik.generates r word n @>
    //     )
    //
    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsFalse Beispiele 1`` (): unit =
    //     Assertify.IsFalse <@ 1N = 2N @>
    //     Assertify.IsFalse <@ 1N = 1N @>
    //
    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsFalse Beispiele 2`` (): unit =
    //     let f (n: Nat): bool = n > 10N
    //     Assertify.IsFalse <@ f 9N @>
    //     Assertify.IsFalse <@ f 11N @>
    //
    // [<TestMethod>] [<Timeout(10000)>]
    // member _.``IsFalse Zufall`` (): unit =
    //     Check.One (
    //         { Config.QuickThrowOnFailure with MaxTest = 1000 },
    //         fun (r: SimpleRegExp<char>, word: char list, SmallNat n: SmallNat) ->
    //             let r: RegExp<char> = toRegExp r
    //             Assertify.IsFalse <@ Reduktionssemantik.generates r word n |> not @>
    //     )