module Program


open System.IO


[<EntryPoint>]
let main (args: string array): int =
    @"D:\Bachelorarbeit\24-ba-jakob-meyerhoefer\error-pattern\data\Exercises\GdP23\Uploads"
    |> Directory.GetDirectories
    |> Array.map (fun (sheet: string) ->
        // printfn "%s" sheet
        sheet
        |> Directory.GetDirectories
        |> Array.map Path.GetFileName
        |> Array.filter (fun (name: string) -> not (name.EndsWith "t"))
        |> Array.groupBy (fun (name: string) -> (name.Split "_")[0])
        |> Array.map (fun x ->
            snd x |> Array.maxBy (fun elem -> (elem.Split "_")[1] |> int)
        )
    )
    |> Array.collect id
    |> Array.groupBy (fun (name: string) -> (name.Split "_")[0])
    |> Array.map (fun x ->
        snd x |> Array.maxBy (fun elem -> (elem.Split "_")[1] |> int)
    )
    |> printfn "%A"

    [19; 21; 21; 20; 21; 19; 18; 18; 22; 21; 20] |> List.sum |> printfn "Anzahl Studenten: %d"

    0