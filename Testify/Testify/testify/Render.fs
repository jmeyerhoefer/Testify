namespace MiniLib.Testify


type FailureBranch =
    {
        Expectation: string
        Observed: string option
        ExpectedValue: string option
        ActualValue: string option
        Because: string option
    }


type FailureDetails =
    {
        Observed: string option
        ExpectedValue: string option
        ActualValue: string option
        Branches: FailureBranch list
    }


[<RequireQualifiedAccess>]
module Render =
    let internal splitLines (text: string) : string list =
        text.Replace("\r\n", "\n").Split '\n'
        |> Array.toList

    let private formatLabeledBlock
        (indent: string)
        (label: string)
        (text: string)
        : string list =
        let lines = splitLines text

        match lines with
        | [] -> []
        | [ single ] -> [ $"{indent}{label}: {single}" ]
        | _ ->
            [
                $"{indent}{label}:"
                yield! lines |> List.map (fun line -> $"{indent}  {line}")
            ]

    let formatFailureDetailsLines
        (details: FailureDetails)
        : string list =
        [
            match details.ExpectedValue with
            | Some expectedValue ->
                yield! formatLabeledBlock "" "Expected value" expectedValue
            | None -> ()

            match details.ActualValue with
            | Some actualValue ->
                yield! formatLabeledBlock "" "Actual value" actualValue
            | None -> ()

            match details.Observed with
            | Some observed ->
                yield! formatLabeledBlock "" "Observed" observed
            | None -> ()

            match details.Branches with
            | [] -> ()
            | branches ->
                yield "Failed branches:"

                for index, branch in List.indexed branches do
                    yield $"  {index + 1}. {branch.Expectation}"

                    match branch.ExpectedValue with
                    | Some expectedValue ->
                        yield! formatLabeledBlock "     " "Expected value" expectedValue
                    | None -> ()

                    match branch.ActualValue with
                    | Some actualValue ->
                        yield! formatLabeledBlock "     " "Actual value" actualValue
                    | None -> ()

                    match branch.Observed with
                    | Some observed ->
                        yield! formatLabeledBlock "     " "Observed" observed
                    | None -> ()

                    match branch.Because with
                    | Some because ->
                        yield! formatLabeledBlock "     " "Because" because
                    | None -> ()
        ]

    let formatFailureDetails
        (details: FailureDetails)
        : string =
        details
        |> formatFailureDetailsLines
        |> String.concat "\n"

    let formatValue (value: 'T) : string =
        $"%A{value}"

    let internal formatTupleLike (values: obj seq) : string =
        values
        |> Seq.map formatValue
        |> String.concat ", "
        |> fun valuesText -> $"({valuesText})"

    let private previewLimit = 5

    let private takePreviewItems
        (values: 'T seq)
        : 'T list * bool =
        use enumerator = values.GetEnumerator()

        let rec loop
            (remaining: int)
            (acc: 'T list)
            : 'T list * bool =
            if remaining <= 0 then
                if enumerator.MoveNext() then
                    List.rev acc, true
                else
                    List.rev acc, false
            elif enumerator.MoveNext() then
                loop (remaining - 1) (enumerator.Current :: acc)
            else
                List.rev acc, false

        loop previewLimit []

    let formatSequencePreview (values: 'T seq) : string =
        let items, hasMore = takePreviewItems values

        let itemsText =
            items
            |> Seq.ofList
            |> Seq.map formatValue
            |> String.concat "; "

        let suffix =
            if hasMore then
                "; ..."
            else
                ""

        $"[{itemsText}{suffix}]"

    let formatOption (value: 'T option) : string =
        match value with
        | Some inner -> $"Some {formatValue inner}"
        | None -> "None"

    let formatException (ex: exn) : string =
        $"{ex.GetType().Name}: {ex.Message}"

    let becauseExpressionThrew (ex: exn) : string =
        $"Expression raised an exception before producing a value: {formatException ex}"
