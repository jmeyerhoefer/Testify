module Tests


open Mini
open Types


open Assertify
open FsCheck


#nowarn "49"


// ====================================================================================================
// Reduktionssemantik
// ====================================================================================================


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


// ====================================================================================================
// RingBuffer
// ====================================================================================================


exception BufferEmptyTest
exception BufferFullTest


[<ReferenceEquality>]
type Necklace<'a> =
    {
        mutable bead: 'a
        mutable next: Necklace<'a>
    }


let private single (x: 'a): Necklace<'a> =
    let rec item: Necklace<'a> =
        {
            bead = x
            next = item
        } in item


let private swap (p: Necklace<'a>, q: Necklace<'a>): unit =
    let tmp: Necklace<'a> = p.next
    p.next <- q.next
    q.next <- tmp


let private append (last1: Necklace<'a>, last2: Necklace<'a>): Necklace<'a> =
    swap (last1, last2)
    last2


let private cons (x: 'a, xs: Necklace<'a>): Necklace<'a> =
    append (single x, xs)


let private head (last: Necklace<'a>): 'a =
    last.next.bead


let private tail (last: Necklace<'a>): Necklace<'a> =
    swap (last, last.next)
    last


let rec private replicate (n: int) (a: 'a): Necklace<'a> =
    if n = 0 then
        single a
    else
        append (replicate (n - 1) a, single a)


let private create<'a> (size: int): Necklace<'a option> =
    replicate (size - 1) None


let private put (buffer: Necklace<'a option> ref) (e: 'a): unit =
    (!buffer).bead <- Some e
    buffer := (!buffer).next


let rec private get (buffer: Necklace<'a option> ref) (size: int): 'a =
    if (!buffer).bead = None then
        if size > 0 then
            get (ref (!buffer).next) (size-1)
        else
            raise BufferEmptyTest
    else
        let res: 'a option = (!buffer).bead
        (!buffer).bead <- None
        match res with
        | None -> raise BufferEmptyTest
        | Some (x: 'a) -> x


let private r2nBuffer<'a> (r: RingBuffer<'a>): Necklace<'a option> =
    let len: int = r.buffer.Length
    let n: Necklace<'a option> ref = ref (create<'a> len)
    let offset: int -> int = fun (i: int) -> (!r.readPos + i) % len
    let rec help (i: int): unit =
        if i < !r.size then 
            put n r.buffer[offset i]
            help (i + 1)
    help 0
    !n

// ====================================================================================================
// ArbitraryModifiers
// ====================================================================================================

[<StructuredFormatDisplay("{ToString}")>]
type TestInput<'a> =
    | TI of RingBuffer<'a> * string
    member this.ToString: string =
        let (TI (_, s: string): TestInput<'a>) = this
        s


type ArbitraryModifiers =
    static member Nat (): Arbitrary<Nat> =
        let nonNegativeIntArb: Gen<Nat> =
            Arb.from<NonNegativeInt>
            |> Arb.convert
                (fun (i: NonNegativeInt) -> Nat.Make (int i))
                (fun (n: Nat) -> NonNegativeInt (int n))
            |> Arb.toGen
        let bigintArb: Gen<Nat> =
            Arb.from<bigint>
            |> Arb.filter (fun (i: bigint) -> i >= 0I)
            |> Arb.convert Nat.Make _.ToBigInteger()
            |> Arb.toGen
        [ nonNegativeIntArb; bigintArb ]
        |> Gen.oneof
        |> Arb.fromGen


    static member SmallNat (): Arbitrary<SmallNat> =
        Arb.from<Nat>
        |> Arb.filter (fun (n: Nat) -> n < 7N)
        |> Arb.convert SmallNat (fun (SmallNat n) -> n)


    static member TestInput<'a> (): Arbitrary<TestInput<'a>> =
        Arb.from<'a array * int * int>
        |> Arb.filter (fun (buffer: 'a array, size: int, readPos: int) ->
            size <= buffer.Length && readPos < buffer.Length && size >= 0 && readPos >= 0
        )
        |> Arb.convert
            (fun (buffer: 'a array, size: int, readPos: int) ->
                let rb: RingBuffer<'a> = { buffer = buffer; size = ref size; readPos = ref readPos }
                TI (rb, $"%A{rb}"))
            (fun (TI (i: RingBuffer<'a>, _): TestInput<'a>) -> i.buffer, !i.size, !i.readPos)


// ====================================================================================================
// Tests
// ====================================================================================================


[<TestClass>]
type Tests () =
    do Arb.register<ArbitraryModifiers> () |> ignore
    do Assertify.ShowHistory <- true
    do Assertify.ShowReductions <- true

    let ex1RegExp: RegExp<char> = Cat (Lit 'a', Lit 'b')
    let ex2RegExp: RegExp<char> = Cat (Lit 'a', Eps)
    let ex3RegExp: RegExp<char> = Cat (Eps, Lit 'b')
    let ex4RegExp: RegExp<char> = Or (Lit 'a', Lit 'b')
    let ex5RegExp: RegExp<char> = Star (Lit 'a')
    let ex1Ring': unit -> RingBuffer<int> =
        fun () -> { buffer = [| 0; 0; 0 |]; size = ref 0; readPos = ref 0 }
    let ex2Ring': unit -> RingBuffer<int> =
        fun () -> { buffer = [| 1; 2; 3 |]; size = ref 1; readPos = ref 0 }
    let ex3Ring': unit -> RingBuffer<int> =
        fun () -> { buffer = [| 7; 3; 6; 1; 20; 15; 17; 4; 9; 12 |]; size = ref 6; readPos = ref 7 }

    // ====================================================================================================
    // Reduktionssemantik
    // ====================================================================================================

    [<TestMethod>] [<Timeout(10000)>]
    member _.``isWord Beispiele`` (): unit =
        (?) <@ Reduktionssemantik.isWord Empty = None @>
        (?) <@ Reduktionssemantik.isWord Eps = Some [] @>
        (?) <@ Reduktionssemantik.isWord (Lit 'a') = Some ['a'] @>
        (?) <@ Reduktionssemantik.isWord ex1RegExp = Some ['a'; 'b'] @>
        (?) <@ Reduktionssemantik.isWord ex2RegExp = Some ['a'] @>
        (?) <@ Reduktionssemantik.isWord ex5RegExp = None @>
        (?) <@ Reduktionssemantik.isWord ex4RegExp = None @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``isWord Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>) ->
                let r: RegExp<char> = toRegExp r
                (?) <@ Reduktionssemantik.isWord r = isWord r @>
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduceStep Beispiele`` (): unit =
        (?) <@ Reduktionssemantik.reduceStep Empty = [] @>
        (?) <@ Reduktionssemantik.reduceStep Eps = [] @>
        (?) <@ Reduktionssemantik.reduceStep (Lit 'a') = [] @>
        (?) <@ Reduktionssemantik.reduceStep ex1RegExp = [] @>
        (?) <@ Reduktionssemantik.reduceStep ex2RegExp = [Lit 'a'] @>
        (?) <@ Reduktionssemantik.reduceStep ex3RegExp = [Lit 'b'] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduceStep ex4RegExp) = Set.ofList [Lit 'a'; Lit 'b'] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduceStep ex5RegExp) = Set.ofList [Cat (Lit 'a', ex5RegExp); Eps] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduceStep Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>) ->
                let r: RegExp<char> = toRegExp r
                (?) <@ Set.ofList (Reduktionssemantik.reduceStep r) = Set.ofList (reduceStep r) @>
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduce Beispiele`` (): unit =
        (?) <@ Reduktionssemantik.reduce Empty 0N = [ Empty ] @>
        (?) <@ Reduktionssemantik.reduce Eps 0N = [ Eps ] @>
        (?) <@ Reduktionssemantik.reduce (Lit 'a') 0N = [ Lit 'a' ] @>
        (?) <@ Reduktionssemantik.reduce ex2RegExp 0N = [ ex2RegExp ] @>
        (?) <@ Reduktionssemantik.reduce ex4RegExp 0N = [ ex4RegExp ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex4RegExp 1N) = Set.ofList [ ex4RegExp; Lit 'a'; Lit 'b' ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex4RegExp 2N) = Set.ofList [ ex4RegExp; Lit 'a'; Lit 'b' ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex3RegExp 1N) = Set.ofList [ ex3RegExp; Lit 'b' ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex1RegExp 1N) = Set.ofList [ ex1RegExp ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex5RegExp 1N) = Set.ofList [ ex5RegExp; Eps; Cat (Lit 'a', ex5RegExp) ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce ex5RegExp 2N) = Set.ofList [ ex5RegExp; Eps; Cat (Lit 'a', ex5RegExp); Cat (Lit 'a', Cat (Lit 'a', ex5RegExp)); ex2RegExp ] @>
        (?) <@ Set.ofList (Reduktionssemantik.reduce (Cat (ex4RegExp, Lit 'c')) 2N) = Set.ofList [ Cat (ex4RegExp, Lit 'c'); Cat (Lit 'a', Lit 'c'); Cat (Lit 'b', Lit 'c') ] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``reduce Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                (?) <@ Set.ofList (Reduktionssemantik.reduce r n) = Set.ofList (reduce r n) @>
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``words Beispiele`` (): unit =
        (?) <@ Set.ofList (Reduktionssemantik.words Empty 10N) = Set.ofList [] @>
        (?) <@ Set.ofList (Reduktionssemantik.words Eps 0N) = Set.ofList [ [] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words (Lit 'a') 0N) = Set.ofList [ ['a'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words ex2RegExp 1N) = Set.ofList [ ['a'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words ex4RegExp 1N) = Set.ofList [ ['a']; ['b'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words ex5RegExp 1N) = Set.ofList [ [] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words ex5RegExp 2N) = Set.ofList [ []; ['a'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words ex5RegExp 3N) = Set.ofList [ []; ['a']; ['a'; 'a'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words (Cat (ex4RegExp, Lit 'c')) 1N) = Set.ofList [ ['a'; 'c']; ['b'; 'c'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words (Star ex4RegExp) 2N) = Set.ofList [ [] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words (Star ex4RegExp) 3N) = Set.ofList [ []; ['a']; ['b'] ] @>
        (?) <@ Set.ofList (Reduktionssemantik.words (Star ex4RegExp) 5N) = Set.ofList [ []; ['a']; ['b']; ['a'; 'a']; ['a'; 'b']; ['b'; 'a']; ['b'; 'b'] ] @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``words Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                (?) <@ Set.ofList (Reduktionssemantik.words r n) = Set.ofList (words r n) @>
        )

    [<TestMethod>] [<Timeout(10000)>]
    member _.``generates Beispiele`` (): unit =
        (?) <@ Reduktionssemantik.generates Empty [] 0N = false @>
        (?) <@ Reduktionssemantik.generates Eps [] 0N = true @>
        (?) <@ Reduktionssemantik.generates (Lit 'a') [ 'a' ] 0N = true @>
        (?) <@ Reduktionssemantik.generates (Lit 'a') [ 'b' ] 0N = false @>
        (?) <@ Reduktionssemantik.generates ex2RegExp [ 'a' ] 1N = true @>
        (?) <@ Reduktionssemantik.generates ex2RegExp [ 'b' ] 1N = false @>
        (?) <@ Reduktionssemantik.generates ex4RegExp [ 'a' ] 0N = false @>
        (?) <@ Reduktionssemantik.generates ex4RegExp [ 'a' ] 1N = true @>
        (?) <@ Reduktionssemantik.generates ex4RegExp [ 'b' ] 1N = true @>
        (?) <@ Reduktionssemantik.generates ex4RegExp [ 'c' ] 1N = false @>
        (?) <@ Reduktionssemantik.generates (Cat (ex4RegExp, Lit 'c')) [ 'a'; 'c' ] 1N = true @>
        (?) <@ Reduktionssemantik.generates (Star ex4RegExp) [ 'a'; 'a' ] 5N = true @>
        (?) <@ Reduktionssemantik.generates ex5RegExp [] 1N = true @>
        (?) <@ Reduktionssemantik.generates ex5RegExp [ 'a' ] 3N = true @>
        (?) <@ Reduktionssemantik.generates ex5RegExp [ 'a'; 'a' ] 3N = true @>
        (?) <@ Reduktionssemantik.generates ex5RegExp [ 'a'; 'a' ] 5N = true @>
        (?) <@ Reduktionssemantik.generates (Star ex4RegExp) [ 'a' ] 2N = false @>
        (?) <@ Reduktionssemantik.generates (Star ex4RegExp) [ 'a' ] 3N = true @>
        (?) <@ Reduktionssemantik.generates (Star ex4RegExp) [ 'a'; 'b' ] 5N = true @>

    [<TestMethod>] [<Timeout(10000)>]
    member _.``generates Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (r: SimpleRegExp<char>, word: char list, SmallNat n: SmallNat) ->
                let r: RegExp<char> = toRegExp r
                (?) <@ Reduktionssemantik.generates r word n = generates r word n @>
        )

    // ====================================================================================================
    // RingBuffer
    // ====================================================================================================

    [<TestMethod>] [<Timeout(1000)>]
    member _.``a) Beispiel`` (): unit =
        let rb: RingBuffer<int> = RingBuffer.create<int> 10
        <@ rb.buffer.Length = 10 @> -?> "Die Kapazität des Puffers stimmt nicht."
        <@ !rb.size = 0 @>          -?> "Das size stimmt nicht"
        <@ !rb.readPos = 0 @>       -?> "readPos stimmt nicht"

    [<TestMethod>] [<Timeout(1000)>]
    member _.``a) Zufall`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (n: Nat) ->
                if n > 0N then
                    let capacity: int = int n
                    let rb: RingBuffer<string> = RingBuffer.create<string> capacity
                    <@ rb.buffer.Length = capacity @> -?> "Kapazität des Puffers stimmt nicht."
                    <@ !rb.size = 0 @>                -?> "size stimmt nicht"
                    <@ !rb.readPos = 0 @>             -?> "readPos stimmt nicht"
        )

    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) Beispiel 1`` (): unit =
        let ex1Ring: RingBuffer<int> = ex1Ring' ()
        try
            RingBuffer.get ex1Ring |> ignore
            !! "Keine RingEmpty Ausnahme geworfen."
        with
        | RingEmpty -> ()
        (?) <@ ex1Ring = ex1Ring' () @>

    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) Beispiel 2`` (): unit =
        let ex2Ring: RingBuffer<int> = ex2Ring' ()
        (?) <@ RingBuffer.get ex2Ring = 1 @>
        <@ !ex2Ring.readPos = 1 @> -?> "readPos stimmt nicht"
        <@ !ex2Ring.size = 0 @>    -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."

    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) Beispiel 3`` (): unit =
        let ex3Ring: RingBuffer<int> = ex3Ring' ()
        let es: int list = [ 4; 9; 12; 7; 3; 6 ]
        let count: int ref = ref es.Length
        let expectedReadPos: int ref = ref 7
        for expected: int in es do
            count := !count - 1
            expectedReadPos := (!expectedReadPos + 1) % ex3Ring.buffer.Length
            (?) <@ RingBuffer.get ex3Ring = expected @>
            <@ !ex3Ring.readPos = !expectedReadPos @> -?> "readPos stimmt nicht"
            <@ !ex3Ring.size = !count @>              -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."
        try
            RingBuffer.get ex3Ring |> ignore
            !! "Ringpuffer sollte leer sein; keine Exception geworfen."
        with
        | RingEmpty -> ()

    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) Zufall: Array von n Zufallszahlen, size=n, readPos=0. get bis size=0 soll alle Inhalte des ursprünglichen Arrays ergeben`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (ar: int array) ->
                let rb: RingBuffer<int> = { buffer = ar; size = ref ar.Length; readPos = ref 0 }
                let rec help (idx: int) =
                    let sizeVorher: int = !rb.size
                    try
                        <@ RingBuffer.get rb = ar[idx] @> -?> $"get sollte Array Element an Stelle {string idx} ausgeben."
                        <@ !rb.size = sizeVorher - 1 @>   -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."
                    with
                    | RingEmpty -> <@ idx = ar.Length @> -?> "get liefert None obwohl noch Elemente vorhanden sind."
                    if idx < ar.Length then
                        help (idx + 1)
                    else
                        try
                            RingBuffer.get rb |> ignore
                            !! "Ringpuffer gibt Elemente zurück, obwohl None erwartet wird."
                        with
                        | RingEmpty -> ()
                help 0
        )

    [<TestMethod>] [<Timeout(1000)>]
    member _.``b) Zufall RingBuffer: ein get`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (TI (rb: RingBuffer<int>, _): TestInput<int>) ->
                let n: Necklace<int option> ref = ref (r2nBuffer rb)
                try
                    <@ RingBuffer.get rb = get n rb.buffer.Length @> -?> "Hinweis: null in dieser Fehlermeldung ist None"
                with
                | BufferEmptyTest ->
                    try
                        RingBuffer.get rb |> ignore
                        !! "Exception nicht geworfen, obwohl Buffer leer."
                    with
                    | BufferEmpty -> ()
                | BufferEmpty -> !! "Exception geworfen, obwohl Buffer nicht leer."
        )

    [<TestMethod>] [<Timeout(2000)>]
    member _.``b) Zufall RingBuffer: size gets`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (TI (rb: RingBuffer<string>, _): TestInput<string>) ->
                let n: Necklace<string option> ref = ref (r2nBuffer rb)
                for i: int in 0 .. !rb.size do
                    try
                        (?) <@ RingBuffer.get rb = get n rb.buffer.Length @>
                    with
                    | BufferEmptyTest ->
                        try
                            RingBuffer.get rb |> ignore
                            !! "Exception nicht geworfen, obwohl Buffer leer."
                        with
                        | BufferEmpty -> ()
                    | BufferEmpty -> !! "Exception geworfen, obwohl Buffer nicht leer."
        )

    [<TestMethod>] [<Timeout(1000)>]
    member _.``c) Beispiel 1`` (): unit =
        let ex1Ring: RingBuffer<int> = ex1Ring' ()
        let history: History = History <@ RingBuffer.put ex1Ring 30 @>
        (<@ ex1Ring.buffer[0] = 30 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex1Ring.size = 1 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex1Ring.readPos = 0 @>, history)   -??> "readPos wurde verändert"

    [<TestMethod>] [<Timeout(1000)>]
    member _.``c) Beispiel 2`` (): unit =
        let ex2Ring: RingBuffer<int> = ex2Ring' ()
        let history: History = History <@ RingBuffer.put ex2Ring 30 @>
        (<@ ex2Ring.buffer[1] = 30 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex2Ring.size = 2 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex2Ring.readPos = 0 @> , history)  -??> "readPos wurde verändert"
        history.EvalAndAdd <@ RingBuffer.put ex2Ring 40 @>
        (<@ ex2Ring.buffer[2] = 40 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex2Ring.size = 3 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex2Ring.readPos = 0 @>, history)   -??> "readPos wurde verändert"
        try
            history.EvalAndAdd <@ RingBuffer.put ex2Ring 50 @>
            // TODO: does this work? what if it fails? then it shouldn't be added and an exception should be raised so BufferFull catches
            !! "Element wurde in vollen Buffer eingefügt."
        with
        | BufferFull -> ()
        (<@ !ex2Ring.size = 3 @>, history)    -??> "size nicht korrekt (soll nicht Größer sein als die Kapazität des Ringpuffers)."
        (<@ !ex2Ring.readPos = 0 @>, history) -??> "readPos wurde verändert"

    [<TestMethod>] [<Timeout(1000)>]
    member _.``c) Beispiel 3`` (): unit =
        let ex3Ring: RingBuffer<int> = ex3Ring' ()
        let history: History = History <@ RingBuffer.put ex3Ring 30 @>
        (<@ ex3Ring.buffer[3] = 30 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex3Ring.size = 7 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex3Ring.readPos = 7 @>, history)   -??> "readPos wurde verändert"
        history.EvalAndAdd <@ RingBuffer.put ex3Ring 40 @>
        (<@ ex3Ring.buffer[4] = 40 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex3Ring.size = 8 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex3Ring.readPos = 7 @>, history)   -??> "readPos wurde verändert"
        history.EvalAndAdd <@ RingBuffer.put ex3Ring 50 @>
        (<@ ex3Ring.buffer[5] = 50 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex3Ring.size = 9 @>, history)      -??> "size wurde nicht erhöht."
        (<@ !ex3Ring.readPos = 7 @>, history)   -??> "readPos wurde verändert"
        history.EvalAndAdd <@ RingBuffer.put ex3Ring 60 @>
        (<@ ex3Ring.buffer[6] = 60 @>, history) -??> "Element nicht korrekt eingefügt."
        (<@ !ex3Ring.size = 10 @>, history)     -??> "size wurde nicht erhöht."
        (<@ !ex3Ring.readPos = 7 @>, history)   -??> "readPos wurde verändert"
        try
            history.EvalAndAdd <@ RingBuffer.put ex3Ring 70 @> // TODO: does this work?
            !! "Element wurde in vollen Buffer eingefügt."
        with
        | BufferFull -> ()
        (<@ !ex3Ring.size = 10 @>, history)   -??> "size nicht korrekt (soll nicht größer sein als die Kapazität des Ringpuffers)."
        (<@ !ex3Ring.readPos = 7 @>, history) -??> "readPos wurde verändert"

    [<TestMethod>] [<Timeout(1000)>]
    member _.``c) Zufall: (setzt voraus, dass get funktioniert)`` (): unit =
        Check.One (
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (TI (rb: RingBuffer<int>, _): TestInput<int>, elems: int list) ->
                let sizeBegin: int = !rb.size 
                let n: Necklace<int option> ref = ref (r2nBuffer rb)
                let possibleInsertions: int = rb.buffer.Length - !rb.size
                let truncatedElems: int list = List.truncate possibleInsertions elems
                for e: int in truncatedElems do // alle elems einfügen
                    try
                        RingBuffer.put rb e
                        put n e
                    with
                    | BufferFull -> !! "Ausnahme geworfen, obwohl Buffer noch nicht voll ist."
                if truncatedElems.Length < elems.Length then
                    try
                        RingBuffer.put rb 0
                        !! "Element eingefügt, obwohl Buffer voll ist."
                    with
                    | BufferFull -> ()
                let sizeEnd: int = min (sizeBegin + truncatedElems.Length) rb.buffer.Length
                for i: int in 0 .. sizeEnd do // alle verfügbaren lesen
                    try
                        (?) <@ RingBuffer.get rb = get n rb.buffer.Length @>
                    with
                    | BufferEmptyTest ->
                        try
                            RingBuffer.get rb |> ignore
                            !! "get wirft keine Ausnahme trotz leeren Buffers."
                        with
                        | BufferEmpty -> ()
                    | BufferEmpty -> !! "Ausnahme geworfen, obwohl Buffer nicht leer ist."
        )