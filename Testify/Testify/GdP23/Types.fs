namespace GdP23

open System
open System.IO

type CliStage =
    | Full
    | OnlyMaterialize
    | OnlyRun
    | OnlyNormalize
    | Cleanup

type CliOptions =
    {
        SheetId: string option
        AssignmentId: string option
        GroupIdTeamId: string option
        Timestamp: string option
        MaxParallel: int
        MaxExamplesPerTask: int
        AllSnapshots: bool
        CleanupAfter: bool
        Force: bool
        Stage: CliStage
    }

type SnapshotRecord =
    {
        SheetId: string
        AssignmentId: string
        GroupId: string
        TeamId: string
        SnapshotTimestamp: string
        Compiled: bool
        InternalError: bool
        TestsPassed: int option
        TestsTotal: int option
        ResultJson: string option
    }

type RemovedRecord =
    {
        SheetId: string
        AssignmentId: string
        GroupId: string
        TeamId: string
        PhysicalFileName: string
        DeleteTimestamp: string
    }

type TaskInfo =
    {
        SheetId: string
        AssignmentId: string
        ProjectFileName: string
        TemplateDirectory: string
        SolutionDirectory: string
        UploadsSheetDirectory: string
        ResultsRootDirectory: string
        PrimarySourceFileName: string
        ExpectedOriginalMethods: string list
        ExpectedTestifyMethods: string list
    }
    member self.SubmissionsDirectory(groupIdTeamId: string) : string =
        Path.Combine(self.UploadsSheetDirectory, groupIdTeamId, self.AssignmentId)

    member self.ResultDirectory(groupIdTeamId: string, timestamp: string) : string =
        Path.Combine(self.ResultsRootDirectory, self.SheetId, groupIdTeamId, self.AssignmentId, timestamp)

    member self.AllExpectedMethodNames: string list =
        Set.union (self.ExpectedOriginalMethods |> Set.ofList) (self.ExpectedTestifyMethods |> Set.ofList)
        |> Set.toList
        |> List.sort

    member self.DisplayName: string = $"{self.SheetId}/{self.AssignmentId}"

type SnapshotFile =
    {
        LogicalName: string
        PhysicalPath: string
        UploadedAt: string
    }

type SnapshotInfo =
    {
        Task: TaskInfo
        GroupIdTeamId: string
        Timestamp: string
        Files: SnapshotFile list
        SourceFilePresent: bool
        HistoricalRecord: SnapshotRecord option
    }
    member self.ResultDirectory: string = self.Task.ResultDirectory(self.GroupIdTeamId, self.Timestamp)

type MaterializedUpload =
    {
        LogicalName: string
        SourcePath: string
        RelativePath: string
    }

type HarnessConflict =
    {
        LogicalName: string
        SourcePath: string
    }

type WorkspaceManifest =
    {
        Snapshot: SnapshotInfo
        WorkspaceDirectory: string
        ProjectFilePath: string
        IncludedUploads: MaterializedUpload list
        IgnoredHarnessFiles: HarnessConflict list
        WorkspaceFiles: string list
    }

type RawRunArtifacts =
    {
        ResultDirectory: string
        BuildLogPath: string
        TestResultsPath: string
        TestifyResultsDirectory: string
        RunMetadataPath: string
    }

type ParsedTestResult =
    {
        SuiteName: string
        MethodName: string
        Outcome: string
        Output: string option
        FailureSummary: string option
        DurationSeconds: float option
    }

type PairedMethodResult =
    {
        SheetId: string
        AssignmentId: string
        GroupIdTeamId: string
        Timestamp: string
        MethodName: string
        OriginalOutcome: string option
        TestifyOutcome: string option
        OriginalOutput: string option
        TestifyOutput: string option
        OriginalFailureSummary: string option
        TestifyFailureSummary: string option
        OriginalDuration: float option
        TestifyDuration: float option
        BuildSucceeded: bool
        PairStatus: string
        SourceFilePresent: bool
    }

type SnapshotComparison =
    {
        SheetId: string
        AssignmentId: string
        GroupIdTeamId: string
        Timestamp: string
        BuildSucceeded: bool
        SourceFilePresent: bool
        Rows: PairedMethodResult list
    }

[<AutoOpen>]
module GdP23Paths =
    [<Literal>]
    let TestifySourceDirectoryName = "TestifySource"
    let RootPath: string = __SOURCE_DIRECTORY__
    let RepoRoot: string = Directory.GetParent(RootPath).FullName
    let UploadsRoot: string = Path.Combine(RootPath, "Uploads")
    let TemplatesRoot: string = Path.Combine(RootPath, "Templates")
    let DockerResultsRoot: string = Path.Combine(RootPath, "DockerResults")
    let WorkspacesRoot: string = Path.Combine(RootPath, ".workspaces")
    let DockerCacheRoot: string = Path.Combine(RootPath, ".cache")
    let DockerNuGetPackagesHostPath: string = Path.Combine(DockerCacheRoot, "nuget-packages")
    let DockerNuGetHttpCacheHostPath: string = Path.Combine(DockerCacheRoot, "nuget-http-cache")
    let DockerCliHomeHostPath: string = Path.Combine(DockerCacheRoot, "dotnet-cli-home")
    let DockerImageTag: string = "testify-gdp23-runner:local"
    let DockerWorkspacePath: string = "/workspace"
    let DockerResultsPath: string = "/results"
    let DockerNuGetPackagesPath: string = "/root/.nuget/packages"
    let DockerNuGetHttpCachePath: string = "/root/.local/share/NuGet/v3-cache"
    let DockerCliHomePath: string = "/tmp/dotnet-cli-home"
