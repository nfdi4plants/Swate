module Components.Generic

open Feliz.DaisyUI
open Feliz

let BoxedField (title: string option) (description: string option) (content: ReactElement list) =
    Html.div [
        prop.className "space-y-6 rounded-lg border border-black p-6 lg:p-8 shadow-md shadow-base-300 prose prose-headings:text-primary container max-w-full lg:max-w-[800px]"
        prop.children [
            Html.div [
                prop.children [
                    if title.IsSome then
                        Html.h2 [
                            prop.text title.Value
                        ]
                    if description.IsSome then
                        Html.p [
                            prop.className "text-sm text-gray-500"
                            prop.text description.Value
                        ]
                ]
            ]
            Html.div [
                prop.className "space-y-4 divide-y divide-base-content"
                prop.children content
            ]
        ]
    ]

let Section (children: ReactElement seq) =
    Html.section [
        prop.className "container py-2 lg:py-8 space-y-8"
        prop.children children
    ]