module Renderer.Components.MainContent.InitTarget

open Feliz

[<ReactComponent>]
let InitTarget () =

    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
        prop.children [
            Html.div [
                prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                prop.children [ Renderer.Components.InitState.InitState() ]
            ]
        ]
    ]
