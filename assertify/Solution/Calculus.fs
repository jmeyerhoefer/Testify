module Solution.Calculus


// ----------------------------------------------------------------------------
// Funktionale Modellierung

////beginTypeFun)
type Function =
    | Constant of Nat               // Konstante Funktion
    | Id                            // Identität
    | Add of (Function * Function)  // Addition
    | Mul of (Function * Function)  // Multiplikation
    | Pow of (Function * Nat)       // Potenz
    | Comp of (Function * Function) // Verkettung
////endTypeFun)

let rec toString (f: Function): String =
    match f with
    | Constant n    -> show n
    | Id            -> "x"
    | Add (f1, f2)  -> "(" + toString f1 + " + " + toString f2 + ")"
    ////beginFunToString)
    | Mul (f1, f2)  -> "(" + toString f1 + " * " + toString f2 + ")"
    | Pow (f1, n)   -> toString f1 + " ^ " + show n
    | Comp (f1, f2) -> "(" + toString f1 + " o " + toString f2 + ")"
    ////endFunToString)

let rec apply (f: Function) (x: Nat): Nat =
    match f with
    | Constant n    -> n
    | Id            -> x
    | Add (f1, f2)  -> apply f1 x + apply f2 x
    ////beginFunApply)
    | Mul (f1, f2)  -> apply f1 x * apply f2 x
    | Pow (f1, n)   -> apply f1 x ** n
    | Comp (f1, f2) -> apply f1 (apply f2 x)
    ////endFunApply)

////beginFunDerive)
let rec derive (f: Function): Function =
    match f with
    | Constant n    -> Constant 0N
    | Id            -> Constant 1N
    | Add (f1, f2)  -> Add (derive f1, derive f2)
    | Mul (f1, f2)  -> Add (Mul (derive f1, f2), Mul (f1, derive f2))
    | Pow (f1, n)   -> Mul (Mul (Constant n, Pow (f1, n - 1N)), derive f1)
    | Comp (f1, f2) -> Mul (Comp (derive f1, f2), derive f2)
////endFunDerive)


// ----------------------------------------------------------------------------
// Objektorientierte Modellierung

////beginTypeObj)
type IFunction =
    interface
        abstract member ToString: Unit -> String
        abstract member Apply: Nat -> Nat
        abstract member Derive: Unit -> IFunction
    end
////endTypeObj)

let rec constant (c: Nat): IFunction =
    { new IFunction with
        member self.ToString (): String = show c
        member self.Apply (x: Nat): Nat = c
        ////beginObjConst)
        member self.Derive (): IFunction = constant 0N
        ////endObjConst)
    }

let id (): IFunction =
    { new IFunction with
        member self.ToString (): String = "x"
        member self.Apply (x: Nat): Nat = x
        ////beginObjId)
        member self.Derive (): IFunction = constant 1N
        ////endObjId)
    }

let rec add (f1: IFunction, f2: IFunction): IFunction =
    { new IFunction with
        member self.ToString (): String = "(" + f1.ToString () + " + " + f2.ToString () + ")"
        member self.Apply (x: Nat): Nat = f1.Apply(x) + f2.Apply(x)
        ////beginObjAdd)
        member self.Derive (): IFunction = add (f1.Derive(), f2.Derive())
        ////endObjAdd)
    }

////beginObjA)
let rec mul (f1: IFunction, f2: IFunction): IFunction =
    { new IFunction with
        member self.ToString (): String =
            "(" + f1.ToString () + " * " + f2.ToString () + ")"
        member self.Apply (x: Nat): Nat =
            f1.Apply(x) * f2.Apply(x)
        member self.Derive (): IFunction =
            add (mul (f1.Derive(), f2),  mul (f1, f2.Derive()))
    }

let rec pow (f1: IFunction, n: Nat): IFunction =
    { new IFunction with
        member self.ToString (): String =
            f1.ToString () + " ^ " + show n
        member self.Apply (x: Nat): Nat =
            f1.Apply(x) ** n
        member self.Derive (): IFunction =
            mul (mul (constant n, pow (f1, (n-1N))), f1.Derive())
    }

let rec comp (f1: IFunction, f2: IFunction): IFunction =
    { new IFunction with
        member self.ToString (): String =
            "(" + f1.ToString () + " o " + f2.ToString () + ")"
        member self.Apply (x: Nat): Nat =
            f1.Apply(f2.Apply(x))
        member self.Derive (): IFunction =
            mul (comp (f1.Derive(), f2), f2.Derive())
    }
////endObjA)
