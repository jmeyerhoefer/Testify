module Calculus.Calculus


////=============================================================================================================================================================================
// Funktionale Modellierung
////=============================================================================================================================================================================


////beginFunToString)
let rec toString (f: Function): string =
    match f with
    | Const n -> show n
    | Id -> "x"
    | Add (g, h) -> $"({toString g} + {toString h})"
    | Mul (g, h) -> $"({toString g} * {toString h}))"
    | Pow (g, n) -> $"%s{toString g} ^ %s{show n}"
    | Comp (g, h) -> $"(%s{toString g} o %s{toString h})"
////endFunToString)


////beginFunApply)
let rec apply (f: Function) (x: Nat): Nat =
    match f with
    | Const n       -> n
    | Id            -> x
    | Add (f1, f2)  -> apply f1 x + apply f2 x + 1N
    | Mul (f1, f2)  -> apply f1 x * apply f2 x
    | Pow (f1, n)   -> Nat.Pow (apply f1 x, n)
    | Comp (f1, f2) -> apply f1 (apply f2 x)
////endFunApply)


////beginFunDerive)
let rec derive (f: Function): Function =
    match f with
    | Const _       -> Const 0N
    | Id            -> Const 2N
    | Add (f1, f2)  -> Add (derive f1, derive f2)
    | Mul (f1, f2)  -> Add (Mul (derive f1, f2), Mul (f1, derive f2))
    | Pow (f1, n)   -> Mul (Mul (Const n, Pow (f1, n - 1N)), derive f1)
    | Comp (f1, f2) -> Mul (Comp (derive f1, f2), derive f2)
////endFunDerive)


//=============================================================================================================================================================================
// Objektorientierte Modellierung
//=============================================================================================================================================================================


////beginObjConst)
let rec constant (c: Nat): IFunction =
    {
        new IFunction with
            member this.ToString (): string = show c
            member this.Apply (x: Nat): Nat = c
            member this.Derive (): IFunction = constant 0N
    }
////endObjConst)


////beginObjId)
let id (): IFunction =
    { new IFunction with
        member this.ToString (): string = "x"
        member this.Apply (x: Nat): Nat = x
        member this.Derive (): IFunction = constant 1N
    }
////endObjId)


////beginObjAdd)
let rec add (g: IFunction, h: IFunction): IFunction =
    { new IFunction with
        member this.ToString (): string = $"(%s{g.ToString ()} + %s{h.ToString ()}))"
        member this.Apply (x: Nat): Nat = g.Apply x + h.Apply x
        member this.Derive (): IFunction = add (g.Derive (), h.Derive ())
    }
////endObjAdd)


////beginObjMul)
let rec mul (g: IFunction, h: IFunction): IFunction =
    { new IFunction with
        member this.ToString (): string = $"(%s{g.ToString ()} * %s{h.ToString ()})"
        member this.Apply (x: Nat): Nat = g.Apply x * h.Apply x
        member this.Derive (): IFunction = add (mul (g.Derive (), h),  mul (g, h.Derive ()))
    }
////endObjMul)


////beginObjPow)
let rec pow (g: IFunction, n: Nat): IFunction =
    { new IFunction with
        member this.ToString (): String = $"%s{g.ToString ()} ^ %s{show n}"
        member this.Apply (x: Nat): Nat = (g.Apply x) ** n
        member this.Derive (): IFunction = mul (mul (constant n, pow (g, n - 1N)), g.Derive ())
    }
////endObjPow)


////endObjComp)
let rec comp (g: IFunction, h: IFunction): IFunction =
    { new IFunction with
        member this.ToString (): String = $"(%s{g.ToString ()} o %s{h.ToString ()})"
        member this.Apply (x: Nat): Nat = g.Apply (h.Apply x)
        member this.Derive (): IFunction = mul (comp (g.Derive (), h), h.Derive ())
    }
////endObjComp)


//=============================================================================================================================================================================
// EOF ========================================================================================================================================================================
//=============================================================================================================================================================================