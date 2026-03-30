namespace MiniLib.Testify


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
        (mappedSourceLocation: Diagnostics.SourceLocation option)
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
                mappedSourceLocation
                |> Option.orElse fallbackSourceLocation
                |> Option.orElseWith Diagnostics.tryFindRelevantCallerLocation

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
        (mappedSourceLocation: Diagnostics.SourceLocation option)
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

            match toFailure expectation testData replayText mappedSourceLocation state with
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
        let mappedSourceLocation =
            SourceMapping.tryFindSourceLocationFromQuotation actual

        TestExecution.recordTestedSourceLocation mappedSourceLocation

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
            finalize expectation mappedSourceLocation state
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
        let mappedSourceLocation =
            SourceMapping.tryFindSourceLocationFromQuotation actual

        TestExecution.recordTestedSourceLocation mappedSourceLocation

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
            finalize expectation mappedSourceLocation state
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
        let mappedSourceLocation =
            SourceMapping.tryFindSourceLocationFromQuotation actual

        TestExecution.recordTestedSourceLocation mappedSourceLocation

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
            finalize expectation mappedSourceLocation state
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
        let mappedSourceLocation =
            SourceMapping.tryFindSourceLocationFromQuotation actual

        TestExecution.recordTestedSourceLocation mappedSourceLocation

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
            finalize expectation mappedSourceLocation state
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
        let mappedSourceLocation =
            SourceMapping.tryFindSourceLocationFromQuotation actual

        TestExecution.recordTestedSourceLocation mappedSourceLocation

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
            finalize expectation mappedSourceLocation state
        with ex ->
            Errored $"Check runner threw {Render.formatException ex}."


[<RequireQualifiedAccess>]
/// <summary>
/// Property-style testing helpers that compare quoted implementations against reference functions.
/// </summary>
module Check =
    let private configured (config: FsCheck.Config) : FsCheck.Config =
        CheckConfig.addMiniArbs config

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
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

        CheckCore.run
            config
            (config.ArbMap.ArbFor<'Args> ())
            actual
            reference
            expectation

    /// <summary>Runs a property-style check with an explicit FsCheck configuration.</summary>
    let checkWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        let config = configured config

        CheckCore.run
            config
            (config.ArbMap.ArbFor<'Args> ())
            actual
            reference
            expectation

    let checkUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        CheckCore.run
            CheckConfig.defaultConfig
            arbitrary
            actual
            reference
            expectation

    let checkUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : CheckResult<'Args, 'Actual, 'Expected> =
        CheckCore.run
            (configured config)
            arbitrary
            actual
            reference
            expectation

    let checkGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

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
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configured config

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
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedUsingWithResolvedConfig
            CheckConfig.defaultConfig
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedUsingWithResolvedConfig
            (configured config)
            arbitrary1
            arbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

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
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        let config = configured config

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
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedDependingOnWithResolvedConfig
            CheckConfig.defaultConfig
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation

    let checkGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : CheckResult<'Group1 * 'Group2, 'Actual, 'Expected> =
        checkGroupedDependingOnWithResolvedConfig
            (configured config)
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation

    let check2
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

        check2WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check2With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = configured config

        check2WithResolvedConfig
            config
            (config.ArbMap.ArbFor<'Arg1> ())
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

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
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2, 'Actual, 'Expected> =
        let config = configured config

        check2WithResolvedConfig
            config
            arbitrary1
            (config.ArbMap.ArbFor<'Arg2> ())
            actual
            reference
            expectation

    let check3
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

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
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = configured config

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
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = CheckConfig.defaultConfig

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
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected> =
        let config = configured config

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
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        check
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkWith
            config
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary actual reference CheckExpectation.equalToReference

    let checkEqualUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkUsingWith config arbitrary actual reference CheckExpectation.equalToReference

    let checkEqualGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsing arbitrary2 actual reference CheckExpectation.equalToReference

    let checkEqualGroupedUsingWith
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingWith config arbitrary2 actual reference CheckExpectation.equalToReference

    let checkEqualGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingBoth
            arbitrary1
            arbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedUsingBothWith
            config
            arbitrary1
            arbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOn
            provideArbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnWith
            config
            provideArbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnUsing
            arbitrary1
            provideArbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqualGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : CheckResult<'Group1 * 'Group2, 'T, 'T> =
        checkGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
            CheckExpectation.equalToReference

    let checkEqual2
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2 actual reference CheckExpectation.equalToReference

    let checkEqual2With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2With config actual reference CheckExpectation.equalToReference

    let checkEqual2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2Using arbitrary1 actual reference CheckExpectation.equalToReference

    let checkEqual2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : CheckResult<'Arg1 * 'Arg2, 'T, 'T> =
        check2UsingWith config arbitrary1 actual reference CheckExpectation.equalToReference

    let checkEqual3
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3 actual reference CheckExpectation.equalToReference

    let checkEqual3With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3With config actual reference CheckExpectation.equalToReference

    let checkEqual3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3Using arbitrary1 actual reference CheckExpectation.equalToReference

    let checkEqual3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : CheckResult<'Arg1 * 'Arg2 * 'Arg3, 'T, 'T> =
        check3UsingWith config arbitrary1 actual reference CheckExpectation.equalToReference

    let checkEqualBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        check actual reference (CheckExpectation.equalToReferenceBy projection)

    let checkEqualUsingBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary actual reference (CheckExpectation.equalToReferenceBy projection)

    let checkEqualWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        check actual reference (CheckExpectation.equalToReferenceWithDiff diffOptions)

    let checkEqualUsingWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary actual reference (CheckExpectation.equalToReferenceWithDiff diffOptions)

    let checkEqualUsingComparer
        (comparer: 'T -> 'T -> bool)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        check actual reference (CheckExpectation.equalToReferenceWith comparer)

    let checkEqualUsingComparerUsing
        (comparer: 'T -> 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : CheckResult<'Args, 'T, 'T> =
        checkUsing arbitrary actual reference (CheckExpectation.equalToReferenceWith comparer)

    /// <summary>Converts a check result into a structured Testify failure report when it did not pass.</summary>
    let toFailureReport
        (result: CheckResult<'Args, 'Actual, 'Expected>)
        : TestifyFailureReport option =
        match result with
        | Passed ->
            None
        | Exhausted message ->
            Some {
                TestifyReport.create
                    PropertyExhausted
                    (Some "PropertyExhausted")
                    "[PropertyExhausted] Property testing stopped before reaching enough successful test cases." with
                    Because = Some message
            }
        | Errored message ->
            Some {
                TestifyReport.create
                    PropertyError
                    (Some "PropertyError")
                    "[PropertyError] Property testing failed unexpectedly." with
                    Because = Some message
            }
        | Failed failure ->
            let originalActual = Observed.format failure.Original.ActualObserved
            let originalExpected = Observed.format failure.Original.ExpectedObserved

            let showOriginal =
                failure.Original.Test <> failure.Test
                || originalExpected <> failure.Expected
                || originalActual <> failure.Actual

            let details = TestifyReport.detailsText failure.Details

            Some {
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

            failwith (toDisplayString result)
        | Exhausted _
        | Errored _ ->
            result
            |> toFailureReport
            |> Option.iter TestExecution.recordFailureReport

            failwith (toDisplayString result)

    /// <summary>Raises an exception when a property-style check fails.</summary>
    let should
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : unit =
        check actual reference expectation
        |> assertPassed

    let shouldWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : unit =
        checkWith config actual reference expectation
        |> assertPassed

    let shouldUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : unit =
        checkUsing arbitrary actual reference expectation
        |> assertPassed

    let shouldUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'Actual>)
        (reference: 'Args -> 'Expected)
        (expectation: CheckExpectation<'Args, 'Actual, 'Expected>)
        : unit =
        checkUsingWith config arbitrary actual reference expectation
        |> assertPassed

    let shouldGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedUsing arbitrary2 actual reference expectation
        |> assertPassed

    let shouldGroupedUsingWith
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedUsingWith config arbitrary2 actual reference expectation
        |> assertPassed

    let shouldGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedUsingBoth
            arbitrary1
            arbitrary2
            actual
            reference
            expectation
        |> assertPassed

    let shouldGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedUsingBothWith
            config
            arbitrary1
            arbitrary2
            actual
            reference
            expectation
        |> assertPassed

    let shouldGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedDependingOn provideArbitrary2 actual reference expectation
        |> assertPassed

    let shouldGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedDependingOnWith
            config
            provideArbitrary2
            actual
            reference
            expectation
        |> assertPassed

    let shouldGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedDependingOnUsing
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation
        |> assertPassed

    let shouldGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'Actual>)
        (reference: 'Group1 -> 'Group2 -> 'Expected)
        (expectation: CheckExpectation<'Group1 * 'Group2, 'Actual, 'Expected>)
        : unit =
        checkGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
            expectation
        |> assertPassed

    let should2
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : unit =
        check2 actual reference expectation
        |> assertPassed

    let should2With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : unit =
        check2With config actual reference expectation
        |> assertPassed

    let should2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : unit =
        check2Using arbitrary1 actual reference expectation
        |> assertPassed

    let should2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2, 'Actual, 'Expected>)
        : unit =
        check2UsingWith config arbitrary1 actual reference expectation
        |> assertPassed

    let should3
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : unit =
        check3 actual reference expectation
        |> assertPassed

    let should3With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : unit =
        check3With config actual reference expectation
        |> assertPassed

    let should3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : unit =
        check3Using arbitrary1 actual reference expectation
        |> assertPassed

    let should3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'Actual>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Expected)
        (expectation: CheckExpectation<'Arg1 * 'Arg2 * 'Arg3, 'Actual, 'Expected>)
        : unit =
        check3UsingWith config arbitrary1 actual reference expectation
        |> assertPassed

    /// <summary>Raises an exception when a reference-equality property check fails.</summary>
    let shouldEqual
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqual actual reference
        |> assertPassed

    let shouldEqualUsing
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsing arbitrary actual reference
        |> assertPassed

    let shouldEqualWith
        (config: FsCheck.Config)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualWith config actual reference
        |> assertPassed

    let shouldEqualUsingWith
        (config: FsCheck.Config)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsingWith config arbitrary actual reference
        |> assertPassed

    let shouldEqualGroupedUsing
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedUsing arbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedUsingWith
        (config: FsCheck.Config)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedUsingWith config arbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedUsingBoth
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedUsingBoth arbitrary1 arbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedUsingBothWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (arbitrary2: FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedUsingBothWith config arbitrary1 arbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedDependingOn
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedDependingOn provideArbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedDependingOnWith
        (config: FsCheck.Config)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedDependingOnWith config provideArbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedDependingOnUsing
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedDependingOnUsing arbitrary1 provideArbitrary2 actual reference
        |> assertPassed

    let shouldEqualGroupedDependingOnUsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Group1>)
        (provideArbitrary2: 'Group1 -> FsCheck.Arbitrary<'Group2>)
        (actual: Expr<'Group1 -> 'Group2 -> 'T>)
        (reference: 'Group1 -> 'Group2 -> 'T)
        : unit =
        checkEqualGroupedDependingOnUsingWith
            config
            arbitrary1
            provideArbitrary2
            actual
            reference
        |> assertPassed

    let shouldEqual2
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : unit =
        checkEqual2 actual reference
        |> assertPassed

    let shouldEqual2With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : unit =
        checkEqual2With config actual reference
        |> assertPassed

    let shouldEqual2Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : unit =
        checkEqual2Using arbitrary1 actual reference
        |> assertPassed

    let shouldEqual2UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'T)
        : unit =
        checkEqual2UsingWith config arbitrary1 actual reference
        |> assertPassed

    let shouldEqual3
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : unit =
        checkEqual3 actual reference
        |> assertPassed

    let shouldEqual3With
        (config: FsCheck.Config)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : unit =
        checkEqual3With config actual reference
        |> assertPassed

    let shouldEqual3Using
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : unit =
        checkEqual3Using arbitrary1 actual reference
        |> assertPassed

    let shouldEqual3UsingWith
        (config: FsCheck.Config)
        (arbitrary1: FsCheck.Arbitrary<'Arg1>)
        (actual: Expr<'Arg1 -> 'Arg2 -> 'Arg3 -> 'T>)
        (reference: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'T)
        : unit =
        checkEqual3UsingWith config arbitrary1 actual reference
        |> assertPassed

    let shouldEqualBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualBy projection actual reference
        |> assertPassed

    let shouldEqualUsingBy<'Args, 'T, 'Key when 'T: equality and 'Key: equality>
        (projection: 'T -> 'Key)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsingBy projection arbitrary actual reference
        |> assertPassed

    let shouldEqualWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualWithDiff diffOptions actual reference
        |> assertPassed

    let shouldEqualUsingWithDiff<'Args, 'T when 'T: equality>
        (diffOptions: DiffOptions)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsingWithDiff diffOptions arbitrary actual reference
        |> assertPassed

    let shouldEqualUsingComparer
        (comparer: 'T -> 'T -> bool)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsingComparer comparer actual reference
        |> assertPassed

    let shouldEqualUsingComparerUsing
        (comparer: 'T -> 'T -> bool)
        (arbitrary: FsCheck.Arbitrary<'Args>)
        (actual: Expr<'Args -> 'T>)
        (reference: 'Args -> 'T)
        : unit =
        checkEqualUsingComparerUsing comparer arbitrary actual reference
        |> assertPassed
