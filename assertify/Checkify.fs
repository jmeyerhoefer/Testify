//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// CHECKIFY %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


namespace Assertify.Checkify


open Assertify.Types
open Assertify.Types.Configurations
open Assertify.Core
open Assertify.Expressions
open Assertify.Expressions.Operators

open Microsoft.FSharp.Quotations
open Swensen.Unquote


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
type CaptureRunner (inner: IRunner) =
    /// <summary>TODO</summary>
    let mutable original: obj list option = None


    /// <summary>TODO</summary>
    let mutable shrunk: obj list option = None


    interface IRunner with
        /// <summary>TODO</summary>
        member _.OnStartFixture (t: System.Type): unit =
            inner.OnStartFixture t


        /// <summary>TODO</summary>
        member _.OnArguments (n: int, args: obj list, shr: int -> obj list -> string): unit =
            inner.OnArguments (n, args, shr)


        /// <summary>TODO</summary>
        member _.OnShrink (args: obj list, shr: obj list -> string): unit =
            inner.OnShrink (args, shr)


        /// <summary>TODO</summary>
        member _.OnFinished (name: string, result: FsCheck.TestResult): unit =
            let correctOrder (xs: obj list): obj list = xs |> List.permute (fun x -> (x + 1) % xs.Length)
            match result with
            | TestResult.Failed (_, o, s, _, _, _, _) ->
                original <- Some (correctOrder o)
                shrunk   <- Some (correctOrder s)
            | _ -> ()
            inner.OnFinished (name, result)


    /// <summary>TODO</summary>
    member _.Original: obj list option = original


    /// <summary>TODO</summary>
    member _.Shrunk: obj list option = shrunk


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
type Checkify =
    /// <summary>TODO</summary>
    static member ArbFromGenPair (generator: Gen<'a * 'b>): Arbitrary<'a * 'b> =
        FsCheck.FSharp.Arb.fromGenShrink (
            generator,
            fun (a: 'a, b: 'b) ->
                seq {
                    for a' in defaultConfig.ArbMap.ArbFor<'a>().Shrinker a -> a', b
                    for b' in defaultConfig.ArbMap.ArbFor<'b>().Shrinker b -> a, b'
                }
        )


    /// <summary>TODO</summary>
    static member ArbFromGenTripple (generator: Gen<'a * 'b * 'c>): Arbitrary<'a * 'b * 'c> =
        FsCheck.FSharp.Arb.fromGenShrink (
            generator,
            fun (a: 'a, b: 'b, c: 'c) ->
                seq {
                    for a' in defaultConfig.ArbMap.ArbFor<'a>().Shrinker a -> a', b, c
                    for b' in defaultConfig.ArbMap.ArbFor<'b>().Shrinker b -> a, b', c
                    for c' in defaultConfig.ArbMap.ArbFor<'c>().Shrinker c -> a, b, c'
                }
        )


    /// <summary>TODO</summary>
    static member ForAll (expr: Expr<'a -> bool>, arb: Arbitrary<'a>): Property =
        expr
        |> Expressions.eval<'a -> bool>
        |> FsCheck.FSharp.Prop.forAll arb


    /// <summary>TODO</summary>
    static member ForAllGen (expr: Expr<'a * 'b -> bool>, generator: Gen<'a * 'b>, ?shrinkA: 'a -> 'a seq, ?shrinkB: 'b -> 'b seq): Property =
        let shrinkA: 'a -> 'a seq = defaultArg shrinkA (defaultConfig.ArbMap.ArbFor<'a>().Shrinker)
        let shrinkB: 'b -> 'b seq = defaultArg shrinkB (defaultConfig.ArbMap.ArbFor<'b>().Shrinker)
        let arb: FsCheck.Arbitrary<'a * 'b> =
            FsCheck.FSharp.Arb.fromGenShrink (
                generator,
                fun (a: 'a, b: 'b) ->
                    seq {
                        for a' in shrinkA a -> a', b
                        for b' in shrinkB b -> a, b'
                    }
            )

        expr
        |> Expressions.eval<'a * 'b -> bool>
        |> FsCheck.FSharp.Prop.forAll arb


    /// <summary>TODO</summary>
    static member ForAllGenShrink (expr: Expr<'a * 'b -> bool>, generator: Gen<'a * 'b>, shrinker: 'a * 'b -> ('a * 'b) seq): Property =
        let arb = FsCheck.FSharp.Arb.fromGenShrink (
            generator,
            fun (a: 'a, b: 'b) -> shrinker (a, b)
        )
        expr
        |> Expressions.eval<'a * 'b -> bool>
        |> FsCheck.FSharp.Prop.forAll arb


    static member CheckTest (expr: Expr, test: Expr -> 'Testable, ?config: Config): unit =
        let config: Config = defaultArg config defaultConfig
        let captureRunner: CaptureRunner = CaptureRunner config.Runner
        let config: Config = config.WithRunner captureRunner

        try Check.One (config, test expr) with ex ->
            let actualOption, expectedOption = Expressions.extractActualAndExpected expr captureRunner.Shrunk

            Core.failNow
            <| AssertifyResult.MakeResult (
                "CheckTest",
                expression = Expressions.toReadableExpression captureRunner.Shrunk expr,
                expected = expectedOption,
                actual = actualOption,
                ?originalInputs = captureRunner.Original,
                ?shrunkInputs = captureRunner.Shrunk,
                errorMessage = ex.Message,
                stacktrace = ex.StackTrace
            )

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="expr">TODO</param>
    /// <param name="test">TODO</param>
    /// <param name="config">TODO</param>
    // static member CheckAdvanced<'a, 'b> (expr: Expr<'a -> 'b -> bool>, test: Expr<'a -> 'b -> bool> -> 'b -> FsCheck.Property, ?config: Config): unit =
    //     let config: Config = defaultArg config defaultConfig
    //     let captureRunner: CaptureRunner = CaptureRunner config.Runner
    //     let config: Config = config.WithRunner captureRunner
    //
    //     try Check.One (config, test expr) with ex ->
    //         let expectedOption, actualOption = Expressions.extractActualAndExpected expr captureRunner.Shrunk
    //
    //         Core.failNow
    //         <| AssertifyResult.MakeResult (
    //             "CheckAdvanced",
    //             expression = (
    //                 captureRunner.Shrunk
    //                 |> fun s ->
    //                     if s.IsSome then printfn $"{s.Value}"
    //                     s
    //                 |> Option.map (Expressions.simplifyExpression expr)
    //                 |> Option.defaultValue (expr.Decompile ())
    //             ),
    //             expected = expectedOption,
    //             actual = actualOption,
    //             ?originalInputs = captureRunner.Original,
    //             ?shrunkInputs = captureRunner.Shrunk,
    //             errorMessage = ex.Message,
    //             stacktrace = ex.StackTrace
    //         )


    static member CheckWithProperty (expr: Expr<'a -> bool>, test: Expr<'a -> bool> -> FsCheck.Property, ?config: Config): unit =
        let config: Config = defaultArg config defaultConfig
        let captureRunner: CaptureRunner = CaptureRunner config.Runner
        let config: Config = config.WithRunner captureRunner

        try Check.One (config, test expr) with ex ->
            let actualOption, expectedOption = Expressions.extractActualAndExpected expr captureRunner.Shrunk
            Core.failNow
            <| AssertifyResult.MakeResult (
                "CheckWithProperty",
                expression = Expressions.toReadableExpression captureRunner.Shrunk expr,
                expected = expectedOption,
                actual = actualOption,
                ?originalInputs = captureRunner.Original,
                ?shrunkInputs = captureRunner.Shrunk,
                errorMessage = ex.Message,
                stacktrace = ex.StackTrace
            )


    /// <summary>
    /// Example:
    /// <code>
    /// <![CDATA[[<TestMethod; Timeout 1000>]]]>
    /// member this.``some test`` (): unit =
    ///     let solution (x: Nat) (y: Nat) (z: Nat): Nat = ...
    ///     Checkify.Check (
    ///         <![CDATA[<@ fun (x: int) (y: int) (z: int) -> methodStudent x y z = solution x y z @>]]>
    ///     , Config.QuickThrowOnFailure
    /// )
    /// </code>
    /// </summary>
    /// <param name="expr">An expression of a curried, anonymous function with comparison: <c>fun param1 param2 ... -> methodName params = soution params</c> See example for reference.</param>
    /// <param name="config">The configuration to use for the tests.</param>
    // TODO: Add inline function like (???) instead of Checkify.Check
    // TODO: Add overrides like: CheckEquality, CheckGreaterThan, CheckBool, CheckProperty (takes FsCheck.Property as param)
    static member Check (expr: Expr<'a>, ?config: Config): unit =
        let config: Config = defaultArg config defaultConfig
        let captureRunner: CaptureRunner = CaptureRunner config.Runner
        let config: Config = config.WithRunner captureRunner

        try Check.One (config, Expressions.eval<'a> expr) with ex ->
            let actualOption, expectedOption  = Expressions.extractActualAndExpected expr captureRunner.Shrunk
            Core.failNow
            <| AssertifyResult.MakeResult (
                "Check",
                expression = Expressions.toReadableExpression captureRunner.Shrunk expr,
                expected = expectedOption,
                actual = actualOption,
                ?originalInputs = captureRunner.Original,
                ?shrunkInputs = captureRunner.Shrunk,
                errorMessage = ex.Message,
                stacktrace = ex.StackTrace
            )


    static member CheckBoolean (expr: Expr<'a -> bool list>, ?config: Config): unit =
        let config: Config = defaultArg config defaultConfig
        let captureRunner: CaptureRunner = CaptureRunner config.Runner
        let config: Config = config.WithRunner captureRunner

        try Check.One (config, fun (x: 'a) -> (expr @@ [ x ]) |> Expressions.eval<bool list> |> List.forall id) with ex ->
            let actualOption, expectedOption = Expressions.extractActualAndExpected expr captureRunner.Shrunk

            Core.failNow
            <| AssertifyResult.MakeResult (
                "CheckTest",
                expression = Expressions.toReadableExpression captureRunner.Shrunk expr,
                expected = expectedOption,
                actual = actualOption,
                ?originalInputs = captureRunner.Original,
                ?shrunkInputs = captureRunner.Shrunk,
                errorMessage = ex.Message,
                stacktrace = ex.StackTrace
            )


//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


/// <summary>TODO</summary>
module Operators =
    /// <summary>TODO</summary>
    let inline (|?>) (expr: Expr) (test: Expr -> 'Testable): unit =
        Checkify.CheckTest (expr, test)




//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
// EOF %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%