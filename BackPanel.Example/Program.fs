open System
open System.Diagnostics
open BackPanel
open BackPanel.Document

type Model = {
    Switch1 : bool
    Switch2 : bool
}

type Event = 
    | SwitchedOne
    | SwitchedTwo

let model = {
    Switch1 = false
    Switch2 = true
}

let render (model: Model) =

    section [] [!!"BackPanel Example"] [
        section [] [!!"Status"] []

        section [] [!!"Settings"] [
            row [] [
                column [] [
                    checkbox [] [!!"Setting 1"] model.Switch1 SwitchedOne
                    checkbox [] [!!"Setting 2"] model.Switch2 SwitchedTwo
                ]
                column [] [
                    checkbox [] [!!"Setting 1"] model.Switch1 SwitchedOne
                    checkbox [] [!!"Setting 2"] model.Switch2 SwitchedTwo
                ]
            ]
        ]
    ]

let update (model: Model) = function
    | SwitchedOne 
        -> { model with Switch1 = not model.Switch1 }
    | SwitchedTwo
        -> { model with Switch2 = not model.Switch2 }

[<EntryPoint>]
let main argv = 

    let configuration = { 
        BackPanel.defaultConfiguration() with
            Title = "BackPanel Example"
            Page = BackPanel.page model render update
    }

    use panel = BackPanel.startLocallyAt (Port 8181) configuration

    Process.Start("http://localhost:8181") |> ignore

    Console.ReadKey true |> ignore

    0
