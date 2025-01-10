module Statistics


open Plotly.NET
open Plotly.NET.ImageExport
open Plotly.NET.LayoutObjects
open RelevantInfo
open System
open System.IO
open System.Xml


let countSuccessfulAndFailedBuildsTotal (buildResults: string seq): int * int =
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

    let averagePassedPercentage, totalFiles, totalInvalidFiles: float * int * int =
        testResults
        |> Seq.fold (fun (totalPassedPercentage: float, totalFiles: int, totalInvalidFiles: int) (testResult: string) ->
            let lines: string array = File.ReadAllLines testResult
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
    |> List.filter (fun (taskInfo: TaskInfo) -> taskInfo.SheetId <> "08" || taskInfo.AssignmentId <> "2")
    |> List.iter (fun (taskInfo: TaskInfo) ->
        let averagePassedPercentage, totalFiles, totalInvalidFiles: float * int * int =
            taskInfo.GetStacktracePaths ()
            |> Seq.collect (fun (stacktracePath: string) ->
                (stacktracePath, "*.xml", SearchOption.AllDirectories)
                |> Directory.GetFiles
                |> Array.toSeq
            )
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
    |> List.filter (fun (taskInfo: TaskInfo) -> taskInfo.SheetId <> "08" || taskInfo.AssignmentId <> "2")
    |> List.map (fun (taskInfo: TaskInfo) ->
        let averagePassedPercentage, _totalFiles, _totalInvalidFiles: float * int * int =
            taskInfo.GetStacktracePaths ()
            |> Seq.collect (fun (stacktracePath: string) ->
                (stacktracePath, "*.xml", SearchOption.AllDirectories)
                |> Directory.GetFiles
                |> Array.toSeq
            )
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
    |> Chart.withTitle $"Average percentage of passed tests"
    |> Chart.savePNG (Path.Combine (statisticsPath, $"testResultsColumnChart-Combined"))

(*

Index: 2
Average passed percentage: 82.75%
Total files: 341
Total invalid files: 0

Index: 3
Average passed percentage: 79.57%
Total files: 553
Total invalid files: 6

Index: 4
Average passed percentage: 70.59%
Total files: 729
Total invalid files: 0

Index: 5
Average passed percentage: 73.74%
Total files: 669
Total invalid files: 18

Index: 6
Average passed percentage: 70.20%
Total files: 654
Total invalid files: 0

Index: 7
Average passed percentage: 61.43%
Total files: 919
Total invalid files: 18

Index: 8
Average passed percentage: 81.63%
Total files: 230
Total invalid files: 0

Index: 9
Average passed percentage: NaN%
Total files: 0
Total invalid files: 0

Index: 10
Average passed percentage: 67.02%
Total files: 601
Total invalid files: 0

Index: 11
Average passed percentage: 67.94%
Total files: 614
Total invalid files: 0

Index: 12
Average passed percentage: NaN%
Total files: 0
Total invalid files: 0

/


// Average passed percentage: 70.84%
// Total files: 5310 (Uploads excluded for sheet: 08, assignment: 2, because there are no tests)
// Total invalid files: 42

 *)


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
/// <param name="relevantTasks">TODO</param>
let generateStatistics (exerciseId: string) (relevantTasks: TaskInfo list): unit =
    // generateBuildResultsStatisticsTotal exerciseId
    // generateTestResultsStatisticsTotal exerciseId
    // generateTestResultsStatisticsPerTask exerciseId relevantTasks
    generateTestResultsStatisticsPerTaskCombined exerciseId relevantTasks


// EOF