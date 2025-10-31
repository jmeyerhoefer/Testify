module Types.NatsTypes


type Nats = | Nil | Cons of Nat * Nats
type NatOption = | SomeNat of Nat | NoNat