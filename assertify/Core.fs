//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// CORE %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.Core


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// Serializable result for failures
type AssertifyResult =
    {
        TestName: string option
        Message: string option
        Expression: string option
        SimplifiedExpression: string option
        Expected: obj option
        Actual: obj option
        History: obj option
        Reductions: string list option
        OriginalInputs: string list option
        ShrunkInputs: string list option
        ErrorMessage: string option
        Stacktrace: string option
        Timestamp: string
    }


    /// <summary>TODO</summary>
    static let stringifyObjs (objs: obj list option): string list option =
        objs
        |> Option.map (List.map (fun (o: obj) -> if isNull o then "NULL" else o.ToString ()))


    /// <summary>TODO</summary>
    static member MakeResult (
        name: string, ?message: string, ?expression: string, ?simplified: string, ?expected: obj, ?actual: obj, ?history: obj,
        ?reductions: string list, ?originalInputs: obj list, ?shrunkInputs: obj list, ?errorMessage: string, ?stacktrace: string
    ): AssertifyResult =
        {
            TestName = Some name
            Message = message
            Expression = expression
            SimplifiedExpression = simplified
            Expected = expected
            Actual = actual
            History = history
            Reductions = reductions
            OriginalInputs = stringifyObjs originalInputs
            ShrunkInputs = stringifyObjs shrunkInputs
            ErrorMessage = errorMessage
            Stacktrace = stacktrace
            Timestamp = System.DateTime.Now.ToString "Dyyyy-MM-ddTHH:mm:ss"
        }


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
module Core =
    /// <summary>Internal fail — used by Assertify and History</summary>
    let failNow (result: AssertifyResult): 'a =
        failwith <| Assertify.Serialization.Serialization.serialize result


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%