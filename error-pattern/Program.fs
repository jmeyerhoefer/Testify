module Program


open Docker.DotNet
open DockerController
open DataProcessor
open System.IO


let processAllData (): unit =
    use dockerClientConfiguration: DockerClientConfiguration = new DockerClientConfiguration ()
    use dockerClient: DockerClient = dockerClientConfiguration.CreateClient ()
    
    let taskInfo: TaskInfo = relevantTasksGdP23 |> List.item 0
    let groupAndTeamId: string = "01_17"
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


[<EntryPoint>]
let main (_: array<string>): int =
    
    let imageId: string = "fsharp-image-2"
    let taskInfo: TaskInfo = relevantTasksGdP23 |> List.head
    let snapshots: list<string * list<string>> =
        getAllSnapshots taskInfo "01_17"
        |> List.sortBy fst
    
    let snapshotTimestamp, submissions: string * list<string> = snapshots |> List.head
    
    printfn "Template:"
    taskInfo.GetTemplatePath ()
    |> Directory.GetFiles
    |> Array.iter (printfn "%s")
    
    printfn $"\nSnapshotTimestamp: %s{snapshotTimestamp}"
    printfn "Submissions:"
    submissions
    |> List.iter (printfn "%s")
    
    printfn "\nTry to create and run container:"
    use dockerClientConfiguration: DockerClientConfiguration = new DockerClientConfiguration ()
    use dockerClient: DockerClient = dockerClientConfiguration.CreateClient ()
    let createAndRunContainerSuccess: bool = createAndRunContainer dockerClient imageId snapshotTimestamp taskInfo submissions |> Async.RunSynchronously
    printfn $"createAndRunContainerSuccess: %b{createAndRunContainerSuccess}"

    0


// EOF
