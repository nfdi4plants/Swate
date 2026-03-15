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
            icon: string,
            tooltip: string,
            (onClick: MouseEvent -> unit),
            ?toolTipPosition: string,
            ?debug: bool,
            ?buttonTestId: string,
            ?isDisabled: bool
        ) =

        let debug = defaultArg debug false

        let toolTipPosition = defaultArg toolTipPosition "swt:tooltip-right"
        let isDisabled = defaultArg isDisabled false

        Html.div [
            prop.title tooltip
            prop.children [
                Html.button [
                    if debug then
                        prop.testId "button-test"
                    prop.ariaLabel tooltip
                    prop.className [
                        "swt:btn swt:btn-primary swt:btn-square swt:btn-ghost swt:p-0 swt:btn-xs"
                        if isDisabled then
                            "swt:btn-disabled swt:opacity-50"
                    ]
                    prop.disabled isDisabled
                    if buttonTestId.IsSome then
                        prop.testId buttonTestId.Value
                    prop.children [ Html.i [ prop.className [ "swt:iconify " + icon ] ] ]
                    prop.onClick (fun e -> onClick e)
                ]
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
                ContextMenuItem(Html.li [ prop.text button.toolTip ], Actionbar.MaterialIcon(button.icon))
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
    static member RestElement(buttons: ButtonInfo[], maxNumber, ?debug: bool) =

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
            Html.div []
        else
            Html.div [
                prop.ref containerRef
                if debug then
                    prop.testId "actionbar-test"
                prop.children [
                    Actionbar.Button(
                        "swt:fluent--line-horizontal-1-dot-20-regular swt:size-5",
                        "Show more options",
                        (fun e ->
                            match containerRef.current with
                            | Some container -> fireOpenContextEvent container e.clientX e.clientY
                            | None -> ()
                        ),
                        debug = debug,
                        buttonTestId = "actionbar-rest-button"
                    )

                    let restButtons = buttons.[maxNumber..] |> Array.map (fun button -> button)

                    Actionbar.ContextMenu(containerRef, restButtons, debug = debug)
                ]
            ]

    [<ReactComponent>]
    static member Main(buttons: ButtonInfo[], maxNumber: int, ?debug: bool) =

        let debug = defaultArg debug false

        let selectedElements =
            React.useMemo (
                (fun _ ->
                    let onClick button =
                        fun (_: MouseEvent) ->
                            if not button.isDisabled then
                                button.onClick ()

                    if buttons.Length > 0 && buttons.Length > maxNumber + 1 then
                        Array.take maxNumber buttons
                        |> Array.map (fun button ->
                            Actionbar.Button(
                                button.icon,
                                button.toolTip,
                                (onClick button),
                                debug = debug,
                                isDisabled = button.isDisabled
                            )
                        )
                    else
                        buttons
                        |> Array.map (fun button ->
                            Actionbar.Button(
                                button.icon,
                                button.toolTip,
                                (onClick button),
                                debug = debug,
                                isDisabled = button.isDisabled
                            )
                        )
                ),
                [| buttons |]
            )

        let restElements =
            React.useMemo ((fun _ -> Actionbar.RestElement(buttons, maxNumber, debug = debug)), [| buttons |])

        let selectedElement = React.Fragment selectedElements

        Html.div [
            prop.className $"swt:flex swt:items-center swt:w-max swt:p-1"
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
