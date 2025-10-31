module Types.RingBufferTypes


type RingBuffer<'a> =
    {
        buffer: Array<'a>
        size: Ref<Int>
        readPos: Ref<Int>
    }

exception BufferEmpty
exception BufferFull
