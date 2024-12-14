module Program


open Docker.DotNet
open DockerController
open DataProcessor
open System.IO


[<EntryPoint>]
let main (_: array<string>): int =
    let tasks: list<TaskInfo> =
        relevantTasksGdP23
        |> List.take 1 // for test reasons
    
    processData tasks

    0


// EOF
