module Solution.Program


////b)
let rec queryNat (msg: String): Nat =
    putstring msg
    let s = getline ()
    if s <> "" && String.forall Char.IsDigit s then
         readNat s
    else putline "Eingabe ist keine natuerliche Zahl!"
         queryNat msg

////c)
let main (): Unit =
    putline "Bitte geben Sie drei natuerliche Zahlen ein."
    let n1 = queryNat "Erste Zahl: "
    let n2 = queryNat "Zweite Zahl: "
    let n3 = queryNat "Dritte Zahl: "
    let min3 = min n1 (min n2 n3)
    putline ("Minimum: " + (string min3))
////end)
