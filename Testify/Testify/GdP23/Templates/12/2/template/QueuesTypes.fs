namespace GdP23.S12.A2.Template

module QueuesTypes =

    open Mini

    ////iqueue)
    type IQueue<'a> =
        interface
            abstract member isEmpty: Unit -> Bool
            abstract member Add: 'a -> Unit
            abstract member Remove: Unit -> Option<'a>
        end

    ////qelem)
    type QElem<'a> =       
        { priority: Nat    
          value: 'a }      

    ////tree)
    type Tree<'a> =
        | Empty
        | Node of Tree<'a> * 'a * Tree<'a>

    ////end)

