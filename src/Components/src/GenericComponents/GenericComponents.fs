namespace Swate.Components

open Feliz
open Feliz.DaisyUI

[<AutoOpen>]
module DaisyUiExtensions =

    type modal with
        static member active = prop.className "swt:modal swt:modal-open"

type Components =

    static member DeleteButton(?children, ?className: string, ?props: IReactProperty list) =
        //Daisy.button.button [
        Html.button [
            prop.className [
                "swt:btn swt:btn-square"
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            prop.children [
                if children.IsSome then
                    yield! children.Value
                Svg.svg [
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.className "swt:h-6 swt:w-6"
                    svg.fill "none"
                    svg.viewBox (0, 0, 24, 24)
                    svg.stroke "currentColor"
                    svg.children [
                        Svg.path [
                            svg.strokeLineCap "round"
                            svg.strokeLineJoin "round"
                            svg.strokeWidth 2
                            svg.d "M6 18L18 6M6 6l12 12"
                        ]
                    ]
                ]
            ]
        ]

    static member CollapseButton(isCollapsed, setIsCollapsed, ?collapsedIcon, ?collapseIcon, ?classes: string) =
        Html.label [
            prop.className [
                "swt:btn swt:btn-square swt:swap swt:swap-rotate swt:grow-0"
                if classes.IsSome then
                    classes.Value
            ]
            prop.onClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()
                not isCollapsed |> setIsCollapsed)
            prop.children [
                Html.input [
                    prop.type'.checkbox
                    prop.isChecked isCollapsed
                    prop.onChange (fun (_: bool) -> ())
                ]
                Html.i [
                    prop.className [
                        "swt:swap-off fa-solid"
                        if collapsedIcon.IsSome then
                            collapsedIcon.Value
                        else
                            "fa-solid fa-chevron-down"
                    ]
                ]
                Html.i [
                    prop.className [
                        "swt:swap-on"
                        if collapseIcon.IsSome then
                            collapseIcon.Value
                        else
                            "fa-solid fa-x"
                    ]
                ]
            ]
        ]