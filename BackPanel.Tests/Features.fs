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

        let str = Async.RunSynchronously ^ WebSocketClient.connectAndReset 8888

        str.Contains("Model: true")
        |> should equal true
