module Student.Leibniz


// aus der Vorlesung (Folie~248)
let binarySearch (oracle: Nat -> Bool, lowerBound: Nat, upperBound: Nat): Nat =
    let rec search (l: Nat, u: Nat): Nat =
        if l >= u then u
        else
            let m = (l + u) / 2N
            if oracle m then search (l, m)
            else search (m + 1N, u)
    in search (lowerBound, upperBound)

//a)
let log2 (n: Nat): Nat =
    binarySearch (failwith "TODO", failwith "TODO", failwith "TODO")

//b)
let rec sortedDigits (n: Nat): Bool =
    failwith "TODO"