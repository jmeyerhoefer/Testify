namespace GdP23.S09.A2.Template

[<AutoOpen>]
module Types =
    open Mini

    type Karte =
      | Zwei | Drei | Vier | Fuenf | Sechs | Sieben | Acht | Neun // Wert 2-9
      | Zehn | Bube | Dame | Koenig // Wert 10
      | Ass // Wert 11 oder 1

