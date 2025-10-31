module Types.CalculusTypes

// Funktionale Modellierung

type Function =
    | Constant of Nat
    | Id
    | Add of (Function * Function)
    // Für Teilaufgabe a aktivieren:
    | Mul of (Function * Function)
    | Pow of (Function * Nat)
    | Comp of (Function * Function)


// Objektorientierte Modellierung

type IFunction =
    interface
        abstract member ToString: Unit -> String
        abstract member Apply: Nat -> Nat
        // Für Teilaufgabe b aktivieren:
        abstract member Derive: Unit -> IFunction
    end
