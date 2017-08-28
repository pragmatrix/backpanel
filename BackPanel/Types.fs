namespace BackPanel

open BackPanel.Document

type Port = Port of int

type Page<'model, 'event> = {
    Initial: 'model
    Render: 'model -> Document
    Update: 'model -> 'event -> 'model
}

type Configuration<'model, 'event> = {
    Title: string
    Page: Page<'model, 'event>
}
