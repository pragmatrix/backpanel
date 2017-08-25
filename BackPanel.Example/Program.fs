// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open BackPanel
open BackPanel.Document
open System
open System.Diagnostics

type Model = {
    SwitchSetting1 : bool
    SwitchSetting2 : bool
    SwitchSetting3 : bool
}

let render (model: Model) =

    section [title "BackPanel Example"] [
        section [title "Status"] []

        section [title "Settings"] [
            row [] [
                column [] [
                    checkbox [bind <@ model.SwitchSetting1 @>] [!!"Setting 1"]
                    checkbox [bind <@ model.SwitchSetting2 @>] [!!"Setting 2"]
                    checkbox [bind <@ model.SwitchSetting3 @>] [!!"Setting 3"]
                ]
                column [] [
                    checkbox [bind <@ model.SwitchSetting1 @>] [!!"Setting 1"]
                    checkbox [bind <@ model.SwitchSetting2 @>] [!!"Setting 2"]
                    checkbox [bind <@ model.SwitchSetting3 @>] [!!"Setting 3"]
                ]
            ]
        ]
    ]


[<EntryPoint>]
let main argv = 

    let model = {
        SwitchSetting1 = false
        SwitchSetting2 = true
        SwitchSetting3 = false
    }

    let configuration = { 
        Title = "BackPanel Example"
        Description = "BackPanel Example, a feature demo"
    }

    let panel = BackPanel.startLocallyAt (Port 8181) configuration

    Process.Start("http://localhost:8181") |> ignore

    Console.ReadKey true |> ignore

    panel()

    0 // return an integer exit code
