namespace Testify


[<RequireQualifiedAccess>]
module Diagnostics =
    open System
    open System.IO

    type SourceLocation =
        {
            FilePath: string
            Line: int
            Column: int option
            Context: string option
        }

    let private fileNameOrFallback
        (filePath: string)
        : string =
        match Path.GetFileName filePath with
        | null -> filePath
        | fileName when String.IsNullOrWhiteSpace fileName -> filePath
        | fileName -> fileName

    let formatLocation
        (location: SourceLocation)
        : string =
        [
            $"Location: {fileNameOrFallback location.FilePath}"
            $"Line: {location.Line}"

            match location.Column with
            | Some column -> $"Approximate character: {column}"
            | None -> ()
        ]
        |> String.concat Environment.NewLine

    let tryReadContext
        (filePath: string)
        (line: int)
        (radius: int)
        : string option =
        if not (File.Exists filePath) || line <= 0 || radius < 0 then
            None
        else
            let lines = File.ReadAllLines filePath

            if lines.Length = 0 then
                None
            else
                let startLine = max 1 (line - radius)
                let endLine = min lines.Length (line + radius)

                [
                    for currentLine in startLine .. endLine do
                        let marker = if currentLine = line then ">" else " "
                        let content = lines[currentLine - 1]
                        yield $"{marker} {currentLine, 4}: {content}"
                ]
                |> String.concat Environment.NewLine
                |> Some

    let getStacktrace (ex: exn) : System.Diagnostics.StackTrace =
        System.Diagnostics.StackTrace (ex, true)

    let private normalizeFilePath
        (filePath: string)
        : string =
        filePath.Replace('/', '\\').ToLowerInvariant ()

    let private containsAny
        (needles: string list)
        (value: string)
        : bool =
        needles
        |> List.exists value.Contains

    let private ignoredRuntimePathFragments =
        [
            "\\microsoft.net\\"
            "\\fsharp.core\\"
            "\\dotnet\\"
            "\\.nuget\\packages\\"
        ]

    let tryGetFrames (ex: exn) : System.Diagnostics.StackFrame array option =
        let stacktrace = getStacktrace ex
        let frames = stacktrace.GetFrames ()

        match box frames with
        | null -> None
        | _ -> Some frames

    let isRelevantSourceFile (filePath: string | null) : bool =
        match filePath with
        | null -> false
        | filePath ->
            if System.String.IsNullOrWhiteSpace filePath then
                false
            else
                let normalized = normalizeFilePath filePath
                not (normalized.EndsWith "\\tests.fs")
                    && not (normalized.Contains "\\testify\\")
                    && not (containsAny ignoredRuntimePathFragments normalized)

    let private tryFrameToSourceLocationWhen
        (isRelevantFile: string | null -> bool)
        (frame: System.Diagnostics.StackFrame)
        : SourceLocation option =
        match frame.GetFileName () with
        | null -> None
        | filePath ->
            let line = frame.GetFileLineNumber ()

            if not (isRelevantFile filePath) || line <= 0 then
                None
            else
                let column =
                    match frame.GetFileColumnNumber () with
                    | value when value > 0 -> Some value
                    | _ -> None

                Some {
                    FilePath = filePath
                    Line = line
                    Column = column
                    Context = tryReadContext filePath line 4
                }

    let tryFrameToSourceLocation (frame: System.Diagnostics.StackFrame) : SourceLocation option =
        tryFrameToSourceLocationWhen isRelevantSourceFile frame

    let tryFindRelevantExceptionLocation (ex: exn) : SourceLocation option =
        tryGetFrames ex
        |> Option.bind (Array.tryPick tryFrameToSourceLocation)
