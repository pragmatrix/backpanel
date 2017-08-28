namespace BackPanel

open BackPanel.Document

type Port = Port of int

type Configuration<'model> = {
    Title: string
    Document: 'model -> Document
}
