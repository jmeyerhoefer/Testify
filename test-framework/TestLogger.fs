namespace TestLogger


open Microsoft.VisualStudio.TestTools.UnitTesting
open System


type TestLogger (testContext: TestContext) =
    member _.LogSuccess (methodName: string, input: obj array, expected: obj, actual: obj): unit =
        let inputString: string = String.Join (" ", input)
        testContext.WriteLine $"########################"
        testContext.WriteLine $"✅ %s{methodName} PASSED"
        testContext.WriteLine $"Input: %s{inputString}"
        testContext.WriteLine $"Expected: %O{expected}"
        testContext.WriteLine $"Actual: %O{actual}\n"
        testContext.WriteLine $"########################"

    member _.LogFailure (methodName: string, input: obj array, expected: obj, actual: obj): unit =
        let inputString: string = String.Join (" ", input)
        testContext.WriteLine $"❌ %s{methodName} FAILED"
        testContext.WriteLine $"Input: %s{inputString}"
        testContext.WriteLine $"Expected: %O{expected}"
        testContext.WriteLine $"Actual: %O{actual}\n"

    member _.LogInfo (message: string): unit =
        testContext.WriteLine $"ℹ️ %s{message}\n"
