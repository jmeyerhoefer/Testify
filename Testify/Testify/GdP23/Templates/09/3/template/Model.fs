namespace GdP23.S09.A3.Template

module Model =

    open Mini

    type Reg<'T> =
        | Eps                       // das leere Wort
        | Sym of 'T                 // einzelnes Zeichen / Terminalsymbol
        | Cat of Reg<'T> * Reg<'T>  // Konkatenation / Sequenz
        | Empty                     // die leere Sprache
        | Alt of Reg<'T> * Reg<'T>  // Alternative
        | Rep of Reg<'T>            // Wiederholung

    type Automaton<'T when 'T: comparison> = Map<Reg<'T>, Map<'T, Reg<'T>> * Bool>

