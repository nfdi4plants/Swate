namespace Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Logo =

    [<ReactComponent>]
    static member Main(?className: string, ?onClick) =
        Html.div [
            prop.className [
                "swt:px-1 swt:py-1 swt:rounded swt:bg-[#00776f]"
                if onClick.IsSome then
                    "swt:cursor-pointer"
            ]
            if onClick.IsSome then
                prop.onClick onClick.Value
            prop.ariaLabel "logo"
            prop.children [
                Html.img [
                    prop.style [ style.maxHeight (length.perc 100); style.width 100 ]
                    prop.src @"assets/Swate_logo_for_excel.svg"
                ]
            ]
        ]