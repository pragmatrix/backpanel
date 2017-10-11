module BackPanel.BackPanel

open System
open System.Threading
open System.Text
open System.IO
open System.Reflection
open System.Collections.Generic
open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Suave.Successful
open Suave.Embedded
open DotLiquid
open BackPanel
open BackPanel.Document
open Newtonsoft.Json

/// Template arguments for DotLiquid (must be public, otherwise DotLiquid won't pick it up).
type TemplateArguments = {
    Title: string
    WebsocketURL: string
} with
    member this.Dictionary = [
        "title", box this.Title
        "websocket_url", box this.WebsocketURL
    ]

/// Incoming requests.
[<RQA>]
type Request = 
    | Reset
    | Event of string

[<RQA>]
type Response = 
    | Update of int * string

module internal WS = 

    let UTF8 = Encoding.UTF8
    let deserialize<'t> = 
        UTF8.GetString >> JsonConvert.DeserializeObject<'t>
    let serialize<'t> : 't -> byte[] = 
        JsonConvert.SerializeObject >> UTF8.GetBytes

    [<Literal>]
    let uniqueString = "{FC6A3BDC-8C16-4F5D-8B6D-71E42FD8CCAA}{ACF0ACDC-0519-4940-9984-77429107125E}"
    let uniqueStringWithQuotes = sprintf "\"%s\"" uniqueString

    let renderEventHandler renderable = 
        let json =
            match renderable with
            | FlatUI.RenderableEvent.Simple command ->
                command |> JsonConvert.SerializeObject
            | FlatUI.RenderableEvent.ParameterizedThis(variable, generator) ->
                let replacement = "this." + variable;
                generator uniqueString
                |> JsonConvert.SerializeObject
                |> fun str -> str.Replace(uniqueStringWithQuotes, replacement)

        sprintf "BackPanel.sendEvent(%s)" json

    /// Produce a command invocation from an arbitrary event.
    let flatUIConfiguration : FlatUI.Configuration = {
        RenderEventHandler = renderEventHandler
    }

    [<RQA>]
    type Msg<'event> = 
        | Reset of WebSocket
        | Update of 'event
        | Close of WebSocket
    
    type Sender<'event> = MailboxProcessor<Msg<'event>>

    // The sender manages state, calls update, renders the views, and sends out 
    // updates to all active web sockets.
    let sender (page: Page<'event,'state>) cancellation = MailboxProcessor.Start(fun inbox ->

        let render state = 
            page.View state
            |> FlatUI.render flatUIConfiguration
            |> HTML.renderJSON

        let update = page.Update

        let sockets = HashSet<WebSocket>()

        let post (socket: WebSocket) opcode data = 
            // ignore socket.send errors and assume that errors are 
            // detected on the read side.
            let job = async {
                try
                    do! socket.send opcode (ByteSegment data) true
                        |> Async.Ignore
                with _ -> ()
            }
            Async.Start(job, cancellation)

        let postAll data =
            sockets
            |> Seq.iter ^ fun socket ->
                post socket Opcode.Text data

        let rec loop state = async {
            let! msg = inbox.Receive()
            let! state = async { 
                try
                    match msg with
                    | Msg.Reset socket ->
                        render state
                        |> fun str -> Response.Update(0, str)
                        |> serialize
                        |> post socket Opcode.Text
                        sockets.Add(socket) |> ignore
                        return Some state
                    | Msg.Close socket ->
                        sockets.Remove(socket) |> ignore
                        post socket Opcode.Close [||]
                        return Some state
                    | Msg.Update event ->
                        let state = update state event
                        render state
                        |> fun str -> Response.Update(0, str)
                        |> serialize
                        |> postAll
                        return Some state
                with e ->
                    // #7
                    return None
            }

            match state with
            | Some state -> return! loop state
            | None -> ()
        }

        loop page.Initial
    , cancellation)

    let ws 
        (sender: Sender<'event>) 
        (webSocket: WebSocket)
        _ = socket {

        let rec receiver() = socket {
            let! msg = webSocket.read()
            match msg with
            | (Opcode.Text, data, true) ->
                let request = deserialize<Request> data
                match request with
                | Request.Reset -> 
                    sender.Post(Msg.Reset webSocket)
                    return! receiver()
                | Request.Event eventData ->
                    let event = 
                        eventData
                        |> JsonConvert.DeserializeObject<'event>
                    sender.Post(Msg.Update event)
                    return! receiver()
            | (Opcode.Close , _, _) 
                -> ()
            | _ -> return! receiver()
        }

        try
            return! receiver()
        finally
            sender.Post(Msg.Close webSocket)
    }    
[<AutoOpen>]
module private Private =

    [<Literal>]
    let wsPath = "ws"

    let assembly = Assembly.GetExecutingAssembly()

    let resourceString name = 
        let stream = assembly.GetManifestResourceStream(name)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()

    module Template =
    
        let render template (arguments: TemplateArguments) =
            let template = Template.Parse template
            arguments.Dictionary 
            |> dict
            |> Hash.FromDictionary 
            |> template.Render

    let resolveMimeTypeFromName name = 
        fun ctx ->
            let ext = Path.GetExtension(name)
            let mimeType = 
                ctx.runtime.mimeTypesMap ext
                |> Option.defaultValue { name = "text/html"; compression = false }
            Writers.setMimeType mimeType.name ctx

    let resource name = 
        resolveMimeTypeFromName name
        >=> sendResource assembly name false

    let resourceTemplate resource arguments : WebPart = 
        let str = resourceString resource
        let rendered = Template.render str arguments
        resolveMimeTypeFromName resource
        >=> OK rendered
            
let startLocallyAt (port:int) (configuration: Configuration<'model, 'event>) = 

    let ip = "127.0.0.1"
    let binding = HttpBinding.createSimple HTTP ip port
    let cancellation = new CancellationTokenSource()
    let cancellationToken = cancellation.Token

    let arguments = {
        Title = configuration.Title
        WebsocketURL = sprintf "ws://%s:%d/%s" ip port wsPath
    }

    let config = 
        { defaultConfig with 
            cancellationToken = cancellationToken
            bindings = [binding] 
            autoGrow = configuration.AutoGrowBuffers
            tcpServerFactory = new BackPanelTcpServerFactory()
        }

    let externalEvents = Async.createEventQueue()

    let sender = WS.sender configuration.Page cancellationToken

    let rec dispatcher() = async {
        let! event = externalEvents.Dequeue()
        sender.Post(WS.Msg.Update event)
        return! dispatcher()
    }

    Async.Start(dispatcher(), cancellationToken)

    let app = 
        choose [
            GET >=> choose [
                path "/" >=> resourceTemplate "index.html" arguments
                path "/bootstrap.min.css" >=> resource "bootstrap.min.css"
                path "/flat-ui.min.css" >=> resource "flat-ui.min.css"
                path "/jquery.min.js" >=> resource "jquery.min.js"
                path "/flat-ui.min.js" >=> resource "flat-ui.min.js"
                path "/fonts/lato/lato-regular.woff" >=> resource "lato-regular.woff"
                path "/fonts/lato/lato-bold.woff" >=> resource "lato-bold.woff"
                path "/fonts/glyphicons/flat-ui-icons-regular.woff" >=> resource "flat-ui-icons-regular.woff"
                path "/picodom.js" >=> resource "picodom.js"
                path "/backpanel.js" >=> resourceTemplate "backpanel.js" arguments
                path ("/" + wsPath) 
                    >=> Writers.setMimeType "application/json"
                    >=> handShake (WS.ws sender)
                
            ]
            configuration.WebPart
            GET >=> choose [
                NOT_FOUND "Not found."
            ]
        ]

    // start the server.

    let awaitTaskAndUnwrapException task = async {
        try return! Async.AwaitTask task
        with 
        | :? AggregateException as a ->
            raise a.InnerException
    }

    match configuration.StartupMode with
    | StartupMode.Synchronous ->
        let listening, server = startWebServerAsync config app
        let server = Async.StartAsTask(server, cancellationToken = config.cancellationToken)
        Async.Choice [
            listening |> Async.map (fun _ -> Some())
            awaitTaskAndUnwrapException server
            |> Async.map Some
        ] 
        |> Async.RunSynchronously
        |> ignore

    | StartupMode.Asynchronous retryAfter ->
        let rec startServer() = async {
            let listening, server = startWebServerAsync config app
            let server = Async.StartAsTask(server, cancellationToken = config.cancellationToken)
            let! retry = async {
                try
                    do! Async.Choice [
                            listening |> Async.map (fun _ -> Some())
                            awaitTaskAndUnwrapException server
                            |> Async.map Some
                        ] 
                        |> Async.Ignore
                    return false
                with _ ->
                    return true
            }
            match retry, retryAfter with
            | true, Some retryAfter ->
                do! Async.Sleep (int retryAfter.TotalMilliseconds)
                return! startServer()
            | _ -> 
                return ()
        }

        Async.Start(startServer(), cancellationToken)
        
    { new Server<'event> with
        member this.Post e = externalEvents.Enqueue e
        member this.Dispose() = cancellation.Cancel()
    }

[<GeneralizableValue>]
let defaultConfiguration<'model, 'event> : Configuration<'model, 'event> = {
    Title = "BackPanel"
    Page = id {
        Initial = Unchecked.defaultof<'model>
        View = fun _ -> section [!!"Please configure a document for this BackPanel"] []
        Update = fun state _ -> state
    }
    StartupMode = StartupMode.Synchronous
    WebPart = WebPart.choose []
    AutoGrowBuffers = false
}

let page initial update view = {
    Initial = initial
    Update = update
    View = view
}
