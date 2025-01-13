module Assertify


open Decorator
open System
open System.Text
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.VisualStudio.TestTools.UnitTesting
open Swensen.Unquote


/// <summary>
/// Library to assert and (ver)ify. 
/// </summary>
[<CompiledName("Assertify")>]
type Assertify =
    static let methodNameMessage: string    = "🔧 Method Name:"
    static let expressionMessage: string    = "🧪 Tested expression:"
    static let conditionMessage: string     = "🌡️ Tested condition:"
    static let objectMessage: string        = "📦 Tested object:"
    static let inputMessage: string         = "🔤 Input(s):"
    static let expectedMessage: string      = "🎯 Expected Result:"
    static let actualMessage: string        = "❌ Actual Result:"

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="methodName">TODO</param>
    /// <param name="input">TODO</param>
    /// <param name="expected">TODO</param>
    /// <param name="actual">TODO</param>
    static let AreEqualStdOutMessage (methodName: string, input: obj array, expected: 'T, actual: 'T): string =
        let inputString: string = String.Join ("; ", input |> Array.map (sprintf "%A"))
        StringBuilder()
            .AppendLine()
            .AppendLine("=================== COMPACT TEST RESULT ===================")
            .AppendLine()
            .AppendLine($"%-21s{methodNameMessage} %s{methodName}")
            .AppendLine($"%-21s{inputMessage} %s{inputString}")
            .AppendLine($"%-21s{expectedMessage} %A{expected}")
            .AppendLine($"%-20s{actualMessage} %A{actual}")
            .AppendLine()
            .AppendLine("=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static let TestStdOutMessage (expr: Expr<bool>): string =
        let stringBuilder: StringBuilder = StringBuilder ()
        let reductions: Expr list = expr.ReduceFully ()
        let lastReduction: Expr = reductions |> List.last // should be true or false
        let secondLastReduction: Expr = reductions |> List.item (reductions.Length - 2)
        stringBuilder
            .AppendLine()
            .AppendLine("=================== COMPACT TEST RESULT ===================")
            .AppendLine()
        |> ignore
        match expr with
        // expression format: <@ leftSide = rightSide @>
        | SpecificCall <@ (=) @> (_, _, [ left; _ ]) ->
            // TODO take third reduction instead of first to print Or [Lit a; Lit b] instead of self.ex1
            stringBuilder.AppendLine $"%-21s{expressionMessage} %s{left.Decompile ()}" |> ignore
            match secondLastReduction with
            | SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                stringBuilder
                    .AppendLine($"%-21s{expectedMessage} %s{right.Decompile ()}")
                    .AppendLine($"%-20s{actualMessage} %s{left.Decompile ()}")
                |> ignore
            | _ -> ()
        // expression format: <@ methodName args @>
        | Call _ ->
            match secondLastReduction with
            | Call (None, methodInfo, args) ->
                let argumentsString: string =
                    args
                    |> List.map _.Decompile()
                    |> String.concat " "
                stringBuilder
                    .AppendLine($"%-21s{expressionMessage} %s{methodInfo.DeclaringType.Name}.%s{methodInfo.Name} %s{argumentsString}")
                    .AppendLine($"%-21s{expectedMessage} true")
                    .AppendLine($"%-20s{actualMessage} %s{lastReduction.Decompile ()}")
                |> ignore
            | _ -> ()
        | _ -> stringBuilder.AppendLine "Unknown expression" |> ignore

        stringBuilder
            .AppendLine()
            .AppendLine("=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="condition">TODO</param>
    static let IsTrueStdOutMessage (expr: Expr<bool>, condition: Boolean): string =
        StringBuilder()
            .AppendLine()
            .AppendLine("=================== COMPACT TEST RESULT ===================")
            .AppendLine()
            .AppendLine($"%-22s{conditionMessage} %s{expr.Decompile ()}")
            .AppendLine($"%-21s{expectedMessage} true")
            .AppendLine($"%-20s{actualMessage} %b{condition}")
            .AppendLine()
            .AppendLine("=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="condition">TODO</param>
    static let IsFalseStdOutMessage (expr: Expr<bool>, condition: Boolean): string =
        StringBuilder()
            .AppendLine()
            .AppendLine("=================== COMPACT TEST RESULT ===================")
            .AppendLine()
            .AppendLine($"%-22s{conditionMessage} %s{expr.Decompile ()}")
            .AppendLine($"%-21s{expectedMessage} false")
            .AppendLine($"%-20s{actualMessage} %b{condition}")
            .AppendLine()
            .AppendLine("=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="methodName">TODO</param>
    /// <param name="input">TODO</param>
    /// <param name="expected">TODO</param>
    /// <param name="actual">TODO</param>
    static member AreEqual<'T when 'T: equality> (methodName: string, input: obj array, expected: 'T, actual: 'T): unit =
        if not (expected = actual) then
            AreEqualStdOutMessage (methodName, input, expected, actual)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.AreEqual (expected, actual)

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static member Test (expr: Expr<bool>): unit =
        match expr.ReduceFully () |> List.last with
        | Bool true -> ()
        | _ ->
            TestStdOutMessage expr
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        test expr

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="condition">TODO</param>
    static member IsTrue (expr: Expr<bool>, ?condition: Boolean): unit =
        let condition: bool = expr.Eval ()
        if not condition then
            IsTrueStdOutMessage (expr, condition)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.IsTrue condition

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="condition">TODO</param>
    static member IsFalse (expr: Expr<bool>, ?condition: Boolean): unit =
        let condition: bool = expr.Eval ()
        if condition then
            IsFalseStdOutMessage (expr, condition)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.IsFalse condition