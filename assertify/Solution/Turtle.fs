module Solution.Turtle


open Types.TurtleTypes


////ex)
let ex = [D; F 50.0; L 45.0; F 50.0]

////a)
let right (angle: Double): Command = L (- angle)

////b)
let rec substF (transformF: Double -> Program) (p: Program): Program =
    match p with
    | []          -> []
    | (F len)::ps -> transformF len @ substF transformF ps
    | cmd::ps     -> cmd::(substF transformF ps)

////c)
// F
let levyStart (len: Double) = [D; F len]

// +F--F+
let levyTransform (len: Double): Program =
    let l = len / sqrt(2.0)
    [L 45.0; F l; right 90.0; F l; L 45.0]

////d)
// F--F--F
let kochflockeStart (len: Double) =
    let a = 60.0
    [D; F len
      ; right (2.0*a); F len
      ; right (2.0*a); F len]

// F -> F+F--F+F
let kochflockeTransform (len: Double): Program =
    let l = len / 3.0
    let a = 60.0
    let flf = [F l; L a; F l]
    flf @ [right (2.0*a)] @ flf

////e)
// F++F++F++F++F
let pentaplexityStart (len: Double) =
    let a = 36.0
    let lf = [L (2.0*a); F len]
    [D; F len] @ lf @ lf @ lf @ lf

// F -> F++F++F|F-F++F
let pentaplexityTransform (len: Double): Program =
    let phi = (1.0 + sqrt 5.0) / 2.0
    let l = len / (phi ** 2.0)
    let a = 36.0
    [F l; L (2.0*a); F l
        ; L (2.0*a); F l
        ; L 180.0;   F l
        ; right a;   F l
        ; L (2.0*a); F l]
////end)