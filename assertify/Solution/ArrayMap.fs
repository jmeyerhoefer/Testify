module Solution.ArrayMap


open Types.ArrayMapTypes


////a)
let map<'a,'b> (f: 'a -> 'b) (ar: Array<'a>) : Array<'b> =
    [| for x in ar -> f x |]

////b)
let inplaceMap<'a> (f: 'a -> 'a) (ar: Array<'a>) : Unit =
    for i in 0 .. ar.Length - 1 do
        ar.[i] <- f ar.[i]

////end)
