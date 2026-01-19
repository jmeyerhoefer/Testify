module Calculus.Types


////beginTypeFun)
type Function =
    | Const of Nat                    // Konstante Funktion   f(x) = c
    | Id                              // Identität            f(x) = x
    | Add of Function * Function      // Addition             f(x) = g(x) + h(x)
    | Mul of Function * Function      // Multiplikation       f(x) = g(x) * h(x)
    | Pow of Function * Nat           // Potenz               f(x) = g(x) ^ n
    | Comp of Function * Function     // Komposition          f(x) = g(h(x))
////endTypeFun)


////beginTypeIFun)
type IFunction =
    abstract member ToString: unit -> string
    abstract member Apply: Nat -> Nat
    abstract member Derive: unit -> IFunction
////endTypeIFun)