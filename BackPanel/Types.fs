namespace BackPanel

open System
open Suave
open BackPanel.Document

type Page<'model, 'event> = {
    Initial: 'model
    Update: 'model -> 'event -> 'model
    View: 'model -> Document
}

[<RQA>]
type StartupMode =
    /// Wait until the server starts and throw an exception when the server 
    /// can't listen to it's interfaces / ports.
    | Synchronous
    /// Start up the server asynchronously and retry starting up the server 
    /// after a given timeout. 
    /// If the timeout is not set, the server stays offline when it was not 
    /// able to start.
    | Asynchronous of retryAfter: TimeSpan option

type Configuration<'model, 'event> = {
    Title: string
    Page: Page<'model, 'event>
    StartupMode: StartupMode
    /// An additional Suave web part that is inserted after the WebParts the
    /// BackPanel uses and before a 404 NOT FOUND handler.
    WebPart: WebPart
    /// Autogrow Suave Buffers. Default: false.
    AutoGrowBuffers: bool
}

type Server<'event> = 
    inherit IDisposable
    /// Post an event to the server that will trigger an update to the model
    /// and rebuilds the view.
    abstract Post : 'event -> unit