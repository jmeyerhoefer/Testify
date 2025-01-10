module TestWrapper


open System
open System.Text
open Microsoft.VisualStudio.TestTools.UnitTesting


type Wrapper =
    static member AreEqual (expected: obj, actual: obj, methodName: string, input: obj array): unit =
        if not (expected = actual) then
            let inputString: string = String.Join (" ", input)
            StringBuilder()
                .AppendLine()
                .AppendLine($"Method name: %s{methodName}")
                .AppendLine($"Input: %s{inputString}")
                .AppendLine($"Expected result: %O{expected}")
                .AppendLine($"Actual result:  %O{actual}")
                .ToString ()
            |> failwith
        else
            Assert.AreEqual (expected = expected, actual = actual)


// EOF