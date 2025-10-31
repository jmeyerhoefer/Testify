module Tests.RingBufferTests


open Assertify
open Types.RingBufferTypes
#nowarn "49"


exception BufferEmptyTest
exception BufferFullTest


// ----- verwende Necklace aus Vorlesung als Referenzimplementierung
[<ReferenceEquality>]
type Necklace<'elem> = { mutable bead : 'elem; mutable next : Necklace<'elem> }

let single x =
    let rec item = { bead = x; next = item }
    in item

let swap (p : Necklace<'elem>, q : Necklace<'elem>) =
    let tmp = p.next
    p.next <- q.next
    q.next <- tmp

let append (last1 : Necklace<'elem>, last2 : Necklace<'elem>) : Necklace<'elem> =
    swap (last1, last2)
    last2

let cons (x, xs) = append (single x, xs)

let head (last : Necklace<'elem>) : 'elem =
    last.next.bead

let tail (last : Necklace<'elem>) : Necklace<'elem> =
    swap (last, last.next)
    last

let rec replicate (n : Int) (a :'a) : Necklace<'a> =  // a necklace with n + 1 beads
    if n = 0 then single a else append (replicate (n - 1) a, single a)


// ------
let create<'elem> (size: Int): Necklace<'elem option> =
    let mutable buffer = replicate (size-1) None
    buffer

let put (buffer: Necklace<'elem option> ref) (e: 'elem): unit =
    (!buffer).bead <- Some e
    buffer := (!buffer).next


let rec get (buffer: Necklace<'elem option> ref) (size: Int): 'elem =
    if (!buffer).bead = None then (if size > 0 then get (ref (!buffer).next) (size-1) else raise BufferEmptyTest)
    else let res = (!buffer).bead
         (!buffer).bead <- None
         match res with
         | None -> raise BufferEmptyTest
         | Some x -> x

let r2nBuffer<'a> (r: RingBuffer<'a>): Necklace<Option<'a>> =
    //let len = max r.buffer.Length 1 // generiere keine leeren RingBuffer (ist in TestInput behoben)
    let len = r.buffer.Length
    let n = ref (create<'a> len)
    let offset = fun (i: Int) -> (!r.readPos  + i) % len
    let rec help (i: Int) =
        if i < !r.size then 
            put n r.buffer.[offset i]
            help (i+1)
    help 0
    !n

// ------


[<StructuredFormatDisplay("{ToString}")>]
type TestInput<'a> =
    | TI of RingBuffer<'a> * string
    member this.ToString: string =
        let (TI (_, s)) = this
        s


type ArbitraryModifiers =
    inherit NatModifier

    static member TestInput<'a> (): Arbitrary<TestInput<'a>> =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<Array<'a> * Int * Int>
        |> FsCheck.FSharp.Arb.filter (fun (buffer, size, readPos) -> size <= buffer.Length && readPos < buffer.Length && size >= 0 && readPos >= 0)
        |> FsCheck.FSharp.Arb.convert
            (fun (buffer, size, readPos) ->
                let rb = { buffer=buffer; size=ref size; readPos=ref readPos }
                TI (rb, $"%A{rb}"))
            (fun (TI (i, _)) -> (i.buffer, !i.size, !i.readPos)) 


[<TestClass>]
type RingBufferTests () =
    let config: Config =
        Config
            .QuickThrowOnFailure
            .WithArbitrary([typeof<ArbitraryModifiers>])

    let ex1 = fun () -> { buffer = [|0; 0; 0|]; size = ref 0; readPos = ref 0 }
    let ex2 = fun () -> { buffer = [|1; 2; 3|]; size = ref 1; readPos = ref 0 }
    let ex3 = fun () -> { buffer = [|7; 3; 6; 1; 20; 15; 17; 4; 9; 12|]; size = ref 6; readPos = ref 7 }


    // ------------------------------------------------------------------------
    // a)

    [<TestMethod; Timeout(1000)>]
    member _.``a) Beispiel`` (): unit =
        let capacity = 10
        let rb = Student.RingBuffer.create<Int> capacity
        <@ Array.length rb.buffer = capacity @> -?> "Kapazität des Puffers stimmt nicht."
        <@ !rb.size = 0 @>                      -?> "size stimmt nicht"
        <@ !rb.readPos = 0 @>                   -?> "readPos stimmt nicht"

    [<TestMethod; Timeout(1000)>]
    member _.``a) Zufall`` (): unit =
        Assertify.Check (
            <@ fun (n: Nat) ->
                if n > 0N then
                    let capacity = int n
                    let rb = Student.RingBuffer.create<String> capacity
                    <@ Array.length rb.buffer = capacity @> -?> "Kapazität des Puffers stimmt nicht."
                    <@ !rb.size = 0 @> -?> "size stimmt nicht"
                    <@ !rb.readPos = 0 @> -?> "readPos stimmt nicht" @>,
            config.WithMaxTest 1000
        )


    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 1`` (): unit =
        let ex = ex1 ()
        try
            Student.RingBuffer.get ex |> ignore
            (!!) "Keine RingEmpty Ausnahme geworfen."
        with
        | RingEmpty -> ()
        (?) <@ ex = ex1 () @>

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 2`` (): unit =
        let ex = ex2 ()
        (?) <@ Student.RingBuffer.get ex = 1 @>
        <@ !ex.readPos = 1 @> -?> "readPos stimmt nicht"
        <@ !ex.size = 0 @> -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."

    [<TestMethod; Timeout(1000)>]
    member _.``b) Beispiel 3`` (): unit =
        let ex = ex3()
        let es = [4; 9; 12; 7; 3; 6]
        let count = ref (List.length es)
        let expectedReadPos = ref 7
        for expected in es do
            count := !count - 1
            expectedReadPos := (!expectedReadPos + 1) % ex.buffer.Length
            (?) <@ Student.RingBuffer.get ex = expected @>
            <@ !ex.readPos = !expectedReadPos @> -?> "readPos stimmt nicht"
            <@ !ex.size = !count @> -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."
        try
            Student.RingBuffer.get ex |> ignore
            (!!) "Ringpuffer sollte leer sein; keine Exception geworfen."
        with
        | RingEmpty -> ()

    [<TestMethod; Timeout(10000)>]
    member _.``b) Zufall: Array von n Zufallszahlen, size=n, readPos=0. get bis size=0 soll alle Inhalte des ursprünglichen Arrays ergeben`` (): unit =
        Assertify.Check (
            <@ fun (ar: Array<Int>) ->
                let rb = { buffer=ar; size=ref ar.Length; readPos=ref 0 }
                let rec help (idx: Int) =
                    let sizeVorher = !rb.size
                    try
                      let e = Student.RingBuffer.get rb
                      <@ e = ar.[idx] @> -?> $"get sollte Array Element an Stelle %s{string idx} ausgeben."
                      <@ !rb.size = sizeVorher-1 @> -?> "Anzahl enthaltener Elemente ist nicht um 1 kleiner geworden."
                    with
                    | RingEmpty -> <@ idx = ar.Length @> -?> "get liefert None obwohl noch Elemente vorhanden sind."
                    if idx < ar.Length then help (idx + 1) else
                        try
                            Student.RingBuffer.get rb |> ignore
                            (!!) "Ringpuffer gibt Elemente zurück, obwohl None erwartet wird."
                        with
                        | RingEmpty -> ()
                help 0 @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout(10000)>]
    member _.``b) Zufall RingBuffer: ein get`` (): unit =
        Assertify.Check (
            <@ fun ( TI (rb, _): TestInput<Int> ) ->
                let n = ref (r2nBuffer rb)
                try
                    <@ Student.RingBuffer.get rb = get n rb.buffer.Length @> -?> "Hinweis: null in dieser Fehlermeldung ist None"
                with
                | BufferEmptyTest ->
                    try
                        Student.RingBuffer.get rb |> ignore
                        (!!) "Exception nicht geworfen, obwohl Buffer leer."
                    with
                    | BufferEmpty -> ()
                | BufferEmpty -> (!!) "Exception geworfen, obwohl Buffer nicht leer." @>,
            config.WithMaxTest 1000
        )

    [<TestMethod; Timeout(10000)>]
    member _.``b) Zufall RingBuffer: size gets`` (): unit =
        Assertify.Check (
            <@ fun ( TI (rb, _): TestInput<String> ) ->
                let n = ref (r2nBuffer rb)
                for i in 0..!rb.size do
                    try
                        <@ Student.RingBuffer.get rb = get n (rb.buffer.Length) @> -?> "Hinweis: null in dieser Fehlermeldung ist None"
                    with
                    | BufferEmptyTest ->
                        try
                            Student.RingBuffer.get rb |> ignore
                            (!!) "Exception nicht geworfen, obwohl Buffer leer."
                        with
                        | BufferEmpty -> ()
                    | BufferEmpty -> (!!) "Exception geworfen, obwohl Buffer nicht leer." @>,
            config.WithMaxTest 1000
        )


    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout(1000)>]
    member _.``c) Beispiel 1`` (): unit =
        let ex = ex1()
        Student.RingBuffer.put ex 30
        <@ ex.buffer.[0] = 30 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 1 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 0 @> -?> "readPos wurde verändert"

    [<TestMethod; Timeout(1000)>]
    member _.``c) Beispiel 2`` (): unit =
        let ex = ex2()
        Student.RingBuffer.put ex 30
        <@ ex.buffer.[1] = 30 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 2 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 0 @> -?> "readPos wurde verändert"
        Student.RingBuffer.put ex 40
        <@ ex.buffer.[2] = 40 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 3 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 0 @> -?> "readPos wurde verändert"
        try
            Student.RingBuffer.put ex 50
            (!!) "Element wurde in vollen Buffer eingefügt."
        with
        | BufferFull -> ()
        <@ !ex.size = 3 @> -?> "size nicht korrekt (soll nicht Größer sein als die Kapazität des Ringpuffers)."
        <@ !ex.readPos = 0 @> -?> "readPos wurde verändert"

    [<TestMethod; Timeout(1000)>]
    member _.``c) Beispiel 3`` (): unit =
        let ex = ex3()
        Student.RingBuffer.put ex 30
        <@ ex.buffer.[3] = 30 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 7 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 7 @> -?> "readPos wurde verändert"
        Student.RingBuffer.put ex 40
        <@ ex.buffer.[4] = 40 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 8 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 7 @> -?> "readPos wurde verändert"
        Student.RingBuffer.put ex 50
        <@ ex.buffer.[5] = 50 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 9 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 7 @> -?> "readPos wurde verändert"
        Student.RingBuffer.put ex 60
        <@ ex.buffer.[6] = 60 @> -?> "Element nicht korrekt eingefügt."
        <@ !ex.size = 10 @> -?> "size wurde nicht erhöht."
        <@ !ex.readPos = 7 @> -?> "readPos wurde verändert"
        try
            Student.RingBuffer.put ex 70
            (!!) "Element wurde in vollen Buffer eingefügt."
        with
        | BufferFull -> ()
        <@ !ex.size = 10 @> -?> "size nicht korrekt (soll nicht größer sein als die Kapazität des Ringpuffers)."
        <@ !ex.readPos = 7 @> -?> "readPos wurde verändert"

    [<TestMethod; Timeout(10000)>]
    member _.``c) Zufall: (setzt voraus, dass get funktioniert)`` (): unit =
        Assertify.Check (
            <@ fun ( TI (rb, _): TestInput<Int>, elems: Int list ) ->
                let sizeBegin = !rb.size 
                let n = ref (r2nBuffer rb)
                let possibleInsertions = rb.buffer.Length - !rb.size
                let truncatedElems = List.truncate possibleInsertions elems
                for e in truncatedElems do // alle elems einfügen
                    try
                        Student.RingBuffer.put rb e
                        put n e
                    with
                    | BufferFull -> (!!) "Ausnahme geworfen, obwohl Buffer noch nicht voll ist."
                if List.length truncatedElems < List.length elems then
                    try
                        Student.RingBuffer.put rb 0
                        (!!) "Element eingefügt, obwohl Buffer voll ist."
                    with
                    | BufferFull -> ()
                let sizeEnd = min (sizeBegin + List.length truncatedElems) rb.buffer.Length
                for i in 0..sizeEnd do // alle verfügbaren lesen
                    try (?) <@ Student.RingBuffer.get rb = get n (rb.buffer.Length) @> with
                    | BufferEmptyTest ->
                        try
                            Student.RingBuffer.get rb |> ignore
                            (!!) "get wirft keine Ausnahme trotz leeren Buffers."
                        with
                        | BufferEmpty -> ()
                    | BufferEmpty -> (!!) "Ausnahme geworfen, obwohl Buffer nicht leer ist." @>,
            config.WithMaxTest 1000
        )

