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

[<AutoOpen>]
module Representation =

    type Document = 
        | Section of Properties * title: Inline * Document list
        | Row of Properties * Column list

    type Properties = Property list

    type Property =
        | NoneYet

    type Columns = Column list 

    type Column = 
        | Column of Properties * Box list

    /// A box.
    type Box = 
        | Paragraph of Inline
        | Checkbox of Properties * Inline * bool * obj
        | Button of ButtonType * Inline * obj

    /// One single line.
    type Inline = InlineFragment list
    
    type InlineFragment = 
        | Text of string

let (!!) text = Text(text)

/// Sections build a hierarchical document structure and are rendered as `h*` html elements.
let section properties title content = 
    Section(properties, title, content)

let row properties content = 
    Row(properties, content)

let column properties content = 
    Column(properties, content)

let checkbox properties label state command =
    Checkbox(properties, label, state, box command)
    
let button buttonType label command = 
    Button(buttonType, label, command)