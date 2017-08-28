module BackPanel.HTML

open System.Xml.Linq

type Attribute = Attribute of string * string
type Content = 
    | Text of string
    | Element of string * Attribute list * Content list

let element name attributes content = 
    Element(name, attributes, content)
let attr name value = 
    Attribute(name, value)

let clazz = attr "class"
let h level = element (sprintf "h%d" level)

let div = element "div"
let label = element "label"
let input = element "input"
let p = element "p"

let render content = 

    let xname = XName.op_Implicit

    let renderAttribute (Attribute(name, value)) = 
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

