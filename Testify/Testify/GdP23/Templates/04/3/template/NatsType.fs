namespace GdP23.S04.A3.Template

[<AutoOpen>]
module NatsType =
    open Mini

    type Nats = | Nil | Cons of Nat * Nats
    type NatOption = | SomeNat of Nat | NoNat

