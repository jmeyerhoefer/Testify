namespace Testify


type CheckCase<'Args, 'Actual, 'Expected> =
    {
        Arguments: 'Args
        Test: string
        ActualObserved: Observed<'Actual>
        ExpectedObserved: Observed<'Expected>
    }


type CheckExpectation<'Args, 'Actual, 'Expected> =
    {
        Label: string
        Description: string
        Verify: 'Args -> Observed<'Actual> -> Observed<'Expected> -> bool
        FormatActual: 'Args -> Observed<'Actual> -> string
        FormatExpected: 'Args -> Observed<'Expected> -> string
        Because:
            'Args -> Observed<'Actual> -> Observed<'Expected> -> string option
        Details:
            'Args -> Observed<'Actual> -> Observed<'Expected> -> FailureDetails option
    }


[<RequireQualifiedAccess>]
module CheckExpectation =
    let private becauseValuesDiffer
        (diffOptions: DiffOptions)
        (expected: 'T)
        (actual: 'T)
        : string =
        match Diff.tryDescribeWith diffOptions expected actual with
        | Some diff when diff.StartsWith("Structural diff:") ->
            $"Expected {Render.formatValue expected} but got {Render.formatValue actual}."
        | Some diff ->
            diff
        | None ->
            $"Expected {Render.formatValue expected} but got {Render.formatValue actual}."

    let private fixedValueDescription (expected: 'T) : string =
        $"be equal to {Render.formatValue expected}"

    let private fixedValueMismatch
        (diffOptions: DiffOptions)
        (side: string)
        (expected: 'T)
        (actual: 'T)
        : string =
        $"{side}: {becauseValuesDiffer diffOptions expected actual}"

    let private create
        (label: string)
        (description: string)
        (verify: 'Args -> Observed<'Actual> -> Observed<'Expected> -> bool)
        (formatActual: 'Args -> Observed<'Actual> -> string)
        (formatExpected: 'Args -> Observed<'Expected> -> string)
        (because:
            'Args -> Observed<'Actual> -> Observed<'Expected> -> string option)
        (details:
            'Args -> Observed<'Actual> -> Observed<'Expected> -> FailureDetails option)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        {
            Label = label
            Description = description
            Verify = verify
            FormatActual = formatActual
            FormatExpected = formatExpected
            Because = because
            Details = details
        }

    let private createBranch
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (args: 'Args)
        (actual: Observed<'Actual>)
        (expected: Observed<'Expected>)
        : FailureBranch option =
        if expectation.Verify args actual expected then
            None
        else
            let because =
                expectation.Because args actual expected
                |> Option.orElseWith (fun () ->
                    expectation.Details args actual expected
                    |> Option.map Render.formatFailureDetails)

            Some
                {
                    Expectation = expectation.Description
                    Observed = None
                    ExpectedValue = None
                    ActualValue = None
                    Because = because
                }

    let equalToReference<'Args, 'T when 'T: equality>
        : CheckExpectation<'Args, 'T, 'T> =
        let diffOptions = Diff.defaultOptions

        create
            "EqualToReference"
            "match the reference behavior"
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    actualValue = expectedValue
                | Result.Error actualEx, Result.Error expectedEx ->
                    actualEx.GetType () = expectedEx.GetType ()
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue
                    when actualValue <> expectedValue ->
                    Some
                        $"Tested code returned {Render.formatValue actualValue} \
                        but the reference returned {Render.formatValue expectedValue}. \
                        {becauseValuesDiffer diffOptions expectedValue actualValue}"
                | Result.Error actualEx, Result.Error expectedEx
                    when actualEx.GetType () <> expectedEx.GetType () ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference threw {Render.formatException expectedEx}."
                | Result.Error actualEx, Result.Ok expectedValue ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference returned {Render.formatValue expectedValue}."
                | Result.Ok actualValue, Result.Error expectedEx ->
                    Some
                        $"Tested code returned {Render.formatValue actualValue} \
                        but the reference threw {Render.formatException expectedEx}."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let equalToWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (expected: 'T)
        : CheckExpectation<'Args, 'T, 'T> =
        create
            "EqualTo"
            "equal the fixed expected value"
            (fun _ actual reference ->
                match actual, reference with
                | Result.Ok actualValue, Result.Ok referenceValue ->
                    actualValue = expected && referenceValue = expected
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ _ -> fixedValueDescription expected)
            (fun _ actual reference ->
                match actual, reference with
                | Result.Ok actualValue, Result.Ok referenceValue ->
                    let actualMatches = actualValue = expected
                    let referenceMatches = referenceValue = expected

                    match actualMatches, referenceMatches with
                    | true, true ->
                        None
                    | false, true ->
                        Some
                            $"Tested code did not return the fixed expected value \
                            {Render.formatValue expected}. \
                            {becauseValuesDiffer diffOptions expected actualValue}"
                    | true, false ->
                        Some
                            $"Reference did not return the fixed expected value \
                            {Render.formatValue expected}. \
                            {becauseValuesDiffer diffOptions expected referenceValue}"
                    | false, false ->
                        let testedCodeMismatch =
                            fixedValueMismatch
                                diffOptions
                                "Tested code mismatch"
                                expected
                                actualValue

                        let referenceMismatch =
                            fixedValueMismatch
                                diffOptions
                                "Reference mismatch"
                                expected
                                referenceValue

                        Some
                            $"Neither side returned the fixed expected value \
                            {Render.formatValue expected}. \
                            {testedCodeMismatch} \
                            {referenceMismatch}"
                | Result.Error actualEx, Result.Ok referenceValue ->
                    let referenceContext =
                        if referenceValue = expected then
                            $"The reference returned {Render.formatValue referenceValue}."
                        else
                            fixedValueMismatch
                                diffOptions
                                "Reference mismatch"
                                expected
                                referenceValue

                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        instead of returning {Render.formatValue expected}. \
                        {referenceContext}"
                | Result.Ok actualValue, Result.Error referenceEx ->
                    let actualContext =
                        if actualValue = expected then
                            $"The tested code returned {Render.formatValue actualValue}."
                        else
                            fixedValueMismatch
                                diffOptions
                                "Tested code mismatch"
                                expected
                                actualValue

                    Some
                        $"Reference threw {Render.formatException referenceEx} \
                        instead of returning {Render.formatValue expected}. \
                        {actualContext}"
                | Result.Error actualEx, Result.Error referenceEx ->
                    Some
                        $"Both sides failed to return the fixed expected value \
                        {Render.formatValue expected}. Tested code threw \
                        {Render.formatException actualEx}; reference threw \
                        {Render.formatException referenceEx}.")
            (fun _ _ _ -> None)

    let equalTo<'Args, 'T when 'T: equality>
        (expected: 'T)
        : CheckExpectation<'Args, 'T, 'T> =
        equalToWithDiff Diff.defaultOptions expected

    let equalToReferenceWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        : CheckExpectation<'Args, 'T, 'T> =
        create
            "EqualToReference"
            "match the reference behavior"
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    actualValue = expectedValue
                | Result.Error actualEx, Result.Error expectedEx ->
                    actualEx.GetType () = expectedEx.GetType ()
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue
                    when actualValue <> expectedValue ->
                    Some
                        $"Tested code returned {Render.formatValue actualValue} \
                        but the reference returned {Render.formatValue expectedValue}. \
                        {becauseValuesDiffer diffOptions expectedValue actualValue}"
                | Result.Error actualEx, Result.Error expectedEx
                    when actualEx.GetType () <> expectedEx.GetType () ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference threw {Render.formatException expectedEx}."
                | Result.Error actualEx, Result.Ok expectedValue ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference returned {Render.formatValue expectedValue}."
                | Result.Ok actualValue, Result.Error expectedEx ->
                    Some
                        $"Tested code returned {Render.formatValue actualValue} \
                        but the reference threw {Render.formatException expectedEx}."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let equalToReferenceBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        : CheckExpectation<'Args, 'T, 'T> =
        create
            "EqualToReferenceBy"
            "match the reference behavior after projection"
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    projection actualValue = projection expectedValue
                | Result.Error actualEx, Result.Error expectedEx ->
                    actualEx.GetType () = expectedEx.GetType ()
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    let projectedActual = projection actualValue
                    let projectedExpected = projection expectedValue

                    if projectedActual = projectedExpected then
                        None
                    else
                        Some
                            $"Projected tested value was {Render.formatValue projectedActual} \
                            but projected reference value was \
                            {Render.formatValue projectedExpected}."
                | Result.Error actualEx, Result.Error expectedEx
                    when actualEx.GetType () <> expectedEx.GetType () ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference threw {Render.formatException expectedEx}."
                | Result.Error actualEx, Result.Ok _ ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        before projection could be compared."
                | Result.Ok _, Result.Error expectedEx ->
                    Some
                        $"Reference behavior threw {Render.formatException expectedEx} \
                        before projection could be compared."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let equalToReferenceWith<'Args, 'T>
        (comparer: 'T -> 'T -> bool)
        : CheckExpectation<'Args, 'T, 'T> =
        create
            "EqualToReferenceWith"
            "match the reference behavior using custom equality"
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    comparer actualValue expectedValue
                | Result.Error actualEx, Result.Error expectedEx ->
                    actualEx.GetType () = expectedEx.GetType ()
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    if comparer actualValue expectedValue then
                        None
                    else
                        Some
                            $"Custom equality reported a mismatch between \
                            {Render.formatValue actualValue} and \
                            {Render.formatValue expectedValue}."
                | Result.Error actualEx, Result.Error expectedEx
                    when actualEx.GetType () <> expectedEx.GetType () ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference threw {Render.formatException expectedEx}."
                | Result.Error actualEx, Result.Ok _ ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference returned successfully."
                | Result.Ok _, Result.Error expectedEx ->
                    Some
                        $"Reference behavior threw {Render.formatException expectedEx} \
                        but the tested code returned successfully."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let throwsSameExceptionType<'Args, 'Actual, 'Expected>
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "ThrowsSameExceptionType"
            "throw the same exception type as the reference"
            (fun _ actual expected ->
                match actual, expected with
                | Result.Error actualEx, Result.Error expectedEx ->
                    actualEx.GetType () = expectedEx.GetType ()
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Error actualEx, Result.Error expectedEx ->
                    if actualEx.GetType () = expectedEx.GetType () then
                        None
                    else
                        Some
                            $"Tested code threw {Render.formatException actualEx} \
                            but the reference threw {Render.formatException expectedEx}."
                | Result.Ok actualValue, Result.Error expectedEx ->
                    Some
                        $"Tested code returned {Render.formatValue actualValue} \
                        but the reference threw {Render.formatException expectedEx}."
                | Result.Error actualEx, Result.Ok expectedValue ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        but the reference returned {Render.formatValue expectedValue}."
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    Some
                        $"Both sides returned successfully: tested code returned \
                        {Render.formatValue actualValue}, reference returned \
                        {Render.formatValue expectedValue}."
                )
            (fun _ _ _ -> None)

    let satisfiesRelation
        (description: string)
        (relation: 'Args -> 'Actual -> 'Expected -> bool)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "SatisfiesRelation"
            description
            (fun args actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    relation args actualValue expectedValue
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Error actualEx, Result.Ok _ ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        before the relation could be checked."
                | Result.Ok _, Result.Error expectedEx ->
                    Some
                        $"Reference behavior threw {Render.formatException expectedEx} \
                        before the relation could be checked."
                | Result.Error actualEx, Result.Error expectedEx ->
                    Some
                        $"Both sides threw before the relation could be checked. \
                        Tested code threw {Render.formatException actualEx}; \
                        reference threw {Render.formatException expectedEx}."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let satisfyWith
        (description: string)
        (predicate: 'Args -> 'Actual -> 'Expected -> bool)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "SatisfyWith"
            description
            (fun args actual expected ->
                match actual, expected with
                | Result.Ok actualValue, Result.Ok expectedValue ->
                    predicate args actualValue expectedValue
                | _ ->
                    false)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ actual expected ->
                match actual, expected with
                | Result.Error actualEx, Result.Ok _ ->
                    Some
                        $"Tested code threw {Render.formatException actualEx} \
                        before producing a value."
                | Result.Ok _, Result.Error expectedEx ->
                    Some
                        $"Reference behavior threw {Render.formatException expectedEx} \
                        before producing a value."
                | Result.Error actualEx, Result.Error expectedEx ->
                    Some
                        $"Both sides threw before producing values. \
                        Tested code threw {Render.formatException actualEx}; \
                        reference threw {Render.formatException expectedEx}."
                | _ ->
                    None)
            (fun _ _ _ -> None)

    let satisfyObservedWith
        (description: string)
        (predicate: 'Args -> Observed<'Actual> -> Observed<'Expected> -> bool)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "SatisfyObservedWith"
            description
            predicate
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun _ _ _ -> None)
            (fun _ _ _ -> None)

    let orElse
        (a: CheckExpectation<'Args, 'Actual, 'Expected>)
        (b: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "OrElse"
            $"({a.Description}) or ({b.Description})"
            (fun args actual expected ->
                a.Verify args actual expected
                || b.Verify args actual expected)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun args actual expected ->
                if a.Verify args actual expected then
                    None
                elif b.Verify args actual expected then
                    None
                else
                    [ a.Because args actual expected
                      b.Because args actual expected ]
                    |> List.choose id
                    |> function
                        | [] -> None
                        | causes -> Some (String.concat "\nOR\n" causes))
            (fun args actual expected ->
                if a.Verify args actual expected || b.Verify args actual expected then
                    None
                else
                    Some
                        {
                            Observed = None
                            ExpectedValue = None
                            ActualValue = None
                            Branches =
                                [
                                    createBranch a args actual expected
                                    createBranch b args actual expected
                                ]
                                |> List.choose id
                        })

    let andAlso
        (a: CheckExpectation<'Args, 'Actual, 'Expected>)
        (b: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        create
            "AndAlso"
            $"({a.Description}) and ({b.Description})"
            (fun args actual expected ->
                a.Verify args actual expected
                && b.Verify args actual expected)
            (fun _ -> Observed.format)
            (fun _ -> Observed.format)
            (fun args actual expected ->
                if Microsoft.FSharp.Core.Operators.not (a.Verify args actual expected) then
                    a.Because args actual expected
                elif Microsoft.FSharp.Core.Operators.not (b.Verify args actual expected) then
                    b.Because args actual expected
                else
                    None)
            (fun args actual expected ->
                if a.Verify args actual expected && b.Verify args actual expected then
                    None
                else
                    Some
                        {
                            Observed = None
                            ExpectedValue = None
                            ActualValue = None
                            Branches =
                                [
                                    createBranch a args actual expected
                                    createBranch b args actual expected
                                ]
                                |> List.choose id
                        })


type CheckExpectation<'Args, 'Actual, 'Expected> with
    static member OrElse
        (
            a: CheckExpectation<'Args, 'Actual, 'Expected>,
            b: CheckExpectation<'Args, 'Actual, 'Expected>
        )
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        CheckExpectation.orElse a b

    static member AndAlso
        (
            a: CheckExpectation<'Args, 'Actual, 'Expected>,
            b: CheckExpectation<'Args, 'Actual, 'Expected>
        )
        : CheckExpectation<'Args, 'Actual, 'Expected> =
        CheckExpectation.andAlso a b
