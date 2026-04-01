module Zahlen
open Mini

// Wir verwenden hier den Ausdruck failwith "TODO" als Platzhalter
// für Ihren Code. Dieser Ausdruck bewirkt, dass jeder Aufruf der
// Funktion mit der Fehlermeldung TODO fehlschlägt.
// So können wir Ihnen eine Vorlage bereitstellen, die vom Compiler
// akzeptiert wird. Solange der Platzhalter nicht ersetzt wurde,
// liefern die Testfälle für die jeweilige Funktion daher die
// Fehlermeldung TODO.

////a)
let avg3 (a: Nat) (b: Nat) (c: Nat): Nat =
    (a + b + c)/3N

////b)
let min3 (a: Nat) (b: Nat) (c: Nat) =
    min (min a b) c

////c)
let ceil10 (a: Nat): Nat =
    if a % 10N = 0N then a else a + (10N - (a % 10N))

////end)
