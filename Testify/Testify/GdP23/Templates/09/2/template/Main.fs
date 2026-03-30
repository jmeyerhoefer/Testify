namespace GdP23.S09.A2.Template

module Main =
    open Mini
    open Types
    open BlackJack

    let private rng = System.Random()
    let zieheKarte (): Karte =
        let set = [Zwei; Drei; Vier; Fuenf; Sechs; Sieben; Acht; Neun; Zehn; Bube; Dame; Koenig; Ass]
        let i = rng.Next(List.length set)
        set.[i]
    let main argv =
        spiel zieheKarte
        0

