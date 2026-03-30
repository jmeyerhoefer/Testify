namespace GdP23.S09.A3.Template

module Tests =

    open Microsoft.VisualStudio.TestTools.UnitTesting
    open FsCheck
    open Model
    open Helpers
    open Program

    type AlphabetAB = | A | B

    type AlphabetVWXYZ = | V | W | X | Y | Z


    // Generiere eine anonyme F# Funktion für den Automaten
    #nowarn "40"
    let compile<'T when 'T: comparison> (r: Reg<'T>): ('T list -> bool) =
        let r = simplify r
        let choices = cases<'T>()
        let automaton = calculateAutomaton r |> Map.toSeq
        let stateNumbers = automaton |> Seq.mapi (fun i (r, _) -> (r, i)) |> Map.ofSeq
        let rec compiled: ('T list -> bool) array =
            automaton |> Seq.map (
                fun (r, (transitions, isFinite)) ->
                    let transitions =
                        choices |> List.fold (
                            fun transitions' x ->
                                match transitions |> Map.tryFind x with
                                | Some r' ->
                                    match stateNumbers |> Map.tryFind r' with
                                    | Some i -> transitions' |> Map.add x i
                                    | None ->
                                        failwithf "Rechtsfaktor %A\\%s ist %s aber dieser Zustand ist nicht im Automat enthalten!"
                                            x
                                            (formatRegex r)
                                            (formatRegex r')
                                | None ->
                                    failwithf "Die Transition für den Rechtsfaktor %A\\%s fehlt!"
                                        x
                                        (formatRegex r)
                        ) Map.empty
                    fun input ->
                        match input with
                        | [] -> isFinite
                        | x::rest ->
                            let i = transitions |> Map.find x
                            compiled.[i] rest
            )
            |> Seq.toArray
        match stateNumbers |> Map.tryFind r with
        | Some i -> compiled.[i]
        | None -> failwithf "Der initiale reguläre Ausdruck %s ist nicht im Automat enthalten!" (formatRegex r)


    let rec generateRegex<'T> (size: int): Gen<Reg<'T>> =
        gen {
            // 0: Eps
            // 1: Empty
            // 2: Sym
            // 3: Cat
            // 4: Alt
            // 5: Rep
            let choices = [0; 1; 2; 2; 2; 2; 2]
            let choices =
                if size <= 0 then choices
                else 3::3::4::4::5::choices
            let! choice = Gen.elements choices
            match choice with
            | 0 -> return Eps
            | 1 -> return Empty
            | 2 ->
                let! symbol = Gen.elements (cases<'T>())
                return Sym symbol
            | 3 ->
                let! left = generateRegex (size / 2)
                let! right = generateRegex (size / 2)
                return Cat (left, right)
            | 4 ->
                let! left = generateRegex (size / 2)
                let! right = generateRegex (size / 2)
                return Alt (left, right)
            | _ ->
                let! r = generateRegex (size / 2)
                return Rep r
        }


    type ArbitraryModifiers =
        static member Reg<'T>() =
            Arb.fromGen << Gen.sized <| generateRegex


    [<TestClass>]
    type Tests() =
        let config = Config.QuickThrowOnFailure.WithArbitrary [typeof<ArbitraryModifiers>]

        let abstar: Reg<AlphabetAB> = Rep (Cat (Sym A, Sym B))

        [<TestMethod>] [<Timeout(1000)>]
        member this.``nullable Beispiel 1`` (): unit =
            Assert.IsTrue(nullable abstar)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``nullable Beispiel 2`` (): unit =
            Assert.IsTrue(nullable Eps)

        [<TestMethod>] [<Timeout(1000)>]
        member this.``nullable Beispiel 3`` (): unit =
            Assert.IsFalse(nullable (Sym A))

        [<TestMethod>] [<Timeout(1000)>]
        member this.``divide Beispiel 1`` (): unit =
            let res = divide A (Sym A)
            Assert.IsTrue(
                simplify res = Eps,
                sprintf "Ergebnis %A ist falsch!" res
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``divide Beispiel 2`` (): unit =
            let res = divide B (Sym A)
            Assert.IsTrue(
                simplify res = Empty,
                sprintf "Ergebnis %A ist falsch!" res
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``divide Beispiel 3`` (): unit =
            let res = divide A (Cat (Sym A, Sym B))
            Assert.IsTrue(
                simplify res = Sym B,
                sprintf "Ergebnis %A ist falsch!" res
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``divide Beispiel 4`` (): unit =
            let res = divide A abstar
            Assert.IsTrue(
                simplify res = Cat (Sym B, abstar),
                sprintf "Ergebnis %A ist falsch!" res
            )

        [<TestMethod>] [<Timeout(1000)>]
        member this.``accept float Beispiele`` (): unit =
            let accept = compile floatRegex
            Assert.IsTrue(accept [Zero; Dot; One; Zero; One; Zero], "0.1010 nicht erkannt")
            Assert.IsTrue(accept [Dot; One; Zero; One; Zero], ".1010 nicht erkannt")
            Assert.IsTrue(accept [One; Dot], "1. nicht erkannt")
            Assert.IsFalse(accept [Zero; One; Dot; One], "01.1 fälschlicherweise erkannt")
            Assert.IsFalse(accept [Dot], ". fälschlicherweise erkannt")

        [<TestMethod>] [<Timeout(10000)>]
        member this.``accept float Zufall`` (): unit =
            let accept = compile floatRegex
            Check.One ({Config.QuickThrowOnFailure with MaxTest = 10000}, fun (input: Alphabet list) ->
                let expected =
                    // Mindestens eine Ziffer vor oder nach dem Dezimalpunkt
                    input.Length >= 2
                    // Genau ein Dezimalpunkt
                    && (List.filter ((=) Dot) input).Length = 1
                    // Keine führende Nullen
                    &&
                        let front = List.takeWhile ((<>) Dot) input
                        front.Length <= 1 || front.Head = One
                Assert.AreEqual(expected, accept input)
            )

        [<TestMethod>] [<Timeout(10000)>]
        member this.``accept abstar Zufall`` (): unit =
            let rec h input =
                match input with
                | [] -> true
                | A::B::rest -> h rest
                | _ -> false
            let accept = compile abstar
            Check.One ({Config.QuickThrowOnFailure with MaxTest = 10000}, fun (input: AlphabetAB list) ->
                Assert.AreEqual(h input, accept input)
            )

        [<TestMethod>] [<Timeout(20000)>]
        member this.``calculateAutomaton Zufall`` (): unit =
            Check.One ({Config.QuickThrowOnFailure with EndSize = 20; MaxTest = 300}, fun (r: Reg<AlphabetVWXYZ>) ->
                compile r |> ignore
            )

