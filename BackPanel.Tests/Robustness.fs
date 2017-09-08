module BackPanel.Tests.Robustness

open BackPanel.Prelude

open Xunit
open FsUnit.Xunit
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Web
open BackPanel
open System
open System.Net
open System.Net.Sockets

module Startup = 


    [<Fact>]
    let ``default startup mode is synchronous, and may immediately fail``() =
    
        let config = BackPanel.defaultConfiguration

        use listener = new HttpListener()
        listener.Prefixes.Add("http://127.0.0.1:8888/")
        listener.Start()

        fun () -> BackPanel.startLocallyAt 8888 config |> ignore
        |> should throw typeof<SocketException>

    [<Fact>]
    let ``asynchonrous startup mode never fails``() =

        let config = 
            { BackPanel.defaultConfiguration with
                StartupMode = StartupMode.Asynchronous (Some <| TimeSpan.FromSeconds(1.0)) }

        use listener = new HttpListener()
        listener.Prefixes.Add("http://127.0.0.1:8888/")
        listener.Start()

        use __ = BackPanel.startLocallyAt 8888 config
        ()

module ErrorHandling =

    [<Fact>]
    let ``encoding error closes socket and supports a reconnect``() = 
        let config = BackPanel.defaultConfiguration
        use __ = BackPanel.startLocallyAt 8888 config
        Async.RunSynchronously ^ async {
            do! async {
                use ws = WebSocketClient.create()
                do! WebSocketClient.connect 8888 ws
                do! WebSocketClient.send "invalid JSON!!!" ws
                let! str = WebSocketClient.read ws
                str |> should equal "" // < closed
            }
            let! dom = WebSocketClient.connectAndReset 8888
            dom.Contains("Please configure a document") 
            |> should equal true
        }
    
    [<Fact>]
    let ``client websocket gets disconnected after reset and can reconnect after``() = 
        let config = BackPanel.defaultConfiguration
        use __ = BackPanel.startLocallyAt 8888 config
        Async.RunSynchronously ^ async {
            do! async {
                use ws = WebSocketClient.create()
                do! WebSocketClient.connect 8888 ws
                do! WebSocketClient.reset ws
                do! WebSocketClient.close ws
            }
            let! dom = WebSocketClient.connectAndReset 8888
            dom.Contains("Please configure a document") 
            |> should equal true
        }

            
#if false
    // just recognized that i've no idea what to do when update fails.

    [<Fact>]
    let ``one time exception in update gets ignored``() =
        let config = 
            let page = 
                BackPanel.page 
                    false 
                    (fun _ _ -> failwith "holy shit") 
                    (fun _ -> para[!!"nothing there to see"])

            { BackPanel.defaultConfiguration with
                Page = page
            }

        use x = BackPanel.startLocallyAt 8888 config
        x.Post(true)
        ignore ^ Async.RunSynchronously ^ WebSocketClient.connectAndReset 8888
#endif

module Suave =

    let ip = "127.0.0.1"
    let binding = HttpBinding.createSimple HTTP ip 8888

    let config = 
        { defaultConfig with 
            // cancellationToken = cancellationToken
            bindings = [binding] }

    let app = 
        choose [
            GET >=> choose [
                path"/" >=> OK "Hello World"
            ]
        ]

    [<Fact>]
    let ``Catching a Suave listener error using Async.Choice``() = 

        use listener = new HttpListener()
        listener.Prefixes.Add("http://localhost:8888/")
        listener.Start()

        let listening, server = startWebServerAsync config app
    
        fun () ->
            Async.Choice [ 
                server 
                |> Async.map (fun () -> None)
                listening 
                |> Async.map Some
            ] 
            |> Async.RunSynchronously
            |> ignore
        |> should throw typeof<SocketException>

    [<Fact>]
    let ``Choice pattern works when then listener gets online``() =

        let listening, server = startWebServerAsync config app
    
        fun () ->
            Async.Choice [ 
                server 
                |> Async.map (fun () -> None)
                listening 
                |> Async.map Some
            ] 
            |> Async.RunSynchronously
            |> ignore
