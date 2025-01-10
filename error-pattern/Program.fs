module Program


open DataProcessor
open RelevantInfo
open Statistics
open System


let startProcessing (exerciseId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    getRelevantTasks exerciseId |> processData

    Console.Write $"\r%-20s{finished}"


let startProcessingFrom (exerciseId: string) (sheetId: string) (assignmentId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    getRelevantTasks exerciseId
    |> List.skipWhile (fun (taskInfo: TaskInfo) -> taskInfo.SheetId <> sheetId && taskInfo.AssignmentId <> assignmentId)
    |> processData

    Console.Write $"\r%-20s{finished}"


let startProcessingSingleTask (exerciseId: string) (sheetId: string) (assignmentId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    getRelevantTasks exerciseId
    |> List.filter (fun (taskInfo: TaskInfo) -> taskInfo.SheetId = sheetId && taskInfo.AssignmentId = assignmentId)
    |> processData
    Console.Write $"\r%-20s{finished}"


[<EntryPoint>]
let main (args: string array): int =
    match args with
    | [| "processData"; exerciseId |] ->
        startProcessing exerciseId
        0
    | [| "generateStatistics"; exerciseId |] ->
        getRelevantTasks exerciseId |> generateStatistics exerciseId
        0
    | _ ->
        Console.Write $"Invalid arguments: %A{args}"
        1


// EOF