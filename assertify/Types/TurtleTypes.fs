module Types.TurtleTypes


type Command =
    | D           // Drop:    Stift absetzen/anfangen zu zeichnen
    | F of Double // Forward: Vorwärts bewegen
    | L of Double // Left:    Nach links/gegen den Uhrzeigersinn drehen

type Program = List<Command>