module Student.Lists2


// Beispiel vom Übungsblatt
let ex = [2N; 4N; 3N; 4N; 2N; 1N]

// a)
let rec minAndMax<'a when 'a: comparison>(xs: List<'a>): Option<'a * 'a> =
    failwith "TODO"

// b)
let rec map<'a, 'b> (f: 'a -> 'b) (xs: List<'a>): List<'b> =
    failwith "TODO"

// c)
let rec duplicate<'a> (xs: List<'a>): List<'a> =
    failwith "TODO"

// d)
let rec collect<'a, 'b> (f: 'a -> List<'b>) (xs: List<'a>): List<'b> =
    failwith "TODO"
    // List.collect f (xs @ xs)

// e)
let rec intersperse<'a> (sep: 'a) (xs: List<'a>): List<'a> =
    failwith "TODO"

// f)
let rec runs<'a when 'a: comparison> (xs: List<'a>): List<List<'a>> =
    failwith "TODO"

