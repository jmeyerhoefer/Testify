namespace MiniLib.Testify


type AssertExpectation<'T> =
    {
        Label: string
        Description: string
        Verify: Observed<'T> -> bool
        Format: Observed<'T> -> string
        Because: Observed<'T> -> string option
        Details: Observed<'T> -> FailureDetails option
    }


[<RequireQualifiedAccess>]
module AssertExpectation =
    let private create
        (label: string)
        (description: string)
        (verify: Observed<'T> -> bool)
        (format: Observed<'T> -> string)
        (because: Observed<'T> -> string option)
        (details: Observed<'T> -> FailureDetails option)
        : AssertExpectation<'T> =
        {
            Label = label
            Description = description
            Verify = verify
            Format = format
            Because = because
            Details = details
        }

    let private createValue
        (label: string)
        (description: string)
        (verifyValue: 'T -> bool)
        (formatValue: 'T -> string)
        (becauseValue: 'T -> string option)
        : AssertExpectation<'T> =
        let verify =
            function
            | Result.Ok value -> verifyValue value
            | Result.Error _ -> false

        let because =
            function
            | Result.Ok value -> becauseValue value
            | Result.Error ex -> Some (Render.becauseExpressionThrew ex)

        create
            label
            description
            verify
            (Observed.formatValueOrException formatValue)
            because
            (fun _ -> None)

    let private createValueSimple
        (label: string)
        (description: string)
        (verifyValue: 'T -> bool)
        (formatValue: 'T -> string)
        : AssertExpectation<'T> =
        createValue label description verifyValue formatValue (fun _ -> None)

    let private createObserved
        (label: string)
        (description: string)
        (verify: Observed<'T> -> bool)
        (format: Observed<'T> -> string)
        (because: Observed<'T> -> string option)
        : AssertExpectation<'T> =
        create label description verify format because (fun _ -> None)

    let private renderNestedDetails
        (expectation: AssertExpectation<'T>)
        (observed: Observed<'T>)
        : string option =
        expectation.Details observed
        |> Option.map Render.formatFailureDetails

    let private createBranch
        (sharedObserved: string option)
        (expectation: AssertExpectation<'T>)
        (observed: Observed<'T>)
        : FailureBranch option =
        if expectation.Verify observed then
            None
        else
            let observedText = expectation.Format observed
            let branchObserved =
                match sharedObserved with
                | Some shared when shared = observedText -> None
                | _ -> Some observedText

            let because =
                expectation.Because observed
                |> Option.orElseWith (fun () -> renderNestedDetails expectation observed)

            Some
                {
                    Expectation = expectation.Description
                    Observed = branchObserved
                    ExpectedValue = None
                    ActualValue = None
                    Because = because
                }

    let private becauseValuesDiffer
        (diffOptions: DiffOptions)
        (expected: 'T)
        (actual: 'T)
        : string =
        match Diff.tryDescribeWith diffOptions expected actual with
        | Some diff when diff.StartsWith("Structural diff:") ->
            $"Expected {Render.formatValue expected} but got {Render.formatValue actual}.\n{diff}"
        | Some diff ->
            diff
        | None ->
            $"Expected {Render.formatValue expected} but got {Render.formatValue actual}."

    let equalTo (expected: 'T) : AssertExpectation<'T> =
        let diffOptions = Diff.defaultOptions

        createValue
            "EqualTo"
            $"be equal to {Render.formatValue expected}"
            (fun actual -> actual = expected)
            Render.formatValue
            (fun actual ->
                if actual = expected then
                    None
                else
                    Some (becauseValuesDiffer diffOptions expected actual))

    let equalToWithDiff
        (diffOptions: DiffOptions)
        (expected: 'T)
        : AssertExpectation<'T> =
        createValue
            "EqualTo"
            $"be equal to {Render.formatValue expected}"
            (fun actual -> actual = expected)
            Render.formatValue
            (fun actual ->
                if actual = expected then
                    None
                else
                    Some (becauseValuesDiffer diffOptions expected actual))

    let notEqualTo (expected: 'T) : AssertExpectation<'T> =
        createValueSimple
            "NotEqualTo"
            $"do not be equal to {Render.formatValue expected}"
            (fun actual -> actual <> expected)
            Render.formatValue

    let satisfy
        (description: string)
        (predicate: 'T -> bool)
        : AssertExpectation<'T> =
        createValueSimple
            "Satisfy"
            description
            predicate
            Render.formatValue

    let satisfyObserved
        (description: string)
        (predicate: Observed<'T> -> bool)
        : AssertExpectation<'T> =
        createObserved
            "SatisfyObserved"
            description
            predicate
            Observed.format
            (fun _ -> None)

    let doesNotThrow<'T> : AssertExpectation<'T> =
        createObserved
            "DoesNotThrow"
            "complete without throwing an exception"
            (function
                | Result.Ok _ -> true
                | Result.Error _ -> false)
            Observed.format
            (function
                | Result.Ok _ -> None
                | Result.Error ex ->
                    Some $"Expression threw {Render.formatException ex}.")

    let throwsAny<'T> : AssertExpectation<'T> =
        createObserved
            "ThrowsAny"
            "throw an exception"
            (function
                | Result.Error _ -> true
                | Result.Ok _ -> false)
            Observed.format
            (function
                | Result.Error _ -> None
                | Result.Ok value ->
                    Some
                        $"Expression completed successfully and returned \
                        {Render.formatValue value} instead of throwing.")

    let throws<'T, 'TException when 'TException :> exn>
        : AssertExpectation<'T> =
        createObserved
            "Throws"
            $"throw {typeof<'TException>.Name}"
            (function
                | Result.Error ex -> ex :? 'TException
                | Result.Ok _ -> false)
            Observed.format
            (function
                | Result.Error ex when (ex :? 'TException) -> None
                | Result.Error ex ->
                    Some
                        $"Expression threw {Render.formatException ex} \
                        instead of {typeof<'TException>.Name}."
                | Result.Ok value ->
                    Some
                        $"Expression completed successfully and returned \
                        {Render.formatValue value} instead of throwing \
                        {typeof<'TException>.Name}.")

    let doesNotThrowAsync<'T> : AssertExpectation<'T> =
        createObserved
            "DoesNotThrowAsync"
            "complete asynchronously without throwing an exception"
            (function
                | Result.Ok _ -> true
                | Result.Error _ -> false)
            Observed.format
            (function
                | Result.Ok _ -> None
                | Result.Error ex ->
                    Some $"Async expression threw {Render.formatException ex}.")

    let throwsAsync<'T, 'TException when 'TException :> exn>
        : AssertExpectation<'T> =
        createObserved
            "ThrowsAsync"
            $"throw {typeof<'TException>.Name} asynchronously"
            (function
                | Result.Error ex -> ex :? 'TException
                | Result.Ok _ -> false)
            Observed.format
            (function
                | Result.Error ex when (ex :? 'TException) -> None
                | Result.Error ex ->
                    Some
                        $"Async expression threw {Render.formatException ex} \
                        instead of {typeof<'TException>.Name}."
                | Result.Ok value ->
                    Some
                        $"Async expression completed successfully and returned \
                        {Render.formatValue value} instead of throwing \
                        {typeof<'TException>.Name}.")

    let lessThan<'T when 'T : comparison>
        (expected: 'T)
        : AssertExpectation<'T> =
        createValueSimple
            "LessThan"
            $"be less than {Render.formatValue expected}"
            (fun actual -> actual < expected)
            Render.formatValue

    let lessThanOrEqualTo<'T when 'T : comparison>
        (expected: 'T)
        : AssertExpectation<'T> =
        createValueSimple
            "LessThanOrEqualTo"
            $"be less than or equal to {Render.formatValue expected}"
            (fun actual -> actual <= expected)
            Render.formatValue

    let greaterThan<'T when 'T : comparison>
        (expected: 'T)
        : AssertExpectation<'T> =
        createValueSimple
            "GreaterThan"
            $"be greater than {Render.formatValue expected}"
            (fun actual -> actual > expected)
            Render.formatValue

    let greaterThanOrEqualTo<'T when 'T : comparison>
        (expected: 'T)
        : AssertExpectation<'T> =
        createValueSimple
            "GreaterThanOrEqualTo"
            $"be greater than or equal to {Render.formatValue expected}"
            (fun actual -> actual >= expected)
            Render.formatValue

    let between<'T when 'T : comparison>
        (lowerBound: 'T)
        (upperBound: 'T)
        : AssertExpectation<'T> =
        createValueSimple
            "Between"
            $"be between {Render.formatValue lowerBound} and \
                {Render.formatValue upperBound} (inclusive)"
            (fun actual -> lowerBound <= actual && actual <= upperBound)
            Render.formatValue

    let equalBy
        (projection: 'T -> 'Key)
        (expected: 'Key)
        : AssertExpectation<'T> =
        createValue
            "EqualBy"
            $"have projected value {Render.formatValue expected}"
            (fun actual -> projection actual = expected)
            Render.formatValue
            (fun actual ->
                let projected = projection actual

                if projected = expected then
                    None
                else
                    Some
                        $"Projected value was {Render.formatValue projected} \
                        instead of {Render.formatValue expected}.")

    let equalWith
        (comparer: 'T -> 'T -> bool)
        (expected: 'T)
        : AssertExpectation<'T> =
        createValue
            "EqualWith"
            $"be equal to {Render.formatValue expected}"
            (fun actual -> comparer actual expected)
            Render.formatValue
            (fun actual ->
                if comparer actual expected then
                    None
                else
                    Some
                        $"Custom equality reported a mismatch between \
                        {Render.formatValue actual} and {Render.formatValue expected}.")

    let sequenceEqual<'T when 'T: equality>
        (expected: seq<'T>)
        : AssertExpectation<'T seq> =
        let expectedItems = Seq.toArray expected

        createValue
            "SequenceEqual"
            $"be sequence-equal to {Render.formatSequencePreview expectedItems}"
            (fun actual -> Seq.toArray actual = expectedItems)
            Render.formatSequencePreview
            (fun actual ->
                let actualItems = Seq.toArray actual

                if actualItems = expectedItems then
                    None
                else
                    Diff.seq expectedItems actualItems)

    let isTrue : AssertExpectation<bool> =
        equalTo true

    let isFalse : AssertExpectation<bool> =
        equalTo false

    let isSome<'T> : AssertExpectation<'T option> =
        createValue
            "IsSome"
            "be Some _"
            Option.isSome
            Render.formatOption
            (function
                | Some _ -> None
                | None -> Some "Expected Some _ but got None.")

    let isNone<'T> : AssertExpectation<'T option> =
        createValue
            "IsNone"
            "be None"
            Option.isNone
            Render.formatOption
            (function
                | None -> None
                | Some value ->
                    Some $"Expected None but got Some {Render.formatValue value}.")

    let isOk<'T, 'TError> : AssertExpectation<Result<'T, 'TError>> =
        createValue
            "IsOk"
            "be Ok _"
            Result.isOk
            Render.formatValue
            (function
                | Result.Ok _ -> None
                | Result.Error error ->
                    Some $"Expected Ok _ but got Error {Render.formatValue error}.")

    let isError<'T, 'TError> : AssertExpectation<Result<'T, 'TError>> =
        createValue
            "IsError"
            "be Error _"
            Result.isError
            Render.formatValue
            (function
                | Result.Error _ -> None
                | Result.Ok value ->
                    Some $"Expected Error _ but got Ok {Render.formatValue value}.")

    let contains<'T when 'T: equality>
        (expectedItem: 'T)
        : AssertExpectation<'T seq> =
        createValue
            "Contains"
            $"contain {Render.formatValue expectedItem}"
            (Seq.contains expectedItem)
            Render.formatSequencePreview
            (fun actual ->
                if Seq.contains expectedItem actual then
                    None
                else
                    Some
                        $"Expected sequence to contain {Render.formatValue expectedItem} \
                        but got {Render.formatSequencePreview actual}.")

    let startsWith
        (prefix: string)
        : AssertExpectation<string> =
        createValue
            "StartsWith"
            $"start with {Render.formatValue prefix}"
            (fun actual -> actual.StartsWith prefix)
            Render.formatValue
            (fun actual ->
                if actual.StartsWith prefix then
                    None
                else
                    Some
                        $"Expected {Render.formatValue actual} to start with \
                        {Render.formatValue prefix}.")

    let endsWith
        (suffix: string)
        : AssertExpectation<string> =
        createValue
            "EndsWith"
            $"end with {Render.formatValue suffix}"
            (fun actual -> actual.EndsWith suffix)
            Render.formatValue
            (fun actual ->
                if actual.EndsWith suffix then
                    None
                else
                    Some
                        $"Expected {Render.formatValue actual} to end with \
                        {Render.formatValue suffix}.")

    let hasLength
        (expectedLength: int)
        : AssertExpectation<'T seq> =
        createValue
            "HasLength"
            $"have length {expectedLength}"
            (fun actual -> Seq.length actual = expectedLength)
            Render.formatSequencePreview
            (fun actual ->
                let length = Seq.length actual

                if length = expectedLength then
                    None
                else
                    Some $"Expected length {expectedLength} but got {length}.")

    let not
        (expectation: AssertExpectation<'T>)
        : AssertExpectation<'T> =
        let prefix = "Not "

        let label =
            if expectation.Label.StartsWith prefix then
                expectation.Label.Substring prefix.Length
            else
                $"{prefix}{expectation.Label}"

        create
            label
            $"not ({expectation.Description})"
            (function
                | Result.Ok _ as observed -> not (expectation.Verify observed)
                | Result.Error _ -> false)
            expectation.Format
            (function
                | Result.Ok _ as observed ->
                    if expectation.Verify observed then
                        Some
                            $"Value satisfied the original expectation: {expectation.Description}."
                    else
                        None
                | Result.Error ex ->
                    Some (Render.becauseExpressionThrew ex))
            (fun observed ->
                if expectation.Verify observed then
                    Some
                        {
                            Observed = Some (Observed.format observed)
                            ExpectedValue = None
                            ActualValue = None
                            Branches =
                                [
                                    {
                                        Expectation = expectation.Description
                                        Observed = None
                                        ExpectedValue = None
                                        ActualValue = None
                                        Because =
                                            Some
                                                $"Value satisfied the original expectation: {expectation.Description}."
                                    }
                                ]
                        }
                else
                    None)

    let orElse
        (a: AssertExpectation<'T>)
        (b: AssertExpectation<'T>)
        : AssertExpectation<'T> =
        create
            "OrElse"
            $"({a.Description}) or ({b.Description})"
            (fun observed ->
                a.Verify observed
                || b.Verify observed)
            Observed.format
            (fun observed ->
                if a.Verify observed then
                    None
                elif b.Verify observed then
                    None
                else
                    [ a.Because observed; b.Because observed ]
                    |> List.choose id
                    |> function
                        | [] -> None
                        | causes -> Some (String.concat "\nOR\n" causes))
            (fun observed ->
                if a.Verify observed || b.Verify observed then
                    None
                else
                    let sharedObserved = Observed.format observed

                    Some
                        {
                            Observed = Some sharedObserved
                            ExpectedValue = None
                            ActualValue = None
                            Branches =
                                [
                                    createBranch (Some sharedObserved) a observed
                                    createBranch (Some sharedObserved) b observed
                                ]
                                |> List.choose id
                        })

    let andAlso
        (a: AssertExpectation<'T>)
        (b: AssertExpectation<'T>)
        : AssertExpectation<'T> =
        create
            "AndAlso"
            $"({a.Description}) and ({b.Description})"
            (fun observed ->
                a.Verify observed
                && b.Verify observed)
            Observed.format
            (fun observed ->
                if Microsoft.FSharp.Core.Operators.not (a.Verify observed) then
                    a.Because observed
                elif Microsoft.FSharp.Core.Operators.not (b.Verify observed) then
                    b.Because observed
                else
                    None)
            (fun observed ->
                if a.Verify observed && b.Verify observed then
                    None
                else
                    let sharedObserved = Observed.format observed

                    Some
                        {
                            Observed = Some sharedObserved
                            ExpectedValue = None
                            ActualValue = None
                            Branches =
                                [
                                    createBranch (Some sharedObserved) a observed
                                    createBranch (Some sharedObserved) b observed
                                ]
                                |> List.choose id
                        })

    let private combine
        (label: string)
        (description: string)
        (connector: string)
        (succeeds: bool list -> bool)
        (expectations: seq<AssertExpectation<'T>>)
        : AssertExpectation<'T> =
        let expectations = Seq.toList expectations

        create
            label
            description
            (fun observed ->
                expectations
                |> List.map (fun expectation -> expectation.Verify observed)
                |> succeeds)
            (fun observed ->
                expectations
                |> List.map (fun expectation -> expectation.Format observed)
                |> String.concat $"\n{connector}\n")
            (fun observed ->
                expectations
                |> List.choose (fun expectation -> expectation.Because observed)
                |> function
                    | [] -> None
                    | causes -> Some (String.concat "\n" causes))
            (fun observed ->
                let branchResults =
                    expectations
                    |> List.map (fun expectation -> createBranch (Some (Observed.format observed)) expectation observed)
                    |> List.choose id

                if succeeds (expectations |> List.map (fun expectation -> expectation.Verify observed)) then
                    None
                else
                    Some
                        {
                            Observed = Some (Observed.format observed)
                            ExpectedValue = None
                            ActualValue = None
                            Branches = branchResults
                        })

    let all
        (expectations: seq<AssertExpectation<'T>>)
        : AssertExpectation<'T> =
        combine
            "All"
            "satisfy all expectations"
            "AND"
            (List.forall id)
            expectations

    let any
        (expectations: seq<AssertExpectation<'T>>)
        : AssertExpectation<'T> =
        combine
            "Any"
            "satisfy at least one expectation"
            "OR"
            (List.exists id)
            expectations


type AssertExpectation<'T> with
    static member OrElse
        (a: AssertExpectation<'T>, b: AssertExpectation<'T>)
        : AssertExpectation<'T> =
        AssertExpectation.orElse a b

    static member AndAlso
        (a: AssertExpectation<'T>, b: AssertExpectation<'T>)
        : AssertExpectation<'T> =
        AssertExpectation.andAlso a b
