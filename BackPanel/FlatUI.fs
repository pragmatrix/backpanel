/// FlatUI / Bootstrap HTML renderer
module BackPanel.FlatUI

open BackPanel.Document
open BackPanel.HTML

let render (document: Document) : Content =

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
                ] []
            let fragments = renderInline inlineLabel
            label [clazz "checkbox"] (input :: fragments) 

    let renderColumn (Column(properties, boxes)) =
        boxes 
        |> List.map renderBox
        |> div [clazz "col"]

    let rec renderDocument level = function
        | Section(properties, title, documents) ->
            let title = renderInline title
            let documents = documents |> List.map ^ renderDocument (level+1)
            div [] (h (level+1) [] title :: documents)
        | Row(properties, columns) ->
            columns 
            |> List.map renderColumn
            |> div [clazz "row"]

    document
    |> renderDocument 0
    |> fun content -> div [clazz "container"] [content]




