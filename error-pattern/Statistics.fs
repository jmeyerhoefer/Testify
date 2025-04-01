module Statistics


open System.Text.RegularExpressions
open ErrorDescriptions
open Plotly.NET
open Plotly.NET.ImageExport
open Plotly.NET.LayoutObjects
open RelevantInfo
open System
open System.IO
open System.Xml


let private countSuccessfulAndFailedBuildsTotal (buildResults: string seq): int * int =
    let buildSucceeded: string = "Build succeeded."
    buildResults
    |> Seq.fold (fun (counterSuccessful: int, counterFailed: int) (buildResult: string) ->
        let buildWasSuccessful: bool =
            buildResult
            |> File.ReadAllLines
            |> Array.exists (fun (line: string) -> line.Contains buildSucceeded)
        if buildWasSuccessful then
            counterSuccessful + 1, counterFailed
        else
            counterSuccessful, counterFailed + 1
    ) (0, 0)


let private getAveragePassedTestsInfos (submissions: string seq): float * int * int =
    submissions
    |> Seq.fold (fun (totalPassedPercentage: float, totalFiles: int, totalInvalidFiles: int) (stacktracePath: string) ->
        let lines: string array = File.ReadAllLines stacktracePath
        if lines.Length > 1 then
            let firstLineTrimmed: string = lines[0].Remove (0, lines[0].IndexOf "<")
            let middleLines: string array = lines[1 .. lines.Length - 2]
            let lastLine: string = lines[lines.Length - 1]
            let lastLineTrimmed: string = lastLine.Remove (lastLine.IndexOf ">" + 1)
            let cleanedXmlContent: string = String.Join (Environment.NewLine, [| firstLineTrimmed; yield! middleLines; lastLineTrimmed |])
            let xmlDocument: XmlDocument = XmlDocument ()
            xmlDocument.LoadXml cleanedXmlContent
            let testResultsCollection: XmlNodeList = xmlDocument.SelectNodes "//collection"

            if testResultsCollection.Count = 1 then
                let testResultsCollectionNode: XmlNode = testResultsCollection[0]
                let total: int = int testResultsCollectionNode.Attributes["total"].Value
                let passed: int =  int testResultsCollectionNode.Attributes["passed"].Value
                let passedPercentage: float = 100.0 * float passed / float total

                totalPassedPercentage + passedPercentage, totalFiles + 1, totalInvalidFiles
            else
                totalPassedPercentage, totalFiles + 1, totalInvalidFiles + 1
        else
            totalPassedPercentage, totalFiles + 1, totalInvalidFiles + 1
    ) (0.0, 0, 0)
    |> fun (totalPassedPercentage: float, totalFiles: int, totalInvalidFiles: int) -> totalPassedPercentage / float totalFiles, totalFiles, totalInvalidFiles


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
let generateBuildResultsStatisticsTotal (exerciseId: string): unit =
    let stacktracePath: string = getStacktracePath exerciseId
    let statisticsPath: string = getStatisticsPath exerciseId
    Directory.CreateDirectory statisticsPath |> ignore

    let buildResults: string seq =
        (stacktracePath, "*.log", SearchOption.AllDirectories)
        |> Directory.GetFiles
        |> Array.toSeq

    let counterSuccessful, counterFailed: int * int = 6829, 5983
    // let counterSuccessful, counterFailed: int * int = countSuccessfulAndFailedBuildsTotal buildResults
    let buildResultsValues: int list = [ counterSuccessful; counterFailed ]
    let buildResultsKeys: string list = [ "Successful builds"; "Failed builds" ]
    let buildResultsAnnotationSuccessful: Annotation =
        Annotation.init (
            X = 0,
            Y = counterSuccessful,
            Text = $"%d{counterSuccessful}",
            BGColor = Color.fromString "white",
            BorderColor = Color.fromString "black"
        )
    let buildResultsAnnotationFailed: Annotation =
        Annotation.init (
            X = 1.0,
            Y = counterFailed,
            Text = $"%d{counterFailed}",
            BGColor = Color.fromString "white",
            BorderColor = Color.fromString "black"
        )
    let successRate: float = 100.0 * float counterSuccessful / float (counterSuccessful + counterFailed)
    let buildResultsChart: GenericChart =
        [
            Chart.Column (
                Name = "Successful builds",
                values = [ buildResultsValues[0] ],
                Keys = [ buildResultsKeys[0] ],
                Width = 0.5,
                MarkerColor = Color.fromString "green"
            )
            Chart.Column (
                Name = "Failed builds",
                values = [ buildResultsValues[1] ],
                Keys = [ buildResultsKeys[1] ],
                Width = 0.5,
                MarkerColor = Color.fromString "red")
        ]
        |> Chart.combine
        |> Chart.withTitle $"Overview \'dotnet build\' results (Success rate: %.2f{successRate}%%)"
        |> Chart.withAnnotations [ buildResultsAnnotationSuccessful; buildResultsAnnotationFailed ]
        |> Chart.withYAxisStyle (MinMax = (0, 100))

    buildResultsChart |> Chart.savePNG (Path.Combine (statisticsPath, "buildResultsColumnChart-Total"))


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
let generateTestResultsStatisticsTotal (exerciseId: string): unit =
    let stacktracePath: string = getStacktracePath exerciseId
    let statisticsPath: string = getStatisticsPath exerciseId
    Directory.CreateDirectory statisticsPath |> ignore

    let testResults: string seq =
        (stacktracePath, "*.xml", SearchOption.AllDirectories)
        |> Directory.GetFiles
        |> Array.toSeq

    let averagePassedPercentage, totalFiles, totalInvalidFiles: float * int * int = getAveragePassedTestsInfos testResults

    let testResultsAnnotation: Annotation =
        Annotation.init (
            X = 0,
            Y = averagePassedPercentage,
            Text = $"Average: %.2f{averagePassedPercentage}%% (%d{totalFiles - totalInvalidFiles} total files)",
            BGColor = Color.fromString "white",
            BorderColor = Color.fromString "black"
        )
    let testResultsChart: GenericChart =
        Chart.Column (
            Name = "Average percentage of passed tests",
            values = [ averagePassedPercentage ],
            Keys = [ "Passed Tests" ],
            Width = 0.25,
            MarkerColor = Color.fromString "blue"
        )
        |> Chart.withTitle "Average percentage of passed tests"
        |> Chart.withAnnotation testResultsAnnotation
        |> Chart.withYAxisStyle (MinMax = (0, 100))

    testResultsChart |> Chart.savePNG (Path.Combine (statisticsPath, "testResultsColumnChart-Total"))


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
/// <param name="relevantTasks">TODO</param>
let generateTestResultsStatisticsPerTask (exerciseId: string) (relevantTasks: TaskInfo list): unit =
    let statisticsPath: string = getStatisticsPath exerciseId

    relevantTasks
    |> List.iter (fun (taskInfo: TaskInfo) ->
        let averagePassedPercentage, totalFiles, totalInvalidFiles: float * int * int =
            taskInfo.GetStacktracePaths ()
            |> Seq.collect (fun (stacktracePath: string) ->
                (stacktracePath, "*.xml", SearchOption.AllDirectories)
                |> Directory.GetFiles
                |> Array.toSeq
            )
            |> getAveragePassedTestsInfos

        let testResultsAnnotation: Annotation =
            Annotation.init (
                X = 0,
                Y = averagePassedPercentage,
                Text = $"Average: %.2f{averagePassedPercentage}%% (%d{totalFiles - totalInvalidFiles} total files)",
                BGColor = Color.fromString "white",
                BorderColor = Color.fromString "black"
            )
        let testResultsChart: GenericChart =
            Chart.Column (
                Name = "Average percentage of passed tests",
                values = [ averagePassedPercentage ],
                Keys = [ "Passed Tests" ],
                Width = 0.25,
                MarkerColor = Color.fromString "blue"
            )
            |> Chart.withTitle $"Average percentage of passed tests for task {taskInfo.SheetId}-{taskInfo.AssignmentId}"
            |> Chart.withAnnotation testResultsAnnotation
            |> Chart.withYAxisStyle (MinMax = (0, 100))

        testResultsChart |> Chart.savePNG (Path.Combine (statisticsPath, $"testResultsColumnChart-{taskInfo.SheetId}-{taskInfo.AssignmentId}"))
    )


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
/// <param name="relevantTasks">TODO</param>
let generateTestResultsStatisticsPerTaskCombined (exerciseId: string) (relevantTasks: TaskInfo list): unit =
    let statisticsPath: string = getStatisticsPath exerciseId

    relevantTasks
    |> List.map (fun (taskInfo: TaskInfo) ->
        let averagePassedPercentage, _totalFiles, _totalInvalidFiles: float * int * int =
            taskInfo.GetStacktracePaths ()
            |> Seq.collect (fun (stacktracePath: string) ->
                (stacktracePath, "*.xml", SearchOption.AllDirectories)
                |> Directory.GetFiles
                |> Array.toSeq
            )
            |> getAveragePassedTestsInfos

        Chart.Column (
            Name = $"%s{taskInfo.SheetId}-%s{taskInfo.AssignmentId}",
            values = [ averagePassedPercentage ],
            Keys = [ $"%s{taskInfo.SheetId}-%s{taskInfo.AssignmentId}" ],
            Width = 0.25
        )
        |> Chart.withXAxisStyle (AxisType = StyleParam.AxisType.Category)
        |> Chart.withYAxisStyle (MinMax = (0, 100))
    )
    |> Chart.combine
    |> Chart.withTitle "Average percentage of passed tests"
    |> Chart.savePNG (Path.Combine (statisticsPath, "testResultsColumnChart-Combined"))


let generateTestResultsFirstAndLastSubmission (exerciseId: string) (relevantTasks: TaskInfo list): unit =
    let statisticsPath: string = getStatisticsPath exerciseId

    relevantTasks
    |> List.map (fun (taskInfo: TaskInfo) ->
        taskInfo.GetStacktracePaths ()
        |> Seq.map (fun (groupAndTeamPath: string) ->
            groupAndTeamPath
            |> Directory.GetDirectories
            |> Seq.collect (fun (stacktracePath: string) ->
                (stacktracePath, "*.xml", SearchOption.AllDirectories)
                |> Directory.GetFiles
                |> Array.toSeq
            )
        )
        |> Seq.filter (fun (directories: string seq) -> directories |> Seq.length >= 2)
        |> Seq.map (fun (directories: string seq) -> directories |> Seq.head, directories |> Seq.last)
        |> List.ofSeq
        |> List.unzip
        |> fun (firstSubmissions: string list, lastSubmissions: string list) ->
            let firstSubmissionsAverage, _, _: float * int * int = getAveragePassedTestsInfos firstSubmissions
            let lastSubmissionAverage, _, _: float * int * int = getAveragePassedTestsInfos lastSubmissions
            let chart: GenericChart = Chart.Column (
                Name = $"%s{taskInfo.SheetId}-%s{taskInfo.AssignmentId}",
                values = [ firstSubmissionsAverage; lastSubmissionAverage ],
                Keys = [ $"%s{taskInfo.SheetId}-%s{taskInfo.AssignmentId}: First"; $"%s{taskInfo.SheetId}-%s{taskInfo.AssignmentId}: Last" ],
                Width = 0.25
            )
            firstSubmissionsAverage, lastSubmissionAverage, chart
    )
    |> fun (lst: (float * float * GenericChart) list) ->
        let averageFirst: float = lst |> List.averageBy (fun (a: float, _, _) -> a)
        printfn $"Average First: %f{averageFirst}"
        let averageLast: float = lst |> List.averageBy (fun (_, b: float, _) -> b)
        printfn $"Average Last: %f{averageLast}"
        lst |> List.map (fun (_, _, chart: GenericChart) -> chart)
    |> Chart.combine
    |> Chart.savePNG (Path.Combine (statisticsPath, "testResultsColumnChartFirstAndLastSubmission-Combined"))


let generateBuildResultsTop10Errors (exerciseId: string): unit =
    let stacktracePath: string = getStacktracePath exerciseId
    let statisticsPath: string = getStatisticsPath exerciseId

    let fsharpErrorPattern: string = @"1>(\/.*?): error (FS\d{4}): (.*?)(?=\n)"
    let regex: Regex = Regex (fsharpErrorPattern, RegexOptions.Singleline)
    let errorCodes: (string * int) seq =
        (stacktracePath, "*.log", SearchOption.AllDirectories)
        |> Directory.GetFiles
        |> Array.toSeq
        |> Seq.collect (fun (buildResult: string) ->
            let fileContent: string = File.ReadAllText buildResult
            [ for m: Match in regex.Matches fileContent -> m.Groups[2].Value ]
        )
        |> Seq.groupBy id
        |> Seq.map (fun (code: string, group: string seq) -> code, (group |> Seq.length))
        |> Seq.sortByDescending snd

    Chart.Column (
        Name = "Top error codes",
        values = (errorCodes |> Seq.map snd),
        Keys = (errorCodes |> Seq.map fst),
        Width = 1
    )
    |> Chart.savePNG (Path.Combine (statisticsPath, "buildResultsColumnChartTop10Errors"))

    let totalCount: int = errorCodes |> Seq.sumBy snd
    // printfn $"{totalCount}"

    let errorCodesTableRows: string list seq =
        errorCodes
        |> Seq.mapi (fun (index: int) (code: string, count: int) ->
            let errorCode: int = int Regex.Match(code, @"FS(\d{4})").Groups[1].Value
            let errorCodeDescription: ErrorCodeDescription = getErrorCodeDescription errorCode
            [ string (index + 1); code; errorCodeDescription.ShortDescription; string count; string (Math.Round((double count / double totalCount) * 100.0, 3)) + "%" ]
        )
    Chart.Table (
        Name = "Top error codes",
        headerValues = [ "<b>#</b>"; "<b>Error code</b>"; "<b>Short Description</b>"; "<b>Count</b>"; "<b>Percentage</b>" ],
        cellsValues = errorCodesTableRows,
        CellsMultiAlign = [
            StyleParam.HorizontalAlign.Center
            StyleParam.HorizontalAlign.Center
            StyleParam.HorizontalAlign.Left
            StyleParam.HorizontalAlign.Right
            StyleParam.HorizontalAlign.Right
        ],
        MultiColumnWidth = [ 40.; 70.; 300.; 60.; 60.]
    )
    |> fun (genericChart: GenericChart) ->
        genericChart
        |> Chart.savePNG (
            path = Path.Combine (statisticsPath, "buildResultsTableChartTop10Errors"),
            Width = 1000,
            Height = 2500
        )
        genericChart
        |> Chart.saveHtml (Path.Combine (statisticsPath, "buildResultsTableChartTop10Errors"))


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
/// <param name="relevantTasks">TODO</param>
let generateStatistics (exerciseId: string) (relevantTasks: TaskInfo list): unit =
    // generateBuildResultsStatisticsTotal exerciseId
    // generateTestResultsStatisticsTotal exerciseId
    // generateTestResultsStatisticsPerTask exerciseId relevantTasks
    // generateTestResultsStatisticsPerTaskCombined exerciseId relevantTasks
    generateTestResultsFirstAndLastSubmission exerciseId relevantTasks
    // generateBuildResultsTop10Errors exerciseId


// EOF