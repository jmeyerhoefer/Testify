module Solution.Weihnachtsbaum


open Types.WeihnachtsbaumTypes


////a)
let schmuckGewicht (schmuck: Schmuck): Nat =
    match schmuck with
    | Kugel   -> 2N
    | Lametta -> 1N

////b)
let rec baumGewicht (tree: Weihnachtsbaum): Nat =
    match tree with
    | Leaf            -> 0N
    | ENode (l,    r) -> baumGewicht l +                    baumGewicht r
    | Node  (l, s, r) -> baumGewicht l + schmuckGewicht s + baumGewicht r

////c)
let rec istBalanciert (tree: Weihnachtsbaum): Bool =
    match tree with
    | Leaf -> true
    | ENode (l, r) | Node  (l, _, r) ->
        baumGewicht l = baumGewicht r && istBalanciert l && istBalanciert r

// Effizientere Lösung, jeder Knoten wird nur einmal betrachtet
let istBalanciert' (tree: Weihnachtsbaum): Bool =
    // Rückgabe enthält auch das Gewicht, wenn der Baum balanciert ist
    let rec balanciertMitGewicht (tree: Weihnachtsbaum): Option<Nat> =
        match tree with
        | Leaf -> Some 0N
        | ENode (l, r) ->
            match (balanciertMitGewicht l, balanciertMitGewicht r) with
            | (Some gl, Some gr) when gl = gr -> Some (gl + gr)
            | _                               -> None
        | Node (l, s, r) ->
            match (balanciertMitGewicht l, balanciertMitGewicht r) with
            | (Some gl, Some gr) when gl = gr -> Some (gl + schmuckGewicht s + gr)
            | _                               -> None

    Option.isSome (balanciertMitGewicht tree)


////d)
let rec moeglicheGewichte<'a> (tree: Tree<'a>): List<Nat> =
    // Hilfsfunktion zur Berechnung der Schnittmenge von zwei Listen
    let intersect (xs: List<Nat>) (ys: List<Nat>): List<Nat> =
        xs |> List.filter (fun x -> List.contains x ys)

    match tree with
    | Leaf           -> [0N]
    | ENode (l,   r) -> intersect (moeglicheGewichte l) (moeglicheGewichte r)
                        |> List.map (fun g ->
                            g + g // Gesamtgewicht: links + rechts (beides g)
                        )
    | Node (l, _, r) -> intersect (moeglicheGewichte l) (moeglicheGewichte r)
                        |> List.collect (fun g -> [
                            g + 1N + g; // Lametta hinzufügen erhöht Gewicht um 1
                            g + 2N + g  // Kugel   hinzufügen erhöht Gewicht um 2
                        ])
                        |> List.distinct // Optional: Duplikate entfernen

////e)
let rec schmuecken (tree: Tree<Unit>) (g: Nat): Option<Weihnachtsbaum> =
    match tree with
    | Leaf when g = 0N -> Some Leaf

    | ENode (l, r) when g % 2N = 0N -> // Nur möglich bei geradem Zielgewicht
        let g' = g / 2N // Zielgewicht hälftig auf beide Teilbäume verteilen

        // Rekursiv beide Teilbäume schmücken und dann mit ENode zusammensetzen
        match (schmuecken l g', schmuecken r g') with
        | (Some l', Some r') -> Some (ENode (l', r'))
        | _                  -> None

    | Node (l, _, r) when g > 0N -> // Nur möglich bei positivem Zielgewicht
        // Bestimme Schmuck im Node und Zielgewicht der beiden Teilbäume
        let (s, g') = if g % 2N = 0N then (Kugel,   (g - 2N) / 2N)
                                     else (Lametta, (g - 1N) / 2N)

        // Rekursiv beide Teilbäume schmücken und dann mit Node zusammensetzen
        match (schmuecken l g', schmuecken r g') with
        | (Some l', Some r') -> Some (Node (l', s, r'))
        | _                  -> None

    | _ -> None // falls die "when" Bedingungen nicht erfüllt sind

////f)
let schmueckungen (tree: Tree<Unit>): List<Weihnachtsbaum> =
    tree |> moeglicheGewichte |> List.choose (schmuecken tree)

////f2)
let rec schmueckungenMitGewicht (tree: Tree<'a>): List<Nat * Weihnachtsbaum> =
  // Hilfsfunktion: Finde in zwei Listen (mögliche linke und rechte Teilbäume)
  // die Baum-Paare mit gleichem Schmuck-Gewicht. Rückgabe-Liste enthält Tupel
  // der Form (Gesamtgewicht, linker Teilbaum, rechter Teilbaum).
  let findePaare
    (ls: List<Nat * Weihnachtsbaum>)
    (rs: List<Nat * Weihnachtsbaum>): List<Nat * Weihnachtsbaum * Weihnachtsbaum> =
      // Kreuzprodukt von ls und rs bilden
      ls |> List.collect (fun l -> rs |> List.map (fun r -> (l, r)))
      // Filter: Nur Paare mit gleichem Schmuck-Gewicht
      |> List.filter (fun ((gl, _), (gr, _)) -> gl = gr)
      // Umformung: Schmuck-Gewichte heraus ziehen und addieren
      |> List.map (fun ((gl, l), (gr, r)) -> (gl + gr, l, r))

  match tree with
  | Leaf           -> [(0N, Leaf)]
  | ENode (l,   r) -> findePaare (schmueckungenMitGewicht l) (schmueckungenMitGewicht r)
                      |> List.map (fun (g, l', r') ->
                          // Geschmückte Teilbäume wieder mit ENode zusammensetzen
                          (g, ENode (l', r'))
                      )
  | Node (l, _, r) -> findePaare (schmueckungenMitGewicht l) (schmueckungenMitGewicht r)
                      |> List.collect (fun (g, l', r') -> [
                          // Geschmückte Teilbäume wieder mit Node zusammensetzen
                          (g + 1N, Node (l', Lametta, r')); // Lametta im Node wiegt 1
                          (g + 2N, Node (l', Kugel,   r'))  // Kugel   im Node wiegt 2
                      ])

////f3)
let moeglicheGewichte'<'a> (tree: Tree<'a>): List<Nat> =
    schmueckungenMitGewicht tree
    |> List.map fst // Gewicht extrahieren

let schmuecken' (tree: Tree<Unit>) (g: Nat): Option<Weihnachtsbaum> =
    schmueckungenMitGewicht tree
    |> List.tryFind (fun (g', _) -> g' = g) // Nach Zielgewicht filtern
    |> Option.map snd // Geschmückten Baum extrahieren

let schmueckungen' (tree: Tree<Unit>): List<Weihnachtsbaum> =
    schmueckungenMitGewicht tree
    |> List.map snd // Geschmückten Baum extrahieren
