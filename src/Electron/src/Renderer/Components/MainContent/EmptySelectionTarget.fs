module Renderer.Components.MainContent.EmptySelectionTarget

open Feliz

[<ReactComponent>]
let EmptySelectionTarget () =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
        prop.children [
            // Html.h1 [
            //     prop.text "No file selected"
            //     prop.className
            //         "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
            // ]
            Html.h1 "Select files in the file tree to edit them."
        ]
    ]