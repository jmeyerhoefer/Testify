module Program


open DataProcessor
open RelevantInfo
open Statistics
open System
open System.IO
open System.Xml
open TestWrapper


let startProcessing (exerciseId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    let relevantTasks: TaskInfo list =
        match exerciseId with
        | "GdP18" -> relevantTasksGdP18
        | "GdP19" -> relevantTasksGdP19
        | "GdP20" -> relevantTasksGdP20
        | "GdP21" -> relevantTasksGdP21
        | "GdP22" -> relevantTasksGdP22
        | "GdP23" -> relevantTasksGdP23
        | "GdP24" -> relevantTasksGdP24
        | "All" -> allRelevantTasks
        | _ -> []

    processData relevantTasks
    Console.Write $"\r%-20s{finished}"


let startProcessingFrom (exerciseId: string) (sheetId: string) (assignmentId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    let relevantTasks: TaskInfo list =
        match exerciseId with
        | "GdP18" -> relevantTasksGdP18
        | "GdP19" -> relevantTasksGdP19
        | "GdP20" -> relevantTasksGdP20
        | "GdP21" -> relevantTasksGdP21
        | "GdP22" -> relevantTasksGdP22
        | "GdP23" -> relevantTasksGdP23
        | "GdP24" -> relevantTasksGdP24
        | "All" -> allRelevantTasks
        | _ -> []

    relevantTasks
    |> List.skipWhile (fun (taskInfo: TaskInfo) -> taskInfo.SheetId <> sheetId && taskInfo.AssignmentId <> assignmentId)
    |> processData
    Console.Write $"\r%-20s{finished}"


let startProcessingSingleTask (exerciseId: string) (sheetId: string) (assignmentId: string): unit =
    let running: string = "Running ..."
    let finished: string = "Finished."
    Console.Write $"%-20s{running}"

    let relevantTasks: TaskInfo list =
        match exerciseId with
        | "GdP18" -> relevantTasksGdP18
        | "GdP19" -> relevantTasksGdP19
        | "GdP20" -> relevantTasksGdP20
        | "GdP21" -> relevantTasksGdP21
        | "GdP22" -> relevantTasksGdP22
        | "GdP23" -> relevantTasksGdP23
        | "GdP24" -> relevantTasksGdP24
        | "All" -> allRelevantTasks
        | _ -> []

    relevantTasks
    |> List.filter (fun (taskInfo: TaskInfo) -> taskInfo.SheetId = sheetId && taskInfo.AssignmentId = assignmentId)
    |> processData
    Console.Write $"\r%-20s{finished}"


[<EntryPoint>]
let main (args: string array): int =
    printfn "Running ..."
    0
    // match args with
    // | [| "processData"; exerciseId |] ->
    //     startProcessing exerciseId
    //     0
    // | [| "generateStatistics"; exerciseId |] ->
    //     let relevantTasks: TaskInfo list =
    //         match exerciseId with
    //         | "GdP18" -> relevantTasksGdP18
    //         | "GdP19" -> relevantTasksGdP19
    //         | "GdP20" -> relevantTasksGdP20
    //         | "GdP21" -> relevantTasksGdP21
    //         | "GdP22" -> relevantTasksGdP22
    //         | "GdP23" -> relevantTasksGdP23
    //         | "GdP24" -> relevantTasksGdP24
    //         | "All" -> allRelevantTasks
    //         | _ -> []
    //     generateStatistics exerciseId relevantTasks
    //     0
    // | _ ->
    //     Console.Write $"Invalid arguments: %A{args}"
    //     1


// EOF