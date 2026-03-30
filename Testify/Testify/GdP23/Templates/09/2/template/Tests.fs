namespace GdP23.S09.A2.Template

module Tests =
    open Mini
    open Types
    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Swensen.Unquote
    open TestUtilsIO

    [<StructuredFormatDisplay("{s}")>]
    type SafeString = SS of s: string

    [<StructuredFormatDisplay("{s}")>]
    type Schlitten = SL of s: List<Karte>

    type ArbitraryModifiers =
        static member Nat() =
            Arb.from<bigint>
            |> Arb.filter (fun i -> i >= 0I)
            |> Arb.convert (Nat.Make) (fun n -> n.ToBigInteger())

        static member SafeString() =
            Arb.from<string>
            |> Arb.filter (not << isNull)
            |> Arb.convert (String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) (id)
            |> Arb.convert (SS) (fun (SS s) -> s)

        // Der Kartenschlitten muss bei einer Runde mit einem Spieler maximal 42 Karten enthalten
        static member Schlitten() =
            Gen.listOfLength 42 Arb.generate
            |> Arb.fromGen

    // erstellt aus einer Liste von Karten eine Funktion vom Typ Unit -> Karte,
    // die bei jedem Aufruf die jeweils nächste Karte in der Liste zurückgibt
    type Kartenlistenkonverter (ks: List<Karte>) =
        class
            let mutable mks = ks
            let mutable mem = []
            member self.peek() =
                List.head mks
            member self.counter() =
                mem
            member self.zieheKarte() =
                let h = List.head mks
                mem <- h::mem // zähle Karten
                mks <- List.tail mks
                h
        end

    [<TestClass>]
    type Tests() =
        do Arb.register<ArbitraryModifiers>() |> ignore

        let ioTimeout = 1000

        // ------------------------------------------------------------------------
        // a)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Beispiele kartenwert`` (): unit =
            test <@ BlackJack.kartenwert Fuenf = [5N] @>
            test <@ BlackJack.kartenwert Dame = [10N] @>
            test <@ BlackJack.kartenwert Ass |> List.contains 1N  = true @>
            test <@ BlackJack.kartenwert Ass |> List.contains 11N = true @>

        [<TestMethod>] [<Timeout(1000)>]
        member this.``a) Zufallstest kartenwert`` (): unit =
            Check.QuickThrowOnFailure(fun (k: Karte) ->
                let expected =
                    match k with
                    | Ass -> 2
                    | _   -> 1
                Assert.AreEqual(expected, BlackJack.kartenwert k |> List.length)
            )

        // ------------------------------------------------------------------------
        // b)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``b) Beispiele kartenPunkte`` (): unit =
            test <@ BlackJack.kartenPunkte [Zwei; Drei] = [5N] @>
            test <@ BlackJack.kartenPunkte [Bube; Dame; Koenig] = [30N] @>
            test <@ BlackJack.kartenPunkte [Ass; Ass; Ass] |> List.sort = [3N; 13N; 23N; 33N] @>
            test <@ BlackJack.kartenPunkte [Ass; Fuenf; Ass] |> List.sort = [7N; 17N; 27N] @>
            test <@ BlackJack.kartenPunkte [Vier; Sechs; Sieben; Acht; Neun; Zehn] |> List.sort = [44N] @>

        // ------------------------------------------------------------------------
        // c)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``c) Beispiele punkteBerechnen`` (): unit =
            test <@ BlackJack.punkteBerechnen [Zwei; Drei] = 5N @>
            test <@ BlackJack.punkteBerechnen [Bube; Dame; Koenig] = 30N @>
            test <@ BlackJack.punkteBerechnen [Ass; Ass; Ass] = 13N @>
            test <@ BlackJack.punkteBerechnen [Ass; Fuenf; Ass] = 17N @>
            test <@ BlackJack.punkteBerechnen [Ass; Bube; Dame; Koenig] = 31N @>

        // ------------------------------------------------------------------------
        // d)
        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) Zufall zugCroupier 1`` (): unit =
            Check.QuickThrowOnFailure(fun (sl: Schlitten) ->
                let (SL kartenschlitten) = sl
                let klk = Kartenlistenkonverter kartenschlitten
                let zieheKarte = klk.zieheKarte
                let zug = BlackJack.zugCroupier zieheKarte [Zwei]
                match zug with
                | None -> Assert.Fail(sprintf "Croupier haette eine Karte ziehen muessen")
                | Some zug1Karte ->
                    match (BlackJack.zugCroupier zieheKarte (zug1Karte::[Zwei])) with
                    | None -> Assert.Fail(sprintf "Um auf mindestens 17 Punkte zu kommen sind zusaetzlich zur Zwei noch zwei weitere Karten nötig.")
                    | Some zug2Karte -> ()
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``d) Zufall zugCroupier 2`` (): unit =
            Check.QuickThrowOnFailure(fun (sl: Schlitten) (ks: List<Karte>) ->
                let (SL kartenschlitten) = sl
                let klk = Kartenlistenkonverter kartenschlitten
                let zieheKarte = klk.zieheKarte
                let c0 = klk.counter ()
                let zug = BlackJack.zugCroupier klk.zieheKarte ks
                let c1 = klk.counter ()
                match zug with
                | None ->
                    Assert.IsTrue(BlackJack.punkteBerechnen ks >= 17N)
                    Assert.AreEqual(c0, c1, "zieheKarte () wurde aufgerufen, obwohl der Croupier nicht gezogen hat. Haben Sie evtl. eine let-Bindung verwendet, die ausgewertet wird, bevor sich der Croupier entscheidet, ob er noch eine Karte ziehen soll?")
                | Some zugKarte -> Assert.IsTrue(BlackJack.punkteBerechnen ks < 17N)
            )

        // ------------------------------------------------------------------------
        // e)
        [<TestMethod>] [<Timeout(10000)>]
        member this.``e) Beispiel 1`` (): unit =
            Check.QuickThrowOnFailure (fun (sl: Schlitten) ->
                let (SL kartenschlitten) = sl
                executeIOTest (
                    (fun () ->
                        let klk = Kartenlistenkonverter kartenschlitten
                        let peek = klk.peek ()
                        let x = BlackJack.zugSpieler klk.zieheKarte [] in
                        match x with
                        | None -> Assert.Fail(sprintf "Der Spieler wollte eine Karte ziehen, hat aber keine erhalten.")
                        | Some karte -> Assert.AreEqual(peek, karte)
                        |> ignore),
                    fun io ->
                        io.timeout <- ioTimeout
                        io.ExpectLine("")
                        io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
                        io.WriteLine("42")
                        io.ExpectLine("Ungueltige Eingabe.")
                        io.ExpectLine("")
                        io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
                        io.WriteLine("j") // beenden
                )
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``e) Beispiel 2`` (): unit =
            Check.QuickThrowOnFailure(fun () ->
                executeIOTest (
                    (fun () ->
                        let klk = Kartenlistenkonverter [Zwei; Drei; Vier]
                        let x = BlackJack.zugSpieler klk.zieheKarte [Ass; Koenig] in
                        match x with
                        | None -> ()
                        | Some karte -> Assert.Fail(sprintf "Der Spieler hatte schon 21 Punkte, er soll nicht noch eine Karte ziehen.")
                        |> ignore),
                    fun io ->
                        io.timeout <- ioTimeout
                        ()
                )
            )

        // ------------------------------------------------------------------------
        // f)
        [<TestMethod>] [<Timeout(10000)>]
        member this.``f) Zufall`` (): unit =
            Check.QuickThrowOnFailure (fun (sl: Schlitten) (k0: Karte) ->
                let (SL kartenschlitten) = sl
                let mutable gezogeneKarten = []
                executeIOTest (
                    (fun () ->
                        let klk = Kartenlistenkonverter kartenschlitten
                        let punkte = BlackJack.zuegeCroupier klk.zieheKarte [k0] in
                        gezogeneKarten <- klk.counter () @ [k0]
                        Assert.AreEqual(BlackJack.punkteBerechnen gezogeneKarten, punkte)
                        |> ignore),
                    fun io ->
                        io.timeout <- ioTimeout
                        io.ExpectLine("")
                        io.Expect("Karten des Croupiers: ")
                )
            )

        // ------------------------------------------------------------------------
        // g)
        [<TestMethod>] [<Timeout(10000)>]
        member this.``g) Beispiel`` (): unit =
            executeIOTest (
                (fun () ->
                    let klk = Kartenlistenkonverter [Koenig; Drei; Zehn]
                    let punkte = BlackJack.zuegeSpieler klk.zieheKarte [Zwei; Drei] in
                    Assert.AreEqual(18N, punkte)
                    |> ignore),
                fun io ->
                    io.timeout <- ioTimeout
                    io.ExpectLine("")
                    io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
                    io.WriteLine("j")
                    io.ExpectLine("Sie haben Koenig gezogen, damit haben Sie folgende Karten: [Koenig; Zwei; Drei]")
                    io.ExpectLine("")
                    io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
                    io.WriteLine("j")
                    io.ExpectLine("Sie haben Drei gezogen, damit haben Sie folgende Karten: [Drei; Koenig; Zwei; Drei]")
                    io.ExpectLine("")
                    io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
                    io.WriteLine("n")
            )

        // // ------------------------------------------------------------------------
        // // h)
        // [<TestMethod>] [<Timeout(10000)>]
        // member this.``h) Beispiel 1`` (): unit =
        //     executeIOTest (
        //         (fun () ->
        //             let klk = Kartenlistenkonverter [Drei; Vier; Zwei; Neun; Dame; Ass; Koenig; Ass; Koenig]
        //             BlackJack.spiel klk.zieheKarte
        //             |> ignore),
        //         fun io ->
        //             io.timeout <- ioTimeout
        //             io.ExpectLine("Lista Black Jack")
        //             io.ExpectLine("================")
        //             io.ExpectLine("Karten des Croupiers: [Vier]")
        //             io.ExpectLine("Ihre Karten: [Zwei; Drei]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Neun gezogen, damit haben Sie folgende Karten: [Neun; Zwei; Drei]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Dame gezogen, damit haben Sie folgende Karten: [Dame; Neun; Zwei; Drei]")
        //             io.ExpectLine("Sie haben sich ueberkauft (24N Punkte).")
        //     )

        // [<TestMethod>] [<Timeout(10000)>]
        // member this.``h) Beispiel 2`` (): unit =
        //     executeIOTest (
        //         (fun () ->
        //             let klk = Kartenlistenkonverter [Vier; Zehn; Ass; Acht; Acht; Zehn; Zehn; Ass; Koenig; Ass; Koenig]
        //             BlackJack.spiel klk.zieheKarte
        //             |> ignore),
        //         fun io ->
        //             io.timeout <- ioTimeout
        //             io.ExpectLine("Lista Black Jack")
        //             io.ExpectLine("================")
        //             io.ExpectLine("Karten des Croupiers: [Zehn]")
        //             io.ExpectLine("Ihre Karten: [Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Acht gezogen, damit haben Sie folgende Karten: [Acht; Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Acht gezogen, damit haben Sie folgende Karten: [Acht; Acht; Ass; Vier]")
        //             io.ExpectLine("")
        //             io.ExpectLine("Karten des Croupiers: [Zehn; Zehn]")
        //             io.ExpectLine("Sie haben gewonnen.")
        //     )

        // [<TestMethod>] [<Timeout(10000)>]
        // member this.``h) Beispiel 3`` (): unit =
        //     executeIOTest (
        //         (fun () ->
        //             let klk = Kartenlistenkonverter [Ass; Sieben; Koenig; Sieben; Sieben; Ass; Koenig; Ass; Koenig]
        //             BlackJack.spiel klk.zieheKarte
        //             |> ignore),
        //         fun io ->
        //             io.timeout <- ioTimeout
        //             io.ExpectLine("Lista Black Jack")
        //             io.ExpectLine("================")
        //             io.ExpectLine("Karten des Croupiers: [Sieben]")
        //             io.ExpectLine("Ihre Karten: [Koenig; Ass]")
        //             io.ExpectLine("")
        //             io.ExpectLine("Karten des Croupiers: [Sieben; Sieben; Sieben]")
        //             io.ExpectLine("Das Spiel endet unentschieden.")
        //     )

        // [<TestMethod>] [<Timeout(10000)>]
        // member this.``h) Beispiel 4`` (): unit =
        //     executeIOTest (
        //         (fun () ->
        //             let klk = Kartenlistenkonverter [Vier; Zehn; Ass; Fuenf; Ass; Koenig; Ass; Koenig]
        //             BlackJack.spiel klk.zieheKarte
        //             |> ignore),
        //         fun io ->
        //             io.timeout <- ioTimeout
        //             io.ExpectLine("Lista Black Jack")
        //             io.ExpectLine("================")
        //             io.ExpectLine("Karten des Croupiers: [Zehn]")
        //             io.ExpectLine("Ihre Karten: [Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Fuenf gezogen, damit haben Sie folgende Karten: [Fuenf; Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("n")
        //             io.ExpectLine("")
        //             io.ExpectLine("Karten des Croupiers: [Ass; Zehn]")
        //             io.ExpectLine("Sie haben verloren.")
        //     )

        // [<TestMethod>] [<Timeout(10000)>]
        // member this.``h) Beispiel 5`` (): unit =
        //     executeIOTest (
        //         (fun () ->
        //             let klk = Kartenlistenkonverter [Vier; Zehn; Ass; Fuenf; Fuenf; Zehn; Koenig; Ass; Koenig]
        //             BlackJack.spiel klk.zieheKarte
        //             |> ignore),
        //         fun io ->
        //             io.timeout <- ioTimeout
        //             io.ExpectLine("Lista Black Jack")
        //             io.ExpectLine("================")
        //             io.ExpectLine("Karten des Croupiers: [Zehn]")
        //             io.ExpectLine("Ihre Karten: [Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("j")
        //             io.ExpectLine("Sie haben Fuenf gezogen, damit haben Sie folgende Karten: [Fuenf; Ass; Vier]")
        //             io.ExpectLine("")
        //             io.Expect("Moechten Sie eine weitere Karte ziehen? [j/n] ")
        //             io.WriteLine("n")
        //             io.ExpectLine("")
        //             io.ExpectLine("Karten des Croupiers: [Zehn; Fuenf; Zehn]")
        //             io.ExpectLine("Der Croupier hat sich ueberkauft, Sie gewinnen.")
        //     )

