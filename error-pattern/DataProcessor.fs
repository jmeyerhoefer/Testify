module DataProcessor


open Docker.DotNet
open DockerController
open FSharp.Data
open System.Diagnostics
open System.IO
open System.Threading
open Types


/// The relevant tasks for GdP18 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP18: list<TaskInfo> = []


/// The relevant tasks for GdP19 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP19: list<TaskInfo> = []


/// The relevant tasks for GdP20 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP20: list<TaskInfo> = []


/// The relevant tasks for GdP21 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP21: list<TaskInfo> = []


/// The relevant tasks for GdP22 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP22: list<TaskInfo> = []


/// The relevant tasks for GdP23 for analysis stored as <c>TaskInfo</c>.
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


/// The relevant tasks for GdP24 for analysis stored as <c>TaskInfo</c>.
let relevantTasksGdP24: list<TaskInfo> = []


/// <summary>
/// Executes <c>dotnet build</c> or <c>dotnet test</c> at a given path.
/// </summary>
/// <param name="arguments">Arguments for <c>ProcessStartInfo</c>.</param>
/// <param name="projectPath">Path to the project.</param>
/// <returns>
/// <c>true</c> if the command was successful;
/// <c>false</c> if command failed for some reason.
/// </returns>
let executeDotnetCommand (arguments: string) (projectPath: string): bool =
    let processStartInfo: ProcessStartInfo =
        ProcessStartInfo (
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            CreateNoWindow = true
        )

    use buildProcess: Process = new Process (StartInfo=processStartInfo)
    buildProcess.Start () |> ignore
    buildProcess.WaitForExit ()
    buildProcess.ExitCode = 0 // return value


/// <summary>
/// Executes <c>dotnet build</c> at a given path.
/// </summary>
/// <param name="projectPath">Path to the project.</param>
/// <param name="stacktracePath">Path to the destination directory for the stacktrace.</param>
/// <param name="timestamp">Timestamp of this snapshot.</param>
/// <returns>
/// <c>true</c> if the command was successful;
/// <c>false</c> if command failed for some reason.
/// </returns>
let executeDotnetBuild (projectPath: string) (stacktracePath: string) (timestamp: string): bool =
    let logFilePath: string = Path.Combine (stacktracePath, $"%s{timestamp}-buildResults.log")
    let arguments: string = $"build -flp:\"Summary;Verbosity=normal;LogFile=%s{logFilePath}\""
    executeDotnetCommand arguments projectPath


/// <summary>
/// Executes <c>dotnet test</c> at a given path.
/// <p>Stores the output at <c>RootPath/data/{exerciseId}/Stacktrace/{sheetId}/{groupAndTeamId}/{assignmentId}/{timestamp}-output.xml</c></p>
/// </summary>
/// <param name="projectPath">Path to the project</param>
/// <param name="stacktracePath">Path to the destination directory for the stacktrace.</param>
/// <param name="timestamp">The timestamp that was assigned to the submission of this project.</param>
/// <returns>
/// <c>true</c> if <c>dotnet test</c> was successful;
/// <c>false</c> if <c>dotnet test</c> failed for some reason.
/// </returns>
let executeDotnetTest (projectPath: string) (stacktracePath: string) (timestamp: string): bool =
    let logFilePath: string = Path.Combine (stacktracePath, $"%s{timestamp}-testResults.xml")
    let arguments: string = $"test -l:\"trx;LogFileName=%s{logFilePath}\""
    executeDotnetCommand arguments projectPath


/// <summary>
/// Tries to build and test project.
/// </summary>
/// <param name="projectPath">Path to the project.</param>
/// <param name="taskInfo">Contains the relevant info to the task.</param>
/// <param name="groupAndTeamId">ID of the group and team to this project.</param>
/// <param name="timestamp">The timestamp that was assigned to the submission of this project.</param>
/// <returns>
/// <c>Success</c> if the build and test were successful;
/// <c>BuildFailed projectPath</c> if the build failed;
/// <c>TestFailed projectPath</c> if the test failed;
/// <c>UnexpectedError projectPath</c> otherwise.
/// </returns>
let buildAndTestProject (projectPath: string) (taskInfo: TaskInfo) (groupAndTeamId: string) (timestamp: string): BuildAndTestResult =
    try
        let stacktracePath: string = taskInfo.GetStacktracePath groupAndTeamId
        match executeDotnetBuild projectPath stacktracePath timestamp, executeDotnetTest projectPath stacktracePath timestamp with
        | false, _ -> BuildFailed
        | true, false -> TestFailed
        | true, true -> Success
    with
    | ex -> UnexpectedError ex.Message


/// <summary>
/// Retries an <c>action</c> with a maximum of <c>maxRetries</c> and a delay of <c>delayInMillis</c> in between.
/// </summary>
/// <param name="action">The action to perform</param>
/// <param name="maxRetries">Maximum number of attempts.</param>
/// <param name="delayInMillis">Delay in between the attempts.</param>
let retryWithDelay (action: unit -> unit) (maxRetries: int) (delayInMillis: int): unit =
    let rec attempt (count: int): unit =
        try action () with
        | :? IOException when count < maxRetries ->
            Thread.Sleep delayInMillis
            attempt (count + 1)
        | ex -> raise ex
    attempt 0


/// <summary>
/// Deletes all contents of a directory
/// </summary>
/// <param name="path">The path to the directory.</param>
let deleteContents (path: string): unit =
    if Directory.Exists path then
        Directory.GetFiles path
        |> Array.iter (fun (file: string) -> retryWithDelay (fun () -> File.Delete file) 5 1000)
        
        Directory.GetDirectories path
        |> Array.iter (fun (directory: string) -> retryWithDelay (fun () -> Directory.Delete (directory, true)) 5 1000)


/// <summary>
/// Copies the files from <c>templatePath</c> to <c>destinationPath</c> except <c>relevantFileName</c>.
/// </summary>
/// <param name="templatePath">The path to the project template.</param>
/// <param name="destinationPath">The path to the project.</param>
/// <param name="relevantFileName">The file to exclude.</param>
let copyTemplate (templatePath: string) (destinationPath: string) (relevantFileName: string): unit =
    templatePath
    |> Directory.GetFiles
    |> Array.iter (fun (filePath: string) ->
        let fileName: string = Path.GetFileName filePath
        if fileName <> relevantFileName then
            File.Copy (filePath, Path.Combine (destinationPath, fileName), true)
    )


/// <summary>
/// Copies the submissions over to <c>destinationPath</c>.
/// </summary>
/// <param name="submissions">Array of the paths to the submissions.</param>
/// <param name="destinationPath">The path of the project.</param>
let copySubmissionFiles (submissions: array<string>) (destinationPath: string): unit =
    submissions
    |> Array.iter (fun (submission: string) ->
        let fileName: string = (Path.GetFileName submission)
        let fileNameWithoutTimestamp: string = fileName.Substring (fileName.IndexOf "-" + 1)
        File.Copy(submission, Path.Combine (destinationPath, fileNameWithoutTimestamp), true)
    )


/// TODO
let getSnapshotTimestamps (taskInfo: TaskInfo) (groupAndTeamId: string) : list<string> =
    let path: string = Path.Combine (RootPath, "data", "Tests", $"%s{taskInfo.ExerciseId}.csv")
    use csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()
    
    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"
    
    let filtered: Runtime.CsvFile<CsvRow> =
        csvFile.Filter (fun (row: CsvRow) ->
            row.GetColumn "SHEET" = taskInfo.SheetId &&
            row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId &&
            row.GetColumn "GROUPID" = groupId &&
            row.GetColumn "TEAMID" = teamId
        )
    
    filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "SNAPSHOT_TIMESTAMP")
    |> Seq.toList


/// TODO
let getDeletedFiles (taskInfo: TaskInfo) (groupAndTeamId: string): list<string * string> =
    let path: string = Path.Combine (RootPath, "data", "Removed", $"{taskInfo.ExerciseId}.csv")
    use csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()
    
    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"
    
    let filtered: Runtime.CsvFile<CsvRow> =
        csvFile.Filter (fun (row: CsvRow) ->
            row.GetColumn "SHEET" = taskInfo.SheetId &&
            row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId &&
            row.GetColumn "GROUPID" = groupId &&
            row.GetColumn "TEAMID" = teamId
        )
    
    filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "DELETE_TIMESTAMP", row.GetColumn "PHYSICAL_FILENAME")
    |> Seq.toList


/// TODO
let getAllSnapshots (taskInfo: TaskInfo) (groupAndTeamId: string): list<string * list<string>> =
    let snapshotTimestamps: list<string> = getSnapshotTimestamps taskInfo groupAndTeamId
    let deletedFiles: list<string * string> = getDeletedFiles taskInfo groupAndTeamId
    let allSubmissions: list<string> =
        taskInfo.GetSubmissionsPath groupAndTeamId
        |> Directory.GetFiles
        |> Array.toList
        |> List.map Path.GetFileName
    
    snapshotTimestamps
    // get all relevant submissions
    |> List.map (fun (snapshotTimestamp: string) ->
        let relevantFiles: list<string> =
            allSubmissions
            |> List.filter (fun (submission: string) -> (submission.Split "-" |> Array.item 0) <= snapshotTimestamp)
        snapshotTimestamp, relevantFiles
    )
    // remove all files that were deleted before the snapshot timestamp
    |> List.map (fun (snapshotTimestamp: string, submissions: list<string>) ->
        let filesToBeDeleted: list<string> =
            deletedFiles
            |> List.filter (fun (deleteTimestamp: string, _: string) -> deleteTimestamp < snapshotTimestamp)
            |> List.map snd
        
        let filteredSubmissions: list<string> =
            submissions
            |> List.filter (fun (submission: string) -> not (filesToBeDeleted |> List.contains submission))
        
        snapshotTimestamp, filteredSubmissions
    )
    // only take the latest version of each file
    |> List.map (fun (snapshotTimestamp: string, submissions: list<string>) ->
        let onlyLatestFiles: list<string> =
            submissions
            |> List.groupBy (fun (submission: string) -> submission.Substring (submission.IndexOf "-" + 1))
            |> List.map (fun (_: string, group: list<string>) ->
                group
                |> List.maxBy (fun (submission: string) -> submission.Split "-" |> Array.item 0)
            )
        snapshotTimestamp, onlyLatestFiles
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
    let workingDirectory: string = "/home/coder/Error-Pattern"
    let command: string = "dotnet"
    
    let containerBuildResultsPath: string = $"%s{workingDirectory}/buildResults.log"
    let buildArguments: array<string> = [| "build"; $"-flp:\"Summary;Verbosity=normal;LogFile=%s{containerBuildResultsPath}\"" |]
    
    let containerTestResultsPath: string = $"%s{workingDirectory}/testResults.trx"
    let testArguments: array<string> = [| "test"; $"-l:\"trx;LogFileName=%s{containerTestResultsPath}\"" |]
    
    let stacktracePath: string = taskInfo.GetStacktracePath groupAndTeamId

    getAllSnapshots taskInfo groupAndTeamId
    |> List.iter (fun (snapshotTimestamp: string, submissions: list<string>) ->
        // snapshotTimestamp <=> containerId
        
        let log (message: string): unit =
            printfn $"%s{message}\ntaskInfo: %A{taskInfo}\ngroupAndTeamId: %s{groupAndTeamId}\nsubmissions:%A{submissions}"
        
        // create and run container
        if not (createAndRunContainer dockerClient imageId snapshotTimestamp submissions |> Async.RunSynchronously) then
            log "Failed to create and run container."
        // execute 'dotnet build'
        elif not (executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command buildArguments |> Async.RunSynchronously) then
            log "Failed to run 'dotnet build'"
        else
            // extract build results
            let hostBuildResultsPath: string = Path.Combine (stacktracePath, $"%s{snapshotTimestamp}-buildResults.log")
            if not (copyFilesFromContainer dockerClient snapshotTimestamp containerBuildResultsPath hostBuildResultsPath |> Async.RunSynchronously) then 
                log "Failed to extract build results from container."
            // execute 'dotnet test'
            elif not (executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command testArguments |> Async.RunSynchronously) then
                log "Failed to run 'dotnet test'"
            else
                // extract test results
                let hostTestResultsPath: string = Path.Combine (stacktracePath, $"%s{snapshotTimestamp}-testResults.trx")
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
    File.Delete (Path.Combine (RootPath, "Output.log"))
    printfn "Previous log deleted successfully."

    relevantTasks
    |> List.iteri (fun (index: int) (taskInfo: TaskInfo) ->
        printf $"In progress: %d{index + 1}/%d{relevantTasks.Length}\r"
        processTask taskInfo
    )


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