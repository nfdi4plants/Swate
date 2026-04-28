namespace Swate.Components.ARCObjectExplorer

open System
open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type ARCObjectPanel =

    // Reuseable panel for displaying ARC objects in the object explorer
    [<ReactComponent>]
    static member Main(name: string, ?content: ReactElement) =
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
                "swt:flex swt:flex-1 swt:min-h-0 swt:min-w-0 swt:flex-col swt:gap-3 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:h-full swt:overflow-hidden"
            prop.children [
                if String.IsNullOrWhiteSpace name |> not then
                    Html.h3 [
                        prop.className "swt:text-sm swt:font-semibold"
                        prop.text name
                    ]
                Html.div [
                    prop.className "swt:flex-1 swt:min-h-0 swt:min-w-0 swt:overflow-auto"
                    prop.children [ content ]
                ]
            ]
        ]
