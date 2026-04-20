namespace Testify

open System.Threading


type TestifyFailureDispatchPayload =
    {
        Report: TestifyFailureReport
        Rendered: string
        OutputFormat: OutputFormat
    }


[<RequireQualifiedAccess>]
module TestifyRunnerContext =
    type private RunnerContext =
        {
            FailureDispatcher: TestifyFailureDispatchPayload -> unit
        }

    let private current = AsyncLocal<RunnerContext option> ()

    let private defaultFailureDispatcher
        (payload: TestifyFailureDispatchPayload)
        : unit =
        match payload.OutputFormat with
        | OutputFormat.Json -> failwith payload.Rendered
        | OutputFormat.WallOfText -> failwith ("\n\n" + payload.Rendered)
        | _ -> failwith ("\n\n" + payload.Rendered)

    let withFailureDispatcher
        (dispatcher: TestifyFailureDispatchPayload -> unit)
        (action: unit -> 'T)
        : 'T =
        let previous = current.Value
        current.Value <- Some { FailureDispatcher = dispatcher }

        try
            action ()
        finally
            current.Value <- previous

    let withFailureDispatcherAsync
        (dispatcher: TestifyFailureDispatchPayload -> unit)
        (action: Async<'T>)
        : Async<'T> =
        async {
            let previous = current.Value
            current.Value <- Some { FailureDispatcher = dispatcher }

            try
                return! action
            finally
                current.Value <- previous
        }

    let dispatchFailure
        (payload: TestifyFailureDispatchPayload)
        : unit =
        match current.Value with
        | Some context -> context.FailureDispatcher payload
        | None -> defaultFailureDispatcher payload
