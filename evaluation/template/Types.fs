module Types


type Nats<'a> =
    | Nil
    | Cons of 'a * Nats<'a>