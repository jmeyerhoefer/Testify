namespace GdP23.S05.A3.Template

module Simplify =
    open Mini

    // Das angegebene Beispiel:
    let expression0 a = false = (a = true)
    let simplified0 a = not a

    // a)
    let expressionA a b = if a then b else false
    let simplifiedA a b = failwith "TODO"

    // b)
    let expressionB a = if (a = true) then 2N else 3N
    let simplifiedB a = failwith "TODO"

    // c)
    let expressionC x = if (x <> 0N) then false else true
    let simplifiedC x = failwith "TODO"



