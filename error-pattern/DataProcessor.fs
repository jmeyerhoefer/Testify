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
let getSnapshotTimestamps (taskInfo: TaskInfo) (groupAndTeamId: string) : seq<string> =
    let path: string = Path.Combine (RootPath, "data", "Tests", $"%s{taskInfo.ExerciseId}.csv")
    let csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()

    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"

    csvFile.Filter (fun (row: CsvRow) ->
        row.GetColumn "SHEET" = taskInfo.SheetId
        && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
        && row.GetColumn "GROUPID" = groupId
        && row.GetColumn "TEAMID" = teamId
    )
    |> fun (filtered: Runtime.CsvFile<CsvRow>) -> filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "SNAPSHOT_TIMESTAMP")


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
/// <param name="snapshotTimestamp">TODO</param>
let getDeletedFiles (taskInfo: TaskInfo) (groupAndTeamId: string) (snapshotTimestamp: string): seq<string> =
    let path: string = Path.Combine (RootPath, "data", "Removed", $"{taskInfo.ExerciseId}.csv")
    let csvFile: Runtime.CsvFile<CsvRow> = (CsvFile.Load path).Cache ()

    let groupId, teamId: string * string =
        match groupAndTeamId.Split "_" with
        | [| groupId; teamId |] -> groupId, teamId
        | _ -> failwith $"Invalid format for 'groupAndTeamId': %s{groupAndTeamId}"

    csvFile.Filter (fun (row: CsvRow) ->
           row.GetColumn "SHEET" = taskInfo.SheetId
        && row.GetColumn "ASSIGNMENT" = taskInfo.AssignmentId
        && row.GetColumn "GROUPID" = groupId
        && row.GetColumn "TEAMID" = teamId
        && row.GetColumn "DELETE_TIMESTAMP" <= snapshotTimestamp
    )
    |> fun (filtered: Runtime.CsvFile<CsvRow>) -> filtered.Rows
    |> Seq.map (fun (row: CsvRow) -> row.GetColumn "PHYSICAL_FILENAME")


/// <summary>
/// TODO
/// </summary>
/// <param name="taskInfo">TODO</param>
/// <param name="groupAndTeamId">TODO</param>
let getAllSnapshots (taskInfo: TaskInfo) (groupAndTeamId: string): seq<string * list<string>> =
    getSnapshotTimestamps taskInfo groupAndTeamId
    // get all relevant submissions
    |> Seq.map (fun (snapshotTimestamp: string) ->
        let relevantFiles: array<string> =
            groupAndTeamId
            |> taskInfo.GetSubmissionsPath
            |> Directory.GetFiles
            |> Array.filter (fun (submission: string) -> getTimestamp (submission |> Path.GetFileName) <= snapshotTimestamp)
        snapshotTimestamp, relevantFiles
    )
    // remove all files that were deleted before the snapshot timestamp
    |> Seq.map (fun (snapshotTimestamp: string, submissions: array<string>) ->
        let filteredSubmissions: array<string> =
            submissions
            |> Array.filter (fun (submission: string) ->
                getDeletedFiles taskInfo groupAndTeamId snapshotTimestamp
                |> Seq.contains (submission |> Path.GetFileName)
                |> not
            )
        snapshotTimestamp, filteredSubmissions
    )
    // only take the latest version of each file
    |> Seq.map (fun (snapshotTimestamp: string, submissions: array<string>) ->
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

    let imageId: string = "meyerhoefer/fsharpdev:latest"
    let workingDirectory: string = "/home/coder/Error-Pattern/"
    let command: string = "dotnet"
    let xunitPackageArguments: array<string> = [| "add"; "package"; "XunitXml.TestLogger"; "--version"; "4.1.0" |]
    let containerBuildResultsPath: string = $"%s{workingDirectory}buildResults.log"
    let buildArguments: array<string> = [| "build"; $"-flp:\"Summary;Verbosity=normal;LogFile=%s{containerBuildResultsPath}\"" |]
    let containerTestResultsPath: string = $"%s{workingDirectory}testResults.xml"
    let testArguments: array<string> = [| "test"; "-l"; $"\"xunit;LogFilePath=%s{containerTestResultsPath}\"" |]
    let stacktracePath: string = taskInfo.GetGroupAndTeamStacktracePath groupAndTeamId

    getAllSnapshots taskInfo groupAndTeamId
    |> Seq.iter (fun (snapshotTimestamp: string, submissions: list<string>) ->
        // snapshotTimestamp <=> containerId
        let createAndRunOperation: Async<bool> = createAndRunContainer dockerClient imageId snapshotTimestamp taskInfo submissions
        let createAndRunFailureMessage: string = "Failed to create and run container."

        let addXunitTestLoggerOperation: Async<bool> = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command xunitPackageArguments
        let addXunitTestLoggerFailureMessage: string = "Failed to add 'Xunit.TestLogger' package."

        let dotnetBuildOperation: Async<bool> = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command buildArguments
        let _dotnetBuildFailureMessage: string = "Failed to run 'dotnet build'."

        let hostBuildResultsPath: string = Path.Combine (stacktracePath, snapshotTimestamp, $"%s{snapshotTimestamp}-buildResults.log")
        let extractBuildResultsOperation: Async<bool> = copyFilesFromContainer dockerClient snapshotTimestamp containerBuildResultsPath hostBuildResultsPath
        let extractBuildResultsFailureMessage: string = "Failed to extract build results from container."

        let dotnetTestOperation: Async<bool> = executeCommandInsideContainer dockerClient snapshotTimestamp workingDirectory command testArguments
        let _dotnetTestFailureMessage: string = "Failed to run 'dotnet test'."

        let hostTestResultsPath: string = Path.Combine (stacktracePath, snapshotTimestamp, $"%s{snapshotTimestamp}-testResults.xml")
        let extractTestResultsOperation: Async<bool> = copyFilesFromContainer dockerClient snapshotTimestamp containerTestResultsPath hostTestResultsPath
        let extractTestResultsFailureMessage: string = "Failed to extract test results from container."

        let stopAndRemoveOperation: Async<bool> = stopAndRemoveContainer dockerClient snapshotTimestamp
        let stopAndRemoveFailureMessage: string = "Failed to stop and remove container."

        let log (message: string): unit =
            let outputLogPath: string = Path.Combine (taskInfo.GetStacktracePath (), "output.log")
            let logMessage: string = $"taskInfo: %A{taskInfo}\ngroupAndTeamId: %s{groupAndTeamId}\nsubmissions:%A{submissions}\nMessage: %s{message}\n\n"
            File.AppendAllText (outputLogPath, logMessage)

        Path.Combine (stacktracePath, snapshotTimestamp)
        |> Directory.CreateDirectory
        |> ignore

        if not (createAndRunOperation |> Async.RunSynchronously) then log createAndRunFailureMessage
        elif not (dotnetBuildOperation |> Async.RunSynchronously) then
            if not (extractBuildResultsOperation |> Async.RunSynchronously) then log extractBuildResultsFailureMessage
            elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
            else log "Finished after failed build."
        elif not (extractBuildResultsOperation |> Async.RunSynchronously) then log extractBuildResultsFailureMessage
        elif not (addXunitTestLoggerOperation |> Async.RunSynchronously) then log addXunitTestLoggerFailureMessage
        elif not (dotnetTestOperation |> Async.RunSynchronously) then
            if not (extractTestResultsOperation |> Async.RunSynchronously) then log extractTestResultsFailureMessage
            elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
            log "Finished after failed test."
        elif not (extractTestResultsOperation |> Async.RunSynchronously) then log extractTestResultsFailureMessage
        elif not (stopAndRemoveOperation |> Async.RunSynchronously) then log stopAndRemoveFailureMessage
        else log "Finished after successful test."
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


// EOF