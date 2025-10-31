module Student.Calculus


open Types.CalculusTypes


// ----------------------------------------------------------------------------
// Funktionale Modellierung

let rec toString (f: Function): String =
    match f with
    | Constant n -> show n
    | Id -> "x"
    | Add (f1, f2) -> "(" + toString f1 + ") + (" + toString f2 + ")"

let rec apply (f: Function) (x: Nat): Nat =
    match f with
    | Constant n -> n
    | Id -> x
    | Add (f1, f2) -> apply f1 x + apply f2 x

// b)
let rec derive (f: Function): Function =
    failwith "TODO"


// ----------------------------------------------------------------------------
// Objektorientierte Modellierung

let rec constant (c: Nat): IFunction =
    { new IFunction with
        member self.ToString (): String = show c
        member self.Apply (x: Nat): Nat = c
        member self.Derive (): IFunction = self
    }

let id (): IFunction =
    { new IFunction with
        member self.ToString (): String = "x"
        member self.Apply (x: Nat): Nat = x
        member self.Derive (): IFunction = self
    }

let rec add (f1: IFunction, f2: IFunction): IFunction =
    { new IFunction with
        member self.ToString (): String = "(" + f1.ToString () + " + " + f2.ToString () + ")"
        member self.Apply (x: Nat): Nat = f1.Apply(x) + f2.Apply(x)
        member self.Derive (): IFunction = self
    }

// a1)
let rec mul (f1: IFunction, f2: IFunction): IFunction =
    failwith "TODO"

// a2)
let rec pow (f1: IFunction, f2: Nat): IFunction =
    failwith "TODO"

// a3)
let rec comp (f1: IFunction, f2: IFunction): IFunction =
    failwith "TODO"
