module BackPanel.Tests.Features

open System
open System.Net
open System.Text
open FsUnit.Xunit
open Xunit
open BackPanel
open BackPanel.Document
open System.Net.WebSockets
open System.Threading.Tasks

module Server =

    [<Fact>]
    let ``server actually runs after startup``() = 
        let config = BackPanel.defaultConfiguration
        use panel = BackPanel.startLocallyAt 8888 config
        use client = new WebClient()
        client.DownloadString("http://127.0.0.1:8888/")
            .Contains("<!DOCTYPE")
        |> should be True

    [<Fact>]
    let ``server runs after asynchronous startup``() =
        let config = {
            BackPanel.defaultConfiguration with
                StartupMode = StartupMode.Asynchronous None }
        use panel = BackPanel.startLocallyAt 8888 config
        use client = new WebClient()
        client.DownloadString("http://127.0.0.1:8888/")
            .Contains("<!DOCTYPE")
        |> should be True

module Post = 

    [<Fact>]
    let ``post``() = 
        
        let initial = false
        let update _ event = event
        let view m = section [!!(sprintf "Model: %A" m)] []
        let config = {
            BackPanel.defaultConfiguration with
                Page = BackPanel.page initial update view
            }

        use panel = BackPanel.startLocallyAt 8888 config
        panel.Post(true)

        let await t = Async.AwaitTask t
        let awaitu (t: Task) = Async.AwaitTask t

        Async.RunSynchronously ^ async {
            let ws = new ClientWebSocket()
            let tk = Async.DefaultCancellationToken
            do! awaitu ^ ws.ConnectAsync(Uri("ws://127.0.0.1:8888/ws"), tk)
            let req = Encoding.UTF8.GetBytes "{\"Case\":\"Reset\"}"
            do! awaitu ^ ws.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk)
            let buf = Array.zeroCreate 4096
            let buffer = ArraySegment(buf)
            let! r = await ^ ws.ReceiveAsync(buffer, tk)
            if not r.EndOfMessage then failwith "too lazy to receive more!"
            let str = Encoding.UTF8.GetString(buf, 0, r.Count)
            str.Contains("Model: true")
            |> should equal true
        }

