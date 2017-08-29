/// FlatUI / Bootstrap HTML renderer
module BackPanel.FlatUI

open BackPanel.Document
open BackPanel.HTML

type Configuration = {
    CommandToJavaScriptEventHandler: obj -> string
}

let render (configuration: Configuration) (document: Document) : Content =

    let renderEventHandler = configuration.CommandToJavaScriptEventHandler

    let renderInlineFragment = function
        | InlineFragment.Text text -> Text text

    let renderInline = 
        List.map renderInlineFragment

    let renderBox = function
        | Paragraph fragments -> 
            renderInline fragments
            |> p []
        | Checkbox(properties, inlineLabel, state, command) -> 
            let input = 
                input [
                    yield attr "type" "checkbox"
                    yield attr "data-toggle" "checkbox"
                    if state then
                        yield attr "checked" "checked"
                    yield attr "onchange" (renderEventHandler command)
                ] []
            let fragments = renderInline inlineLabel
            label [clazz "checkbox"] (input :: fragments) 

    let renderColumn columnClass (Column(properties, boxes)) =
        boxes 
        |> List.map renderBox
        |> div [clazz columnClass]

    let rowColumnsTo12GridWidth columnCount = 
        match columnCount with
        | 1 -> 12
        | 2 -> 6
        | 3 -> 4
        | 4 -> 3
        | 6 -> 2
        | 12 -> 1
        | _ -> failwithf "%d columns are not supported" columnCount

    let rec renderDocument level = function
        | Section(properties, title, documents) ->
            let title = renderInline title
            let documents = documents |> List.map ^ renderDocument (level+1)
            div [] (h (level+1) [] title :: documents)
        | Row(properties, columns) ->
            let columnWidth = rowColumnsTo12GridWidth columns.Length
            let columnClass = sprintf "col-md-%d" columnWidth
            columns 
            |> List.map ^ renderColumn columnClass
            |> div [clazz "row"]

    document
    |> renderDocument 0
    |> fun content -> div [clazz "container"] [content]




