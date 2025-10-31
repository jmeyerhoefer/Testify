module Solution.RingBuffer


open Types.RingBufferTypes


////a)
let create<'a> (capacity: Int): RingBuffer<'a> =
    { buffer = Array.zeroCreate<'a> capacity; size=ref 0; readPos=ref 0 }

////b)
let get<'a> (r: RingBuffer<'a>): 'a =
    if !r.size > 0 then
        let capacity = r.buffer.Length
        let readPos = !r.readPos
        r.readPos := (readPos + 1) % capacity
        r.size := !r.size - 1
        r.buffer.[readPos]
    else
        raise BufferEmpty

////c)
let put<'a> (r: RingBuffer<'a>) (elem: 'a): Unit =
    if !r.size < r.buffer.Length then
        let capacity = r.buffer.Length
        let writePos = (!r.readPos + !r.size) % capacity
        r.buffer.[writePos] <- elem
        r.size := !r.size + 1
    else
        raise BufferFull

////end)
