module Assertify


open Microsoft.FSharp.Quotations
open Microsoft.VisualStudio.TestTools
open Swensen.Unquote
open System
open System.Text
open System.Text.RegularExpressions


/// <summary>Provides utilities for decorating console output, such as applying colors, styles, and formatting to text strings.</summary>
type Decorator () =
    /// <summary>Gets the ANSI escape code for a foreground color.</summary>
    /// <param name="color">The console color for the foreground text.</param>
    static let foregroundColorCode (color: ConsoleColor): int =
        match color with
            | ConsoleColor.Black    -> 30
            | ConsoleColor.Red      -> 31
            | ConsoleColor.Green    -> 32
            | ConsoleColor.Yellow   -> 33
            | ConsoleColor.Blue     -> 34
            | ConsoleColor.Magenta  -> 35
            | ConsoleColor.Cyan     -> 36
            | ConsoleColor.White    -> 37
            | _ -> 0


    /// <summary>Gets the ANSI escape code for a background color.</summary>
    /// <param name="color">The console color for the background text.</param>
    static let backgroundColorCode (color: ConsoleColor): int =
        match color with
            | ConsoleColor.Black    -> 40
            | ConsoleColor.Red      -> 41
            | ConsoleColor.Green    -> 42
            | ConsoleColor.Yellow   -> 43
            | ConsoleColor.Blue     -> 44
            | ConsoleColor.Magenta  -> 45
            | ConsoleColor.Cyan     -> 46
            | ConsoleColor.White    -> 47
            | _ -> 0


    /// <summary>Applies a foreground color to the given message.</summary>
    /// <param name="color">The console color for the foreground text.</param>
    /// <param name="message">The message to be styled.</param>
    static member ForegroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode color}m%s{message}\u001b[0m"


    /// <summary>Applies an RGB-based foreground color to the given message.</summary>
    /// <param name="redValue">The red component of the RGB color (0-255).</param>
    /// <param name="greenValue">The green component of the RGB color (0-255).</param>
    /// <param name="blueValue">The blue component of the RGB color (0-255).</param>
    /// <param name="message">The message to be styled.</param>
    static member ForegroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[38;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"


    /// <summary>Applies a background color to the given message.</summary>
    /// <param name="color">The console color for the background.</param>
    /// <param name="message">The message to be styled.</param>
    static member BackgroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{backgroundColorCode color}m%s{message}\u001b[0m"


    /// <summary>Applies an RGB-based background color to the given message.</summary>
    /// <param name="redValue">The red component of the RGB color (0-255).</param>
    /// <param name="greenValue">The green component of the RGB color (0-255).</param>
    /// <param name="blueValue">The blue component of the RGB color (0-255).</param>
    /// <param name="message">The message to be styled.</param>
    static member BackgroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[48;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"


    /// <summary>Applies both foreground and background colors to the given message.</summary>
    /// <param name="foregroundColor">The console color for the foreground text.</param>
    /// <param name="backgroundColor">The console color for the background.</param>
    /// <param name="message">The message to be styled.</param>
    static member ForeAndBackgroundColor (foregroundColor: ConsoleColor) (backgroundColor: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode foregroundColor};%d{backgroundColorCode backgroundColor}m%s{message}\u001b[0;0m"


    /// <summary>Makes the given message bold.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Bold (message: string): string =
        $"\u001b[1m%s{message}\u001b[0m"


    /// <summary>Makes the given message italicized.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Italic (message: string): string =
        $"\u001b[3m%s{message}\u001b[0m"


    /// <summary>Underlines the given message.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Underline (message: string): string =
        $"\u001b[4m%s{message}\u001b[0m"


    /// <summary>Returns an emoji representation of a number.</summary>
    /// <param name="number">The number to be converted to an emoji (0-10).</param>
    static member GetNumberEmoji (number: int): string =
        let lookup: string array = [| "0️⃣"; "1️⃣"; "2️⃣"; "3️⃣"; "4️⃣"; "5️⃣"; "6️⃣"; "7️⃣"; "8️⃣"; "9️⃣"; "🔟" |]

        if 0 <= number && number <= 10 then
            lookup[number]
        else
            "#️⃣"


    /// <summary>Returns the ordinal indicator (e.g., "1st", "2nd", "3rd") for a given number.</summary>
    /// <param name="number">The number to be converted to an ordinal string.</param>
    static member GetOrdinalIndicator (number: int): string =
        let suffix: string =
            match number % 100 with
            | 11 | 12 | 13 -> "th"
            | _ ->
                match number % 10 with
                | 1 -> "st"
                | 2 -> "nd"
                | 3 -> "rd"
                | _ -> "th"

        $"%d{number}%s{suffix}"


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
let separatorMessage: string        = "\n------------------------------\n"
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
        StringBuilder().AppendLine(beginStacktraceToken)
            .AppendLine(base.StackTrace)
            .Append(endMarker)
            .ToString()


/// <summary>Maintains a history of evaluated F# expressions.</summary>
type History () =
    /// <summary>
    /// Initializes History with multiple expressions and evaluates them immediately.
    /// Throws an exception if any evaluation fails.
    /// </summary>
    /// <param name="exprs">The list of initial expressions to evaluate and store.</param>
    new (exprs: Expr<unit> list) as self =
        History () then
            exprs
            |> List.iter (fun (expr: Expr<unit>) ->
                try expr.Eval () with
                | _ -> Assertify.Fail $"Failed to execute the following expression: %s{expr.Decompile ()}"
            )
            self.EvaluatedExpressions <- exprs


    /// <summary>
    /// Initializes History with a single expression and evaluates it immediately.
    /// Throws an exception if evaluation fails.
    /// </summary>
    /// <param name="expr">The initial expression to evaluate and store.</param>
    new (expr: Expr<unit>) =
        try expr.Eval () with
        | _ -> Assertify.Fail $"Failed to execute the following expression: %s{expr.Decompile ()}"
        History [ expr ]


    /// <summary>Stores the history of successfully evaluated expressions.</summary>
    member val EvaluatedExpressions: Expr<unit> list = [] with get, set


    /// <summary>Checks whether the history is empty, meaning no expressions have been evaluated.</summary>
    member self.IsEmpty (): bool =
        self.EvaluatedExpressions.IsEmpty


    /// <summary>Evaluates the given expression and adds it to history if successful.</summary>
    /// <param name="expr">The expression to evaluate.</param>
    member self.EvalAndAdd (expr: Expr<unit>): unit =
        try expr.Eval () with
        | _ -> Assertify.Fail $"Failed to execute the following expression: %s{expr.Decompile ()}"
        self.EvaluatedExpressions <- self.EvaluatedExpressions @ [expr]


/// <summary>Library to assert and (ver)ify.</summary>
and Assertify =
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
                        ignore <| stringBuilder.AppendLine($"%-30s{toStringMessage} %s{toStringExpr}")
                            .AppendLine($"%-30s{expressionMessage} {decompiledExpr}")
                            .AppendLine($"%-29s{simplifiedMessage} %s{reduction |> simplifyExpr |> toReadable}")
                            .AppendLine(separatorMessage)
                    else
                        let ordinalIndicator: string = $"%s{Decorator.GetOrdinalIndicator index} reduction:"
                        ignore <| stringBuilder.AppendLine($"%-30s{ordinalIndicator} %s{decompiledExpr}")
                )
            else
                ignore <| stringBuilder.AppendLine $"%-30s{expressionMessage} %s{left |> simplifyExpr |> toReadable}"

            ignore <| stringBuilder.AppendLine($"%s{expectedActualMarker}")
                .AppendLine($"%-30s{expectedMessage} %A{right.Eval ()}")
                .AppendLine($"%-29s{actualMessage} %A{left.Eval ()}")
        | _ -> ()

        stringBuilder.Append(endMarker).ToString()


    static let HistoryStdOutMessage (history: History): string =
        let stringBuilder: StringBuilder = StringBuilder ()
        if showHistory && not (history.IsEmpty ()) then
            ignore <| stringBuilder.AppendLine($"%s{historyMarker}").AppendLine($"%s{historyMessage}")

            history.EvaluatedExpressions
            |> List.iteri (fun (index: int) (evaluatedExpression: Expr<unit>) ->
                let numString: string = $"%02d{index + 1}:"
                ignore <| stringBuilder.AppendLine $"%-30s{numString} %s{evaluatedExpression.Decompile ()}" // TODO: change to |> simplifyExpr |> toReadable
            )

        stringBuilder.ToString ()


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
                ignore <| stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}"
            else
                ignore <| stringBuilder.AppendLine noInfoMessage

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
                ignore <| stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}"
            else
                ignore <| stringBuilder.AppendLine noInfoMessage

            ignore <| stringBuilder.Append (HistoryStdOutMessage history)

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


    /// <summary>Fails the current assertion with an expression and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, message: string): unit =
        StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")
            .AppendLine($"%-30s{expressionMessage} %s{expr.Decompile ()}") // TODO: change to |> simplifyExpr |> toReadable
            .Append(endMarker)
            .ToString()
        |> Decorator.ForegroundColor ConsoleColor.Yellow
        |> failwith


    /// <summary>Fails the current assertion with an expression, a history and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="history">The history of evaluated expressions.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, history: History, message: string): unit =
        let stringBuilder: StringBuilder = StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")

        stringBuilder.Append(HistoryStdOutMessage history)
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


/// <summary>Tests a boolean expression using Assertify.Test with a history and a custom message.</summary>
/// <param name="expr">The boolean expression to test.</param>
/// <param name="history">The history of evaluated expressions.</param>
/// <param name="message">The custom message to include in the test output.</param>
let inline (-??>) (expr: Expr<bool>, history: History) (message: string): unit =
    Assertify.Test (expr, history, message)


/// <summary>Fails the current test with a custom message.</summary>
/// <param name="message">The failure message to display.</param>
let inline (!!) (message: string): unit =
    Assertify.Fail message


/// <summary>Fails the current test with an expression and a custom message.</summary>
/// <param name="expr">The expression to include in the failure output.</param>
/// <param name="message">The failure message to display.</param>
let inline (-!>) (expr: Expr<'T>) (message: string): unit =
    Assertify.Fail (expr, message)


/// <summary>Fails the current test with an expression and a custom message.</summary>
/// <param name="expr">The expression to include in the failure output.</param>
/// <param name="history">The history of evaluated expressions.</param>
/// <param name="message">The failure message to display.</param>
let inline (-!!>) (expr: Expr<'T>, history: History) (message: string): unit =
    Assertify.Fail (expr, history, message)