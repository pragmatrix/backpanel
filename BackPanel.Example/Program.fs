﻿open System
open System.Diagnostics
open BackPanel
open BackPanel.Document

type Model = {
    Switch1 : bool
    Switch2 : bool
    Text : string
}

type Event = 
    | SwitchedOne
    | SwitchedTwo
    | InputChanged of string

let model = {
    Switch1 = false
    Switch2 = true
    Text = ""
}

let update (model: Model) = function
    | SwitchedOne -> { model with Switch1 = not model.Switch1 }
    | SwitchedTwo -> { model with Switch2 = not model.Switch2 }
    | InputChanged str -> { model with Text = str }

let view (model: Model) =

    section [!!"BackPanel Example"] [
        section [!!"Status"] []

        section [!!"Settings"] [
            row [
                column [
                    checkbox [!!"Setting 1"] model.Switch1 SwitchedOne
                    checkbox [!!"Setting 2"] model.Switch2 SwitchedTwo
                    button Primary [!!"Toggle Setting 2"] SwitchedTwo
                ]
                column [
                    checkbox [!!"Setting 1"] model.Switch1 SwitchedOne
                    checkbox [!!"Setting 2"] model.Switch2 SwitchedTwo
                    
                ]
            ]
            row [
                column [
                    input InputType.Enabled "Enter Text" model.Text InputChanged
                    para [!!model.Text]
                ]
            ]
        ]
        section [!!"Tables"] [
            section [!!"With Header"] [
                table [[!!"Firstname"]; [!!"Lastname"]; [!!"Email"]] [
                    [[!!"John"]; [!!"Doe"]; [!!"john@example.com"] ]
                    [[!!"May"]; [!!"Moe"]; [!!"mary@example.com" ] ]
                    [[!!"July"]; [!!"Dooley"]; [!!"july@example.com"] ]
                ]
            ]
            section [!!"Without Header"] [
                table [] [
                    [[!!"John"]; [!!"Doe"]; [!!"john@example.com"] ]
                    [[!!"May"]; [!!"Moe"]; [!!"mary@example.com" ] ]
                    [[!!"July"]; [!!"Dooley"]; [!!"july@example.com"] ]
                ]
            ]

        ]
    ]

[<EntryPoint>]
let main argv = 

    let configuration = { 
        BackPanel.defaultConfiguration with
            Title = "BackPanel Example"
            Page = BackPanel.page model update view
    }

    use panel = BackPanel.startLocallyAt 8181 configuration

    Process.Start("http://localhost:8181") |> ignore

    Console.ReadKey true |> ignore

    0
