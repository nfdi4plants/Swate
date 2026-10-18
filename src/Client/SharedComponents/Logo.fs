namespace Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Logo =

    [<ReactComponent>]
    static member Main(?className: string, ?onClick) =
        Html.div [
            prop.className [
                "swt:rounded"
                if onClick.IsSome then
                    "swt:cursor-pointer"
            ]
            if onClick.IsSome then
                prop.onClick onClick.Value
            prop.ariaLabel "logo"
            prop.children [
                Html.img [
                    prop.style [ style.maxHeight (length.perc 100); style.width 100 ]
                    prop.src @"assets/arcitisa-banner.svg"
                    prop.alt "ARCitISA"
                ]
            ]
        ]
