namespace Swate.Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type ARCObjectDetails =

    [<ReactComponent>]
    static member Main(?content: ReactElement) =
        let content =
            defaultArg
                content
                (Html.div [
                    prop.className
                        "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-md swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40"
                    prop.children [
                        Html.span [
                            prop.className "swt:text-sm swt:opacity-60"
                            prop.text "Empty"
                        ]
                    ]
                ])

        Html.section [
            prop.className
                "swt:flex swt:flex-col swt:gap-3 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:min-h-72 swt:h-full"
            prop.children [
                Html.h3 [
                    prop.className "swt:text-sm swt:font-semibold"
                    prop.text "ARC Object Details"
                ]
                content
            ]
        ]
