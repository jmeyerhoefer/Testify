namespace Testify


open System.Reflection
open System.Threading


type TestExecutionState =
    {
        TestName: string
        TestClassName: string
        MethodIdentity: string
        TestMethodSourceLocation: Diagnostics.SourceLocation option
        mutable FirstTestedSourceLocation: Diagnostics.SourceLocation option
        ReportOptions: TestifyReportOptions
        mutable LastFailureReport: TestifyFailureReport option
    }


[<RequireQualifiedAccess>]
module TestExecution =
    let private current = AsyncLocal<TestExecutionState option>()

    let private updateCurrentState
        (update: TestExecutionState -> unit)
        : unit =
        match current.Value with
        | Some state -> update state
        | None -> ()

    let createState
        (testClassName: string)
        (testName: string)
        (methodInfo: MethodInfo)
        (reportOptions: TestifyReportOptions)
        : TestExecutionState =
        {
            TestName = testName
            TestClassName = testClassName
            MethodIdentity = $"{testClassName}.{testName}"
            TestMethodSourceLocation = SourceMapping.tryFindSourceLocationFromMethodInfo methodInfo
            FirstTestedSourceLocation = None
            ReportOptions = TestifyReportOptions.normalize reportOptions
            LastFailureReport = None
        }

    let beginTest
        (testClassName: string)
        (testName: string)
        (methodInfo: MethodInfo)
        (reportOptions: TestifyReportOptions)
        : unit =
        current.Value <- Some (createState testClassName testName methodInfo reportOptions)

    let currentState () : TestExecutionState option =
        current.Value

    let currentReportOptions () : TestifyReportOptions =
        match current.Value with
        | Some state -> state.ReportOptions
        | None -> TestifySettings.DefaultReportOptions |> TestifyReportOptions.normalize

    let recordTestedSourceLocation
        (location: Diagnostics.SourceLocation option)
        : unit =
        match location with
        | Some location ->
            updateCurrentState (fun state ->
                if state.FirstTestedSourceLocation.IsNone then
                    state.FirstTestedSourceLocation <- Some location)
        | None -> ()

    let recordFailureReport
        (report: TestifyFailureReport)
        : unit =
        updateCurrentState (fun state -> state.LastFailureReport <- Some report)

    let endTest () : TestExecutionState option =
        let state = current.Value
        current.Value <- None
        state
