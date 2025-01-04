module DockerController


open System.Formats.Tar
open Docker.DotNet
open Docker.DotNet.Models
open System.IO


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="imageId">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="taskInfo">TODO</param>
/// <param name="submissions">TODO</param>
let createAndRunContainer (dockerClient: DockerClient) (imageId: string) (containerId: string) (taskInfo: TaskInfo) (submissions: string list): bool Async =
    let workingDirectory: string = "/home/coder/Error-Pattern/"
    let commandArguments: string array = [|
        "--auth"; "none"
        "--disable-telemetry"
        "--disable-update-check"
        "--disable-workspace-trust"
        "--disable-getting-started-override"
        "--bind-addr=0.0.0.0:8080"
        workingDirectory
    |]

    async {
        let config: CreateContainerParameters = CreateContainerParameters ()
        config.Image <- imageId
        config.Name <- containerId
        config.Cmd <- commandArguments
        config.ExposedPorts <- dict [ "8080/tcp", EmptyStruct () ]
        config.HostConfig <- HostConfig ()
        config.HostConfig.PortBindings <- dict [ "8080/tcp", [| PortBinding (HostPort = "8080") |] ]

        let! createContainerResponse = dockerClient.Containers.CreateContainerAsync config |> Async.AwaitTask
        let! startContainerResponse = dockerClient.Containers.StartContainerAsync (createContainerResponse.ID, null) |> Async.AwaitTask

        let mutable isRunning: bool = false
        while not isRunning do
            let! inspectContainerResponse = dockerClient.Containers.InspectContainerAsync containerId |> Async.AwaitTask
            isRunning <- inspectContainerResponse.State.Running
            if isRunning then
                do! Async.Sleep 500

        let containerExecCreateParameters: ContainerExecCreateParameters = ContainerExecCreateParameters ()
        containerExecCreateParameters.Cmd <- [| "mkdir"; "-p"; workingDirectory |]
        let! execCreateDirectoryResponse = dockerClient.Exec.ExecCreateContainerAsync (containerId, containerExecCreateParameters) |> Async.AwaitTask
        do! dockerClient.Exec.StartContainerExecAsync execCreateDirectoryResponse.ID |> Async.AwaitTask

        let templateFiles: string list =
            taskInfo.GetTemplatePath ()
            |> Directory.GetFiles
            |> Array.filter (fun (filePath: string) -> filePath |> Path.GetFileName <> taskInfo.RelevantFileName)
            |> Array.toList

        for filePath: string in submissions do
            let fileNameWithoutTimestamp: string =
                filePath
                |> Path.GetFileName
                |> fun (fileName: string) -> fileName.Substring (fileName.IndexOf "-" + 1)
            let tarMemoryStream: MemoryStream = new MemoryStream ()
            use tarArchive: TarWriter = new TarWriter(tarMemoryStream, leaveOpen = true)
            tarArchive.WriteEntry (filePath, fileNameWithoutTimestamp)
            tarMemoryStream.Seek (0L, SeekOrigin.Begin) |> ignore
            let containerPathStatParameters: ContainerPathStatParameters = ContainerPathStatParameters ()
            containerPathStatParameters.Path <- workingDirectory
            do! dockerClient.Containers.ExtractArchiveToContainerAsync (containerId, containerPathStatParameters, tarMemoryStream) |> Async.AwaitTask

        for filePath: string in templateFiles do
            let tarMemoryStream: MemoryStream = new MemoryStream ()
            use tarArchive: TarWriter = new TarWriter(tarMemoryStream, leaveOpen = true)
            tarArchive.WriteEntry (filePath, filePath |> Path.GetFileName)
            tarMemoryStream.Seek (0L, SeekOrigin.Begin) |> ignore
            let containerPathStatParameters: ContainerPathStatParameters = ContainerPathStatParameters ()
            containerPathStatParameters.Path <- workingDirectory
            do! dockerClient.Containers.ExtractArchiveToContainerAsync (containerId, containerPathStatParameters, tarMemoryStream) |> Async.AwaitTask

        return startContainerResponse
    }


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="workingDirectory">TODO</param>
/// <param name="command">TODO</param>
/// <param name="args">TODO</param>
let executeCommandInsideContainer (dockerClient: DockerClient) (containerId: string) (workingDirectory: string) (command: string) (args: string array) : bool Async =
    async {
        let containerExecCreateParameters: ContainerExecCreateParameters = ContainerExecCreateParameters ()
        containerExecCreateParameters.Cmd <- Array.append [| command |] args
        containerExecCreateParameters.WorkingDir <- workingDirectory

        let! execCreateContainerResponse = dockerClient.Exec.ExecCreateContainerAsync (containerId, containerExecCreateParameters) |> Async.AwaitTask
        let! _startAndAttachContainerExecResponse = dockerClient.Exec.StartAndAttachContainerExecAsync (execCreateContainerResponse.ID, false) |> Async.AwaitTask

        let mutable isRunning: bool = true
        while isRunning do
            let! inspectContainerExecResponse = dockerClient.Exec.InspectContainerExecAsync execCreateContainerResponse.ID |> Async.AwaitTask
            isRunning <- inspectContainerExecResponse.Running
            if isRunning then
                do! Async.Sleep 500

        let! finalInspectContainerExecResponse = dockerClient.Exec.InspectContainerExecAsync execCreateContainerResponse.ID |> Async.AwaitTask
        return finalInspectContainerExecResponse.ExitCode = 0
    }


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="containerId">TODO</param>
/// <param name="containerPath">TODO</param>
/// <param name="hostPath">TODO</param>
let copyFilesFromContainer (dockerClient: DockerClient) (containerId: string) (containerPath: string) (hostPath: string): bool Async =
    async {
        let getArchiveFromContainerParameters: GetArchiveFromContainerParameters = GetArchiveFromContainerParameters ()
        getArchiveFromContainerParameters.Path <- containerPath
        let! getArchiveFromContainerResponse = dockerClient.Containers.GetArchiveFromContainerAsync (containerId, getArchiveFromContainerParameters, false) |> Async.AwaitTask

        use fileStream: FileStream = File.Create hostPath
        do! getArchiveFromContainerResponse.Stream.CopyToAsync fileStream |> Async.AwaitTask

        return true
    }


/// <summary>
/// TODO
/// </summary>
/// <param name="dockerClient">TODO</param>
/// <param name="containerId">TODO</param>
let stopAndRemoveContainer (dockerClient: DockerClient) (containerId: string): bool Async =
    async {
        let! _stopContainerResponse = dockerClient.Containers.StopContainerAsync (containerId, ContainerStopParameters ()) |> Async.AwaitTask

        let mutable isRunning: bool = true
        while isRunning do
            let! inspectContainerResponse = dockerClient.Containers.InspectContainerAsync containerId |> Async.AwaitTask
            isRunning <- inspectContainerResponse.State.Running
            if isRunning then
                do! Async.Sleep 500

        let containerRemoveParameters: ContainerRemoveParameters = ContainerRemoveParameters ()
        containerRemoveParameters.Force <- true
        do! dockerClient.Containers.RemoveContainerAsync (containerId, containerRemoveParameters) |> Async.AwaitTask
        return true
    }


// EOF