namespace Swate.Components.Page.ARCObjectExplorer

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type ArcObjectDetailsLayout =

    [<ReactComponent>]
    static member Section(title: string, children: ReactElement list) =
        Html.div [
            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
            prop.children [
                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-3"; prop.text title ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3"
                    prop.children children
                ]
            ]
        ]

    [<ReactComponent>]
    static member EmptyState(message: string) =
        Html.div [
            prop.className
                "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
            prop.children [
                Html.p [
                    prop.className "swt:text-sm swt:text-center swt:opacity-70"
                    prop.text message
                ]
            ]
        ]
