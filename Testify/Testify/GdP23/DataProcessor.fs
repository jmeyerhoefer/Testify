namespace GdP23

module DataProcessor =
    open Normalization
    open System
    open System.IO
    open System.Threading
    open System.Threading.Tasks
    open TaskPipeline

    let private parseComparisonFileRowCount (path: string) : int =
        if not (File.Exists path) then
            0
        else
            use document = System.Text.Json.JsonDocument.Parse(File.ReadAllText path)
            let mutable rowsElement = Unchecked.defaultof<System.Text.Json.JsonElement>

            if document.RootElement.TryGetProperty("rows", &rowsElement) then
                rowsElement.GetArrayLength()
            else
                0

    let private processSnapshot (options: CliOptions) (snapshot: SnapshotInfo) : unit =
        log $"Processing snapshot {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"

        let shouldSkip =
            options.Stage = CliStage.Full
            && not options.Force
            && hasCompletedSnapshot snapshot

        if shouldSkip then
            log $"Skipping already completed snapshot {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
        else
            match options.Stage with
            | CliStage.Cleanup ->
                log $"Cleaning up snapshot artifacts for {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                cleanupSnapshotArtifacts snapshot
            | CliStage.OnlyNormalize ->
                log $"Normalizing existing snapshot {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                normalizeExistingSnapshot snapshot
            | CliStage.OnlyMaterialize ->
                log $"Materializing workspace for {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                materializeWorkspace snapshot options.Force |> ignore
            | CliStage.OnlyRun ->
                materializeWorkspace snapshot options.Force
                |> fun manifest ->
                    log $"Queueing Docker work for {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                    runSnapshotInDocker manifest
                |> ignore
            | CliStage.Full ->
                let artifacts =
                    materializeWorkspace snapshot options.Force
                    |> fun manifest ->
                        log $"Queueing Docker work for {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                        runSnapshotInDocker manifest

                log $"Normalizing results for {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                normalizeSnapshot snapshot artifacts |> ignore

    let run (options: CliOptions) : int =
        ensureDirectory DockerResultsRoot

        let tasks =
            loadTaskInfos ()
            |> List.filter (fun task ->
                match options.SheetId, options.AssignmentId with
                | Some sheetId, Some assignmentId -> task.SheetId = sheetId && task.AssignmentId = assignmentId
                | Some sheetId, None -> task.SheetId = sheetId
                | None, Some assignmentId -> task.AssignmentId = assignmentId
                | None, None -> true)

        if List.isEmpty tasks then
            log "No rewritten tasks matched the provided filters."
            0
        else
            let dataset = loadDataset ()

            let snapshotsByTask =
                tasks
                |> List.map (fun task -> task, getSnapshots dataset task options)

            for task, taskSnapshots in snapshotsByTask do
                log $"Selected {taskSnapshots.Length} snapshot(s) for {task.DisplayName}."

            let snapshots =
                snapshotsByTask
                |> List.collect snd
                |> List.sortBy (fun snapshot -> snapshot.Task.SheetId, snapshot.GroupIdTeamId, snapshot.Task.AssignmentId, snapshot.Timestamp)

            if List.isEmpty snapshots then
                log "No snapshots matched the provided filters."
                0
            else
                if options.Stage = CliStage.Full || options.Stage = CliStage.OnlyRun then
                    ensureDockerImage ()

                use semaphore = new SemaphoreSlim(options.MaxParallel)

                let totalSnapshots = snapshots.Length
                let completedSnapshots = ref 0
                use heartbeatCts = new CancellationTokenSource()

                let heartbeat =
                    Task.Run(fun () ->
                        while not heartbeatCts.Token.IsCancellationRequested do
                            Task.Delay(30000, heartbeatCts.Token).Wait()

                            if not heartbeatCts.Token.IsCancellationRequested then
                                let completed = !completedSnapshots
                                log $"Heartbeat: completed {completed}/{totalSnapshots} snapshots. Remaining: {totalSnapshots - completed}.")

                let jobs =
                    snapshots
                    |> List.map (fun snapshot ->
                        Task.Run(fun () ->
                            semaphore.Wait()

                            try
                                try
                                    processSnapshot options snapshot
                                    let completed = Interlocked.Increment completedSnapshots
                                    log $"Completed snapshot {completed}/{totalSnapshots}: {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                                with ex ->
                                    ensureDirectory snapshot.ResultDirectory
                                    writeJson
                                        (Path.Combine(snapshot.ResultDirectory, "run-metadata.json"))
                                        {| error = ex.ToString()
                                           sheetId = snapshot.Task.SheetId
                                           assignmentId = snapshot.Task.AssignmentId
                                           groupIdTeamId = snapshot.GroupIdTeamId
                                           timestamp = snapshot.Timestamp |}
                                    let completed = Interlocked.Increment completedSnapshots
                                    log $"Failed snapshot {completed}/{totalSnapshots}: {snapshot.Task.DisplayName} / {snapshot.GroupIdTeamId} / {snapshot.Timestamp}"
                            finally
                                semaphore.Release() |> ignore))

                Task.WhenAll(jobs).GetAwaiter().GetResult()
                heartbeatCts.Cancel()

                try
                    heartbeat.Wait()
                with
                | :? System.AggregateException -> ()

                if options.Stage = CliStage.Full || options.Stage = CliStage.OnlyNormalize then
                    rewriteAggregateJsonl ()
                    rewriteSelectedCsv snapshots

                if options.CleanupAfter && options.Stage <> CliStage.Cleanup then
                    for snapshot in snapshots do
                        cleanupSnapshotArtifacts snapshot

                    log $"Cleaned up transient artifacts for {snapshots.Length} snapshot(s)."

                let completedComparisons =
                    Directory.GetFiles(DockerResultsRoot, "comparison.json", SearchOption.AllDirectories)
                    |> Array.sumBy parseComparisonFileRowCount

                log $"Finished processing {snapshots.Length} snapshot(s). Aggregate comparison rows: {completedComparisons}."
                0
