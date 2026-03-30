namespace MiniLib.Testify


open System
open System.Collections.Concurrent
open System.IO
open System.Reflection
open System.Xml.Linq
open FSharp.Compiler.EditorServices
open Microsoft.FSharp.Quotations
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text


[<RequireQualifiedAccess>]
module SourceMapping =
    type QuotationTarget =
        {
            ContainerName: string option
            MemberName: string
        }

    let private checker: Lazy<FSharpChecker> =
        lazy FSharpChecker.Create (keepAssemblyContents=true)

    let private navigationCache: ConcurrentDictionary<string, NavigationItems> =
        ConcurrentDictionary<string, NavigationItems> ()

    let private projectDirectoryCache : Lazy<string option> =
        lazy
            let rec ascend (directory: string) =
                let projectFile = Path.Combine (directory, "MiniLib.fsproj")

                if File.Exists projectFile then
                    Some directory
                else
                    let parent = Directory.GetParent directory
                    match parent with
                    | null -> None
                    | parent -> ascend parent.FullName

            [
                Directory.GetCurrentDirectory ()
                AppContext.BaseDirectory
                __SOURCE_DIRECTORY__
            ]
            |> List.distinct
            |> List.tryPick (fun directory ->
                if String.IsNullOrWhiteSpace directory || not (Directory.Exists directory) then
                    None
                else
                    ascend directory)

    let private projectFileCache : Lazy<string option> =
        lazy
            projectDirectoryCache.Value
            |> Option.map (fun directory -> Path.Combine (directory, "MiniLib.fsproj"))

    let private sourceFilesCache : Lazy<string list option> =
        lazy
            projectFileCache.Value
            |> Option.bind (fun projectFile ->
                match Path.GetDirectoryName projectFile with
                | null -> None
                | projectDirectory when String.IsNullOrWhiteSpace projectDirectory -> None
                | projectDirectory ->
                    let document = XDocument.Load projectFile

                    let files =
                        document.Descendants (XName.Get "Compile")
                        |> Seq.choose (fun element ->
                            match element.Attribute (XName.Get "Include") with
                            | null -> None
                            | includeAttribute ->
                                let relativePath = includeAttribute.Value

                                if String.IsNullOrWhiteSpace relativePath
                                   || not (
                                       relativePath.EndsWith (
                                           ".fs",
                                           StringComparison.OrdinalIgnoreCase
                                       )
                                   ) then
                                    None
                                else
                                    let fullPath =
                                        Path.Combine (projectDirectory, relativePath)
                                        |> Path.GetFullPath

                                    if Diagnostics.isRelevantSourceFile fullPath then
                                        Some fullPath
                                    else
                                        None)
                        |> Seq.toList

                    Some files)

    let private tryFindProjectDirectory () : string option =
        projectDirectoryCache.Value

    let private tryGetProjectFile () : string option =
        projectFileCache.Value

    let private tryGetProjectSourceFiles () : string list option =
        sourceFilesCache.Value

    let private parseFile (filePath: string) : FSharpParseFileResults =
        let parsingOptions =
            { FSharpParsingOptions.Default with
                SourceFiles = [| filePath |]
            }

        let sourceText =
            File.ReadAllText filePath
            |> SourceText.ofString

        checker.Value.ParseFile (filePath, sourceText, parsingOptions)
        |> Async.RunSynchronously

    let private getNavigationItems (filePath: string) : NavigationItems =
        navigationCache.GetOrAdd (filePath, fun path ->
            let parseResults = parseFile path
            parseResults.GetNavigationItems ())

    let private tryExtractContainerName (declaringType: Type | null) : string option =
        match declaringType with
        | null -> None
        | declaringType ->
            let candidates =
                [ declaringType.FullName; declaringType.Name ]
                |> List.choose (fun value ->
                    match value with
                    | null -> None
                    | value when String.IsNullOrWhiteSpace value -> None
                    | value -> Some value)

            candidates
            |> List.tryPick (fun name ->
                name.Split ([| '.'; '+' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.tryLast
                |> Option.bind (fun segment ->
                    if String.IsNullOrWhiteSpace segment then None
                    else Some segment))

    let private tryCreateTargetFromMethodInfo
        (methodInfo: MethodInfo | null)
        : QuotationTarget option =
        match methodInfo with
        | null -> None
        | methodInfo ->
            Some {
                ContainerName = tryExtractContainerName methodInfo.DeclaringType
                MemberName = methodInfo.Name
            }

    let private tryCreateTargetFromPropertyInfo
        (propertyInfo: PropertyInfo | null)
        : QuotationTarget option =
        match propertyInfo with
        | null -> None
        | propertyInfo ->
            Some {
                ContainerName = tryExtractContainerName propertyInfo.DeclaringType
                MemberName = propertyInfo.Name
            }

    let rec private tryFindTargetInQuotation (expr: Expr) : QuotationTarget option =
        match expr with
        | Patterns.Call (_, methodInfo, _) ->
            tryCreateTargetFromMethodInfo methodInfo
        | Patterns.PropertyGet (_, propertyInfo, _) ->
            tryCreateTargetFromPropertyInfo propertyInfo
        | Patterns.Application (funcExpr, _) ->
            tryFindTargetInQuotation funcExpr
        | Patterns.Let (_, _, bodyExpr) ->
            tryFindTargetInQuotation bodyExpr
        | Patterns.Lambda (_, bodyExpr) ->
            tryFindTargetInQuotation bodyExpr
        | Patterns.Sequential (_, bodyExpr) ->
            tryFindTargetInQuotation bodyExpr
        | Patterns.Coerce (innerExpr, _) ->
            tryFindTargetInQuotation innerExpr
        | Patterns.ValueWithName (_, _, name) ->
            Some {
                ContainerName = None
                MemberName = name
            }
        | ExprShape.ShapeCombination (_, arguments) ->
            arguments
            |> List.tryPick tryFindTargetInQuotation
        | ExprShape.ShapeVar _ ->
            None
        | _ ->
            None

    let private scoreCandidate
        (target: QuotationTarget)
        (filePath: string)
        (topLevelName: string)
        (itemName: string)
        : int =
        let fileName = Path.GetFileNameWithoutExtension filePath

        let containerScore =
            match target.ContainerName with
            | Some container when
                String.Equals(container, topLevelName, StringComparison.Ordinal) -> 100
            | Some container when
                String.Equals(container, fileName, StringComparison.Ordinal) -> 80
            | Some _ -> 0
            | None -> 10

        let memberScore =
            if String.Equals(target.MemberName, itemName, StringComparison.Ordinal) then 50
            else 0

        containerScore + memberScore

    let private tryCreateLocationFromRange (range: Range) : Diagnostics.SourceLocation option =
        if not (Diagnostics.isRelevantSourceFile range.FileName) || range.StartLine <= 0 then
            None
        else
            Some {
                FilePath = range.FileName
                Line = range.StartLine
                Column = Some (range.StartColumn + 1)
                Context = Diagnostics.tryReadContext range.FileName range.StartLine 5
            }

    let private tryFindSourceLocationForTarget
        (target: QuotationTarget)
        : Diagnostics.SourceLocation option =
        let chooseHigherScore
            (best: (int * Diagnostics.SourceLocation) option)
            (candidate: int * Diagnostics.SourceLocation)
            : (int * Diagnostics.SourceLocation) option =
            match best with
            | Some (bestScore, _) when bestScore >= fst candidate ->
                best
            | _ ->
                Some candidate

        tryGetProjectSourceFiles ()
        |> Option.bind (fun sourceFiles ->
            sourceFiles
            |> List.fold (fun best filePath ->
                let navigationItems = getNavigationItems filePath

                navigationItems.Declarations
                |> Array.fold (fun current declaration ->
                    let topLevelName = declaration.Declaration.LogicalName

                    let currentWithDirectMatch =
                        if String.Equals (
                            declaration.Declaration.LogicalName,
                            target.MemberName,
                            StringComparison.Ordinal
                        ) then
                            tryCreateLocationFromRange declaration.Declaration.Range
                            |> Option.map (fun location ->
                                scoreCandidate
                                    target
                                    filePath
                                    topLevelName
                                    declaration.Declaration.LogicalName,
                                location)
                            |> Option.map (chooseHigherScore current)
                            |> Option.defaultValue current
                        else
                            current

                    declaration.Nested
                    |> Array.fold (fun nestedCurrent item ->
                        if String.Equals (
                            item.LogicalName,
                            target.MemberName,
                            StringComparison.Ordinal
                        ) then
                            tryCreateLocationFromRange item.Range
                            |> Option.map (fun location ->
                                scoreCandidate target filePath topLevelName item.LogicalName,
                                location)
                            |> Option.map (chooseHigherScore nestedCurrent)
                            |> Option.defaultValue nestedCurrent
                        else
                            nestedCurrent) currentWithDirectMatch) best) None
            |> Option.map snd)

    let tryFindSourceLocationFromQuotation<'T>
        (expr: Expr<'T>)
        : Diagnostics.SourceLocation option =
        expr
        |> Expressions.simplify
        |> tryFindTargetInQuotation
        |> Option.bind tryFindSourceLocationForTarget

    let tryFindSourceLocationFromMethodInfo
        (methodInfo: MethodInfo)
        : Diagnostics.SourceLocation option =
        methodInfo
        |> tryCreateTargetFromMethodInfo
        |> Option.bind tryFindSourceLocationForTarget
