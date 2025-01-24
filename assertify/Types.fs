module Types


type RegExp<'a> =
    | Eps
    | Lit of 'a
    | Cat of RegExp<'a> * RegExp<'a>
    | Empty
    | Or of RegExp<'a> * RegExp<'a>
    | Star of RegExp<'a>


type RingBuffer<'a> =
    {
        buffer: 'a array
        size: int ref
        readPos: int ref
    }

exception BufferEmpty
exception BufferFull