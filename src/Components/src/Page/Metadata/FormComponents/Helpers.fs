module Swate.Components.Page.Metadata.FormComponents.Helpers

open Browser.Types
open Feliz

let addButton (clickEvent: MouseEvent -> unit) =
    Html.button [
        prop.className "swt:btn swt:btn-info"
        prop.text "+"
        prop.onClick clickEvent
    ]

let deleteButton (clickEvent: MouseEvent -> unit) =
    Html.button [
        prop.className "swt:btn swt:btn-error swt:grow-0"
        prop.text "Delete"
        prop.onClick clickEvent
    ]

let cardFormGroup (content: ReactElement list) =
    Html.div [
        prop.className "swt:grid swt:@md/main:grid-cols-2 swt:@xl/main:grid-flow-col swt:gap-4 not-prose"
        prop.children content
    ]
