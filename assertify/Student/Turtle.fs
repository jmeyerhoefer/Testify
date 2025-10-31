module Student.Turtle


open Types.TurtleTypes


// Beispiel vom Übungsblatt
let ex = [D; F 50.0; L 45.0; F 50.0]

// a)
let right (angle: Double): Command = failwith "TODO"

// b)
let rec substF (transformF: Double -> Program) (p: Program): Program =
    failwith "TODO"

// c)
let levyStart (len: Double) = failwith "TODO"

let levyTransform (len: Double): Program =
    failwith "TODO"

// d)
let kochflockeStart (len: Double) =
    failwith "TODO"

let kochflockeTransform (len: Double): Program =
    failwith "TODO"

// e)
let pentaplexityStart (len: Double) =
    failwith "TODO"

let pentaplexityTransform (len: Double): Program =
    failwith "TODO"
