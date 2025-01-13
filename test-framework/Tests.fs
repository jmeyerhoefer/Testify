module Tests


open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck
open Swensen.Unquote
open TestLogger


/// <summary>
/// Correct implementation of add1.
/// </summary>
/// <param name="a">first number</param>
/// <param name="b">second number</param>
/// <returns>The sum of <c>a</c> and <c>b</c>.</returns>
let add1 (a: int, b: int): int =
    a + b


/// <summary>
/// Correct implementation of add2.
/// </summary>
/// <param name="a">first number</param>
/// <param name="b">second number</param>
/// <returns>The sum of <c>a</c> and <c>b</c>.</returns>
let add2 (a: int) (b: int): int =
    a + b


[<TestClass>]
type Tests () =
    let mutable testLogger: TestLogger option = None
    let a: int = 1
    let b: int = 2

    [<DefaultValue>]
    val mutable private _testContext: TestContext

    member self.TestContext
        with get (): TestContext = self._testContext
        and set (value: TestContext): unit = self._testContext <- value

    [<TestInitialize>]
    member self.Setup (): unit =
        testLogger <- Some (TestLogger self.TestContext)
        if testLogger.IsSome then
            testLogger.Value.LogInfo "Test initialization completed."

    [<TestMethod>] [<Timeout(1000)>]
    member _.``test add1 Beispiel`` (): unit =
        let expected: int = add1 (a, b)
        let actual: int = ExampleImplementation.add1 (a, b)

        match testLogger with
        | Some logger when expected = actual ->
            logger.LogSuccess (
                methodName = nameof add1,
                input = [| (a, b) |],
                expected = expected,
                actual = actual
            )
        | Some logger ->
            logger.LogFailure (
                methodName = nameof add1,
                input = [| (a, b) |],
                expected = expected,
                actual = actual
            )
        | None -> ()
        
        Assert.AreEqual (
            expected = add1 (a, b),
            actual = ExampleImplementation.add1 (a, b)
        )
        
        // Wrapper.AreEqual (
        //     expected = add1 (a, b),
        //     actual = ExampleImplementation.add1 (a, b),
        //     methodName = nameof add1,
        //     input = [| (a, b) |]
        // )

    [<TestMethod>] [<Timeout(1000)>]
    member _.``test add2 Beispiel`` (): unit =
        let expected: int = add2 a b
        let actual: int = ExampleImplementation.add2 a b

        match testLogger with
        | Some logger when expected = actual ->
            logger.LogSuccess (
                methodName = nameof add2,
                input = [| a; b |],
                expected = expected,
                actual = actual
            )
        | Some logger ->
            logger.LogFailure (
                methodName = nameof add2,
                input = [| a; b |],
                expected = expected,
                actual = actual
            )
        | None -> ()

        Assert.AreEqual (
            expected = add2 a b,
            actual = ExampleImplementation.add2 a b
        )

        // Wrapper.AreEqual (
        //     expected = add2 a b,
        //     actual = ExampleImplementation.add2 a b,
        //     methodName = nameof add2,
        //     input = [| a; b |]
        // )


// EOF