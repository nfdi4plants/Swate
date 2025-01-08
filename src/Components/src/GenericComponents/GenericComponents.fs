namespace Components

open Feliz
open Feliz.DaisyUI

[<AutoOpen>]
module DaisyUiExtensions =

    type modal with
        static member active = prop.className "modal-open"

type Components =
    static member DeleteButton(?children, ?props) =
        Daisy.button.button [
            button.square
            if props.IsSome then yield! props.Value
            prop.children [
                if children.IsSome then yield! children.Value
                Svg.svg [
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.className "h-6 w-6"
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
                "btn btn-square swap swap-rotate grow-0"
                if classes.IsSome then classes.Value
            ]
            prop.onClick (fun e -> e.preventDefault(); e.stopPropagation(); not isCollapsed |> setIsCollapsed)
            prop.children [
                Html.input [prop.type'.checkbox; prop.isChecked isCollapsed; prop.onChange(fun (_:bool) -> ())]
                Html.i [
                    prop.className [
                        "swap-off fa-solid"
                        if collapsedIcon.IsSome then collapsedIcon.Value else "fa-solid fa-chevron-down"
                    ]
                ]
                Html.i [
                    prop.className [
                        "swap-on"
                        if collapseIcon.IsSome then collapseIcon.Value else "fa-solid fa-x"
                    ]
                ]
            ]
        ]