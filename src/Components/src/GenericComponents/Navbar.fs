namespace Swate.Components

open System
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types

[<Erase; Mangle(false)>]
type Navbar =

    static member MaterialIcon(icon: string, ?styling: bool) =

        let styling = defaultArg styling false

        Html.i [
            prop.className [ "swt:iconify " + icon ]
            if styling then
                prop.style [ style.transform [ transform.rotate 90 ] ]
        ]

    [<ReactComponent>]
    static member Button
        (icon: string, tooltip: string, (onClick: MouseEvent -> unit), ?styling: bool, ?toolTipPosition: string)
        =

        let styling = defaultArg styling false
        let toolTipPosition = defaultArg toolTipPosition "swt:tooltip-right"

        Html.div [
            prop.className $"swt:tooltip {toolTipPosition}"
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className [ "swt:btn swt:btn-square swt:btn-ghost" ]
                    prop.children [
                        Html.i [
                            prop.className [ "swt:iconify " + icon ]
                            if styling then
                                prop.style [ style.transform [ transform.rotate 90 ] ]
                        ]
                    ]
                    prop.onClick (fun e -> onClick e)
                ]
            ]
        ]

    [<ReactComponent>]
    static member ContextMenu(containerRef, buttons: ButtonInfo[], ?debug) =

        let buttonElements =
            buttons
            |> Array.map (fun (button: ButtonInfo) ->
                ContextMenuItem(Html.li [ prop.text button.toolTip ], Navbar.MaterialIcon(button.icon))
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

            let event = Navbar.createMouseEvent "contextmenu" options
            element.dispatchEvent (event) |> ignore

        if buttons.Length > 0 && buttons.Length <= maxNumber + 1 then
            Html.div []
        else
            Html.div [
                prop.ref containerRef
                if debug then
                    prop.testId "actionbar-test"
                prop.children [
                    Navbar.Button(
                        "swt:fluent--line-horizontal-1-dot-20-regular swt:size-5",
                        "Show more options",
                        (fun e ->
                            match containerRef.current with
                            | Some container -> fireOpenContextEvent container e.clientX e.clientY
                            | None -> ()
                        )
                    )

                    let restButtons = buttons.[maxNumber..] |> Array.map (fun button -> button)

                    Navbar.ContextMenu(containerRef, restButtons)
                ]
            ]

    [<ReactComponent>]
    static member Actionbar(buttons: ButtonInfo[], maxNumber: int, ?debug: bool) =

        let debug = defaultArg debug false

        let selectedElements =
            React.useMemo (
                (fun _ ->
                    if buttons.Length > 0 && buttons.Length > maxNumber + 1 then
                        Array.take maxNumber buttons
                        |> Array.map (fun button -> Navbar.Button(button.icon, button.toolTip, button.onClick))
                    else
                        buttons
                        |> Array.map (fun button -> Navbar.Button(button.icon, button.toolTip, button.onClick))
                ),
                [| buttons |]
            )

        let restElements =
            React.useMemo ((fun _ -> Navbar.RestElement(buttons, maxNumber, debug = debug)), [| buttons |])

        let selectedElement = React.Fragment selectedElements

        Html.div [
            prop.className $"swt:flex swt:border swt:border-neutral swt:rounded-lg swt:w-full"
            prop.children [ selectedElement; restElements ]
        ]

    static member SelectorItem(arcPointer: ARCPointer, onARCClick: ARCPointer -> unit, ?potMaxWidth) =

        let maxWidth = defaultArg potMaxWidth 48

        Html.li [
            prop.key arcPointer.path
            prop.className [ "swt:menu-item" ]
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:justify-between"
                    prop.children [
                        Html.span [
                            prop.className "swt:truncate swt:block swt:min-w-30"
                            prop.style [ style.maxWidth maxWidth ]
                            prop.text arcPointer.name
                        ]
                        if arcPointer.isActive then
                            Html.i [
                                prop.className "swt:iconify swt:fluent--checkmark-20-regular swt:size-5 swt:flex-none"
                            ]
                    ]
                ]
            ]
            prop.onClick (fun _ -> onARCClick (arcPointer))
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member Selector
        (
            arcPointers: ARCPointer[],
            setRecentARCs,
            buttons: ButtonInfo[],
            maxNumberRecentElements,
            maxNumberActionBar,
            ?potMaxWidth: int,
            ?debug: bool
        ) =

        let debug = defaultArg debug false

        let isOpen, setOpen = React.useState (false)

        let latestARCs =
            if arcPointers.Length > maxNumberRecentElements then
                Array.take maxNumberRecentElements arcPointers
            else
                arcPointers

        let setArcActivity (arcElement: ARCPointer) =
            {
                arcElement with
                    isActive = not arcElement.isActive
            }
            : ARCPointer

        let onARCClick (clickedARC) =
            let updated =
                latestARCs
                |> Array.map (fun arc ->
                    if arc = clickedARC then
                        setArcActivity arc
                    else
                        { arc with isActive = false }: ARCPointer
                )

            setRecentARCs updated

        let recentARCElements =
            React.useMemo (
                (fun _ ->
                    latestARCs
                    |> Array.map (fun arcPointer -> Navbar.SelectorItem(arcPointer, onARCClick))
                ),
                [| latestARCs |]
            )

        let dropDownSwitch =
            React.useMemo (
                (fun _ ->
                    Html.button [
                        prop.onClick (fun _ -> setOpen (not isOpen))
                        prop.role.button
                        prop.className "swt:btn swt:btn-xs swt:btn-outline swt:flex-nowrap"
                        if debug then
                            prop.testId "selector-test"
                        prop.children [
                            Html.div [ prop.text "Placeholder" ]
                            Navbar.MaterialIcon "swt:fluent--arrow-fit-height-24-regular swt:size-5"
                        ]
                    ]
                ),
                [| isOpen |]
            )

        Dropdown.Main(
            isOpen,
            setOpen,
            dropDownSwitch,
            recentARCElements,
            Navbar.Actionbar(buttons, maxNumberActionBar, debug = debug),
            ?potMaxWidth = potMaxWidth
        )

    [<ReactComponent>]
    static member Main
        (?left: ReactElement, ?middle: ReactElement, ?right: ReactElement, ?navbarHeight: int, ?debug: bool)
        =
        let debug = defaultArg debug false
        let left = defaultArg left (Html.div [])
        let middle = defaultArg middle (Html.div [])
        let right = defaultArg right (Html.div [])

        Html.div [
            prop.className
                "swt:bg-base-300 swt:text-base-content swt:gap-2 swt:flex swt:items-center swt:w-full swt:h-full swt:p-2"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
            if debug then
                prop.testId "navbar-test"
            prop.children [
                Html.div [
                    prop.className "swt:grow-0 swt:flex swt:flex-row"
                    prop.children left
                ]
                Html.div [
                    prop.className "swt:grow swt:flex swt:flex-row swt:text-center"
                    prop.children middle
                ]
                Html.div [
                    prop.className "swt:grow-0 swt:flex swt:flex-row"
                    prop.children right
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry(?debug: bool) =

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

        let testRecentARCs = [|
            ARCPointer.create ("Test 1", "/Here", false)
            ARCPointer.create ("Test 2", "/Here/Here", false)
            ARCPointer.create ("Test 3", "/Here/Here/Here", false)
            ARCPointer.create (
                "Test jfcesjföisjyfnwjtiewhroiajlkfnnalkfjwarkoiewfanflkndslkfjwiajofkcmscnskjfafdölmsalknoisjfamlkcnkj<ycwaklfnewjföosajö",
                "/Here/Here/Here/Here",
                false
            )
        |]

        let recentARCs, setRecentARCs = React.useState (testRecentARCs)

        let selector =
            Navbar.Selector(recentARCs, setRecentARCs, standardButtons, 5, 3, potMaxWidth = 48, ?debug = debug)

        Navbar.Main(selector, ?debug = debug)