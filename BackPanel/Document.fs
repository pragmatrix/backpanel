/// BackPanel semantic rendering.
module rec BackPanel.Document

open Microsoft.FSharp.Quotations

type 't binding = {
    get: obj -> 't
    set: obj -> 't -> unit
}

type Document = 
    | Section of Properties * Document list
    | Row of Properties * Column list

type Properties = Property list

type Property =
    | Title of string
    | Bind of Expr

type Columns = Column list 

type Column = 
    | Column of Properties * Paragraph list

type Paragraph = 
    | Paragraph of Fragments
    | Checkbox of Properties * Fragments

type Fragments = Fragment list
    
type Fragment = 
    | Text of string

let (!!) text = Text(text)

/// Sections build a hierarchical document structure and are rendered as `h*` html elements.
/// Use section [title "Title"] [{content}]
let section properties content = 
    Section(properties, content)

let title title = 
    Title title

let row properties content = 
    Row(properties, content)

let column properties content = 
    Column(properties, content)

let checkbox properties content =
    Checkbox(properties, content)    
    
let bind (expr: Expr<'t>) = 
    Bind(expr :> Expr)