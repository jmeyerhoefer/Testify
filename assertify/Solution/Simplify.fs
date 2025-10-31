module Solution.Simplify


// Das angegebene Beispiel:
let expression0 a = false = (a = true)
let simplified0 a = not a

// a):
let expressionA a b = if a then b else false
let simplifiedA a b =
////a)
    a && b
////a.)

// b)
let expressionB a = if (a = true) then 2N else 3N
let simplifiedB a =
////b)
    if a then 2N else 3N
////b.)

// c)
let expressionC x = if (x <> 0N) then false else true
let simplifiedC x =
////c)
    x = 0N
////c.)


