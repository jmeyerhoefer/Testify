module Solution.Entwurfsmuster


//b)
let rec add (n: Nat): Nat =
    if n = 0N then 0N
    else n + add (n - 1N)

//c)
let rec mod5 (n: Nat): Nat =
    if n = 0N then 0N
    else
        let m = mod5 (n - 1N)
        if m = 4N then 0N
        else m + 1N

//d)
let rec mult42 (n: Nat): Nat =
    if n = 0N then 0N
    else
        let r = mult42 (n / 2N)
        r + r + if n % 2N = 0N then 0N else 42N

//e)
let rec count5 (n: Nat): Nat =
    if n = 0N then 0N
    else
        if n % 10N = 5N then 1N else 0N
        +
        count5(n / 10N)