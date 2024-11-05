module Components.Generic

open Feliz.Bulma
open Feliz

let BoxedField (title: string option) (description: string option) (content: ReactElement list) = 
    Bulma.field.div [
        Bulma.box [
            Bulma.block [
                Bulma.content [
                    if title.IsSome then Html.h4 title.Value
                    if description.IsSome then
                        Html.p description.Value
                ]
            ]
            Bulma.block [
                prop.className "space-y-2"
                prop.children content
            ]
        ]
    ]