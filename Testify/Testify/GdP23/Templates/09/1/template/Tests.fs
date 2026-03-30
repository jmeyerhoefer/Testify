namespace GdP23.S09.A1.Template

module Tests =
    open Mini
    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open TestUtilsIO

    [<StructuredFormatDisplay("{s}")>]
    type SafeString = SS of s: string


    type ArbitraryModifiers =
        static member Nat() =
            FSharp.ArbMap.defaults |> FSharp.ArbMap.arbitrary<bigint>
            |> FSharp.Arb.filter (fun i -> i >= 0I)
            |> FSharp.Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

        static member SafeString() =
            Arb.from<string>
            |> FSharp.Arb.filter (not << isNull)
            |> FSharp.Arb.convert (String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) (id)
            |> FSharp.Arb.convert (SS) (fun (SS s) -> s)


    [<TestClass>]
    type Tests() =
        let config = Config.QuickThrowOnFailure.WithArbitrary [typeof<ArbitraryModifiers>]

        let ioTimeout = 1000


        // ------------------------------------------------------------------------
        // b)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``b) Beispiele ungültige natürliche Zahl`` (): unit =
            executeIOTest (
                (fun () -> let x = Program.queryNat "Eine Nachricht: " in Assert.AreEqual(4711N, x) |> ignore),
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

        [<TestMethod>] [<Timeout(10000)>]
        member this.``b) Zufall gültige natürliche Zahl`` (): unit =
            Check.QuickThrowOnFailure (fun (num: Nat) (msg: SafeString) ->
                let (SS msg) = msg
                executeIOTest (
                    (fun () -> let x = Program.queryNat msg in Assert.AreEqual(num, x) |> ignore),
                    fun io ->
                        io.timeout <- ioTimeout
                        if msg <> "" then io.Expect(msg)
                        io.WriteLine(string (int num))
                )
            )

        // ------------------------------------------------------------------------
        // c)

        [<TestMethod>] [<Timeout(10000)>]
        member this.``c) Zufall main`` (): unit =
            Check.QuickThrowOnFailure (fun (n1: Nat) (n2: Nat) (n3: Nat) ->
                executeIOTest (
                    (fun () -> Program.main () |> ignore),
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
                        | CompletedLine s -> Assert.IsTrue(let x = readNat s in x <= n1 && x <= n2 && x <= n3)
                        | IncompleteLineEOF s -> Assert.Fail(sprintf "Zeilenumbruch fehlt. %A" s)
                        | IncompleteLineTimeout s -> Assert.Fail(sprintf "Timeout überschritten. %A" s)
                )
            )

