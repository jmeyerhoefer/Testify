module Statistics


open Plotly.NET
open Plotly.NET.Interactive
open Plotly.NET.ImageExport
open Plotly.NET.LayoutObjects
open RelevantInfo
open System.IO


let generateBuildResultsStatistics (exerciseId: string): unit =
    let stacktracePath: string = getStacktracePath exerciseId
    let statisticsPath: string = getStatisticsPath exerciseId

    statisticsPath
    |> Directory.CreateDirectory
    |> ignore

    let buildResults: string seq =
        (stacktracePath, "*.log", SearchOption.AllDirectories)
        |> Directory.GetFiles
        |> Array.toSeq

    let buildSucceeded: string = "Build succeeded."
    // let buildFailed: string = "Build FAILED."
    let counterSuccessful, counterFailed: int * int = 6829, 5983
        // buildResults
        // |> Seq.fold (fun (counterSuccessful: int, counterFailed: int) (buildResult: string) ->
        //     let buildWasSuccessful: bool =
        //         buildResult
        //         |> File.ReadAllLines
        //         |> Array.exists (fun (line: string) -> line.Contains buildSucceeded)
        //     if buildWasSuccessful then
        //         counterSuccessful + 1, counterFailed
        //     else
        //         counterSuccessful, counterFailed + 1
        // ) (0, 0)

    let buildResultsValues: int list = [ counterSuccessful; counterFailed ]
    let buildResultsKeys: string list = [ "Successful builds"; "Failed builds" ]
    let buildResultsAnnotationSuccessful: Annotation =
        let text: string = $"%d{counterSuccessful}"
        Annotation.init (X = 0, Y = counterSuccessful, Text = text, BGColor = Color.fromString "white", BorderColor = Color.fromString "black")
    let buildResultsAnnotationFailed: Annotation =
        let text: string = $"%d{counterFailed}"
        Annotation.init (X = 1.0, Y = counterFailed, Text = text, BGColor = Color.fromString "white", BorderColor = Color.fromString "black")
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

    let buildResultsChartPath: string = Path.Combine (statisticsPath, "buildResultsColumnChart")
    buildResultsChart
    |> Chart.savePNG buildResultsChartPath


let generateTestResultsStatistics (exerciseId: string): unit =
    let stacktracePath: string = getStacktracePath exerciseId
    let statisticsPath: string = getStatisticsPath exerciseId

    statisticsPath
    |> Directory.CreateDirectory
    |> ignore

    let testResults: string seq =
        (stacktracePath, "*.xml", SearchOption.AllDirectories)
        |> Directory.GetFiles
        |> Array.toSeq

    testResults
    |> Seq.iter (fun (testResult: string) ->
        ()
    )

let generateStatistics (exerciseId: string): unit =
    generateBuildResultsStatistics exerciseId
    // generateTestResultsStatistics exerciseId


// EOF