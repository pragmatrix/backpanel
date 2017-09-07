/// BackPanel semantic document structure.
module rec BackPanel.Document

type ButtonType =
    | Primary
    | Warning
    | Default
    | Danger
    | Success
    | Inverse
    | Info
    | Link
    | Disabled

type InputType =
    | Enabled
    | Disabled

[<AutoOpen>]
module Representation =

    type Document = Box

    type Box = 
        | Section of title: Inline * Document list
        | Row of Column list
        | Paragraph of Inline
        | Checkbox of Inline * bool * obj
        | Button of ButtonType * Inline * obj
        | Input of InputType * string * string * (string -> obj)
        | Table of header: Inline list * rows: Inline list list

    type Columns = Column list 

    type Column = 
        | Column of Box list

    /// One single line.
    type Inline = InlineFragment list
    
    type InlineFragment = 
        | Text of string

let (!!) text = Text(text)

/// Sections build a hierarchical document structure and are rendered as `h*` html elements.
let section title content = 
    Section(title, content)

let row content = 
    Row(content)

let column content = 
    Column(content)

let checkbox label state command =
    Checkbox(label, state, box command)
    
let button buttonType label command = 
    Button(buttonType, label, command)

let input enabled placeholder content command = 
    Input(enabled, placeholder, content, command >> box)

let para content = 
    Paragraph content

let table header rows = 
    Table(header, rows)