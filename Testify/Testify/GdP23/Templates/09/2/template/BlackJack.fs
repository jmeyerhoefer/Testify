namespace GdP23.S09.A2.Template

module BlackJack =
    open Mini
    open Types

    // Um Fehler beim Abtippen der Strings zu vermeiden, stellen wir diese hier bereit.
    // Sie können die Bezeichner in Ihrem Code verwenden oder einfach die Strings
    // kopieren und an den entsprechenden Stellen in Ihren Code einfügen.
    // Der Teil im Namen der Bezeichner vor dem Unterstrich "_" zeigt Ihnen in welcher
    // Funktion der String verwendet wird bzw. werden soll.

    let zugSpieler_weitereKarte = "Moechten Sie eine weitere Karte ziehen? [j/n] "
    let zugSpieler_ungueltigeEingabe = "Ungueltige Eingabe."
    let zuegeCroupier_karten = "Karten des Croupiers: "
    let zuegeSpieler_teil1 = "Sie haben "
    let zuegeSpieler_teil2 = " gezogen, damit haben Sie folgende Karten: "
    let spiel_begruessung = "Lista Black Jack\n================"
    let spiel_kartenCroupier = "Karten des Croupiers: "
    let spiel_kartenSpieler = "Ihre Karten: "
    let spiel_ueberkauft1 = "Sie haben sich ueberkauft ("
    let spiel_ueberkauft2 = " Punkte)."
    let spiel_endeCroupierUeberkauft = "Der Croupier hat sich ueberkauft, Sie gewinnen."
    let spiel_endeSpielerGewinnt = "Sie haben gewonnen."
    let spiel_endeSpielerVerliert = "Sie haben verloren."
    let spiel_endeUnentschieden = "Das Spiel endet unentschieden."


    // a)
    let kartenwert (k: Karte): List<Nat> =
        failwith "TODO"

    // b)
    let rec kartenPunkte (karten: List<Karte>): List<Nat> =
        failwith "TODO"

    // c)
    let punkteBerechnen (karten: List<Karte>): Nat =
        failwith "TODO"

    // d)
    let zugCroupier (zieheKarte: Unit -> Karte) (karten: List<Karte>): Option<Karte> =
        failwith "TODO"

    // e)
    let zugSpieler (zieheKarte: Unit -> Karte) (karten: List<Karte>): Option<Karte> =
        failwith "TODO"

    // f)
    let rec zuegeCroupier (zieheKarte: Unit -> Karte) (karten: List<Karte>): Nat =
        failwith "TODO"

    // g)
    let rec zuegeSpieler (zieheKarte: Unit -> Karte) (karten: List<Karte>): Nat =
        failwith "TODO"

    // h)
    let spiel (zieheKarte: Unit -> Karte): Unit =
        // Startkonfiguration herstellen
        putline spiel_begruessung
        // Reihenfolge für Tests wichtig
        let kartenSpieler0 = [zieheKarte ()]
        let kartenCroupier = [zieheKarte ()]
        let kartenSpieler = (zieheKarte ()) :: kartenSpieler0
        putline (spiel_kartenCroupier + (show kartenCroupier))
        putline (spiel_kartenSpieler + (show kartenSpieler))

        // Mensch zieht
        let p1 =
            if punkteBerechnen kartenSpieler = 21N then
                punkteBerechnen kartenSpieler
            else
                zuegeSpieler zieheKarte kartenSpieler
        if p1 > 21N then
            putline (spiel_ueberkauft1 + (show p1) + spiel_ueberkauft2)
        else
            // Computer zieht
            let p2 = zuegeCroupier zieheKarte kartenCroupier
            let msg =
                if   p2 > 21N then spiel_endeCroupierUeberkauft
                elif p1 > p2  then spiel_endeSpielerGewinnt
                elif p1 < p2  then spiel_endeSpielerVerliert
                else spiel_endeUnentschieden
            putline msg

