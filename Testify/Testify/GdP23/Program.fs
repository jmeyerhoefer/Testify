module Program

open GdP23
open GdP23.DataProcessor
open System

let private defaultMaxParallel : int =
    Environment.ProcessorCount / 2
    |> max 1
    |> min 8

let private usage () : string =
    String.concat
        Environment.NewLine
        [
            "GdP23 snapshot runner"
            ""
            "Options:"
            "  --sheet <id>"
            "  --assignment <id>"
            "  --group-team <group_team>"
            "  --timestamp <yyyyMMddHHmmss>"
            "  --max-parallel <n>"
            "  --max-examples-per-task <n>"
            "  --all-snapshots"
            "  --cleanup"
            "  --cleanup-after"
            "  --force"
            "  --only-materialize"
            "  --only-run"
            "  --only-normalize"
            "  --help"
        ]

let private parseArgs (args: string array) : Result<CliOptions, string> =
    let rec loop index (options: CliOptions) =
        if index >= args.Length then
            Ok options
        else
            match args[index] with
            | "--sheet" when index + 1 < args.Length ->
                loop (index + 2) { options with SheetId = Some args[index + 1] }
            | "--assignment" when index + 1 < args.Length ->
                loop (index + 2) { options with AssignmentId = Some args[index + 1] }
            | "--group-team" when index + 1 < args.Length ->
                loop (index + 2) { options with GroupIdTeamId = Some args[index + 1] }
            | "--timestamp" when index + 1 < args.Length ->
                loop (index + 2) { options with Timestamp = Some args[index + 1] }
            | "--max-parallel" when index + 1 < args.Length ->
                match Int32.TryParse(args[index + 1]) with
                | true, value when value > 0 ->
                    loop (index + 2) { options with MaxParallel = value }
                | _ ->
                    Error $"Invalid value for --max-parallel: {args[index + 1]}"
            | "--max-examples-per-task" when index + 1 < args.Length ->
                match Int32.TryParse(args[index + 1]) with
                | true, value when value > 0 ->
                    loop (index + 2) { options with MaxExamplesPerTask = value }
                | _ ->
                    Error $"Invalid value for --max-examples-per-task: {args[index + 1]}"
            | "--all-snapshots" ->
                loop (index + 1) { options with AllSnapshots = true }
            | "--cleanup" ->
                loop (index + 1) { options with Stage = CliStage.Cleanup }
            | "--cleanup-after" ->
                loop (index + 1) { options with CleanupAfter = true }
            | "--force" ->
                loop (index + 1) { options with Force = true }
            | "--only-materialize" ->
                loop (index + 1) { options with Stage = CliStage.OnlyMaterialize }
            | "--only-run" ->
                loop (index + 1) { options with Stage = CliStage.OnlyRun }
            | "--only-normalize" ->
                loop (index + 1) { options with Stage = CliStage.OnlyNormalize }
            | "--help" ->
                Error(usage ())
            | unknown ->
                Error $"Unknown or incomplete argument: {unknown}{Environment.NewLine}{Environment.NewLine}{usage ()}"

    loop
        0
        ({
            SheetId = None
            AssignmentId = None
            GroupIdTeamId = None
            Timestamp = None
            MaxParallel = defaultMaxParallel
            MaxExamplesPerTask = 10
            AllSnapshots = false
            CleanupAfter = false
            Force = false
            Stage = CliStage.Full
        }: CliOptions)

[<EntryPoint>]
let main (args: string array) : int =
    try
        match parseArgs args with
        | Ok options -> run options
        | Error message ->
            eprintfn "%s" message
            if message = usage () then 0 else 1
    with ex ->
        eprintfn "%s" ex.Message
        1
