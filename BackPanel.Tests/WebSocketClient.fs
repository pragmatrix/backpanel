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

/// Connects and sends a reset. Returns the DOM.
let connectAndReset(port: int) = async {
    let ws = new ClientWebSocket()
    let tk = Async.DefaultCancellationToken
    do! awaitu ^ ws.ConnectAsync(Uri(sprintf "ws://127.0.0.1:%d/ws" port), tk)
    let req = Encoding.UTF8.GetBytes "{\"Case\":\"Reset\"}"
    do! awaitu ^ ws.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk)
    let buf = Array.zeroCreate 4096
    let buffer = ArraySegment(buf)
    let! r = await ^ ws.ReceiveAsync(buffer, tk)
    if not r.EndOfMessage then failwith "too lazy to receive more!"
    return Encoding.UTF8.GetString(buf, 0, r.Count)
}

