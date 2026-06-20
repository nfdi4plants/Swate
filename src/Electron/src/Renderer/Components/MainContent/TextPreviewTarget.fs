module Renderer.Components.MainContent.TextPreviewTarget

open Feliz

[<ReactComponent>]
let TextPreviewTarget (content: string) =
    Html.div [
        prop.className "swt:size-full swt:p-4 swt:overflow-auto swt:bg-base-100"
        prop.children [|
            Html.pre [
                prop.className "swt:text-sm swt:font-mono"
                prop.text content
            ]
        |]
    ]
