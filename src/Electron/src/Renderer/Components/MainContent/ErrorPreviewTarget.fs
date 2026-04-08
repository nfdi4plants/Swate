module Renderer.Components.MainContent.ErrorPreviewTarget

open Feliz

[<ReactComponent>]
let ErrorPreviewTarget (errMsg: string) =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center swt:flex-col swt:gap-2"
        prop.children [|
            Html.h2 [
                prop.className "swt:text-error swt:font-bold"
                prop.text "Preview Error"
            ]
            Html.span [
                prop.className "swt:text-base-content swt:opacity-70"
                prop.text errMsg
            ]
        |]
    ]