module BackPanel.BackPanel

open System.Threading
open Suave
open Suave.Filters
open Suave.Operators
open Suave.DotLiquid
open DotLiquid


type private TemplateArguments = {
    Title: string
    Description: string
    WebsocketURL: string
}


let startLocallyAt (Port port) (configuration: Configuration) = 
    DotLiquid.setRubyNamingConvention()
    DotLiquid.setTemplatesDir "."

    let ip = "127.0.0.1"
    let binding = HttpBinding.createSimple HTTP ip port
    let cancellation = new CancellationTokenSource()
    let cancellationToken = cancellation.Token

    let arguments = {
        Title = configuration.Title
        Description = configuration.Description
        WebsocketURL = sprintf "ws://%s:%d" ip port
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
                path "/backpanel.js" >=> Files.file "backpanel.js"
            ]
        ]

    let _, server = startWebServerAsync config app
    Async.Start(server, cancellationToken)
    fun () ->
        cancellation.Cancel()