module BackPanel.BackPanel

open System.Threading
open System.Text
open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.DotLiquid
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open DotLiquid
open BackPanel.Document
open Newtonsoft.Json

/// Template arguments for DotLiquid (must be public, otherwise DotLiquid won't pick it up).
type TemplateArguments = {
    Title: string
    WebsocketURL: string
}

let defaultConfiguration = {
    Title = "BackPanel"
    Document = fun _ -> section [] [!!"Please configure a document for this BackPanel"] []
}

/// Incoming requests.
type Request = 
    | Reset
    | Command of string

module WS = 

    let UTF8 = Encoding.UTF8
    let deserialize<'t> = UTF8.GetString >> JsonConvert.DeserializeObject<'t>

    let rec ws (webSocket: WebSocket) (context: HttpContext) = socket {
        let again = ws webSocket context
        let! msg = webSocket.read()
        match msg with
        | (Opcode.Text, data, true) ->
            let request = deserialize<Request> data
            match request with
            | Reset -> printfn "Reset"
            | Command str -> printfn "Command: %s" str
            return! again
        | (Opcode.Close, _, _) ->
            do! webSocket.send Opcode.Close (ByteSegment [||]) true
        | _ -> 
            return! again
    }

let startLocallyAt (Port port) (configuration: Configuration<'model>) = 

    let ip = "127.0.0.1"
    let binding = HttpBinding.createSimple HTTP ip port
    let cancellation = new CancellationTokenSource()
    let cancellationToken = cancellation.Token

    let wsPath = "ws"

    let arguments = {
        Title = configuration.Title
        WebsocketURL = sprintf "ws://%s:%d/%s" ip port wsPath
    }

    let config = 
        { defaultConfig with 
            cancellationToken = cancellationToken
            bindings = [binding] }

    let app = 
        choose [
            GET >=> choose [
                path "/" >=> page "index.html" arguments
                path "/bootstrap.min.css" >=> Files.file "bootstrap.min.css"
                path "/flat-ui.min.css" >=> Files.file "flat-ui.min.css"
                path "/jquery.min.js" >=> Files.file "jquery.min.js"
                path "/flat-ui.min.js" >=> Files.file "flat-ui.min.js"
                path "/backpanel.js" >=> page "backpanel.js" arguments
                path ("/" + wsPath) >=> WebSocket.handShake WS.ws
                NOT_FOUND "Not found."
            ]
        ]

    let _, server = startWebServerAsync config app
    Async.Start(server, cancellationToken)
    fun () ->
        cancellation.Cancel()