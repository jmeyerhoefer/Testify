namespace GdP23.S09.A3.Template

module Program =

    open Mini
    open Model
    open Helpers

    // a)
    let rec nullable<'T> (r: Reg<'T>): Bool =
        failwith "TODO"

    // b)
    let rec divide<'T when 'T: comparison> (x: 'T) (r: Reg<'T>): Reg<'T> =
        failwith "TODO"

    // c)
    let calculateAutomaton<'T when 'T: comparison> (r: Reg<'T>): Automaton<'T> =
        failwith "TODO"

    // d)
    type Alphabet = | Zero | One | Dot

    let floatRegex: Reg<Alphabet> =
        Sym Zero // TODO: durch richtigen Ausdruck ersetzen

    // e)

    // Hier einen Regulären Ausdruck definieren, der von `dotnet run` benutzt werden soll:
    type Alphabet2 = | A | B
    let mainRegex = Rep (Cat (Sym A, Sym B)) // (ab)*

    // Alternativ: Den Ausdruck von oben nehmen
    // let mainRegex = floatRegex

