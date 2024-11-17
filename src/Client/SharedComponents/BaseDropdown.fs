namespace Components

open Feliz
open Feliz.DaisyUI

type BaseDropdown =
    [<ReactComponent>]
    static member Main(isOpen, setIsOpen, toggle: ReactElement, children: ReactElement seq, ?style: Style) =
        let ref = React.useElementRef()
        React.useListener.onClickAway(ref, fun _ -> setIsOpen false)
        Html.div [
            prop.ref ref
            prop.className [
                "dropdown"
                if isOpen then "dropdown-open"
                if style.IsSome then style.Value.StyleString
            ]
            prop.children [
                toggle
                Html.ul [
                    prop.className [
                        "dropdown-content min-w-48 menu bg-base-200 rounded-box z-[1] p-2 shadow !top-[110%]"
                        if style.IsSome then style.Value.GetSubclassStyle "content"
                    ]
                    prop.children children
                ]
            ]
        ]