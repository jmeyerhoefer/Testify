namespace Testify


open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Reflection


/// <summary>Structured information describing a failing property-style test case.</summary>
type CheckFailure<'Args, 'Actual, 'Expected> =
    {
        Label: string
        Description: string
        Test: string
        Expected: string
        Actual: string
        ExpectedValueDisplay: string option
        ActualValueDisplay: string option
        Because: string option
        Details: FailureDetails option
        Original: CheckCase<'Args, 'Actual, 'Expected>
        Shrunk: CheckCase<'Args, 'Actual, 'Expected> option
        NumberOfTests: int option
        NumberOfShrinks: int option
        Replay: string option
        SourceLocation: Diagnostics.SourceLocation option
    }


type CheckFailure<'Args, 'Actual, 'Expected> with
    /// <summary>Attempts to reconstruct an FsCheck replay configuration for the failing case.</summary>
    member this.TryGetReplayConfig() : FsCheck.Config option =
        this.Replay
        |> Option.bind CheckConfig.withReplayString


/// <summary>Outcome of a property-style check.</summary>
type CheckResult<'Args, 'Actual, 'Expected> =
    | Passed
    | Failed of CheckFailure<'Args, 'Actual, 'Expected>
    | Exhausted of string
    | Errored of string


[<RequireQualifiedAccess>]
module private CheckCore =
    let private isReferenceStyleExpectation (label: string) : bool =
        match label with
        | "EqualTo"
        | "EqualToReference"
        | "EqualToReferenceBy"
        | "EqualToReferenceWith" -> true
        | _ -> false

    type RunState<'Args, 'Actual, 'Expected> =
        {
            mutable OriginalFailureCase:
                CheckCase<'Args, 'Actual, 'Expected> option
            mutable FinalFailureCase:
                CheckCase<'Args, 'Actual, 'Expected> option
            mutable Finished: FsCheck.TestResult option
            mutable UnexpectedError: exn option
        }

    let createRunState<'Args, 'Actual, 'Expected> () : RunState<'Args, 'Actual, 'Expected> =
        {
            OriginalFailureCase = None
            FinalFailureCase = None
            Finished = None
            UnexpectedError = None
        }

    let formatReplay (replay: FsCheck.Replay) : string =
        match replay.Size with
        | Some size -> $"Rnd=%A{replay.Rnd}; Size={size}"
        | None -> $"Rnd=%A{replay.Rnd}; Size=None"

    let private boxArguments<'Args> (args: 'Args) : objnull list =
        let argType = typeof<'Args>

        if FSharpType.IsTuple argType then
            let tuple =
                match box args with
                | null -> nullArg (nameof args)
                | tuple -> tuple

            FSharpValue.GetTupleFields tuple
            |> Array.toList
        else
            [ box args ]

    let private observeReference
        (reference: 'Args -> 'Expected)
        (args: 'Args)
        : Observed<'Expected> =
        try
            Result.Ok (reference args)
        with ex ->
            Result.Error ex

    let private observeReference2
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (arg1: 'Arg1)
        (arg2: 'Arg2)
        : Observed<'Expected> =
        try
            Result.Ok (reference arg1 arg2)
        with ex ->
            Result.Error ex

    let private observeReference3
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (arg1: 'Arg1)
        (arg2: 'Arg2)
        (arg3: 'Arg3)
        : Observed<'Expected> =
        try
            Result.Ok (reference arg1 arg2 arg3)
        with ex ->
            Result.Error ex

    let private evaluateAppliedCase<'Args, 'Actual, 'Expected>
        (args: 'Args)
        (applied: Expr)
        (expectedObserved: Observed<'Expected>)
        : CheckCase<'Args, 'Actual, 'Expected> =
        {
            Arguments = args
            Test = Expressions.readable applied
            ActualObserved = Observed.observeUntyped<'Actual> applied
            ExpectedObserved = expectedObserved
        }

    let evaluateCase<'Args, 'Actual, 'Expected>
        (args: 'Args)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        : CheckCase<'Args, 'Actual, 'Expected> =
        let applied = Expressions.applyUntyped (boxArguments args) actual
        evaluateAppliedCase args applied (observeReference reference args)

    let evaluateCase2<'Arg1, 'Arg2, 'Actual, 'Expected>
        (arg1: 'Arg1)
        (arg2: 'Arg2)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        : CheckCase<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let applied = Expressions.applyUntyped [ box arg1; box arg2 ] actual
        evaluateAppliedCase
            (arg1, arg2)
            applied
            (observeReference2 reference arg1 arg2)

    let evaluateCase3<'Arg1, 'Arg2, 'Arg3, 'Actual, 'Expected>
        (arg1: 'Arg1)
        (arg2: 'Arg2)
        (arg3: 'Arg3)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        : CheckCase<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let applied =
            Expressions.applyUntyped [ box arg1; box arg2; box arg3 ] actual

        evaluateAppliedCase
            (arg1, arg2, arg3)
            applied
            (observeReference3 reference arg1 arg2 arg3)

    let evaluateGroupedCase<'Group1, 'Group2, 'Actual, 'Expected>
        (group1: 'Group1)
        (group2: 'Group2)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        : CheckCase<'Group1 * 'Group2, 'Actual, 'Expected> =
        let applied =
            Expressions.applyUntyped
                (boxArguments group1 @ boxArguments group2)
                actual

        evaluateAppliedCase
            (group1, group2)
            applied
            (observeReference2 reference group1 group2)

    let recordFailure
        (state: RunState<'Args, 'Actual, 'Expected>)
        (caseData: CheckCase<'Args, 'Actual, 'Expected>)
        : unit =
        if state.OriginalFailureCase.IsNone then
            state.OriginalFailureCase <- Some caseData

        state.FinalFailureCase <- Some caseData

    let recordUnexpectedError
        (state: RunState<'Args, 'Actual, 'Expected>)
        (ex: exn)
        : unit =
        if state.UnexpectedError.IsNone then
            state.UnexpectedError <- Some ex

    let createRunner
        (state: RunState<'Args, 'Actual, 'Expected>)
        : FsCheck.IRunner =
        { new FsCheck.IRunner with
            member _.OnStartFixture _ = ()
            member _.OnArguments (_, _, _) = ()
            member _.OnShrink (_, _) = ()
            member _.OnFinished (_, result) =
                state.Finished <- Some result
        }

    let finalFailureCase
        (state: RunState<'Args, 'Actual, 'Expected>)
        : CheckCase<'Args, 'Actual, 'Expected> option =
        state.FinalFailureCase
        |> Option.orElse state.OriginalFailureCase

    let toFailure
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (testData: FsCheck.TestData)
        (replay: string option)
        (state: RunState<'Args, 'Actual, 'Expected>)
        : CheckFailure<'Args, 'Actual, 'Expected> option =
        match state.OriginalFailureCase, finalFailureCase state with
        | Some originalCase, Some finalCase ->
            let shrunkCase =
                if testData.NumberOfShrinks > 0 then
                    Some finalCase
                else
                    None

            let fallbackSourceLocation =
                match finalCase.ActualObserved with
                | Result.Error ex -> Diagnostics.tryFindRelevantExceptionLocation ex
                | Result.Ok _ -> None

            let sourceLocation =
                fallbackSourceLocation

            TestExecution.recordTestedSourceLocation sourceLocation

            Some {
                Label = expectation.Label
                Description = expectation.Description
                Test = finalCase.Test
                Expected =
                    expectation.FormatExpected
                        finalCase.Arguments
                        finalCase.ExpectedObserved
                Actual =
                    expectation.FormatActual
                        finalCase.Arguments
                        finalCase.ActualObserved
                ExpectedValueDisplay =
                    if isReferenceStyleExpectation expectation.Label then
                        Some (Observed.format finalCase.ExpectedObserved)
                    else
                        None
                ActualValueDisplay =
                    if isReferenceStyleExpectation expectation.Label then
                        Some (Observed.format finalCase.ActualObserved)
                    else
                        None
                Because =
                    expectation.Because
                        finalCase.Arguments
                        finalCase.ActualObserved
                        finalCase.ExpectedObserved
                Details =
                    expectation.Details
                        finalCase.Arguments
                        finalCase.ActualObserved
                        finalCase.ExpectedObserved
                Original = originalCase
                Shrunk = shrunkCase
                NumberOfTests = Some testData.NumberOfTests
                NumberOfShrinks = Some testData.NumberOfShrinks
                Replay = replay
                SourceLocation = sourceLocation
            }
        | _ ->
            None

    let finalize
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (state: RunState<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        match state.UnexpectedError, state.Finished with
        | Some ex, _ ->
            Errored
                $"Check failed while evaluating a property case: \
                {Render.formatException ex}."
        | None, Some (FsCheck.TestResult.Passed _) ->
            Passed
        | None, Some (FsCheck.TestResult.Exhausted testData) ->
            Exhausted $"Property exhausted after {testData.NumberOfTests} tests."
        | None, Some (FsCheck.TestResult.Failed (testData, _, _, _, rnd, _, size)) ->
            let replayText = Some (formatReplay { Rnd = rnd; Size = Some size })

            match toFailure expectation testData replayText state with
            | Some failure -> Failed failure
            | None ->
                Errored
                    "FsCheck reported failure, but Testify could not capture a counterexample."
        | None, None ->
            Errored "FsCheck finished without returning a final test result."

    let run
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        let state = createRunState<'Args, 'Actual, 'Expected> ()
        let runner = createRunner state
        let resolvedConfig = config.WithRunner runner

        let property =
            FsCheck.FSharp.Prop.forAll arbitrary (fun args ->
                try
                    let caseData = evaluateCase args actual reference

                    if expectation.Verify
                        args
                        caseData.ActualObserved
                        caseData.ExpectedObserved then
                        true
                    else
                        recordFailure state caseData
                        false
                with ex ->
                    recordUnexpectedError state ex
                    false)

        try
            FsCheck.Check.One (resolvedConfig, property)
            finalize expectation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."

    let run2
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (arbitrary2: FsCheck.Arbitrary<'Arg2>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let state = createRunState<'Arg1 * 'Arg2, 'Actual, 'Expected> ()
        let runner = createRunner state
        let resolvedConfig = config.WithRunner runner

        let property =
            FsCheck.FSharp.Prop.forAll arbitrary1 (fun arg1 ->
                FsCheck.FSharp.Prop.forAll arbitrary2 (fun arg2 ->
                    try
                        let caseData = evaluateCase2 arg1 arg2 actual reference

                        if expectation.Verify
                            caseData.Arguments
                            caseData.ActualObserved
                            caseData.ExpectedObserved then
                            true
                        else
                            recordFailure state caseData
                            false
                    with ex ->
                        recordUnexpectedError state ex
                        false))

        try
            FsCheck.Check.One (resolvedConfig, property)
            finalize expectation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."

    let run3
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (arbitrary2: FsCheck.Arbitrary<'Arg2>)
        (arbitrary3: FsCheck.Arbitrary<'Arg3>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let state =
            createRunState<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> ()

        let runner = createRunner state
        let resolvedConfig = config.WithRunner runner

        let property =
            FsCheck.FSharp.Prop.forAll arbitrary1 (fun arg1 ->
                FsCheck.FSharp.Prop.forAll arbitrary2 (fun arg2 ->
                    FsCheck.FSharp.Prop.forAll arbitrary3 (fun arg3 ->
                        try
                            let caseData =
                                evaluateCase3 arg1 arg2 arg3 actual reference

                            if expectation.Verify
                                caseData.Arguments
                                caseData.ActualObserved
                                caseData.ExpectedObserved then
                                true
                            else
                                recordFailure state caseData
                                false
                        with ex ->
                            recordUnexpectedError state ex
                            false)))

        try
            FsCheck.Check.One (resolvedConfig, property)
            finalize expectation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."

    let runGroupedUsing
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let state = createRunState<'Group1 * 'Group2, 'Actual, 'Expected> ()
        let runner = createRunner state
        let resolvedConfig = config.WithRunner runner

        let property =
            FsCheck.FSharp.Prop.forAll arbitrary1 (fun group1 ->
                FsCheck.FSharp.Prop.forAll arbitrary2 (fun group2 ->
                    try
                        let caseData =
                            evaluateGroupedCase group1 group2 actual reference

                        if expectation.Verify
                            caseData.Arguments
                            caseData.ActualObserved
                            caseData.ExpectedObserved then
                            true
                        else
                            recordFailure state caseData
                            false
                    with ex ->
                        recordUnexpectedError state ex
                        false))

        try
            FsCheck.Check.One (resolvedConfig, property)
            finalize expectation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."

    let runGroupedDependingOn
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let state = createRunState<'Group1 * 'Group2, 'Actual, 'Expected> ()
        let runner = createRunner state
        let resolvedConfig = config.WithRunner runner

        let property =
            FsCheck.FSharp.Prop.forAll arbitrary1 (fun group1 ->
                try
                    let arbitrary2 = provideArbitrary2 group1

                    FsCheck.FSharp.Prop.forAll arbitrary2 (fun group2 ->
                        try
                            let caseData =
                                evaluateGroupedCase group1 group2 actual reference

                            if expectation.Verify
                                caseData.Arguments
                                caseData.ActualObserved
                                caseData.ExpectedObserved then
                                true
                            else
                                recordFailure state caseData
                                false
                        with ex ->
                            recordUnexpectedError state ex
                            false)
                with ex ->
                    recordUnexpectedError state ex
                    FsCheck.FSharp.Prop.ofTestable false)

        try
            FsCheck.Check.One (resolvedConfig, property)
            finalize expectation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."


[<RequireQualifiedAccess>]
/// <summary>
/// Property-style testing helpers that compare quoted implementations against reference functions.
/// </summary>
module Check =
    type Collector<'Args, 'Actual, 'Expected> =
        private {
            Results: ResizeArray<CheckResult<'Args, 'Actual, 'Expected>>
        }

    let private configuredDefaultConfig () : FsCheck.Config =
        TestifySettings.ApplyCheckConfigTransformers CheckConfig.defaultConfig

    let private check2WithResolvedConfig
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (arbitrary2: FsCheck.Arbitrary<'Arg2>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        CheckCore.run2
            config
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let private check3WithResolvedConfig
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (arbitrary2: FsCheck.Arbitrary<'Arg2>)
        (arbitrary3: FsCheck.Arbitrary<'Arg3>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        CheckCore.run3
            config
            arbitrary1
            arbitrary2
            arbitrary3
            actual
            reference
            expectation

    let private checkGroupedUsingWithResolvedConfig
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        CheckCore.runGroupedUsing
            config
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let private checkGroupedDependingOnWithResolvedConfig
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        CheckCore.runGroupedDependingOn
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation

    /// <summary>
    /// Runs a property-style check with the default configuration and the default arbitrary for the input type.
    /// </summary>
    let check
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        CheckCore.run
            config
            (config.ArbMap.ArbFor<'Args> ())
            actual
            reference
            expectation

    /// <summary>Runs a property-style check with an explicit FsCheck configuration.</summary>
    let checkWith
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        CheckCore.run
            config
            (config.ArbMap.ArbFor<'Args> ())
            actual
            reference
            expectation

    let checkUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        CheckCore.run
            config
            arbitrary
            actual
            reference
            expectation

    let checkUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        CheckCore.run
            config
            arbitrary
            actual
            reference
            expectation

    let checkGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        checkGroupedUsingWithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Group1> ())
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedUsingWith
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedUsingWithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Group1> ())
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        checkGroupedUsingWithResolvedConfig
            config
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedUsingWithResolvedConfig
            config
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        checkGroupedDependingOnWithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Group1> ())
            provideArbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedDependingOnWithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Group1> ())
            provideArbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        checkGroupedDependingOnWithResolvedConfig
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedDependingOnWithResolvedConfig
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation

    let check2
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        check2WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check2With
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        check2WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        check2WithResolvedConfig
            config
            arbitrary1
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        check2WithResolvedConfig
            config
            arbitrary1
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check3
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        check3WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            (config.ArbMap.ArbFor<'Arg3> ())
            actual
            reference
            expectation

    let check3With
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        check3WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            (config.ArbMap.ArbFor<'Arg3> ())
            actual
            reference
            expectation

    let check3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = configuredDefaultConfig ()

        check3WithResolvedConfig
            config
            arbitrary1
            (config.ArbMap.ArbFor<'Arg2> ())
            (config.ArbMap.ArbFor<'Arg3> ())
            actual
            reference
            expectation

    let check3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        check3WithResolvedConfig
            config
            arbitrary1
            (config.ArbMap.ArbFor<'Arg2> ())
            (config.ArbMap.ArbFor<'Arg3> ())
            actual
            reference
            expectation

    /// <summary>Checks that a quoted function matches the reference implementation.</summary>
    let checkEqual
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        check
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualWith
        (config: FsCheck.Config)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkWith
            config
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary CheckExpectation.equalToReference reference actual

    let checkEqualUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkUsingWith config arbitrary CheckExpectation.equalToReference reference actual

    let checkEqualGroupedUsing<'Group1, 'Group2, 'T when 'T: equality>
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsing arbitrary2 CheckExpectation.equalToReference reference actual

    let checkEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T: equality>
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingWith config arbitrary2 CheckExpectation.equalToReference reference actual

    let checkEqualGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingBoth
            arbitrary1
            arbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingBothWith
            config
            arbitrary1
            arbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOn
            provideArbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnWith
            config
            provideArbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnUsing
            arbitrary1
            provideArbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqualGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            CheckExpectation.equalToReference
            reference
            actual

    let checkEqual2
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2 CheckExpectation.equalToReference reference actual

    let checkEqual2With
        (config: FsCheck.Config)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2With config CheckExpectation.equalToReference reference actual

    let checkEqual2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2Using arbitrary1 CheckExpectation.equalToReference reference actual

    let checkEqual2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2UsingWith config arbitrary1 CheckExpectation.equalToReference reference actual

    let checkEqual3
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3 CheckExpectation.equalToReference reference actual

    let checkEqual3With
        (config: FsCheck.Config)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3With config CheckExpectation.equalToReference reference actual

    let checkEqual3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3Using arbitrary1 CheckExpectation.equalToReference reference actual

    let checkEqual3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3UsingWith config arbitrary1 CheckExpectation.equalToReference reference actual

    let checkEqualBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        check (CheckExpectation.equalToReferenceBy projection) reference actual

    let checkEqualUsingBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary (CheckExpectation.equalToReferenceBy projection) reference actual

    let checkEqualWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        check (CheckExpectation.equalToReferenceWithDiff diffOptions) reference actual

    let checkEqualUsingWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary (CheckExpectation.equalToReferenceWithDiff diffOptions) reference actual

    let checkEqualUsingComparer
        (comparer: 'T -> 'T -> bool)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        check (CheckExpectation.equalToReferenceWith comparer) reference actual

    let checkEqualUsingComparerUsing
        (comparer: 'T -> 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary (CheckExpectation.equalToReferenceWith comparer) reference actual

    let private constantReference<'Args, 'T> (expected: 'T) : 'Args -> 'T =
        fun _ -> expected

    let checkBeTrue
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        check (CheckExpectation.equalTo true) (constantReference true) actual

    let checkBeTrueWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkWith config (CheckExpectation.equalTo true) (constantReference true) actual

    let checkBeTrueUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkUsing arbitrary (CheckExpectation.equalTo true) (constantReference true) actual

    let checkBeTrueUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkUsingWith config arbitrary (CheckExpectation.equalTo true) (constantReference true) actual

    let checkBeFalse
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        check (CheckExpectation.equalTo false) (constantReference false) actual

    let checkBeFalseWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkWith config (CheckExpectation.equalTo false) (constantReference false) actual

    let checkBeFalseUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkUsing arbitrary (CheckExpectation.equalTo false) (constantReference false) actual

    let checkBeFalseUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : CheckResult<'Args, bool, bool> =
        checkUsingWith config arbitrary (CheckExpectation.equalTo false) (constantReference false) actual

    /// <summary>Converts a check result into a structured Testify failure report when it did not pass.</summary>
    let toFailureReport
        (result: CheckResult<'Args, 'Actual, 'Expected>)
        : TestifyFailureReport option =
        match result with
        | Passed ->
            None
        | Exhausted message ->
            {
                TestifyReport.create
                    PropertyExhausted
                    (Some "PropertyExhausted")
                    "[PropertyExhausted] Property testing stopped before reaching enough successful test cases." with
                    Because = Some message
            }
            |> TestifyReport.withInferredHint
            |> Some
        | Errored message ->
            {
                TestifyReport.create
                    PropertyError
                    (Some "PropertyError")
                    "[PropertyError] Property testing failed unexpectedly." with
                    Because = Some message
            }
            |> TestifyReport.withInferredHint
            |> Some
        | Failed failure ->
            let originalActual = Observed.format failure.Original.ActualObserved
            let originalExpected = Observed.format failure.Original.ExpectedObserved

            let showOriginal =
                failure.Original.Test <> failure.Test
                || originalExpected <> failure.Expected
                || originalActual <> failure.Actual

            let details = TestifyReport.detailsText failure.Details

            {
                TestifyReport.create
                    PropertyFailure
                    (Some failure.Label)
                    $"[{failure.Label}] Failed property case: {failure.Test}" with
                    Test = Some failure.Test
                    Expectation = Some failure.Description
                    Expected = Some failure.Expected
                    Actual = Some failure.Actual
                    ExpectedValue = failure.ExpectedValueDisplay
                    ActualValue = failure.ActualValueDisplay
                    Because = failure.Because
                    DetailsText = details
                    DiffText = TestifyReport.diffText failure.Because details
                    OriginalTest =
                        if showOriginal then Some failure.Original.Test
                        else None
                    OriginalExpected =
                        if showOriginal then Some originalExpected
                        else None
                    OriginalActual =
                        if showOriginal then Some originalActual
                        else None
                    ShrunkTest = failure.Shrunk |> Option.map _.Test
                    ShrunkExpected =
                        failure.Shrunk
                        |> Option.map (fun shrunkCase ->
                            Observed.format shrunkCase.ExpectedObserved)
                    ShrunkActual =
                        failure.Shrunk
                        |> Option.map (fun shrunkCase ->
                            Observed.format shrunkCase.ActualObserved)
                    NumberOfTests = failure.NumberOfTests
                    NumberOfShrinks = failure.NumberOfShrinks
                    Replay = failure.Replay
                    SourceLocation = failure.SourceLocation
            }
            |> TestifyReport.withInferredHint
            |> Some

    /// <summary>Renders a check result with the supplied reporting options.</summary>
    let toDisplayStringWith
        (options: TestifyReportOptions)
        (result: CheckResult<'Args, 'Actual, 'Expected>)
        : string =
        match result with
        | Passed ->
            "Property passed."
        | Exhausted _
        | Errored _
        | Failed _ ->
            result
            |> toFailureReport
            |> Option.map (TestifyReport.renderWith options)
            |> Option.defaultValue "Property failed."

    /// <summary>Renders a check result using the current Testify report options.</summary>
    let toDisplayString
        (result: CheckResult<'Args, 'Actual, 'Expected>)
        : string =
        toDisplayStringWith (TestExecution.currentReportOptions ()) result

    /// <summary>Raises an exception when a property-style check result does not pass.</summary>
    let assertPassed
        (result: CheckResult<'Args, 'Actual, 'Expected>)
        : unit =
        match result with
        | Passed -> ()
        | Failed failure ->
            TestExecution.recordTestedSourceLocation failure.SourceLocation
            result
            |> toFailureReport
            |> Option.iter TestExecution.recordFailureReport

            failwith ("\n" + toDisplayString result)
        | Exhausted _
        | Errored _ ->
            result
            |> toFailureReport
            |> Option.iter TestExecution.recordFailureReport

            failwith ("\n" + toDisplayString result)

    [<RequireQualifiedAccess>]
    module Collect =
        let create () : Collector<'Args, 'Actual, 'Expected> =
            { Results = ResizeArray () }

        let add
            (collector: Collector<'Args, 'Actual, 'Expected>)
            (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
            (reference: 'Args -> 'Expected)
            (actual: Expr<'Args -> 'Actual>)
            : CheckResult<'Args, 'Actual, 'Expected> =
            let result = check expectation reference actual
            collector.Results.Add result
            result

        let addWith
            (config: FsCheck.Config)
            (collector: Collector<'Args, 'Actual, 'Expected>)
            (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
            (reference: 'Args -> 'Expected)
            (actual: Expr<'Args -> 'Actual>)
            : CheckResult<'Args, 'Actual, 'Expected> =
            let result = checkWith config expectation reference actual
            collector.Results.Add result
            result

        let addUsing
            (arbitrary: FsCheck.Arbitrary<'Args>)
            (collector: Collector<'Args, 'Actual, 'Expected>)
            (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
            (reference: 'Args -> 'Expected)
            (actual: Expr<'Args -> 'Actual>)
            : CheckResult<'Args, 'Actual, 'Expected> =
            let result = checkUsing arbitrary expectation reference actual
            collector.Results.Add result
            result

        let addUsingWith
            (config: FsCheck.Config)
            (arbitrary: FsCheck.Arbitrary<'Args>)
            (collector: Collector<'Args, 'Actual, 'Expected>)
            (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
            (reference: 'Args -> 'Expected)
            (actual: Expr<'Args -> 'Actual>)
            : CheckResult<'Args, 'Actual, 'Expected> =
            let result = checkUsingWith config arbitrary expectation reference actual
            collector.Results.Add result
            result

        let toResultList
            (collector: Collector<'Args, 'Actual, 'Expected>)
            : CheckResult<'Args, 'Actual, 'Expected> list =
            collector.Results
            |> Seq.toList

        let assertAll
            (collector: Collector<'Args, 'Actual, 'Expected>)
            : unit =
            let failures =
                collector.Results
                |> Seq.filter (function
                    | Passed -> false
                    | Failed _
                    | Exhausted _
                    | Errored _ -> true)
                |> Seq.toList

            if not failures.IsEmpty then
                failures
                |> List.iter (fun result ->
                    result
                    |> toFailureReport
                    |> Option.iter TestExecution.recordFailureReport)

                let message =
                    failures
                    |> List.map toDisplayString
                    |> String.concat "\n\n---\n\n"

                failwith
                    $"\nCollected {failures.Length} property failure(s).\n\n{message}"

    /// <summary>Raises an exception when a property-style check fails.</summary>
    let should
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : unit =
        check expectation reference actual
        |> assertPassed

    let shouldWith
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : unit =
        checkWith config expectation reference actual
        |> assertPassed

    let shouldUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : unit =
        checkUsing arbitrary expectation reference actual
        |> assertPassed

    let shouldUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        (reference: 'Args -> 'Expected)
        (actual: Expr<'Args -> 'Actual>)
        : unit =
        checkUsingWith config arbitrary expectation reference actual
        |> assertPassed

    let shouldGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedUsing arbitrary2 expectation reference actual
        |> assertPassed

    let shouldGroupedUsingWith
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedUsingWith config arbitrary2 expectation reference actual
        |> assertPassed

    let shouldGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedUsingBoth
            arbitrary1
            arbitrary2
            expectation
            reference
            actual
        |> assertPassed

    let shouldGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedUsingBothWith
            config
            arbitrary1
            arbitrary2
            expectation
            reference
            actual
        |> assertPassed

    let shouldGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedDependingOn provideArbitrary2 expectation reference actual
        |> assertPassed

    let shouldGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedDependingOnWith
            config
            provideArbitrary2
            expectation
            reference
            actual
        |> assertPassed

    let shouldGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedDependingOnUsing
            arbitrary1
            provideArbitrary2
            expectation
            reference
            actual
        |> assertPassed

    let shouldGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        : unit =
        checkGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            expectation
            reference
            actual
        |> assertPassed

    let should2
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : unit =
        check2 expectation reference actual
        |> assertPassed

    let should2With
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : unit =
        check2With config expectation reference actual
        |> assertPassed

    let should2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : unit =
        check2Using arbitrary1 expectation reference actual
        |> assertPassed

    let should2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        : unit =
        check2UsingWith config arbitrary1 expectation reference actual
        |> assertPassed

    let should3
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : unit =
        check3 expectation reference actual
        |> assertPassed

    let should3With
        (config: FsCheck.Config)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : unit =
        check3With config expectation reference actual
        |> assertPassed

    let should3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : unit =
        check3Using arbitrary1 expectation reference actual
        |> assertPassed

    let should3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        : unit =
        check3UsingWith config arbitrary1 expectation reference actual
        |> assertPassed

    let shouldBeTrue
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeTrue actual
        |> assertPassed

    let shouldBeTrueWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeTrueWith config actual
        |> assertPassed

    let shouldBeTrueUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeTrueUsing arbitrary actual
        |> assertPassed

    let shouldBeTrueUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeTrueUsingWith config arbitrary actual
        |> assertPassed

    let shouldBeFalse
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeFalse actual
        |> assertPassed

    let shouldBeFalseWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeFalseWith config actual
        |> assertPassed

    let shouldBeFalseUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeFalseUsing arbitrary actual
        |> assertPassed

    let shouldBeFalseUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> bool>)
        : unit =
        checkBeFalseUsingWith config arbitrary actual
        |> assertPassed

    /// <summary>Raises an exception when a reference-equality property check fails.</summary>
    let shouldEqual
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqual reference actual
        |> assertPassed

    let shouldEqualUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsing arbitrary reference actual
        |> assertPassed

    let shouldEqualWith
        (config: FsCheck.Config)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualWith config reference actual
        |> assertPassed

    let shouldEqualUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsingWith config arbitrary reference actual
        |> assertPassed

    let shouldEqualGroupedUsing<'Group1, 'Group2, 'T when 'T: equality>
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedUsing arbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedUsingWith<'Group1, 'Group2, 'T when 'T: equality>
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedUsingWith config arbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedUsingBoth arbitrary1 arbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedUsingBothWith config arbitrary1 arbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedDependingOn provideArbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedDependingOnWith config provideArbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedDependingOnUsing arbitrary1 provideArbitrary2 reference actual
        |> assertPassed

    let shouldEqualGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        : unit =
        checkEqualGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            reference
            actual
        |> assertPassed

    let shouldEqual2
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : unit =
        checkEqual2 reference actual
        |> assertPassed

    let shouldEqual2With
        (config: FsCheck.Config)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : unit =
        checkEqual2With config reference actual
        |> assertPassed

    let shouldEqual2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : unit =
        checkEqual2Using arbitrary1 reference actual
        |> assertPassed

    let shouldEqual2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        : unit =
        checkEqual2UsingWith config arbitrary1 reference actual
        |> assertPassed

    let shouldEqual3
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : unit =
        checkEqual3 reference actual
        |> assertPassed

    let shouldEqual3With
        (config: FsCheck.Config)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : unit =
        checkEqual3With config reference actual
        |> assertPassed

    let shouldEqual3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : unit =
        checkEqual3Using arbitrary1 reference actual
        |> assertPassed

    let shouldEqual3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        : unit =
        checkEqual3UsingWith config arbitrary1 reference actual
        |> assertPassed

    let shouldEqualBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualBy projection reference actual
        |> assertPassed

    let shouldEqualUsingBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsingBy projection arbitrary reference actual
        |> assertPassed

    let shouldEqualWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualWithDiff diffOptions reference actual
        |> assertPassed

    let shouldEqualUsingWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsingWithDiff diffOptions arbitrary reference actual
        |> assertPassed

    let shouldEqualUsingComparer
        (comparer: 'T -> 'T -> bool)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsingComparer comparer reference actual
        |> assertPassed

    let shouldEqualUsingComparerUsing
        (comparer: 'T -> 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (reference: 'Args -> 'T)
        (actual: Expr<'Args -> 'T>)
        : unit =
        checkEqualUsingComparerUsing comparer arbitrary reference actual
        |> assertPassed
