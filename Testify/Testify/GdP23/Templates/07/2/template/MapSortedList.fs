namespace GdP23.S07.A2.Template

module MapSortedList =
    open Mini

    type MapSortedList<'k, 'v when 'k: comparison> = List<'k * 'v>

    // Beispiele
    let m1: MapSortedList<Nat, String> = []
    let m2: MapSortedList<Nat, String> = [(1N, "Lisa"); (4N, "Harry")]
    let m3: MapSortedList<Nat, String> = [(1N, "Lisa"); (4N, "Harry"); (5N, "Bob"); (6N, "Schorsch")]

    // a)
    let empty<'k, 'v when 'k: comparison> : MapSortedList<'k, 'v> =
        failwith "TODO"

    // b)
    let rec lookup<'k, 'v when 'k: comparison> (key: 'k) (m: MapSortedList<'k, 'v>): Option<'v> =
        failwith "TODO"

    // c)
    let rec set<'k, 'v when 'k: comparison> (key: 'k) (value: 'v) (m: MapSortedList<'k, 'v>): MapSortedList<'k, 'v> =
        failwith "TODO"

    // d)
    let rec comma<'k, 'v when 'k: comparison> (m1: MapSortedList<'k, 'v>)
                      (m2: MapSortedList<'k, 'v>): MapSortedList<'k, 'v> =
        failwith "TODO"

    // e)
    let rec delete<'k, 'v when 'k: comparison> (key: 'k) (m: MapSortedList<'k, 'v>): MapSortedList<'k, 'v> =
        failwith "TODO"

