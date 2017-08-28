// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open BackPanel
open BackPanel.Document
open System
open System.Diagnostics

type Model = {
    Switch1 : bool
    Switch2 : bool
}

type Commands = 
    | ChangeSwitchOne
    | ChangeSwitchTwo

let update (model: Model) = function
    | ChangeSwitchOne 
        -> { model with Switch1 = not model.Switch1 }
    | ChangeSwitchTwo
        -> { model with Switch2 = not model.Switch2 }

let render (model: Model) =

    section [] [!!"BackPanel Example"] [
        section [] [!!"Status"] []

        section [] [!!"Settings"] [
            row [] [
                column [] [
                    checkbox [] [!!"Setting 1"] model.Switch1 ChangeSwitchOne
                    checkbox [] [!!"Setting 2"] model.Switch2 ChangeSwitchTwo
                ]
                column [] [
                    checkbox [] [!!"Setting 1"] model.Switch1 ChangeSwitchOne
                    checkbox [] [!!"Setting 2"] model.Switch2 ChangeSwitchTwo
                ]
            ]
        ]
    ]


[<EntryPoint>]
let main argv = 

    let model = {
        Switch1 = false
        Switch2 = true
    }

    let configuration = { 
            BackPanel.defaultConfiguration with
                Title = "BackPanel Example"
                Document = render
        }

    let panel =
        BackPanel.startLocallyAt (Port 8181) configuration

    Process.Start("http://localhost:8181") |> ignore

    Console.ReadKey true |> ignore

    panel()

    0 // return an integer exit code
