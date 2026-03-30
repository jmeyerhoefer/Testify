namespace GdP23.S06.A1.Template

module Lists =
    open Mini

    // a)
    let rec map<'a, 'b> (f: 'a -> 'b) (xs: List<'a>): List<'b> =
        failwith "TODO"

    // Anwendungen
    let double (xs: List<Nat>): List<Nat> =
        failwith "TODO"

    let firstComponents<'a, 'b> (xs: List<'a * 'b>): List<'a> =
        failwith "TODO"

    // b)
    let rec collect (f: 'a -> List<'b>) (xs: List<'a>): List<'b> =
        failwith "TODO"

    // Anwendung
    let cloneElements (xs: List<'a>): List<'a> =
        failwith "TODO"

