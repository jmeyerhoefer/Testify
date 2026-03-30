namespace Testify


open System
open System.Reflection
open System.Threading.Tasks
open Microsoft.FSharp.Quotations


[<RequireQualifiedAccess>]
module internal Awaitable =
    let private observeTaskResult<'T>
        (computation: Task<'T>)
        : Task<Observed<'T>> =
        task {
            try
                let! value = computation
                return Result.Ok value
            with ex ->
                return Result.Error ex
        }

    let private observeAsyncImpl<'T>
        (computation: Async<'T>)
        : Task<Observed<'T>> =
        observeTaskResult (Async.StartAsTask computation)

    let private observeTaskImpl<'T>
        (computation: Task<'T>)
        : Task<Observed<'T>> =
        observeTaskResult computation

    let observeUntyped<'T>
        (expr: Expr)
        : Task<Observed<'T>> =
        let value = Common.evalQuotation expr

        match value with
        | :? Async<'T> as asyncValue ->
            observeAsyncImpl asyncValue
        | :? Task<'T> as taskValue ->
            observeTaskImpl taskValue
        | :? Task as taskValue when typeof<'T> = typeof<unit> ->
            observeTaskResult (
                task {
                    do! taskValue
                    return Unchecked.defaultof<'T>
                }
            )
        | _ ->
            invalidArg
                (nameof expr)
                $"Expected an Async<{typeof<'T>.Name}>, Task<{typeof<'T>.Name}>, \
                or Task expression."
