namespace Swate.Components

open Browser.Types

open Feliz
open Fable.Core
open Fable.Core.JsInterop

open Swate.Components.Types.Actionbar

[<Erase; Mangle(false)>]
type Actionbar =

    [<ReactComponent>]
    static member Button
        (
            buttonInfo: ButtonInfo,
            buttonSize: DaisyuiSize,
            tooltipPosition: DaisyuiTooltipPosition,
            ?buttonClassName: string,
            ?buttonTestId: string,
            ?debug: bool
        ) =

        let debug = defaultArg debug false

        let toolTipPosition =
            match tooltipPosition with
            | DaisyuiTooltipPosition.Top -> "swt:tooltip-top"
            | DaisyuiTooltipPosition.Right -> "swt:tooltip-right"
            | DaisyuiTooltipPosition.Bottom -> "swt:tooltip-bottom"
            | DaisyuiTooltipPosition.Left -> "swt:tooltip-left"

        let btnSize =
            match buttonSize with
            | DaisyuiSize.XS -> "swt:btn-xs"
            | DaisyuiSize.SM -> "swt:btn-sm"
            | DaisyuiSize.MD -> "swt:btn-md"
            | DaisyuiSize.LG -> "swt:btn-lg"
            | DaisyuiSize.XL -> "swt:btn-xl"

        let Button =
            Html.button [
                if debug then
                    prop.testId "button-test"
                prop.className [
                    match buttonClassName with
                    | Some customClass -> customClass
                    | None -> "swt:btn swt:btn-primary swt:btn-square swt:btn-ghost"
                    btnSize
                ]
                if buttonTestId.IsSome then
                    prop.testId buttonTestId.Value
                prop.children [
                    Html.i [ prop.className [ "swt:iconify " + buttonInfo.icon ] ]
                ]
                prop.onClick (fun e -> buttonInfo.onClick e)
            ]

        match buttonInfo.toolTip with
        | None -> Button
        | Some tooltip ->
            Html.div [
                prop.className [ "swt:tooltip"; toolTipPosition ]
                prop.ariaLabel tooltip
                prop.children [
                    Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                    Button
                ]
            ]

    static member MaterialIcon(icon: string, ?styling: bool) =

        let styling = defaultArg styling false

        Html.i [
            prop.className [ "swt:iconify " + icon ]
            if styling then
                prop.style [ style.transform [ transform.rotate 90 ] ]
        ]

    [<ReactComponent>]
    static member ContextMenu(containerRef, buttons: ButtonInfo[], ?debug) =

        let buttonElements =
            buttons
            |> Array.map (fun (button: ButtonInfo) ->
                ContextMenuItem(
                    Html.li [ prop.text (Option.defaultValue "" button.toolTip) ],
                    Actionbar.MaterialIcon(button.icon)
                )
            )
            |> List.ofArray

        ContextMenu.ContextMenu(
            (fun _ -> buttonElements),
            ref = containerRef,
            onSpawn =
                (fun e ->
                    let target = e.target :?> Browser.Types.HTMLElement
                    Some target
                ),
            ?debug = debug
        )

    [<Emit("new MouseEvent($0, $1)")>]
    static member createMouseEvent (eventType: string) (options: obj) : MouseEvent = jsNative

    [<ReactComponent>]
    static member RestElement
        (buttons: ButtonInfo[], maxNumber, buttonSize, tooltipPosition, ?buttonClassName, ?debug: bool)
        =

        let debug = defaultArg debug false

        let containerRef = React.useElementRef ()

        let fireOpenContextEvent (element: HTMLElement) clientX clientY =
            let options =
                createObj [
                    "bubbles" ==> true
                    "cancable" ==> true
                    "clientX" ==> clientX
                    "clientY" ==> clientY
                    "button" ==> 2
                ]

            let event = Actionbar.createMouseEvent "contextmenu" options
            element.dispatchEvent (event) |> ignore

        if buttons.Length > 0 && buttons.Length <= maxNumber + 1 then
            Html.none
        else
            let buttonInfo =
                ButtonInfo.create (
                    "swt:fluent--line-horizontal-1-dot-20-regular swt:size-5",
                    "Show more options",
                    (fun e ->
                        match containerRef.current with
                        | Some container -> fireOpenContextEvent container e.clientX e.clientY
                        | None -> ()
                    )
                )

            Html.div [
                prop.ref containerRef
                if debug then
                    prop.testId "actionbar-test"
                prop.children [
                    Actionbar.Button(
                        buttonInfo,
                        buttonSize,
                        tooltipPosition,
                        ?buttonClassName = buttonClassName,
                        debug = debug,
                        buttonTestId = "actionbar-rest-button"
                    )

                    let restButtons = buttons.[maxNumber..] |> Array.map (fun button -> button)

                    Actionbar.ContextMenu(containerRef, restButtons, debug = debug)
                ]
            ]

    [<ReactComponent>]
    static member Main
        (
            buttons: ButtonInfo[],
            maxNumber: int,
            ?debug: bool,
            ?barClassName: string,
            ?buttonSize: DaisyuiSize,
            ?tooltipPosition: DaisyuiTooltipPosition,
            ?buttonClassName: string
        ) =

        let debug = defaultArg debug false
        let buttonSize = defaultArg buttonSize DaisyuiSize.MD
        let tooltipPosition = defaultArg tooltipPosition DaisyuiTooltipPosition.Bottom

        let selectedElements =
            React.useMemo (
                (fun _ ->
                    if buttons.Length > 0 && buttons.Length > maxNumber + 1 then
                        Array.take maxNumber buttons
                    else
                        buttons
                    |> Array.map (fun button ->
                        Actionbar.Button(
                            button,
                            debug = debug,
                            buttonSize = buttonSize,
                            tooltipPosition = tooltipPosition,
                            ?buttonClassName = buttonClassName
                        )
                    )
                ),
                [|
                    buttons
                    buttonSize
                    tooltipPosition
                    buttonClassName
                    maxNumber
                |]
            )

        let restElements =
            React.useMemo (
                (fun _ ->
                    Actionbar.RestElement(
                        buttons,
                        maxNumber,
                        buttonSize,
                        tooltipPosition,
                        ?buttonClassName = buttonClassName,
                        debug = debug
                    )
                ),
                [|
                    buttons
                    buttonSize
                    tooltipPosition
                    buttonClassName
                    maxNumber
                |]
            )

        let selectedElement = React.Fragment selectedElements

        Html.div [
            prop.className [
                barClassName
                |> Option.defaultValue "swt:flex swt:items-center swt:w-max swt:p-1"
            ]
            prop.children [ selectedElement; restElements ]
        ]

    [<ReactComponent>]
    static member Entry(maxNumber, ?debug: bool) =

        let newARCButton =
            ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", fun _ -> ())

        let openARCButton =
            ButtonInfo.create ("swt:fluent--folder-arrow-up-24-regular swt:size-5", "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            ButtonInfo.create (
                "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
                "Download an existing ARC",
                fun _ -> ()
            )

        let standardButtons = [|
            newARCButton
            openARCButton
            downLoadARCButton
            newARCButton
            openARCButton
        |]

        Actionbar.Main(standardButtons, maxNumber, ?debug = debug)