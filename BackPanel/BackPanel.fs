module BackPanel.BackPanel

open System
open System.Threading
open System.Text
open System.IO
open System.Reflection
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

    let ws (page: Page<'model, 'event>) (webSocket: WebSocket) (context: HttpContext) = 

        let render state = 
            page.View state
            |> FlatUI.render flatUIConfiguration
            |> HTML.renderJSON

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
                        |> JsonConvert.DeserializeObject<'event>
                    let state = update state event
                    do! send ^ Update(0, render state)
                    return! loop state
                return! loop state
            | (Opcode.Close, _, _) 
                -> do! webSocket.send Opcode.Close (ByteSegment [||]) true
            | _ -> return! loop state
        }

        loop page.Initial

    
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
            bindings = [binding] }

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
                    >=> handShake (WS.ws configuration.Page)
                NOT_FOUND "Not found."
            ]
        ]


    // start the server.

    match configuration.StartupMode with
    | StartupMode.Synchronously ->
        let listening, server = startWebServerAsync config app
        Async.Choice [
            listening |> Async.map (fun _ -> Some())
            server |> Async.map Some
        ] 
        |> Async.RunSynchronously
        |> ignore

    | StartupMode.Asynchronously retryAfter ->
        let rec startServer() = async {
            let listening, server = startWebServerAsync config app
            try
                do! Async.Choice [
                        listening |> Async.map (fun _ -> Some())
                        server |> Async.map Some
                    ] 
                    |> Async.Ignore
            with _ ->
                match retryAfter with
                | None -> ()
                | Some retryAfter ->
                    do! Async.Sleep (int retryAfter.TotalMilliseconds)
                    return! startServer()
        }

        Async.Start(startServer(), cancellationToken)
        
    { new IDisposable with
            member this.Dispose() = cancellation.Cancel() }

[<GeneralizableValue>]
let defaultConfiguration<'model, 'event> : Configuration<'model, 'event> = {
    Title = "BackPanel"
    Page = id {
        Initial = Unchecked.defaultof<'model>
        View = fun _ -> section [!!"Please configure a document for this BackPanel"] []
        Update = fun state _ -> state
    }
    StartupMode = StartupMode.Synchronously
}

let page initial view update = {
    Initial = initial
    View = view
    Update = update
}
