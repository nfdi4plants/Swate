module Renderer.Components.MainContent.UnknownPreviewTarget

open Feliz

[<ReactComponent>]
let UnknownPreviewTarget () =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
        prop.children [| Html.h1 "Unknown file type" |]
    ]