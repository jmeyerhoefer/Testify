namespace GdP23.S12.A2.Template

module Queues =

    open Mini
    open QueuesTypes


    // Der auf dem Übungsblatt abgebildete Baum zum Testen
    let ex = Node (Node(Node(Empty,1N,Empty),2N,Node(Empty,3N,Empty)) , 4N, Node(Node(Empty,5N,Empty),6N,Node(Empty,7N,Empty)))

    ////a)
    let simpleQueue<'a> (): IQueue<'a> =
        failwith "TODO"

    ////b)
    let priorityQueue<'a> (): IQueue<QElem<'a>> =
        failwith "TODO"

    ////c)
    let advancedQueue<'a> (): IQueue<'a> =
        failwith "TODO"

    ////d)
    let rec enqueue (q: IQueue<'a>) (elems: List<'a>): Unit =
        failwith "TODO"

    let rec dequeue (q: IQueue<'a>): List<'a> =
        failwith "TODO"

    ////e)
    let rec bft (q: IQueue<Tree<'a>>): List<'a>  =
        failwith "TODO"

    ////end)


