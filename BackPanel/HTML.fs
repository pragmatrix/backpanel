module BackPanel.HTML

open System.Text
open System.IO
open System.Xml.Linq
open Newtonsoft.Json

type Attribute = string * string
type Content = 
    | Text of string
    | Element of string * Attribute list * Content list

let element name attributes content = 
    Element(name, attributes, content)

let clazz c = "class", c
let classes : string seq -> Attribute = String.concat " " >> clazz
let h level = element (sprintf "h%d" level)

let div = element "div"
let label = element "label"
let input = element "input"
let p = element "p"
let button = element "button"
let span = element "span"

let table = element "table"
let thead = element "thead"
let tbody = element  "tbody"
let tr = element "tr"
let th = element "th"
let td = element "td"

/// Render a HTML representation.
let render content = 

    let xname = XName.op_Implicit

    let renderAttribute (name, value) = 
        XAttribute(xname name, value)

    let rec renderContent = function
        | Text str -> XText(str) :> XNode
        | Element(name, attributes, content) -> 
            let attributes = attributes |> List.map (renderAttribute >> box)
            let content = content |> List.map (renderContent >> box)
            XElement(xname name, Seq.concat [attributes; content])
            :> XNode

    renderContent content
    |> string

/// Render a picodom compatible JSON representation.
let renderJSON content =

    let sb = StringBuilder()
    do
        use sw = new StringWriter(sb)
        use writer = new JsonTextWriter(sw)

        let rec renderContent = function
            | Text str -> 
                writer.WriteValue(str)
            | Element(name, attributes, content) -> 
                writer.WriteStartObject()
                writer.WritePropertyName("tag")
                writer.WriteValue(name)
                writer.WritePropertyName("data")
                writer.WriteStartObject()
                attributes |> Seq.iter 
                    ^ fun (name, value) ->
                        writer.WritePropertyName(name)
                        writer.WriteValue(value)
                writer.WriteEndObject()
                writer.WritePropertyName("children")
                writer.WriteStartArray()
                content |> Seq.iter renderContent
                writer.WriteEndArray()
                writer.WriteEndObject()

        renderContent content
    
    string sb
