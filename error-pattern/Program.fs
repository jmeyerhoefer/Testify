module Program


open Docker.DotNet
open DockerController
open DataProcessor
open System.IO


let testCreateContainer () =
    let imageId: string = "meyerhoefer/fsharpdev:latest"
    let taskInfo: TaskInfo = relevantTasksGdP23 |> List.head
    let snapshots: seq<string * list<string>> =
        getAllSnapshots taskInfo "01_17"
        |> Seq.sortBy fst

    let snapshotTimestamp, submissions: string * list<string> = snapshots |> Seq.head

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


[<EntryPoint>]
let main (_: array<string>): int =
    processGroupAndTeam (relevantTasksGdP23 |> List.head) "01_17"

    0


// EOF
