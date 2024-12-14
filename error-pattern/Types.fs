[<AutoOpen>]
module Types


open System.IO


/// Path to the source directory.
let RootPath: string = __SOURCE_DIRECTORY__


/// Path to the directory, where they are built and tested.
let ProjectPath: string = Path.Combine (RootPath, "data", "Project")


/// <summary>
/// Used to indicate whether building and testing of a project has succeeded or what failed.
/// </summary>
type BuildAndTestResult =
    | Success
    | BuildFailed
    | TestFailed
    | UnexpectedError of string


/// <summary>
/// Contains the relevant info of a task.
/// </summary>
/// <param name="exerciseId">ID of the exercise.</param>
/// <param name="sheetId">ID of the exercise sheet.</param>
/// <param name="assignmentId">ID of the assignment.</param>
/// <param name="assignmentTitle">Title of the assignment.</param>
/// <param name="relevantFileName">The name of the file that should be submitted.</param>
type TaskInfo (exerciseId: string, sheetId: string, assignmentId: string, assignmentTitle: string, relevantFileName: string) =
    /// ID of the exercise.
    member _.ExerciseId: string = exerciseId

    /// ID of the exercise sheet.
    member _.SheetId: string = sheetId
    
    /// ID of the assignment.    
    member _.AssignmentId: string = assignmentId

    /// Title of the assignment.
    member _.AssignmentTitle: string = assignmentTitle

    /// The name of the file that should be submitted.
    member _.RelevantFileName: string = relevantFileName

    /// <summary>
    /// Builds the path to the template of a task.
    /// <p>Path: <c>~/data/{ExerciseId}/Templates/{SheetId}{AssignmentId}/template/</c></p>
    /// </summary>
    /// <returns>The template path of a task as string.</returns>
    member self.GetTemplatePath (): string =
        Path.Combine (RootPath, "data", "Exercises", self.ExerciseId, "Templates", self.SheetId, self.AssignmentId, "template")

    /// <summary>
    /// Builds the path to the groupAndTeam submissions of a task.
    /// <p>Path: <c>~/data/{ExerciseId}/Uploads/{SheetId}/</c></p>
    /// </summary>
    /// <returns>The path to the uploads of a task as string.</returns>
    member self.GetSheetUploadsPath (): string =
        Path.Combine (RootPath, "data", "Exercises", self.ExerciseId, "Uploads", self.SheetId)

    /// <summary>
    /// Builds the path to the stacktrace of a groupAndTeam specific submission.
    /// <p>Path: <c>~/data/{ExerciseId}/Stacktrace/{SheetId}/{groupAndTeamId}/{AssignmentId}/</c></p>
    /// </summary>
    /// <param name="groupAndTeamId">ID of a specific group and team.</param>
    /// <returns>The path to the stacktrace of a submission as string.</returns>
    member self.GetStacktracePath (groupAndTeamId: string): string =
        Path.Combine (RootPath, "data", "Exercises", self.ExerciseId, "Stacktrace", self.SheetId, groupAndTeamId, self.AssignmentId)

    /// <summary>
    /// Builds the path to a groupAndTeam specific submission.
    /// <p>Path: <c>~/data/{ExerciseId}/Uploads/{SheetId}/{groupAndTeamId}/{AssignmentId}/</c></p>
    /// </summary>
    /// <param name="groupAndTeamId">ID of a specific group and team.</param>
    /// <returns>The path to a specific submission as string.</returns>
    member self.GetSubmissionsPath (groupAndTeamId: string): string =
        Path.Combine (RootPath, "data", "Exercises", self.ExerciseId, "Uploads", self.SheetId, groupAndTeamId, self.AssignmentId)

    /// <summary>
    /// Determines all group and team ID's that submitted something for this task.
    /// </summary>
    /// <returns>An array of all group and team ID's.</returns>
    member self.GetGroupAndTeamIds (): array<string> =
        self.GetSheetUploadsPath ()
        |> Directory.GetDirectories
        |> Array.map Path.GetFileName

    override self.ToString (): string =
        let eId: string = self.ExerciseId
        let sId: string = self.SheetId
        let aId: string = self.AssignmentId
        let aTitle: string = self.AssignmentTitle
        let fileName: string = self.RelevantFileName
        $"Exercise: %s{eId}, Sheet %s{sId}, Assignment %s{aId}: %s{aTitle} (%s{fileName})"


// EOF