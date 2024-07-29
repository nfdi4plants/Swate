module MainComponents.Metadata.Generic

open Feliz.Bulma
open Feliz

let BoxedField (title: string) (description: string option) (content: ReactElement list) = 
    Bulma.field.div [
        Bulma.box [
            Bulma.block [
                Bulma.content [
                    Html.h4 title
                    if description.IsSome then
                        Html.p description.Value
                ]
            ]
            Bulma.block content
        ]
    ]