module Program


open System
open System.IO
open System.Xml
open System.Xml.Serialization
open DataProcessor
open Statistics
open RelevantInfo


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


[<EntryPoint>]
let main (args: string array): int =
    let paths: string seq = [
        Path.Combine (RootPath, "0-testResults.xml")
        Path.Combine (RootPath, "1-testResults.xml")
        Path.Combine (RootPath, "2-testResults.xml")
        Path.Combine (RootPath, "3-testResults.xml")
        Path.Combine (RootPath, "4-testResults.xml")
        Path.Combine (RootPath, "5-testResults.xml")
    ]
    paths
    |> Seq.averageBy (fun (path: string) ->
        let lines: string array = File.ReadAllLines path
        if lines.Length > 1 then
            let firstLineTrimmed: string = lines[0].Remove (0, lines[0].IndexOf "<")
            let middleLines: string array = lines[1 .. lines.Length - 2]
            let lastLine: string = lines[lines.Length - 1]
            let lastLineTrimmed: string = lastLine.Remove (lastLine.IndexOf ">" + 1)
            File.WriteAllLines (path, Array.concat [| [| firstLineTrimmed |]; middleLines; [| lastLineTrimmed |] |])
            let xmlDocument: XmlDocument = XmlDocument ()
            xmlDocument.Load path
            let testResultsCollection: XmlNodeList = xmlDocument.SelectNodes "//collection"
            let mutable total: int = -1
            let mutable passed: int = -1
            // let mutable name: string = "N/A"
            // let mutable failed: int = -1
            if testResultsCollection.Count > 0 then
                for testResultsCollectionNode: XmlNode in testResultsCollection do
                    total <- int testResultsCollectionNode.Attributes.["total"].Value
                    passed <- int testResultsCollectionNode.Attributes.["passed"].Value
                    // name <- testResultsCollectionNode.Attributes.["name"].Value
                    // failed <- int testResultsCollectionNode.Attributes.["failed"].Value
                    // skipped <- int testResultsCollectionNode.Attributes.["skipped"].Value
                    // let time: float = float testResultsCollectionNode.Attributes.["time"].Value
            100.0 * float passed / float total
        else
            0
    )
    |> printfn "%.2f"
    0
    // match args with
    // | [| "processData"; exerciseId |] ->
    //     startProcessing exerciseId
    //     0
    // | [| "generateStatistics"; exerciseId |] ->
    //     generateStatistics exerciseId
    //     0
    // | _ ->
    //     Console.Write $"Invalid arguments: %A{args}"
    //     1


// EOF