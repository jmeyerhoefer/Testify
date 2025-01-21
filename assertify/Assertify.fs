module Assertify


open Decorator
open Microsoft.FSharp.Quotations
open Microsoft.VisualStudio.TestTools
open Swensen.Unquote
open System
open System.Text
open System.Text.RegularExpressions


/// <summary>
/// Maintains a history of evaluated F# expressions.
/// </summary>
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


    /// <summary>
    /// Stores the history of successfully evaluated expressions.
    /// </summary>
    member val EvaluatedExpressions: Expr<unit> list = [] with get, set


    /// <summary>
    /// TODO
    /// </summary>
    member self.IsEmpty (): bool =
        self.EvaluatedExpressions.IsEmpty


    /// <summary>
    /// Evaluates the given expression and adds it to history if successful.
    /// </summary>
    /// <param name="expr">The expression to evaluate.</param>
    member self.EvalAndAdd (expr: Expr<unit>): unit =
        expr.Eval ()
        self.EvaluatedExpressions <- self.EvaluatedExpressions @ [expr]


/// <summary>
/// Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute</c> class.
/// </summary>
type TestClassAttribute () = inherit UnitTesting.TestClassAttribute ()


/// <summary>
/// Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute</c> class.
/// </summary>
type TestMethodAttribute () = inherit UnitTesting.TestMethodAttribute ()


/// <summary>
/// Initializes a new instance of the <c>Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute</c> class.
/// </summary>
[<Timeout(1000)>]
type TimeoutAttribute (timeout: int) =
    inherit Attribute ()
    member _.Timeout: int = timeout


let beginMarker: string = "\n=================== COMPACT TEST RESULT ===================\n\n"
let infoMessage: string = "💡 Message from Harry Hacker:"
let historyMarker: string = "\n------------------------- HISTORY -------------------------\n"
let historyMessage: string = "🕒 Successfully executed expressions before failure:"
let reductionsMarker: string = "\n------------------ EXPRESSION REDUCTIONS ------------------\n"
let expressionMessage: string = "🧪 Tested/Failed expression:"
let toStringMessage: string = "📝 Expression ToString:"
let simplifiedMessage: string = "✅ Simplified Expression:"
let expectedActualMarker: string = "\n------------------- EXPECTED AND ACTUAL -------------------\n"
let expectedMessage: string = "🎯 Expected Result:"
let actualMessage: string = "❌ Actual Result:"
let endMarker: string = "\n=========================== END ==========================="
let beginStacktraceToken: string = "========================== TRACE ==========================\n"


/// <summary>
/// TODO
/// </summary>
type AssertifyException (message: string) =
    inherit Exception (message)


    /// <summary>
    /// TODO
    /// </summary>
    override this.Message: string =
        base.Message


    /// <summary>
    /// TODO
    /// </summary>
    override _.StackTrace: string =
        // null
        StringBuilder()
            .AppendLine(beginStacktraceToken)
            .AppendLine(base.StackTrace)
            .Append(endMarker)
            .ToString()


// /// <summary>
// /// TODO
// /// </summary>
// /// <param name="message">TODO</param>
// let failwith (message: string): unit =
//     raise (AssertifyException message)


/// <summary>
/// Library to assert and (ver)ify.
/// </summary>
type Assertify =
    /// <summary>
    /// TODO
    /// </summary>
    static let mutable showHistory, showReductions: bool * bool = true, true


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
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


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
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


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="stringBuilder">TODO</param>
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
                            .AppendLine("\n------------------------------\n") |> ignore
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


    /// <summary>
    /// TODO
    /// </summary>
    static member ShowHistory with set (value: bool): unit =
        showHistory <- value


    /// <summary>
    /// TODO
    /// </summary>
    static member ShowReductions with set (value: bool): unit =
        showReductions <- value


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="message">TODO</param>
    static member Test (expr: Expr<bool>, ?message: string): unit =
        if not (expr.Eval ()) then
            let stringBuilder: StringBuilder = StringBuilder beginMarker
            if message.IsSome then
                stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}" |> ignore
            else
                stringBuilder.AppendLine $"No messages from Harry Hacker." |> ignore
            TestStdOutMessage (expr, stringBuilder) 
            |> Decorator.ForegroundColor ConsoleColor.Green
            |> failwith


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="history">TODO</param>
    /// <param name="message">TODO</param>
    static member Test (expr: Expr<bool>, history: History, ?message: string): unit =
        if not (expr.Eval ()) then
            let stringBuilder: StringBuilder = StringBuilder beginMarker
            if message.IsSome then
                stringBuilder.AppendLine $"%-30s{infoMessage} %s{message.Value}" |> ignore
            else
                stringBuilder.AppendLine $"No messages from Harry Hacker." |> ignore
            if showHistory && not (history.IsEmpty ()) then
                stringBuilder.AppendLine($"%s{historyMarker}")
                    .AppendLine($"%s{historyMessage}") |> ignore
                history.EvaluatedExpressions
                |> List.iteri (fun (index: int) (evaluatedExpression: Expr<unit>) ->
                    let numString: string = $"%02d{index + 1}:"
                    stringBuilder.AppendLine $"%-30s{numString} %s{evaluatedExpression.Decompile ()}" |> ignore
                )
            TestStdOutMessage (expr, stringBuilder)
            |> Decorator.ForegroundColor ConsoleColor.Cyan
            |> failwith


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="message">TODO</param>
    static member Fail (message: string): unit =
        StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")
            .Append(endMarker)
            .ToString()
        |> Decorator.ForegroundColor ConsoleColor.Magenta
        |> failwith


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="message">TODO</param>
    static member Fail (expr: Expr, message: string): unit =
        StringBuilder(beginMarker).AppendLine($"%-30s{infoMessage} %s{message}")
            .AppendLine($"%-30s{expressionMessage} %s{expr.Decompile ()}")
            .Append(endMarker)
            .ToString()
        |> Decorator.ForegroundColor ConsoleColor.Yellow
        |> failwith


/// <summary>
/// TODO
/// </summary>
/// <param name="expr">TODO</param>
let inline (?) (expr: Expr<bool>): unit =
    Assertify.Test expr


/// <summary>
/// TODO
/// </summary>
/// <param name="expr">TODO</param>
/// <param name="message">TODO</param>
let inline (?->) (expr: Expr<bool>) (message: string): unit =
    Assertify.Test (expr, message)


/// <summary>
/// TODO
/// </summary>
/// <param name="message">TODO</param>
let inline (!!) (message: string): unit =
    Assertify.Fail message


/// <summary>
/// TODO
/// </summary>
/// <param name="expr">TODO</param>
/// <param name="message">TODO</param>
let inline (!!>) (expr: Expr) (message: string): unit =
    Assertify.Fail (expr, message)