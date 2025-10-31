module Solution.Ternary


open Types.TernaryTypes


////ex)
let one   = [P]
let two   = [M; P]
let three = [Z; P]


////a)
let rec bedeutung (n: List<Ternary>): Int =
    match n with
    | [] -> 0
    | M::ns -> 3 * bedeutung ns - 1
    | Z::ns -> 3 * bedeutung ns // + 0
    | P::ns -> 3 * bedeutung ns + 1

////b)
let zCons (ns: List<Ternary>): List<Ternary> =
    match ns with
    | [] -> []
    | _ -> Z::ns

////c)
let rec inc (n: List<Ternary>): List<Ternary> =
    match n with
    | [] -> [P]
    | M::ns -> zCons ns
    | Z::ns -> P::ns
    | P::ns -> M::(inc ns)

////d)
let rec dec (n: List<Ternary>): List<Ternary> =
    match n with
    | [] -> [M]
    | M::ns -> P::(dec ns)
    | Z::ns -> M::ns
    | P::ns -> zCons ns

////e)
let rec fromInt (n: Int): List<Ternary> =
    if n = 0 then []
    else if n % 3 =  2 then M::(inc (fromInt (n/3)))
    else if n % 3 =  1 then P::(fromInt (n/3))
    else if n % 3 = -1 then M::(fromInt (n/3))
    else if n % 3 = -2 then P::(dec (fromInt (n/3)))
    else (* n % 3 =  0 *)   zCons (fromInt (n/3))

////f)
let rec add (m: List<Ternary>) (n: List<Ternary>): List<Ternary> =
    match (m, n) with
    | ([], x) | (x, []) -> x
    | (M::ms, M::ns) -> P :: (add (dec ms) ns)
    | (P::ms, P::ns) -> M :: (add (inc ms) ns)
    | (M::ms, Z::ns) | (Z::ms, M::ns)                  -> M :: (add ms ns)
    | (M::ms, P::ns) | (P::ms, M::ns) | (Z::ms, Z::ns) -> zCons (add ms ns)
    | (P::ms, Z::ns) | (Z::ms, P::ns)                  -> P :: (add ms ns)

////g)
let rec negative (n: List<Ternary>): List<Ternary> =
    match n with
    | [] -> []
    | M::ns -> P::(negative ns)
    | Z::ns -> zCons (negative ns)
    | P::ns -> M::(negative ns)
////end)
