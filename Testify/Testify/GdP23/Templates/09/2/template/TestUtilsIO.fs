namespace GdP23.S09.A2.Template

module TestUtilsIO =

    open System
    open System.IO
    open System.IO.Pipes
    open System.Threading
    open System.Threading.Tasks
    open System.Text


    exception InteractionException of string
        with override this.Message = this.Data0


    type TryReadLineResult =
        | CompletedLine of string
        | IncompleteLineEOF of string
        | IncompleteLineTimeout of string

    type TryReadCharResult =
        | Char of char
        | EOF
        | Timeout


    [<AbstractClass>]
    type IOInteractions() =
        /// How long to wait when reading from the programs stdout (in milliseconds).
        member val timeout: int = 1000 with get, set
        /// Write the given string to the programs stdin.
        abstract member Write: string -> unit
        /// Write the given string to the programs stdin, followed by a line terminator.
        abstract member WriteLine: string -> unit
        /// Expect the given string on the programs stdout.
        /// Consume it from the stream if it matches, otherwise throw an InteractionException after the timeout.
        abstract member Expect: expected: string -> unit
        /// Consume a line on the programs stdout.
        /// Throw an InteractionException if it does not match the given string or waiting for a line times out.
        abstract member ExpectLine: expected: string -> unit
        /// Consume a line on the programs stdout and return it.
        /// Throw an InteractionException if waiting for a line times out.
        abstract member ReadLine: unit -> string
        /// Consume a line on the programs stdout and return it.
        /// Return IncompleteLine if waiting for a line times out.
        abstract member TryReadLine: unit -> TryReadLineResult
        /// Consume the programs stdout and return a sequence of all remaining lines.
        abstract member ReadToEnd: unit -> TryReadLineResult seq
        /// Consume a char on the programs stdout and return it.
        /// Return None if waiting for a line times out.
        abstract member TryReadChar: unit -> TryReadCharResult

        member internal this.TimeoutTask (task: Task<'a>): Async<'a option> =
            async {
                use cts = new CancellationTokenSource()
                use timer = Task.Delay(this.timeout, cts.Token)
                let! completed = Async.AwaitTask <| Task.WhenAny(task, timer)
                if completed = (task :> Task) then
                    cts.Cancel()
                    let! result = Async.AwaitTask(task)
                    return Some result
                else return None
            }

    /// Like sprintf, but replace all occurences of \n in format string with Environment.NewLine.
    let private sprintfNL<'T> (format: Printf.StringFormat<'T>): 'T =
        sprintf <| new Printf.StringFormat<'T>(format.Value.Replace("\n", Environment.NewLine))


    let executeIOTest<'a> (program: unit -> unit, test: IOInteractions -> 'a): 'a =
        let realStdin = Console.In
        let realStdout = Console.Out

        try
            use stdinPipe = new AnonymousPipeServerStream(PipeDirection.In)
            use stdoutPipe = new AnonymousPipeServerStream(PipeDirection.Out)
            use progReader = new StreamReader(stdinPipe)
            use progWriter = new StreamWriter(stdoutPipe)
            progWriter.AutoFlush <- true

            use stdinClient = new AnonymousPipeClientStream(PipeDirection.Out, stdinPipe.GetClientHandleAsString())
            use stdoutClient = new AnonymousPipeClientStream(PipeDirection.In, stdoutPipe.GetClientHandleAsString())
            use testReader = new StreamReader(stdoutClient)
            use testWriter = new StreamWriter(stdinClient)
            testWriter.AutoFlush <- true

            let consoleContent = new StringBuilder()
            let mutable pendingReadOperation: Task<char option> option = None

            let io =
                { new IOInteractions() with
                    member this.Write(s) =
                        consoleContent.Append(s) |> ignore
                        testWriter.Write(s)
                    member this.WriteLine(s) =
                        consoleContent.AppendLine(s) |> ignore
                        testWriter.WriteLine(s)
                    member this.Expect(expected) =
                        let sb = new StringBuilder()
                        while string sb <> expected do
                            match this.TryReadChar() with
                            | Char c -> sb.Append(c) |> ignore
                            | reason ->
                                let result = string sb
                                raise <| InteractionException (
                                    if result.Length = 0 then
                                        sprintfNL "Erwartete Ausgabe nicht erhalten (%A)!\nErwartet: %A" reason expected
                                    else if expected.StartsWith(result) then
                                        sprintfNL "Unvollständige Ausgabe erhalten (%A)!\nErwartet: %A\nErhalten: %A" reason expected result
                                    else
                                        sprintfNL "Falsche Ausgabe erhalten!\nErwartet: %A\nErhalten: %A" expected result
                                )
                    member this.ExpectLine(expected) =
                        let result = this.TryReadLine()
                        match result with
                        | CompletedLine line ->
                            if line <> expected then
                                raise <| InteractionException (
                                    if line.Length = 0 then
                                        sprintfNL "Unerwartete leere Ausgabe-Zeile erhalten! Zeilenumbruch zu viel?\nErwartete Zeile: %A" expected
                                    else
                                        sprintfNL "Falsche Ausgabe-Zeile erhalten!\nErwartete Zeile: %A\nErhaltene Zeile: %A" expected line
                                )
                        | IncompleteLineEOF line | IncompleteLineTimeout line ->
                            let reason = match result with IncompleteLineEOF _ -> "EOF" | _ -> "Timeout"
                            raise <| InteractionException (
                                if line.Length = 0 then
                                    sprintfNL "Erwartete Ausgabe-Zeile nicht erhalten (%s)!\nErwartete Zeile: %A" reason expected
                                else if line = expected then
                                    sprintfNL "Zeilenumbruch nach erwarteter Ausgabe-Zeile nicht erhalten (%s)!\nErwartete Zeile: %A" reason expected
                                else if line.StartsWith expected then
                                    sprintfNL "Unvollständige Ausgabe ohne abschließenden Zeilenumbruch erhalten (%s)!\nErwartete Zeile: %A\nErhalten:        %A" reason expected line
                                else
                                    sprintfNL "Falsche Ausgabe ohne abschließenden Zeilenumbruch erhalten (%s)!\nErwartete Zeile: %A\nErhalten:        %A" reason expected line
                            )
                    member this.ReadLine() =
                        let result = this.TryReadLine()
                        match result with
                        | CompletedLine line -> line
                        | IncompleteLineEOF line | IncompleteLineTimeout line ->
                            let reason = match result with IncompleteLineEOF _ -> "EOF" | _ -> "Timeout"
                            raise <| InteractionException (
                                if line.Length = 0 then
                                    sprintfNL "Fehler beim Warten auf Ausgabe (%s)!" reason
                                else
                                    sprintfNL "Fehler beim Warten auf Ausgabe-Zeile (%s)!\nErhaltene, nicht mit Zeilenumbruch beendete Ausgabe: %A" reason line
                            )
                    member this.TryReadLine() =
                        // We cannot use testReader.ReadLine because that blocks and loses input of incomplete lines.
                        let sb = new StringBuilder()
                        let rec loop(lastWasCR: bool): TryReadLineResult =
                            match this.TryReadChar() with
                            | Char '\n' -> CompletedLine (string sb)
                            | Char c ->
                                if lastWasCR then sb.Append('\r') |> ignore
                                if c = '\r' then loop(true)
                                else
                                    sb.Append(c) |> ignore
                                    loop(false)
                            | EOF -> IncompleteLineEOF (string sb)
                            | Timeout -> IncompleteLineTimeout (string sb)
                        loop(false)
                    member this.ReadToEnd() =
                        seq {
                            let result = this.TryReadLine()
                            yield result
                            match result with
                            | CompletedLine _ -> yield! this.ReadToEnd()
                            | _ -> ()
                        }
                    member this.TryReadChar() =
                        let task =
                            match pendingReadOperation with
                            | Some task -> task
                            | _ ->
                                let task =
                                    async {
                                        let buf = Array.zeroCreate<char>(1)
                                        match testReader.Read(buf, 0, 1) with
                                        | 1 -> return Some buf.[0]
                                        | _ -> return None // EOF
                                    }
                                    |> Async.StartAsTask
                                pendingReadOperation <- Some task
                                task
                        match this.TimeoutTask(task) |> Async.RunSynchronously with
                        | Some result ->
                            pendingReadOperation <- None
                            match result with
                            | Some c ->
                                consoleContent.Append(c) |> ignore
                                Char c
                            | None -> EOF
                        | None -> Timeout
                }

            let programTask =
                async {
                    Console.SetOut(progWriter)
                    Console.SetIn(progReader)
                    try
                        program()
                    finally
                        Console.SetOut(realStdout)
                        progWriter.Close()
                }
                |> Async.Catch
                |> Async.StartAsTask

            let testTask =
                async {
                    return test io
                }
                |> Async.Catch
                |> Async.StartAsTask

            // The test task might update the timeout setting, join it first
            let testResult = Async.AwaitTask testTask |> Async.RunSynchronously
            let programResult = programTask |> io.TimeoutTask |> Async.RunSynchronously

            let getConsoleContentMessage(): string =
                let s = string consoleContent
                sprintfNL "--- Inhalt der Konsole ---\n%s%s--- Ende Inhalt der Konsole ---"
                    s
                    (if s.EndsWith("\n") then "" else Environment.NewLine)

            match (programResult, testResult) with
            | (Some (Choice2Of2 ex), _) | (_, Choice2Of2 ex) -> // prefer exception in program over test exception
                raise <| new Exception (sprintfNL "\n%s\n\n" (getConsoleContentMessage()), ex)
            | (None, _) ->
                // side effect: fills consoleContent
                io.ReadToEnd() |> Seq.truncate(10) |> Seq.iter ignore
                failwith (
                    sprintfNL "Das Programm beendet sich nicht, möglicherweise wartet es noch vergeblich auf eine Eingabe.\n%s\n" (getConsoleContentMessage())
                )
            | (_, Choice1Of2 x) -> x

        finally
            Console.SetIn(realStdin)
            Console.SetOut(realStdout)

