namespace Swate.Components

open Feliz

type Components =

    [<ReactComponent>]
    static member DeleteButton(?children, ?className: string, ?props: IReactProperty list) =
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

    [<ReactComponent>]
    static member CircularExitButton(?children, ?className: string, ?props: IReactProperty list) =
        Html.button [
            prop.className [
                "swt:btn swt:btn-outline swt:btn-circle"
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

    [<ReactComponent>]
    static member CollapseButton
        (isCollapsed, setIsCollapsed, ?collapsedIcon, ?collapseIcon, ?classes: string, ?classFn: bool -> string)
        =
        Html.label [
            prop.className [
                "swt:btn swt:btn-square swt:swap swt:swap-rotate swt:grow-0"
                if classes.IsSome then
                    classes.Value
                match classFn with
                | Some fn -> fn isCollapsed
                | None -> ()
            ]
            prop.onClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()
                not isCollapsed |> setIsCollapsed
            )
            prop.onMouseDown (fun e -> e.stopPropagation ())
            prop.children [
                Html.input [
                    prop.type'.checkbox
                    prop.isChecked isCollapsed
                    prop.onChange (fun (_: bool) -> ())
                ]
                Html.i [
                    prop.className "swt:swap-off"
                    prop.children [
                        if collapsedIcon.IsSome then
                            collapsedIcon.Value
                        else
                            Icons.ChevronDown()
                    ]
                ]
                Html.i [
                    prop.className "swt:swap-on"
                    prop.children [
                        if collapseIcon.IsSome then
                            collapseIcon.Value
                        else
                            Icons.Close()
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member LoadingSpinner(?text: string, ?size: DaisyuiSize, ?color: DaisyuiColors) =
        Html.span [
            prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2 swt:py-10"
            prop.children [
                Html.div [
                    prop.className [
                        "swt:loading"
                        match size with
                        | Some(DaisyuiSize.XS) -> $"swt:loading-xs"
                        | Some(DaisyuiSize.SM) -> $"swt:loading-sm"
                        | Some(DaisyuiSize.MD) -> $"swt:loading-md"
                        | Some(DaisyuiSize.LG) -> $"swt:loading-lg"
                        | Some(DaisyuiSize.XL) -> $"swt:loading-xl"
                        | None -> ()
                        match color with
                        | Some DaisyuiColors.Primary -> "swt:loading-primary"
                        | Some DaisyuiColors.Secondary -> "swt:loading-secondary"
                        | Some DaisyuiColors.Accent -> "swt:loading-accent"
                        | Some DaisyuiColors.Warning -> "swt:loading-warning"
                        | Some DaisyuiColors.Error -> "swt:loading-error"
                        | Some DaisyuiColors.Info -> "swt:loading-info"
                        | Some DaisyuiColors.Success -> "swt:loading-success"
                        | None -> ()
                    ]
                ]
                match text with
                | Some t -> Html.span [ prop.text t ]
                | None -> Html.none
            ]
        ]