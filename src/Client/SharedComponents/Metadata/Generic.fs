namespace Components.Forms

open Feliz.DaisyUI
open Feliz

type Generic =
    static member FieldTitle (title:string) =
        Html.h5 [
            prop.className "text-primary font-semibold mt-6 mb-2"
            prop.text title;
        ]
    static member BoxedField (?title: string, ?description: string, ?content: ReactElement list) =
        Daisy.card [
            card.compact
            prop.className "space-y-6 border-2 border-base-300 shadow-xl bg-base
            prose prose-headings:text-primary container max-w-full lg:max-w-[800px]"
            prop.children [
                Daisy.cardBody [
                    Html.div [
                        prop.children [
                            if title.IsSome then
                                Html.h1 [
                                    prop.className "mt-0"
                                    prop.text title.Value
                                ]
                            if description.IsSome then
                                Html.p [
                                    prop.className "text-sm text-gray-500"
                                    prop.text description.Value
                                ]
                        ]
                    ]
                    if content.IsSome then
                        Html.div [
                            prop.className "space-y-4 divide-y divide-base-content"
                            prop.children content.Value
                        ]
                ]
            ]
        ]
    static member BoxedField (content: ReactElement list) =
        Generic.BoxedField (content = content)

    static member Section (children: ReactElement seq) =
        Html.section [
            prop.className "container py-2 lg:py-8 space-y-8"
            prop.children children
        ]

    static member Collapse (title: ReactElement seq) (content: ReactElement seq) =
        Daisy.collapse [
            prop.className "grow border has-[:checked]:border-transparent has-[:checked]:bg-base-200"
            collapse.plus
            prop.children [
                Html.input [prop.type'.checkbox; prop.className "peer"]
                Daisy.collapseTitle [
                    prop.className "after:text-primary @md/main:after:!size-4 @md/main:after:text-xl flex gap-4"
                    prop.children title
                ]
                Daisy.collapseContent [
                    prop.className "space-y-4 cursor-default"
                    prop.children content
                ]
            ]
        ]

    static member CollapseTitle(title: string, subtitle: string, ?count: string) =
        React.fragment [
            Html.div [
                Html.h5 title
                Html.div [
                    prop.className "not-prose text-sm"
                    prop.children [
                        Html.span [prop.text subtitle ]
                    ]
                ]
            ]
            if count.IsSome then
                Html.div [
                    prop.className "not-prose text-center ml-auto"
                    prop.children [
                        Html.i [
                            prop.className "fa-solid fa-edit"
                        ]
                        Html.div [
                            prop.className "text-sm"
                            prop.text (count.Value);
                        ]
                    ]
                ]
        ]