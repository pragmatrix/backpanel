module BackPanel.BackPanel

open System
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

/// Incoming requests.
type Request = 
    | Reset
    | Event of string

type Response = 
    | Update of int * string

module WS = 

    let UTF8 = Encoding.UTF8
    let deserialize<'t> = 
        UTF8.GetString >> JsonConvert.DeserializeObject<'t>
    let serialize<'t> : 't -> byte[] = 
        JsonConvert.SerializeObject >> UTF8.GetBytes
    module Base64 =  
        let toString = Convert.ToBase64String
        let fromString = Convert.FromBase64String

    /// Produce a command invocation from an arbitrary event.
    let flatUIConfiguration : FlatUI.Configuration = {
        CommandToJavaScriptEventHandler = 
            fun command -> 
                let b64 = command |> serialize |> Base64.toString
                sprintf "BackPanel.sendEvent('%s')" b64
    }

    let ws (page: Page<'model, 'event>) (webSocket: WebSocket) (context: HttpContext) = 

        let render state = 
            page.Render state
            |> FlatUI.render flatUIConfiguration
            |> HTML.render

        let update = page.Update

        let send response = 
            serialize response
            |> ByteSegment
            |> fun data -> webSocket.send Opcode.Text data true

        let rec loop state = socket {
            let! msg = webSocket.read()
            match msg with
            | (Opcode.Text, data, true) ->
                let request = deserialize<Request> data
                match request with
                | Reset 
                    -> do! send ^ Update(0, render state)
                | Event eventData ->
                    let event = 
                        eventData
                        |> Base64.fromString
                        |> deserialize<'event>
                    let state = update state event
                    do! send ^ Update(0, render state)
                    return! loop state
                return! loop state
            | (Opcode.Close, _, _) 
                -> do! webSocket.send Opcode.Close (ByteSegment [||]) true
            | _ -> return! loop state
        }

        loop page.Initial

let startLocallyAt (Port port) (configuration: Configuration<'model, 'event>) = 

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
                path "/fonts/lato/lato-regular.woff" >=> Files.file "lato-regular.woff"
                path "/fonts/lato/lato-bold.woff" >=> Files.file "lato-bold.woff"
                path "/fonts/glyphicons/flat-ui-icons-regular.woff" >=> Files.file "flat-ui-icons-regular.woff"
                path "/backpanel.js" >=> page "backpanel.js" arguments
                path ("/" + wsPath) >=> WebSocket.handShake (WS.ws configuration.Page)
                NOT_FOUND "Not found."
            ]
        ]

    let _, server = startWebServerAsync config app
    Async.Start(server, cancellationToken)
    { new IDisposable with
            member this.Dispose() = cancellation.Cancel() }

[<GeneralizableValue>]
let defaultConfiguration<'model, 'event> : Configuration<'model, 'event> = {
    Title = "BackPanel"
    Page = id {
        Initial = Unchecked.defaultof<'model>
        Render = fun _ -> section [] [!!"Please configure a document for this BackPanel"] []
        Update = fun state _ -> state
    }
}

let page initial render update = {
    Initial = initial
    Render = render
    Update = update
}
