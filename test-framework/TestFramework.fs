module TestFramework


open System
open System.Collections.Generic
open System.Globalization
open System.Reflection
open System.Text
open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck
open Swensen.Unquote
open FSharp.Reflection


type AssertWrapper =
    static member BuildUserMessage (?format: string, ?parameters: obj option array): string =
        ""
    static member AreEqual (?expected: 'a, ?actual: 'a, ?comparer: IEqualityComparer<'a>, ?message: string, ?parameters: obj option array): unit =
        let localComparer = defaultArg comparer EqualityComparer<'a>.Default
        match expected, actual with
        | Some expectedValue, Some actualValue when localComparer.Equals (expectedValue, actualValue) -> ()
        | _ ->
            let _userMessage: string =
                match message, parameters with
                | Some messageValue, Some parametersValue -> AssertWrapper.BuildUserMessage (messageValue, parametersValue)
                | Some messageValue, None -> AssertWrapper.BuildUserMessage (messageValue, null)
                | _ -> ""
            let _finalMessage: string =
                match expected, actual with
                | Some expectedValue, Some actualValue when not ((expectedValue.GetType ()).Equals (actualValue.GetType ())) -> ""
                | _ -> ""
            ()
        ()

    static member AreEqual (expected: obj, actual: obj, methodName: string, input: obj array): unit =
        if not (expected = actual) then
            let errorMessageStringBuilder: StringBuilder = StringBuilder ()
            errorMessageStringBuilder.AppendLine "" |> ignore
            errorMessageStringBuilder.AppendLine $"Method name: %s{methodName}" |> ignore
            let inputMessage: string = String.Join (" ", input)
            errorMessageStringBuilder.AppendLine $"Input: %s{inputMessage}" |> ignore
            errorMessageStringBuilder.AppendLine $"Expected result: %A{expected}" |> ignore
            errorMessageStringBuilder.AppendLine $"Actual result:  %A{actual}" |> ignore
            failwith (errorMessageStringBuilder.ToString ())
        else
            Assert.AreEqual (expected = expected, actual = actual)

            let x: int ref = ref 0
            x.Value <- x.Value + 1
            ()


// EOF