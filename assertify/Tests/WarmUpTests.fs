module Tests.WarmUpTests


open Assertify
open Tests.WarmUpTestUtilsIO


[<StructuredFormatDisplay("{s}")>]
type SafeString = SS of s: string


type ArbitraryModifiers =
    inherit NatModifier

    static member SafeString (): Arbitrary<SafeString> =
        FsCheck.FSharp.ArbMap.defaults
        |> FsCheck.FSharp.ArbMap.arbitrary<string>
        |> FsCheck.FSharp.Arb.filter (not << isNull)
        |> FsCheck.FSharp.Arb.convert (String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) (id)
        |> FsCheck.FSharp.Arb.convert (SS) (fun (SS s) -> s)


[<TestClass>]
type WarmUpTests () =
    // TODO: Usage???
    let config: Config =
        Config
            .QuickThrowOnFailure
            .WithArbitrary([typeof<ArbitraryModifiers>])

    let ioTimeout: int = 1000

    // ------------------------------------------------------------------------
    // b)

    [<TestMethod; Timeout(10000)>]
    member _.``b) Beispiele ungültige natürliche Zahl`` (): unit =
        executeIOTest (
            (fun () -> let x = Student.Program.queryNat "Eine Nachricht: " in (?) <@ x = 4711N @>),
            fun io ->
                io.timeout <- ioTimeout
                io.Expect("Eine Nachricht: ")
                io.WriteLine("abc")
                io.ExpectLine("Eingabe ist keine natuerliche Zahl!")
                io.Expect("Eine Nachricht: ")
                io.WriteLine("")
                io.ExpectLine("Eingabe ist keine natuerliche Zahl!")
                io.Expect("Eine Nachricht: ")
                io.WriteLine("-4711")
                io.ExpectLine("Eingabe ist keine natuerliche Zahl!")
                io.Expect("Eine Nachricht: ")
                io.WriteLine(".")
                io.ExpectLine("Eingabe ist keine natuerliche Zahl!")
                io.Expect("Eine Nachricht: ")
                io.WriteLine("123a+5")
                io.ExpectLine("Eingabe ist keine natuerliche Zahl!")
                io.Expect("Eine Nachricht: ")
                io.WriteLine("4711") // beenden
        )

    [<TestMethod; Timeout(10000)>]
    member _.``b) Zufall gültige natürliche Zahl`` (): unit =
        Assertify.Check
            <@ fun (num: Nat) (msg: SafeString) ->
                let (SS msg) = msg
                executeIOTest (
                    (fun () -> let x = Student.Program.queryNat msg in (?) <@ x = num @>),
                    fun io ->
                        io.timeout <- ioTimeout
                        if msg <> "" then io.Expect(msg)
                        io.WriteLine(string (int num))
                ) @>

    // ------------------------------------------------------------------------
    // c)

    [<TestMethod; Timeout(10000)>]
    member _.``c) Zufall main`` (): unit =
        Assertify.Check
            <@ fun (n1: Nat) (n2: Nat) (n3: Nat) ->
                executeIOTest (
                    (fun () -> Student.Program.main ()),
                    fun io ->
                        io.timeout <- ioTimeout
                        io.ExpectLine ("Bitte geben Sie drei natuerliche Zahlen ein.")
                        io.Expect ("Erste Zahl: ")
                        io.WriteLine(string (int n1))
                        io.Expect ("Zweite Zahl: ")
                        io.WriteLine(string (int n2))
                        io.Expect ("Dritte Zahl: ")
                        io.WriteLine(string (int n3))
                        io.Expect ("Minimum: ")
                        match io.TryReadLine () with
                        | CompletedLine s -> let x = readNat s in (?) <@ x <= n1 && x <= n2 && x <= n3 = true @>
                        | IncompleteLineEOF s -> Assertify.Fail $"Zeilenumbruch fehlt. %A{s}"
                        | IncompleteLineTimeout s -> Assertify.Fail $"Timeout überschritten. %A{s}"
                ) @>
