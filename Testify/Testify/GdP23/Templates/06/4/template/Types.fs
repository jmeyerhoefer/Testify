namespace GdP23.S06.A4.Template

[<AutoOpen>]
module Types =
    open Mini

    type Command =
        | D           // Drop:    Stift absetzen/anfangen zu zeichnen
        | F of Double // Forward: Vorwärts bewegen
        | L of Double // Left:    Nach links/gegen den Uhrzeigersinn drehen

    type Program = List<Command>
