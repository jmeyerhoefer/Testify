module DockerController


open Docker.DotNet
open Docker.DotNet.Models
open System.IO
open Types


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="containerPath">TODO</param>
/// <param name="hostPath">TODO</param>
let copyFilesFromContainer (dockerClient: DockerClient) (containerId: string) (containerPath: string) (hostPath: string): Async<bool> =
    async {
        try
            let getArchiveFromContainerParameters: GetArchiveFromContainerParameters = GetArchiveFromContainerParameters ()
            getArchiveFromContainerParameters.Path <- containerPath
            let! getArchiveFromContainerResponse = dockerClient.Containers.GetArchiveFromContainerAsync (containerId, getArchiveFromContainerParameters, false) |> Async.AwaitTask
            
            use fileStream = File.Create hostPath
            do! getArchiveFromContainerResponse.Stream.CopyToAsync fileStream |> Async.AwaitTask
            
            return true
        with
        | ex ->
            printfn $"copyFilesFromContainer: %s{ex.Message}"
            return false
    }


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="workingDirectory">TODO</param>
/// <param name="command">TODO</param>
/// <param name="args">TODO</param>
let executeCommandInsideContainer (dockerClient: DockerClient) (containerId: string) (workingDirectory: string) (command: string) (args: array<string>) : Async<bool> =
    async {
        try            
            let containerExecCreateParameters: ContainerExecCreateParameters = ContainerExecCreateParameters ()
            containerExecCreateParameters.Cmd <- Array.append [| command |] args
            containerExecCreateParameters.WorkingDir <- workingDirectory

            let! execCreateContainerResponse = dockerClient.Exec.ExecCreateContainerAsync (containerId, containerExecCreateParameters) |> Async.AwaitTask
            let! _startAndAttachContainerExecResponse = dockerClient.Exec.StartAndAttachContainerExecAsync (execCreateContainerResponse.ID, false) |> Async.AwaitTask
            
            let mutable isRunning: bool = true
            while isRunning do
                let! inspectContainerExecResponse = dockerClient.Exec.InspectContainerExecAsync (execCreateContainerResponse.ID) |> Async.AwaitTask
                isRunning <- inspectContainerExecResponse.Running
                if isRunning then
                    do! Async.Sleep 500
            
            let! finalInspectContainerExecResponse = dockerClient.Exec.InspectContainerExecAsync (execCreateContainerResponse.ID) |> Async.AwaitTask
            return finalInspectContainerExecResponse.ExitCode = 0
        with
        | ex ->
            printfn $"executeCommandInsideContainer: %s{ex.Message}"
            return false
    }

/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="imageId">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="submissions">TODO</param>
let createAndRunContainer (dockerClient: DockerClient) (imageId: string) (containerId: string) (submissions: list<string>) =
    let commandArguments: array<string> = [|
        "--auth"; "none"
        "--disable-telemetry"
        "--disable-update-check"
        "--disable-workspace-trust"
        "--disable-getting-started-override"
        "--bind-addr=0.0.0.0:8080"
        "/home/coder/Error-Pattern"
    |]
    
    // TODO
    
    async {
        try
            let config: CreateContainerParameters = CreateContainerParameters ()
            config.Image <- imageId
            config.Name <- containerId
            config.Cmd <- commandArguments
            config.ExposedPorts <- dict [ "8080/tcp", EmptyStruct () ]
            config.HostConfig <- HostConfig ()
            config.HostConfig.PortBindings <- dict [ "8080/tcp", [| PortBinding (HostPort="8080") |] ]
            config.HostConfig.Binds <- [| $"%s{ProjectPath}:/home/coder/Error-Pattern" |]

            let! createContainerResponse = dockerClient.Containers.CreateContainerAsync config |> Async.AwaitTask
            let! startContainerResponse = dockerClient.Containers.StartContainerAsync (createContainerResponse.ID, null) |> Async.AwaitTask
            
            return startContainerResponse
        with
        | ex ->
            printfn $"createAndRunContainer: %s{ex.Message}"
            return false
    }