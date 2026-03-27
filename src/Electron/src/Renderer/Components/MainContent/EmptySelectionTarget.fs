module Renderer.Components.MainContent.EmptySelectionTarget

open Feliz

[<ReactComponent>]
let EmptySelectionTarget () =
    Html.h1 [
        prop.text "No file selected"
        prop.className
            "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
    ]