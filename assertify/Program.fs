module Program


open Microsoft.FSharp.Quotations


[<EntryPoint>]
let main (_: string array): int =
    let regexPattern: string = @"(error|warning) (FS\d{4})\s*(.*?)(?=\n\s*(?:error|warning|$))"
    0