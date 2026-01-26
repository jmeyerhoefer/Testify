module Calculus.Calculus


////================================================================================================
// Funktionale Modellierung ========================================================================
////================================================================================================


// Teilaufgabe a)
let rec toString (f: Function): string =
    failwith "TODO"


// Teilaufgabe b)
let rec apply (f: Function) (x: Nat): Nat =
    failwit "TODO"


// Teilaufgabe c)
let rec derive (f: Function): Function =
    failwith "TODO"


//==================================================================================================
// Objektorientierte Modellierung ==================================================================
//==================================================================================================


let rec constant (c: Nat): IFunction =
    {
        new IFunction with
            member this.ToString (): string = show c
            member this.Apply (x: Nat): Nat = c
            member this.Derive (): IFunction = constant 1N
    }


// Teilaufgabe d)
let id (): IFunction =
    failwith "TODO"


// Teilaufgabe e)
let rec add (g: IFunction, h: IFunction): IFunction =
    failwith "TODO"


// Teilaufgabe f)
let rec mul (g: IFunction, h: IFunction): IFunction =
    failwith "TODO"


// Teilaufgabe g)
let rec pow (g: IFunction, n: Nat): IFunction =
    failwith "TODO"


// Teilaufgabe h)
let rec comp (g: IFunction, h: IFunction): IFunction =
    failwith "TODO"


//=============================================================================================================================================================================
// EOF ========================================================================================================================================================================
//=============================================================================================================================================================================