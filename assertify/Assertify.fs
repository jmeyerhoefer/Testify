module Assertify


open Decorator
open FsCheck
open Microsoft.FSharp.Quotations
open Microsoft.VisualStudio.TestTools
open Swensen.Unquote
open System
open System.Text
open System.Text.RegularExpressions


/// <summary>Maintains a history of evaluated F# expressions.</summary>
type History () =
    /// <summary>
    /// Initializes History with multiple expressions and evaluates them immediately.
    /// Throws an exception if any evaluation fails.
    /// </summary>
    /// <param name="initialExpressions">The list of initial expressions to evaluate and store.</param>
    new (initialExpressions: Expr<unit> list) as self =
        History () then
            initialExpressions |> List.iter _.Eval()
            self.EvaluatedExpressions <- initialExpressions


    /// <summary>
    /// Initializes History with a single expression and evaluates it immediately.
    /// Throws an exception if evaluation fails.
    /// </summary>
    /// <param name="initialExpression">The initial expression to evaluate and store.</param>
    new (initialExpression: Expr<unit>) =
        initialExpression.Eval ()
        History [ initialExpression ]


    /// <summary>Stores the history of successfully evaluated expressions.</summary>
    member val EvaluatedExpressions: Expr<unit> list = [] with get, set


    /// <summary>Checks whether the history is empty, meaning no expressions have been evaluated.</summary>
    member self.IsEmpty (): bool =
        self.EvaluatedExpressions.IsEmpty


    /// <summary>Evaluates the given expression and adds it to history if successful.</summary>
    /// <param name="expr">The expression to evaluate.</param>
    member self.EvalAndAdd (expr: Expr<unit>): unit =
        expr.Eval ()
        self.EvaluatedExpressions <- self.EvaluatedExpressions @ [expr]


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute</c> class.</summary>
type TestClassAttribute () = inherit UnitTesting.TestClassAttribute ()


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute</c> class.</summary>
type TestMethodAttribute () = inherit UnitTesting.TestMethodAttribute ()


/// <summary>Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute</c> class.</summary>
/// <param name="timeout">The timeout of a unit test.</param>
type TimeoutAttribute (timeout: int) =
    inherit Attribute ()

    /// <summary>The timeout of a unit test.</summary>
    member _.Timeout: int = timeout


let beginMarker: string             = "\n=================== COMPACT TEST RESULT ===================\n\n"
let infoMessage: string             = "💡 Comment from Harry Hacker:"
let noInfoMessage: string           = "💤 No comment from Harry Hacker!"
let historyMarker: string           = "\n------------------------- HISTORY -------------------------\n"
let historyMessage: string          = "🕒 Successful expressions before failure:"
let reductionsMarker: string        = "\n------------------ EXPRESSION REDUCTIONS ------------------\n"
let expressionMessage: string       = "🧪 Tested/Failed expression:"
let toStringMessage: string         = "📝 Expression ToString:"
let simplifiedMessage: string       = "✅ Simplified expression:"
let seperatorMessage: string        = "\n------------------------------\n"
let expectedActualMarker: string    = "\n------------------- EXPECTED AND ACTUAL -------------------\n"
let expectedMessage: string         = "🎯 Expected result:"
let actualMessage: string           = "❌ Actual result:"
let endMarker: string               = "\n=========================== END ==========================="
let beginStacktraceToken: string    = "========================== TRACE ==========================\n"


/// <summary>Represents a custom exception used by Assertify for failed assertions.</summary>
type AssertifyException (message: string) =
    inherit Exception (message)


    /// <summary>Gets the exception message, inheriting from the base Exception class.</summary>
    override this.Message: string =
        base.Message


    /// <summary>Gets the stack trace of the exception, formatted with markers for clarity.</summary>
    override _.StackTrace: string =
        // null
        StringBuilder()
            .AppendLine(beginStacktraceToken)
            .AppendLine(base.StackTrace)
            .Append(endMarker)
            .ToString()


/// <summary>Library to assert and (ver)ify.</summary>
type Assertify =
    /// <summary>Determines whether to display the history in the output.</summary>
    static let mutable showHistory: bool = true


    /// <summary>Determines whether to display the reductions in the output.</summary>
    static let mutable showReductions: bool = true


    /// <summary>Simplifies a given expression by recursively reducing unwanted patterns.</summary>
    /// <param name="expr">The expression to simplify.</param>
    static let simplifyExpr (expr: Expr): Expr =
        let unwantedExprPatterns: string list = [ "FieldGet"; "Tests+Tests"; "ValueWithName" ]
        let unwantedRegexPattern: string = unwantedExprPatterns |> List.map Regex.Escape |> String.concat "|"

        let wantedExprPatterns: string list = [ "GetArray"; "PropertyGet" ]
        let wantedRegexPattern: string = wantedExprPatterns |> List.map Regex.Escape |> String.concat "|"

        let rec simplifyHelper (expr: Expr): Expr =
            match expr with
            | Patterns.ValueWithName _ -> expr
            | _ when Regex.IsMatch (expr.ToString (), wantedRegexPattern) -> expr
            | _ when Regex.IsMatch (expr.ToString (), unwantedRegexPattern) -> simplifyHelper (expr.Reduce ())
            | _ -> expr

        simplifyHelper expr


    /// <summary>Converts an expression into an easily readable string.</summary>
    /// <param name="expr">The expression to convert.</param>
    static let rec toReadable (expr: Expr): string =
        let wantedExprPatterns: string list = [ "GetArray"; "op_Dereference" ]
        let wantedNamespaces: string list = [ "Microsoft.FSharp.Collections"; "Microsoft.FSharp.Core" ]

        match expr with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; _ ]) -> left.Decompile ()
        | Patterns.Call (None, methodInfo, args) when not (wantedExprPatterns |> List.contains methodInfo.Name) ->
            let argumentString: string =
                args
                |> List.map (fun (argument: Expr) ->
                    match argument with
                    // | Patterns.ValueWithName (value, _, _) when (value :? list<_> && value = box []) -> $"%s{toReadable argument}"
                    | Patterns.Value (x, _) when (x.GetType().IsPrimitive || x :? Nat) -> $"%s{toReadable argument}"
                    | _ -> $"(%s{toReadable argument})"
                )
                |> String.concat " "
            if wantedNamespaces |> List.contains methodInfo.DeclaringType.Namespace then
                let declaringTypeName: string = methodInfo.DeclaringType.Name
                let moduleString: string = "Module"
                let declaringType: string =
                    if declaringTypeName.EndsWith moduleString then
                        declaringTypeName.Substring (0, declaringTypeName.Length - moduleString.Length)
                    else
                        declaringTypeName
                $"%s{declaringType}.%s{methodInfo.Name} %s{argumentString}"
            else
                $"%s{methodInfo.Name} %s{argumentString}"
        | _ -> expr.Decompile ()


    /// <summary>Builds a detailed test output message for an assertion, showing reductions and evaluated values.</summary>
    /// <param name="expr">The tested expression.</param>
    /// <param name="stringBuilder">A StringBuilder instance to construct the output message with pre-constructed output.</param>
    static let TestStdOutMessage (expr: Expr<bool>, stringBuilder: StringBuilder): string =
        match expr with
        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
            if showReductions then
                stringBuilder.AppendLine reductionsMarker |> ignore

                left.ReduceFully ()
                |> List.iteri (fun (index: int) (reduction: Expr) ->
                    let decompiledExpr: string = reduction.Decompile().Replace("\n", "").Replace("\r", "")
                    let toStringExpr: string = reduction.ToString().Replace("\n", "").Replace("\r", "")

                    if index = 0 then
                        stringBuilder.AppendLine($"%-30s{toStringMessage} %s{toStringExpr}")
                            .AppendLine($"%-30s{expressionMessage} {decompiledExpr}")
                            .AppendLine($"%-29s{simplifiedMessage} %s{reduction |> simplifyExpr |> toReadable}")
                            .AppendLine(seperatorMessage) |> ignore
                    else
                        let ordinalIndicator: string = $"%s{Decorator.GetOrdinalIndicator index} reduction:"
                        stringBuilder.AppendLine($"%-30s{ordinalIndicator} %s{decompiledExpr}") |> ignore
                )
            else
                stringBuilder.AppendLine $"%-30s{expressionMessage} %s{left |> simplifyExpr |> toReadable}" |> ignore

            stringBuilder.AppendLine($"%s{expectedActualMarker}")
                .AppendLine($"%-30s{expectedMessage} %A{right.Eval ()}")
                .AppendLine($"%-29s{actualMessage} %A{left.Eval ()}") |> ignore
        | _ -> ()

        stringBuilder.Append(endMarker).ToString()


    /// <summary>Configures whether the history should be displayed in the output.</summary>
    static member ShowHistory with set (value: bool): unit =
        showHistory <- value


    /// <summary>Configures whether reductions should be displayed in the output.</summary>
    static member ShowReductions with set (value: bool): unit =
        showReductions <- value


    /// <summary>Tests an expression and outputs failure information if the test fails.</summary>
    /// <param name="expr">The expression to test.</param>
    /// <param name="message">An optional message to include in the output.</param>
    static member Test (expr: Expr<bool>, ?message: string): unit =
        if not (expr.Eval ()) then
            let stringBuilder: StringBuilder = StringBuilder beginMarker

            if message.IsSome then
                stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}" |> ignore
            else
                stringBuilder.AppendLine noInfoMessage |> ignore

            TestStdOutMessage (expr, stringBuilder)
            |> Decorator.ForegroundColor ConsoleColor.Green
            |> failwith


    /// <summary>Tests an expression with a history and outputs failure information if the test fails.</summary>
    /// <param name="expr">The expression to test.</param>
    /// <param name="history">The history of evaluated expressions.</param>
    /// <param name="message">An optional message to include in the output.</param>
    static member Test (expr: Expr<bool>, history: History, ?message: string): unit =
        if not (expr.Eval ()) then
            let stringBuilder: StringBuilder = StringBuilder beginMarker

            if message.IsSome then
                stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}" |> ignore
            else
                stringBuilder.AppendLine noInfoMessage |> ignore

            if showHistory && not (history.IsEmpty ()) then
                stringBuilder.AppendLine($"%s{historyMarker}")
                    .AppendLine($"%s{historyMessage}") |> ignore

                history.EvaluatedExpressions
                |> List.iteri (fun (index: int) (evaluatedExpression: Expr<unit>) ->
                    let numString: string = $"%02d{index + 1}:"
                    stringBuilder.AppendLine $"%-30s{numString} %s{evaluatedExpression.Decompile ()}" |> ignore // TODO: change to |> simplifyExpr |> toReadable
                )

            TestStdOutMessage (expr, stringBuilder)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> failwith


    /// <summary>Fails the current assertion with a custom message.</summary>
    /// <param name="message">The failure message to display.</param>
    static member Fail (message: string): unit =
        StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")
            .Append(endMarker)
            .ToString()
        |> Decorator.ForegroundColor ConsoleColor.Magenta
        |> failwith


    /// <summary>Fails the current assertion with an expression and custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, message: string): unit =
        StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")
            .AppendLine($"%-30s{expressionMessage} %s{expr.Decompile ()}") // TODO: change to |> simplifyExpr |> toReadable
            .Append(endMarker)
            .ToString()
        |> Decorator.ForegroundColor ConsoleColor.Yellow
        |> failwith


/// <summary>Tests a boolean expression using Assertify.Test.</summary>
/// <param name="expr">The boolean expression to test.</param>
let inline (?) (expr: Expr<bool>): unit =
    Assertify.Test expr


/// <summary>Tests a boolean expression using Assertify.Test with a custom message.</summary>
/// <param name="expr">The boolean expression to test.</param>
/// <param name="message">The custom message to include in the test output.</param>
let inline (-?>) (expr: Expr<bool>) (message: string): unit =
    Assertify.Test (expr, message)


/// <summary>Fails the current test with a custom message.</summary>
/// <param name="message">The failure message to display.</param>
let inline (!!) (message: string): unit =
    Assertify.Fail message


/// <summary>Fails the current test with an expression and a custom message.</summary>
/// <param name="expr">The expression to include in the failure output.</param>
/// <param name="message">The failure message to display.</param>
let inline (-!>) (expr: Expr<'T>) (message: string): unit =
    Assertify.Fail (expr, message)