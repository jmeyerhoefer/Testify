module Assertify


open System.Reflection
open Decorator
open System
open System.Text
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Core.Operators
open Microsoft.VisualStudio.TestTools.UnitTesting
open Swensen.Unquote


/// <summary>
/// Library to assert and (ver)ify. 
/// </summary>
[<CompiledName("Assertify")>]
type Assertify =
    static let expressionMessage: string    = "🧪 Tested expression:"
    static let expectedMessage: string      = "🎯 Expected Result:"
    static let actualMessage: string        = "❌ Actual Result:"

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static let TestStdOutMessage (expr: Expr<bool>): string =
        let stringBuilder: StringBuilder = StringBuilder ()
        let secondLastReduction: Expr =
            let reductions: Expr list = expr.ReduceFully ()
            reductions |> List.item (reductions.Length - 2)
        stringBuilder.AppendLine("\n=================== COMPACT TEST RESULT ===================\n") |> ignore
        match expr with
        // expression format: <@ leftSide = rightSide @>
        | SpecificCall <@ (=) @> _ ->
            match expr.Reduce().Reduce() with
            | SpecificCall <@ (=) @> (_, _, [ left; _ ]) -> 
                stringBuilder.AppendLine $"%-21s{expressionMessage} %s{left.Decompile ()}" |> ignore
            | _ -> ()

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
                    .AppendLine($"%-20s{actualMessage} %b{expr.Eval ()}")
                |> ignore
            | _ -> ()
        | _ -> stringBuilder.AppendLine "Unknown expression" |> ignore
        stringBuilder.AppendLine("\n=========================== END ===========================").ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="expected">TODO</param>
    /// <param name="actual">TODO</param>
    static let AreEqualStdOutMessage (expr: Expr<'T>, expected: 'T, actual: 'T): string =
        StringBuilder()
            .AppendLine("\n=================== COMPACT TEST RESULT ===================\n")
            .AppendLine($"%-21s{expressionMessage} %s{expr.Reduce().Decompile()}")
            .AppendLine($"%-21s{expectedMessage} %A{expected}")
            .AppendLine($"%-20s{actualMessage} %A{actual}")
            .AppendLine("\n=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="result">TODO</param>
    static let IsTrueStdOutMessage (expr: Expr<bool>, result: Boolean): string =
        StringBuilder()
            .AppendLine("\n=================== COMPACT TEST RESULT ===================\n")
            .AppendLine($"%-21s{expressionMessage} %s{expr.Reduce().Decompile()}")
            .AppendLine($"%-21s{expectedMessage} true")
            .AppendLine($"%-20s{actualMessage} %b{result}")
            .AppendLine("\n=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="result">TODO</param>
    static let IsFalseStdOutMessage (expr: Expr<bool>, result: Boolean): string =
        StringBuilder()
            .AppendLine("\n=================== COMPACT TEST RESULT ===================\n")
            .AppendLine($"%-21s{expressionMessage} %s{expr.Reduce().Decompile()}")
            .AppendLine($"%-21s{expectedMessage} false")
            .AppendLine($"%-20s{actualMessage} %b{result}")
            .AppendLine("\n=========================== END ===========================")
            .ToString()

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static member Test (expr: Expr<bool>): unit =
        if not (expr.Eval ()) then 
            TestStdOutMessage expr
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        test expr

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="expected">TODO</param>
    /// <param name="actual">TODO</param>
    static member AreEqual<'T when 'T: equality> (expr: Expr<'T>, expected: 'T, actual: 'T): unit =
        if not (expected = actual) then
            AreEqualStdOutMessage (expr, expected, actual)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.AreEqual (expected, actual)

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static member IsTrue (expr: Expr<bool>): unit =
        let result: bool = expr.Eval ()
        if not result then
            IsTrueStdOutMessage (expr, result)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.IsTrue result

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    static member IsFalse (expr: Expr<bool>): unit =
        let result: bool = expr.Eval ()
        if result then
            IsFalseStdOutMessage (expr, result)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> printf "%s"
        Assert.IsFalse result


/// <summary>
/// Test the objects with structural equality.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (=!) (x: 'T when 'T: equality) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) = (%%Expr.Value<'T> y: 'T) @>


/// <summary>
/// Test the objects with structural less-than comparison.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (<!) (x: 'T when 'T: comparison) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) < (%%Expr.Value<'T> y: 'T) @>


/// <summary>
/// Test the objects with structural greater-than comparison.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (>!) (x: 'T when 'T: comparison) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) > (%%Expr.Value<'T> y: 'T) @>


/// <summary>
/// Test the objects with structural less-than-or-equal comparison.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (<=!) (x: 'T when 'T: comparison) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) <= (%%Expr.Value<'T> y: 'T) @>


/// <summary>
/// Test the objects with structural greater-than-or-equal comparison.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (>=!) (x: 'T when 'T: comparison) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) >= (%%Expr.Value<'T> y: 'T) @>


/// <summary>
/// Test the objects with structural inequality.
/// </summary>
/// <param name="x">The first parameter.</param>
/// <param name="y">The second parameter.</param>
[<Obsolete("Do not use. Use Assertify.Test instead.")>]
let inline (<>!) (x: 'T when 'T: equality) (y: 'T): unit =
    Assertify.Test <@ (%%Expr.Value<'T> x: 'T) <> (%%Expr.Value<'T> y: 'T) @>