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
open System.Threading

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

        use x = BackPanel.startLocallyAt 8888 config
        ()


    [<Fact>]
    let ``exception in update does not crash the server``() =
        let config = 
            { BackPanel.defaultConfiguration with
                StartupMode = StartupMode.Synchronous }

        use x = BackPanel.startLocallyAt 8888 config
        ()


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
