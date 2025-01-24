module Parser

open System.Xml.Linq


/// <summary>
/// TODO
/// </summary>
/// <param name="path">TODO</param>
let parseStdOutput (path: string): string seq =
    let xDocument: XDocument = XDocument.Load path
    xDocument.Descendants (XName.Get "StdOut")
    |> Seq.map (fun (xElement: XElement) -> xElement.Value)