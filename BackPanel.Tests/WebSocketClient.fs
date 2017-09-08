module BackPanel.Tests.WebSocketClient

open BackPanel.Prelude

open System
open System.Text
open System.Threading.Tasks
open System.Net.WebSockets
open FsUnit
open FsUnit.Xunit

[<AutoOpen>]
module private Private =

    let await t = Async.AwaitTask t
    let awaitu (t: Task) = Async.AwaitTask t

let create() = new ClientWebSocket()

let connect (port: int) (ws: ClientWebSocket) = async {
    let tk = Async.DefaultCancellationToken
    do! awaitu ^ ws.ConnectAsync(Uri(sprintf "ws://127.0.0.1:%d/ws" port), tk)
}

let close (ws: ClientWebSocket) = async {
    let tk = Async.DefaultCancellationToken
    do! awaitu ^ ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "END", tk)
}

let send (str: string) (ws: ClientWebSocket) = async {
    let req = Encoding.UTF8.GetBytes str
    let tk = Async.DefaultCancellationToken
    do! awaitu ^ ws.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk)
}

let read (ws: ClientWebSocket) = async {
    let buf = Array.zeroCreate 4096
    let buffer = ArraySegment(buf)
    let tk = Async.DefaultCancellationToken
    let! r = await ^ ws.ReceiveAsync(buffer, tk)
    if not r.EndOfMessage then failwith "too lazy to receive more!"
    return Encoding.UTF8.GetString(buf, 0, r.Count)
}

let reset = send "{\"Case\":\"Reset\"}"

/// Connects and sends a reset. Returns the DOM.
let connectAndReset(port: int) = async {
    let ws = new ClientWebSocket()
    do! connect port ws
    do! reset ws
    return! read ws
}

