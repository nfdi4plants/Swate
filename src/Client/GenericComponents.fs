namespace Components

open Feliz
open Feliz.DaisyUI

[<AutoOpen>]
module DaisyUiExtensions =

    type modal with
        static member active = prop.className "modal-open"

type Components =

    static member LogicContainer (children: ReactElement list) =
        Html.div [
        //     prop.className "border-l-4 border-transparent px-4 py-2 shadow-md"
        //     prop.style [
        //         let rndVal = rnd.Next(30,70)
        //         let colorArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
        //         style.custom("borderImageSlice", "1")
        //         style.custom("borderImageSource", $"linear-gradient({colorArr.[if order then 0 else 1]} {100-rndVal}%%, {colorArr.[if order then 1 else 0]})")
        //         order <- not order
        //     ]
            prop.className "relative flex p-4 animated-border shadow-md gap-4 flex-col" //experimental
            prop.children children
        ]

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