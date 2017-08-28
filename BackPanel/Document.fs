﻿/// BackPanel semantic rendering.
module rec BackPanel.Document

open Microsoft.FSharp.Quotations

type 't binding = {
    get: obj -> 't
    set: obj -> 't -> unit
}

type Document = 
    | Section of Properties * title: Inline * Document list
    | Row of Properties * Column list

type Properties = Property list

type Property =
    | Title of string
    | Label of string
    | Bind of Expr

type Columns = Column list 

type Column = 
    | Column of Properties * Box list

/// A box.
type Box = 
    | Paragraph of Inline
    | Checkbox of Properties * Inline * bool * obj

/// One single line.
type Inline = InlineFragment list
    
type InlineFragment = 
    | Text of string

let (!!) text = Text(text)

/// Sections build a hierarchical document structure and are rendered as `h*` html elements.
/// Use section [title "Title"] [{content}]
let section properties title content = 
    Section(properties, title, content)

let title title = 
    Title title

let row properties content = 
    Row(properties, content)

let column properties content = 
    Column(properties, content)

let checkbox properties label state command =
    Checkbox(properties, label, state, box command)
    
let bind (expr: Expr<'t>) = 
    Bind(expr :> Expr)