module Components.Generic

open Feliz.DaisyUI
open Feliz

let BoxedField (title: string option) (description: string option) (content: ReactElement list) =
    Html.div [
        prop.className "space-y-4"
        prop.children [
        if title.IsSome then
            Html.h4 title.Value
        if description.IsSome then
            Html.p description.Value
        yield! content
        ]
    ]