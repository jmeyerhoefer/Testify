namespace GdP23

open Microsoft.VisualBasic.FileIO
open System
open System.Collections.Generic

module CsvParsing =
    let private readRows (path: string) : seq<IDictionary<string, string>> =
        seq {
            use parser = new TextFieldParser(path)
            parser.SetDelimiters(",")
            parser.HasFieldsEnclosedInQuotes <- true
            parser.TrimWhiteSpace <- false

            let headers = parser.ReadFields()

            if isNull headers then
                invalidOp $"CSV file '{path}' does not contain a header row."

            while not parser.EndOfData do
                let fields = parser.ReadFields()

                if not (isNull fields) then
                    let row = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

                    for index = 0 to headers.Length - 1 do
                        let value =
                            if index < fields.Length then
                                fields[index]
                            else
                                String.Empty

                        row[headers[index]] <- value

                    yield row :> IDictionary<string, string>
        }

    let loadSnapshotRecords (path: string) : SnapshotRecord list =
        readRows path
        |> Seq.map (fun row ->
            let tryParseBool (value: string) =
                match Boolean.TryParse value with
                | true, parsed -> parsed
                | _ -> false

            let tryParseInt (value: string) =
                match Int32.TryParse value with
                | true, parsed -> Some parsed
                | _ -> None

            let tryGetOptional key =
                match row.TryGetValue key with
                | true, value when not (String.IsNullOrWhiteSpace value) -> Some value
                | _ -> None

            {
                SheetId = row["SHEET"]
                AssignmentId = row["ASSIGNMENT"]
                GroupId = row["GROUPID"]
                TeamId = row["TEAMID"]
                SnapshotTimestamp = row["SNAPSHOT_TIMESTAMP"]
                Compiled = tryParseBool row["COMPILED"]
                InternalError = tryParseBool row["INTERNAL_ERROR"]
                TestsPassed = tryParseInt row["TESTS_PASSED"]
                TestsTotal = tryParseInt row["TESTS_TOTAL"]
                ResultJson = tryGetOptional "RESULT"
            })
        |> Seq.toList

    let loadRemovedRecords (path: string) : RemovedRecord list =
        readRows path
        |> Seq.map (fun row ->
            {
                SheetId = row["SHEET"]
                AssignmentId = row["ASSIGNMENT"]
                GroupId = row["GROUPID"]
                TeamId = row["TEAMID"]
                PhysicalFileName = row["PHYSICAL_FILENAME"]
                DeleteTimestamp = row["DELETE_TIMESTAMP"]
            })
        |> Seq.toList
