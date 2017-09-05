[<AutoOpen>]
module internal BackPanel.Prelude

let (^) = (<|)

type RQA = RequireQualifiedAccessAttribute


module Async =

    let inline unit r = async.Return r
    let inline bind f computation = async.Bind(computation, f)
    let map f = bind (f >> unit)
    let inline mapOption (computation: 'a -> Async<'b>) =
        function
        | None -> 
            async.Return None
        | Some value -> 
            computation value 
            |> map Some
