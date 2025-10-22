module AssertifyJSON


open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Microsoft.FSharp.Quotations
open Microsoft.VisualStudio.TestTools
open Swensen.Unquote
open System
open System.Text.RegularExpressions


/// <summary>TODO</summary>
type CaptureRunner (inner: FsCheck.IRunner) =
    /// <summary>TODO</summary>
    let mutable original: obj list option = None

    /// <summary>TODO</summary>
    let mutable lastShrink: obj list option = None

    interface FsCheck.IRunner with
        /// <summary>TODO</summary>
        member _.OnStartFixture (t: Type): unit =
            inner.OnStartFixture t

        /// <summary>TODO</summary>
        member _.OnArguments (n: int, args: obj list, shr: int -> obj list -> string): unit =
            inner.OnArguments (n, args, shr)

        /// <summary>TODO</summary>
        member _.OnShrink (args: obj list, shr: obj list -> string): unit =
            inner.OnShrink (args, shr)

        /// <summary>TODO</summary>
        member _.OnFinished (name: string, result: FsCheck.TestResult): unit =
            match result with
            | FsCheck.TestResult.Failed (_, originalResults: obj list, shrinkResults: obj list, _, _, _, _) ->
                original <- Some originalResults
                lastShrink <- Some shrinkResults
            | _ -> ()
            inner.OnFinished (name, result)

    /// <summary>TODO</summary>
    member _.Original: obj list option = original

    /// <summary>TODO</summary>
    member _.LastShrink: obj list option = lastShrink


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

and AssertifyResult =
    {
        TestName: string option
        Message: string option
        Expression: string option
        SimplifiedExpression: string option
        Expected: obj option
        Actual: obj option
        History: History option
        Reductions: string list option
        OriginalInputs: obj list option
        ShrunkInputs: obj list option
        ErrorMessage: string option
        Stacktrace: string option
        Timestamp: DateTime
    }

and Json =
    static member serialize (result: AssertifyResult): string =
        let jsonSettings: JsonSerializerSettings =
            JsonSerializerSettings (Formatting=Formatting.Indented)
            |> fun s ->
                s.Converters.Add (ListConverter ())
                s.Converters.Add (OptionConverter ())
                s.Converters.Add (MapConverter ())
                s
        JsonConvert.SerializeObject (result, jsonSettings)

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


    /// <summary>Configures whether the history should be displayed in the output.</summary>
    static member ShowHistory with set (value: bool): unit =
        showHistory <- value


    /// <summary>Configures whether reductions should be displayed in the output.</summary>
    static member ShowReductions with set (value: bool): unit =
        showReductions <- value

    static member private makeResult (
        testName: string,
        ?message: string,
        ?expression: Expr,
        ?expected: obj,
        ?actual: obj,
        ?history: History,
        ?reductions: string list,
        ?originalInputs: obj list,
        ?shrunkInputs: obj list,
        ?errorMessage: string,
        ?stacktrace: string
    ): AssertifyResult =
        {
            TestName = Some testName
            Message = message
            Expression = expression |> Option.map _.Decompile()
            SimplifiedExpression = expression |> Option.map (simplifyExpr >> toReadable)
            Expected = expected
            Actual = actual
            History = history
            Reductions = reductions
            OriginalInputs = originalInputs
            ShrunkInputs = shrunkInputs
            ErrorMessage = errorMessage
            Stacktrace = stacktrace
            Timestamp = DateTime.UtcNow
        }

    /// <summary>Tests an expression and outputs failure information if the test fails.</summary>
    /// <param name="expr">The expression to test.</param>
    /// <param name="message">An optional message to include in the output.</param>
    static member Test (expr: Expr<bool>, ?message: string): unit =
        try
            if not (expr.Eval ()) then
                let expected, actual: obj option * obj option =
                    match expr with
                    | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                        Some (right.Eval ()), Some (left.Eval ())
                    | _ -> None, None

                let result: AssertifyResult =
                    Assertify.makeResult (
                        "Test",
                        ?message = message,
                        expression=expr,
                        ?expected = expected,
                        ?actual = actual
                    )
                Json.serialize result |> failwith
        with ex ->
            let result: AssertifyResult =
                Assertify.makeResult (
                    "Test",
                    ?message = message,
                    expression = expr,
                    stacktrace = ex.StackTrace
                )
            Json.serialize result |> failwith


    /// <summary>Tests an expression with a history and outputs failure information if the test fails.</summary>
    /// <param name="expr">The expression to test.</param>
    /// <param name="history">The history of evaluated expressions.</param>
    /// <param name="message">An optional message to include in the output.</param>
    static member Test (expr: Expr<bool>, history: History, ?message: string): unit =
        try
            if not (expr.Eval ()) then
                let expected, actual: obj option * obj option =
                    match expr with
                    | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                        Some (right.Eval ()), Some (left.Eval ())
                    | _ -> None, None

                let result: AssertifyResult =
                    Assertify.makeResult (
                        "Test",
                        ?message = message,
                        expression=expr,
                        history=history,
                        ?expected = expected,
                        ?actual = actual
                    )
                Json.serialize result |> failwith
        with ex ->
            let result: AssertifyResult =
                Assertify.makeResult (
                    "Test",
                    ?message = message,
                    expression = expr,
                    stacktrace = ex.StackTrace
                )
            Json.serialize result |> failwith


    /// <summary>Fails the current assertion with a custom message.</summary>
    /// <param name="message">The failure message to display.</param>
    static member Fail (message: string): unit =
        let result = Assertify.makeResult ("Fail", message=message)
        Json.serialize result |> failwith


    /// <summary>Fails the current assertion with an expression and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, message: string): unit =
        let result = Assertify.makeResult ("Fail", expression=expr, message=message)
        Json.serialize result |> failwith


    /// <summary>Fails the current assertion with an expression, a history and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="history">The history of evaluated expressions.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, history: History, message: string): unit =
        let result = Assertify.makeResult ("Fail", expression=expr, history=history, message=message)
        Json.serialize result |> failwith


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="prop"></param>
    /// <param name="config"></param>
    // static member CheckProperty (prop: 'a * 'b -> Expr<bool>, ?config: FsCheck.Config): unit =
    //     ()
    static member CheckProperty (prop: 'a -> Expr<bool>, ?config: FsCheck.Config): unit =
        let config: FsCheck.Config = defaultArg config FsCheck.Config.Default
        let captureRunner: CaptureRunner = CaptureRunner config.Runner
        let configuration: FsCheck.Config = config.WithRunner captureRunner
        let arbitrary = configuration.ArbMap.ArbFor<'a> ()

        let mutable failedExpression: Expr<bool> option = None
        let property: FsCheck.Property =
            FsCheck.FSharp.Prop.forAll arbitrary (fun (x: 'a) ->
                let expr: Expr<bool> = prop x
                try
                    if expr.Eval () then true
                    else
                        failedExpression <- Some expr
                        false
                    with
                | ex -> false
            )

        try FsCheck.Check.One (configuration, property) with
        | ex ->
            let result =
                match failedExpression with
                | Some expr ->
                    let expected, actual =
                        match expr with
                        | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [left; right]) ->
                            Some(right.Eval()), Some(left.Eval())
                        | _ -> None, None
                    Assertify.makeResult (
                        "CheckProperty",
                        expression=expr,
                        expected=expected,
                        actual=actual,
                        ?originalInputs=captureRunner.Original,
                        ?shrunkInputs=captureRunner.LastShrink,
                        errorMessage=ex.Message,
                        stacktrace=ex.StackTrace
                    )
                | None ->
                    Assertify.makeResult (
                        "CheckProperty",
                        errorMessage=ex.Message,
                        stacktrace=ex.StackTrace
                    )

            Json.serialize result |> failwithf "%s"


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