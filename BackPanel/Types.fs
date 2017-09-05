namespace BackPanel

open System
open BackPanel.Document

type Page<'model, 'event> = {
    Initial: 'model
    View: 'model -> Document
    Update: 'model -> 'event -> 'model
}

[<RQA>]
type StartupMode =
    /// Wait until the server starts and throw an exception when the server 
    /// can't listen to it's interfaces / ports.
    | Synchronously
    /// Start up the server asynchronously and retry starting up the server 
    /// after a given timeout. 
    /// If the timeout is not set, the server stays offline when it was not 
    /// able to start.
    | Asynchronously of retryAfter: TimeSpan option

type Configuration<'model, 'event> = {
    Title: string
    Page: Page<'model, 'event>
    StartupMode: StartupMode
}