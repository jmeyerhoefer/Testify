module Student.RingBuffer


open Types.RingBufferTypes


////a)
let create<'a> (capacity: Int): RingBuffer<'a> =
    failwith "TODO"

////b)
let get<'a> (r: RingBuffer<'a>): 'a =
    r.buffer[0]

////c)
let put<'a> (r: RingBuffer<'a>) (elem: 'a): Unit =
    failwith "TODO"

////end)
