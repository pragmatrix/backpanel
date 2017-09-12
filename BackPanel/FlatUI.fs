/// FlatUI / Bootstrap HTML renderer
module BackPanel.FlatUI

open BackPanel.Document
open BackPanel.HTML

[<RQA>]
type RenderableEvent = 
    | Simple of obj
    | ParameterizedThis of variableName: string * generator: (string -> obj)

type Configuration = {
    RenderEventHandler: RenderableEvent -> string
}

let render (configuration: Configuration) (document: Document) : Content =

    let renderEventHandler = configuration.RenderEventHandler
    let renderCommand = RenderableEvent.Simple >> renderEventHandler

    let renderInlineFragment = function
        | InlineFragment.Text text -> Text text

    let renderInline = 
        List.map renderInlineFragment

    let rowColumnsTo12GridWidth columnCount = 
        match columnCount with
        | 1 -> 12
        | 2 -> 6
        | 3 -> 4
        | 4 -> 3
        | 6 -> 2
        | 12 -> 1
        | _ -> failwithf "%d columns are not supported" columnCount

    let rec renderBox level = function
        | Section(title, documents) ->
            let title = renderInline title
            let documents = 
                documents 
                |> List.map ^ renderBox (level+1)
            div [] (h (level+1) [] title :: documents)
        | Row(columns) ->
            let columnWidth = rowColumnsTo12GridWidth columns.Length
            let columnClass = sprintf "col-md-%d" columnWidth
            columns 
            |> List.map ^ renderColumn level columnClass
            |> div [clazz "row"]
        | Paragraph fragments -> 
            renderInline fragments
            |> p []
        | Checkbox(inlineLabel, state, command) -> 
            let input = 
                input [
                    yield "type", "checkbox"
                    yield "data-toggle", "checkbox"
                    if state then
                        yield "checked", "checked"
                    yield "onchange", renderCommand command
                ] []
            let fragments = renderInline inlineLabel
            let icons = 
                span [clazz "icons"; "remove-on-init", "remove"] 
                    [
                        span [clazz "icon-unchecked"] []
                        span [clazz "icon-checked"] []
                    ]

            label [clazz "checkbox"] (input :: icons :: fragments) 
        | Button(buttonType, inlineLabel, command) ->
            let buttonTypeClass =
                match buttonType with
                | Primary -> "btn-primary"
                | Warning -> "btn-warning"
                | Default -> "btn-default"
                | Danger -> "btn-danger"
                | Success -> "btn-success"
                | Inverse -> "btn-inverse"
                | Info -> "btn-info"
                | Link -> "btn-link"
                | ButtonType.Disabled -> "disabled"
            let allClasses = 
                ["btn"; "btn-block"; buttonTypeClass]

            button 
                [classes allClasses; "onclick", renderCommand command] 
                (renderInline inlineLabel)
        | Input(inputType, placeholder, text, command) ->
            let attrs = [
                clazz "form-control" 
                "type", "text"
                "value", text
                "placeholder", placeholder
                "oninput", renderEventHandler (RenderableEvent.ParameterizedThis("value", command))
            ]
            
            input attrs []
        | Table(header, rows) ->
            let header = 
                match header with
                | [] -> None
                | _ ->
                    header 
                    |> List.map ^ (renderInline >> th [])
                    |> Some

            let body = 
                rows
                |> List.map ^ List.map (renderInline >> td [])
                |> List.map ^ tr []


            table [classes ["table"]] [
                if header.IsSome then
                    yield thead [] header.Value
                yield tbody [] body
            ]

    and renderColumn level columnClass (Column(boxes)) =
        boxes 
        |> List.map ^ renderBox level
        |> div [clazz columnClass]

    document
    |> renderBox 0
    |> fun content -> div [clazz "container"] [content]




