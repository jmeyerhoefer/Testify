namespace Testify


open Microsoft.VisualStudio.TestTools.UnitTesting


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute</c> class.</summary>
type TestifyClassAttribute =
    TestClassAttribute


/// <summary>
/// MSTest method attribute that also captures Testify assertion/property output and persists it as a Testify XML result file.
/// </summary>
type TestifyMethodAttribute() =
    inherit TestMethodAttribute()

    member val Verbosity = Verbosity.Default with get, set
    member val MaxValueLines = 0 with get, set

    member private this.CreateReportOptions() : TestifyReportOptions =
        let defaults =
            TestifySettings.DefaultReportOptions
            |> TestifyReportOptions.normalize
        let verbosity =
            match this.Verbosity with
            | Verbosity.Default -> defaults.Verbosity
            | value -> value

        let maxValueLines =
            if this.MaxValueLines > 0 then
                this.MaxValueLines
            else
                defaults.MaxValueLines

        {
            defaults with
                Verbosity = verbosity
                MaxValueLines = maxValueLines
        }

    override this.Execute(testMethod: ITestMethod) : TestResult array =
        TestExecution.beginTest
            testMethod.TestClassName
            testMethod.TestMethodName
            (this.CreateReportOptions ())

        let results =
            try
                base.Execute testMethod
            with ex ->
                [| TestResults.createSyntheticFailure testMethod.TestMethodName ex |]

        let state = TestExecution.endTest ()
        TestResults.writeResults state results |> ignore
        results


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute</c> class.</summary>
type TimeoutAttribute =
    Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute
