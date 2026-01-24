//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// ASSERTIFY %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.Assertify


open Assertify.Core
open Assertify.Expressions
open Assertify.History
open Microsoft.FSharp.Quotations
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>Library to assert and (ver)ify.</summary>
type Assertify =
    /// <summary>Determines whether to display the history in the output.</summary>
    static let mutable showHistory: bool = true


    /// <summary>Determines whether to display the reductions in the output.</summary>
    static let mutable showReductions: bool = true


    // /// <summary>Simplifies a given expression by recursively reducing unwanted patterns.</summary>
    // /// <param name="expr">The expression to simplify.</param>
    // static let simplifyExpr' (expr: Expr): Expr =
    //     let unwantedExprPatterns: string list = [ "FieldGet"; "Tests+Tests"; "ValueWithName" ]
    //     let unwantedRegexPattern: string = unwantedExprPatterns |> List.map Regex.Escape |> String.concat "|"
    //
    //     let wantedExprPatterns: string list = [ "GetArray"; "PropertyGet" ]
    //     let wantedRegexPattern: string = wantedExprPatterns |> List.map Regex.Escape |> String.concat "|"
    //
    //     let rec simplifyHelper (expr: Expr): Expr =
    //         match expr with
    //         | Patterns.ValueWithName _ -> expr
    //         | _ when Regex.IsMatch (expr.ToString (), wantedRegexPattern) -> expr
    //         | _ when Regex.IsMatch (expr.ToString (), unwantedRegexPattern) -> simplifyHelper (expr.Reduce ())
    //         | _ -> expr
    //
    //     simplifyHelper expr
    //
    //
    // /// <summary>Converts an expression into an easily readable string.</summary>
    // /// <param name="expr">The expression to convert.</param>
    // static let rec toReadable (expr: Expr): string =
    //     let wantedExprPatterns: string list = [ "GetArray"; "op_Dereference" ]
    //     let wantedNamespaces: string list = [ "Microsoft.FSharp.Collections"; "Microsoft.FSharp.Core" ]
    //
    //     match expr with
    //     | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; _ ]) -> left.Decompile ()
    //     | Patterns.Call (None, methodInfo, args) when not (wantedExprPatterns |> List.contains methodInfo.Name) ->
    //         let argumentString: string =
    //             args
    //             |> List.map (fun (argument: Expr) ->
    //                 match argument with
    //                 // | Patterns.ValueWithName (value, _, _) when (value :? list<_> && value = box []) -> $"%s{toReadable argument}"
    //                 | Patterns.Value (x, _) when (x.GetType().IsPrimitive || x :? Nat) -> $"%s{toReadable argument}"
    //                 | _ -> $"(%s{toReadable argument})"
    //             )
    //             |> String.concat " "
    //         if wantedNamespaces |> List.contains methodInfo.DeclaringType.Namespace then
    //             let declaringTypeName: string = methodInfo.DeclaringType.Name
    //             let moduleString: string = "Module"
    //             let declaringType: string =
    //                 if declaringTypeName.EndsWith moduleString then
    //                     declaringTypeName.Substring (0, declaringTypeName.Length - moduleString.Length)
    //                 else
    //                     declaringTypeName
    //             $"%s{declaringType}.%s{methodInfo.Name} %s{argumentString}"
    //         else
    //             $"%s{methodInfo.Name} %s{argumentString}"
    //     | _ -> expr.Decompile ()


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
        try
            if not (expr.Eval ()) then
                let expected, actual: obj option * obj option =
                    match expr with
                    | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                        Some (right.Eval ()), Some (left.Eval ())
                    | _ -> None, None

                Core.failNow
                <| AssertifyResult.MakeResult (
                    "Test X",
                    ?message = message,
                    expression = expr.Decompile (),
                    expected = expected, // TODO: why null?
                    actual = actual // TODO: why null?
                )
        with ex ->
            match expr with
            | DerivedPatterns.SpecificCall <@ (=) @> (_, _, [ left; right ]) ->
                Core.failNow
                <| AssertifyResult.MakeResult (
                    "Test Y",
                    ?message = message,
                    expression = expr.Reduce().Decompile (),
                    actual = Expressions.evalActual left,
                    expected = Expressions.evalExpected right,
                    stacktrace = ex.StackTrace
                )
            | _ -> Core.failNow <| AssertifyResult.MakeResult (
                "Test",
                message = "Invalid expression pattern for Assertify.Test"
            )



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

                Core.failNow
                <| AssertifyResult.MakeResult (
                    "Test",
                    ?message = message,
                    expression = expr.Decompile (),
                    history = history,
                    expected = expected,
                    actual = actual
                )
        with ex ->
            Core.failNow
            <| AssertifyResult.MakeResult (
                "Test",
                ?message = message,
                expression = expr.Decompile (),
                stacktrace = ex.StackTrace
            )


    /// <summary>Fails the current assertion with a custom message.</summary>
    /// <param name="message">The failure message to display.</param>
    static member Fail (message: string): unit =
        Core.failNow
        <| AssertifyResult.MakeResult (
            "Fail",
            message = message
        )


    /// <summary>Fails the current assertion with an expression and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, message: string): unit =
        Core.failNow
        <| AssertifyResult.MakeResult (
            "Fail",
            expression = expr.Decompile (),
            message = message
        )


    /// <summary>Fails the current assertion with an expression, a history and a custom message.</summary>
    /// <param name="expr">The expression to include in the failure output.</param>
    /// <param name="history">The history of evaluated expressions.</param>
    /// <param name="message">The failure message to display.</param>
    static member Fail (expr: Expr, history: History, message: string): unit =
        Core.failNow
        <| AssertifyResult.MakeResult (
            "Fail",
            expression=expr.Decompile (),
            history=history,
            message=message
        )


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
module Operators =
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


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%