module Renderer.Components.MainContent.ErrorPreviewTarget

open Feliz

[<ReactComponent>]
let ErrorPreviewTarget (message: string) =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center swt:p-6"
        prop.children [
            Html.div [
                prop.className
                    "swt:max-w-2xl swt:w-full swt:rounded-lg swt:border swt:border-error/40 swt:bg-error/10 swt:p-6 swt:flex swt:flex-col swt:gap-3"
                prop.children [
                    Html.h1 [
                        prop.className "swt:text-lg swt:font-semibold swt:text-error"
                        prop.text "Could not load preview"
                    ]
                    Html.pre [
                        prop.className "swt:whitespace-pre-wrap swt:text-sm swt:font-mono swt:text-base-content"
                        prop.text message
                    ]
                ]
            ]
        ]
    ]
