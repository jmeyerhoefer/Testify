module DataProcessor


open Docker.DotNet
open DockerController
open FSharp.Data
open System.IO
open Types


/// <summary>The relevant tasks for GdP18 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP18: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP18", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>The relevant tasks for GdP19 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP19: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP19", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>The relevant tasks for GdP20 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP20: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP20", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>The relevant tasks for GdP21 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP21: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP21", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>The relevant tasks for GdP22 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP22: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP22", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>The relevant tasks for GdP23 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP23: list<TaskInfo> = [
    TaskInfo ("GdP23", "02", "4", "Programmieren mit Zahlen", "Zahlen.fs")
    TaskInfo ("GdP23", "03", "2", "Peano Entwurfsmuster", "Peano.fs")
    TaskInfo ("GdP23", "03", "3", "Leibniz Entwurfsmuster", "Leibnis.fs")
    TaskInfo ("GdP23", "04", "2", "Kalenderdaten", "Dates.fs")
    TaskInfo ("GdP23", "04", "3", "Listen natürlicher Zahlen", "Nats.fs")
    TaskInfo ("GdP23", "05", "2", "Prioritätswarteschlange", "PriorityQueue.fs")
    TaskInfo ("GdP23", "05", "3", "Ausdrücke vereinfachen", "Simplify.fs")
    TaskInfo ("GdP23", "06", "3", "Heaps", "Heaps.fs")
    TaskInfo ("GdP23", "07", "2", "Endliche Abbildungen 1", "MapSortedList.fs")
    TaskInfo ("GdP23", "07", "3", "Endliche Abbildungen 2", "MapPartialFunction.fs")
    TaskInfo ("GdP23", "08", "2", "Datentypen", "Datatypes.fs")
    TaskInfo ("GdP23", "08", "3", "Reguläre Ausdrücke", "RegExp.fs")
    TaskInfo ("GdP23", "09", "2", "Black Jack", "BlackJack.fs")
    TaskInfo ("GdP23", "10", "4", "Veränderbare Listen", "Lists.fs")
    TaskInfo ("GdP23", "11", "4", "Arrays und Zustand", "Arrays.fs")
    TaskInfo ("GdP23", "12", "2", "Warteschlangen", "Queues.fs")
]


/// <summary>The relevant tasks for GdP24 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP24: list<TaskInfo> = [
    // TODO
    TaskInfo ("GdP24", "sheetId", "assignmentId", "assignmentTitle", "relevantFileName")
]


/// <summary>All relevant tasks for analysis stored as <c>TaskInfo</c></summary>
let allTasks: list<TaskInfo> =
    relevantTasksGdP18
    @ relevantTasksGdP19
    @ relevantTasksGdP20
    @ relevantTasksGdP21
    @ relevantTasksGdP22
    @ relevantTasksGdP23
    // @ relevantTasksGdP24


/// <summary>
/// TODO
/// </summary>
/// <param name="fileNameWithTimestamp">TODO</param>
let getTimestamp (fileNameWithTimestamp: string): string =
    let timestampOption: option<string> =
        fileNameWithTimestamp.Split "-"
        |> Array.tryHead
    match timestampOption with
    | Some timestamp -> timestamp
    | None -> failwith $"Failed to retrieve timestamp from: %s{fileNameWithTimestamp}"
    

/// <summary>
/// TODO
/// </summary>
/// <param name="fileNameWithTimestamp">TODO</param>
let getFileNameWithoutTimestamp (fileNameWithTimestamp: string): string =
    let indexOfSeparator: int = fileNameWithTimestamp.IndexOf "-"
    if indexOfSeparator = -1 then
        failwith $"Failed to retrieve file name from: %s{fileNameWithTimestamp}"
    fileNameWithTimestamp.Substring (indexOfSeparator + 1)


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getSnapshotTimestamps (taskInfo: TaskInfo) (groupAndTeamId: string) : list<string> =
    let path: string = Path.Combine (RootPath, "data", "Tests", $"%s{taskInfo.ExerciseId}.csv")
    use csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()
    
    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"
    
    let filtered: Runtime.CsvFile<CsvRow> =
        csvFile.Filter (fun (row: CsvRow) ->
            row.GetColumn "SHEET" = taskInfo.SheetId
            && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
            && row.GetColumn "GROUPID" = groupId
            && row.GetColumn "TEAMID" = teamId
        )
    
    filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "SNAPSHOT_TIMESTAMP")
    |> Seq.toList


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getDeletedFiles (taskInfo: TaskInfo) (groupAndTeamId: string): list<string * string> =
    let path: string = Path.Combine (RootPath, "data", "Removed", $"{taskInfo.ExerciseId}.csv")
    use csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()
    
    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"
    
    let filtered: Runtime.CsvFile<CsvRow> =
        csvFile.Filter (fun (row: CsvRow) ->
            row.GetColumn "SHEET" = taskInfo.SheetId
            && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
            && row.GetColumn "GROUPID" = groupId
            && row.GetColumn "TEAMID" = teamId
        )
    
    filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "DELETE_TIMESTAMP", row.GetColumn "PHYSICAL_FILENAME")
    |> Seq.toList


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getAllSnapshots (taskInfo: TaskInfo) (groupAndTeamId: string): list<string * list<string>> =
    let snapshotTimestamps: list<string> = getSnapshotTimestamps taskInfo groupAndTeamId
    let deletedFiles: list<string * string> = getDeletedFiles taskInfo groupAndTeamId
    let allSubmissions: array<string> = taskInfo.GetSubmissionsPath groupAndTeamId |> Directory.GetFiles
    
    snapshotTimestamps
    // get all relevant submissions
    |> List.map (fun (snapshotTimestamp: string) ->
        let relevantFiles: array<string> =
            allSubmissions
            |> Array.filter (fun (submission: string) -> getTimestamp (submission |> Path.GetFileName) <= snapshotTimestamp)
        snapshotTimestamp, relevantFiles
    )
    // remove all files that were deleted before the snapshot timestamp
    |> List.map (fun (snapshotTimestamp: string, submissions: array<string>) ->
        let filesToBeDeleted: list<string> =
            deletedFiles
            |> List.filter (fun (deleteTimestamp: string, _: string) -> deleteTimestamp <= snapshotTimestamp)
            |> List.map snd
        let filteredSubmissions: array<string> =
            submissions
            |> Array.filter (fun (submission: string) ->
                let fileName: string = submission |> Path.GetFileName
                not (filesToBeDeleted |> List.contains fileName)
            )
        snapshotTimestamp, filteredSubmissions
    )
    // only take the latest version of each file
    |> List.map (fun (snapshotTimestamp: string, submissions: array<string>) ->
        let onlyLatestFilesAsList: list<string> =
            submissions
            |> Array.groupBy getFileNameWithoutTimestamp
            |> Array.map (fun (_: string, group: array<string>) -> group |> Array.maxBy getTimestamp)
            |> Array.toList
        snapshotTimestamp, onlyLatestFilesAsList
    )


/// <summary>
/// Process a specific group and team.
/// </summary>
/// <param name="taskInfo">The task to process.</param>
/// <param name="groupAndTeamId">The specific group and team.</param>
let processGroupAndTeam (taskInfo: TaskInfo) (groupAndTeamId: string): unit =
    use dockerClientConfiguration: DockerClientConfiguration = new DockerClientConfiguration ()
    use dockerClient: DockerClient = dockerClientConfiguration.CreateClient ()
    
    let imageId: string = "fsharp-image"
    let workingDirectory: string = "/home/coder/Error-Pattern/"
    let command: string = "dotnet"
    
    let xunitPackageArguments: array<string> = [| "add"; "package"; "XunitXml.TestLogger"; "--version"; "4.1.0" |]
    
    let containerBuildResultsPath: string = $"%s{workingDirectory}buildResults.log"
    let buildArguments: array<string> = [| "build"; "-flp"; $"\"Summary;Verbosity=normal;LogFile=%s{containerBuildResultsPath}\"" |]
    
    let containerTestResultsPath: string = $"%s{workingDirectory}testResults.xml"
    let testArguments: array<string> = [| "test"; "-l"; $"\"xunit;LogFilePath=%s{containerTestResultsPath}\"" |]
    
    let stacktracePath: string = taskInfo.GetStacktracePath groupAndTeamId

    getAllSnapshots taskInfo groupAndTeamId
    |> List.iter (fun (snapshotTimestamp: string, submissions: list<string>) ->
        // snapshotTimestamp <=> containerId
        
        let log (message: string): unit =
            printfn $"%s{message}\ntaskInfo: %A{taskInfo}\ngroupAndTeamId: %s{groupAndTeamId}\nsubmissions:%A{submissions}"
        
        // create and run container
        if not (createAndRunContainer dockerClient imageId snapshotTimestamp taskInfo submissions |> Async.RunSynchronously) then
            log "Failed to create and run container."
        // execute 'dotnet add package'
        elif not (executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command xunitPackageArguments |> Async.RunSynchronously) then
            log "Failed to add 'Xunit.TestLogger' package."
        // execute 'dotnet build'
        elif not (executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command buildArguments |> Async.RunSynchronously) then
            log "Failed to run 'dotnet build'."
        else
            // extract build results
            let hostBuildResultsPath: string = Path.Combine (stacktracePath, $"%s{snapshotTimestamp}-buildResults.log")
            if not (copyFilesFromContainer dockerClient snapshotTimestamp containerBuildResultsPath hostBuildResultsPath |> Async.RunSynchronously) then 
                log "Failed to extract build results from container."
            // execute 'dotnet test'
            elif not (executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command testArguments |> Async.RunSynchronously) then
                log "Failed to run 'dotnet test'."
            else
                // extract test results
                let hostTestResultsPath: string = Path.Combine (stacktracePath, $"%s{snapshotTimestamp}-testResults.xml")
                if not (copyFilesFromContainer dockerClient snapshotTimestamp containerTestResultsPath hostTestResultsPath |> Async.RunSynchronously) then
                    log "Failed to extract test results from container."
    )


/// <summary>
/// Process data for a single task.
/// </summary>
/// <param name="taskInfo">The task to process.</param>
let processTask (taskInfo: TaskInfo): unit =
    taskInfo.GetGroupAndTeamIds ()
    |> Array.iter (processGroupAndTeam taskInfo)


/// <summary>
/// Process the whole data set.
/// </summary>
/// <param name="relevantTasks">The array of relevant tasks in the data set.</param>
let processData (relevantTasks: list<TaskInfo>): unit =
    relevantTasks
    |> List.iter processTask


// /// <summary>
// /// Process a specific group and team.
// /// </summary>
// /// <param name="taskInfo">The task to process.</param>
// /// <param name="groupAndTeamId">The specific group and team.</param>
// let processGroupAndTeam2 (taskInfo: TaskInfo) (groupAndTeamId: string): unit =
//     let pathToSubmissions: string = taskInfo.GetSubmissionsPath groupAndTeamId
//     let allSubmissions: array<string> =
//         if Directory.Exists pathToSubmissions then
//             Directory.GetFiles pathToSubmissions
//         else
//             Array.empty
//     
//     allSubmissions
//     |> Array.groupBy (fun (submissionPath: string) ->
//         let fileName: string = Path.GetFileName submissionPath
//         let tokens: array<string> = fileName.Split "-"
//         if tokens.Length <> 0 then tokens[0]
//         else raise (System.Exception "File name could not be split.")
//     )
//     |> Array.iter (fun (timestamp: string, submissions: array<string>) ->
//         let pathToTemplate: string = taskInfo.GetTemplatePath ()
//         copyTemplate pathToTemplate ProjectPath taskInfo.RelevantFileName
//         copySubmissionFiles submissions ProjectPath
//
//         let mutable logMessage: string =
//             taskInfo.ToString ()
//             |> fun taskInfoString ->
//                 match buildAndTestProject ProjectPath taskInfo groupAndTeamId timestamp with
//                 | Success ->
//                     $"%s{taskInfoString} -> Success for groupAndTeamId: %s{groupAndTeamId}, timestamp: %s{timestamp}\n"
//                 | BuildFailed ->
//                     $"%s{taskInfoString} -> Build failed for groupAndTeamId: %s{groupAndTeamId}, timestamp: %s{timestamp}\n"
//                 | TestFailed ->
//                     $"%s{taskInfoString} -> Test failed for groupAndTeamId: %s{groupAndTeamId}, timestamp: %s{timestamp}\n"
//                 | UnexpectedError message ->
//                     $"%s{taskInfoString} -> Unexpected error for groupAndTeamId: %s{groupAndTeamId}, timestamp: %s{timestamp} with error message: %s{message}\n"
//         
//         File.AppendAllText (Path.Combine (RootPath, "Output.log"), logMessage)
//         deleteContents ProjectPath
//     )


// EOF