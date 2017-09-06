module BackPanel.Async

open System.Threading
open System.Collections.Generic

type EventQueue<'event> = {
    Enqueue: 'event -> unit
    Dequeue: unit -> Async<'event>
}

let createEventQueue<'element>() = 
    let queue = Queue<'element>()
    let notify = new AutoResetEvent(false)

    let enqueue element = 
        lock queue ^ fun () ->
            queue.Enqueue(element)
            if queue.Count = 1 then
                notify.Set() |> ignore

    let dequeue() = 
        let rec loop() = async {
            let r = 
                lock queue ^ fun() ->
                    if queue.Count = 0 then None
                    else Some ^ queue.Dequeue()
            match r with 
            | Some e -> return e
            | None ->
                do! Async.AwaitWaitHandle(notify) |> Async.Ignore
                return! loop()
        }

        loop()

    {
        Enqueue = enqueue
        Dequeue = dequeue
    }
