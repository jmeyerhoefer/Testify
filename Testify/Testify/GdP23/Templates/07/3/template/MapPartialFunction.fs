namespace GdP23.S07.A3.Template

module MapPartialFunction =
    open Mini

    ////type)
    type MapPartialFunction<'k, 'v> = 'k -> Option<'v>

    // Beispiele
    let m1: MapPartialFunction<Nat, String> = fun k -> None
    let m2: MapPartialFunction<Nat, String> = fun k -> if      k = 1N then Some "Lisa"
                                                       else if k = 4N then Some "Harry"
                                                       else None
    let m3: MapPartialFunction<Nat, String> = fun k -> if      k = 1N then Some "Lisa"
                                                       else if k = 4N then Some "Harry"
                                                       else if k = 5N then Some "Bob"
                                                       else if k = 6N then Some "Schorsch"
                                                       else None

    ////a)
    let empty<'k,'v> : MapPartialFunction<'k, 'v> =
        failwith "TODO"

    ////b)
    let lookup<'k, 'v> (key: 'k) (map: MapPartialFunction<'k, 'v>): Option<'v> =
        failwith "TODO"

    ////c)
    let set<'k,'v when 'k: equality> (key: 'k) (value: 'v) (map: MapPartialFunction<'k, 'v>): MapPartialFunction<'k, 'v> =
        failwith "TODO"

    ////d)
    let comma<'k, 'v> (map1: MapPartialFunction<'k, 'v>) (map2: MapPartialFunction<'k, 'v>): MapPartialFunction<'k, 'v> =
        failwith "TODO"

    ////e)
    let rec delete<'k, 'v when 'k: equality> (key: 'k) (map: MapPartialFunction<'k, 'v>): MapPartialFunction<'k, 'v> =
        failwith "TODO"

