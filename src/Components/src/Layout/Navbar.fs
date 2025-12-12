namespace Swate.Components

open System
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types

type RecentARC(name: string, path: string, onClick: MouseEvent -> unit, isActive: bool) =

    member val Name = name with get, set
    member val Path = path with get, set
    member val OnClick = onClick with get, set
    member val IsActive = isActive with get, set

    member this.SetActive() = this.IsActive <- true
    member this.SetInActive() = this.IsActive <- false

type ARCButton(icon: ReactElement, toolTip: string, onClick: MouseEvent -> unit) =

    member val Icon = icon with get, set
    member val ToolTip = toolTip with get, set
    member val OnClick = onClick with get, set

type Dropdown =

    [<ReactComponent>]
    static member Main
        (isOpen, setIsOpen, toggle: ReactElement, recentARCs: ReactElement[], actionBar: ReactElement, ?potMaxWidth: int) =
        let ref = React.useElementRef ()
        React.useListener.onClickAway (ref, fun _ -> setIsOpen false)

        let maxWidth = defaultArg potMaxWidth 48

        Html.div [
            prop.ref ref
            prop.className [
                "swt:dropdown"
                if isOpen then
                    "swt:dropdown-open"
            ]
            prop.children [
                toggle
                if isOpen then
                    Html.ul [
                        prop.tabIndex 0
                        prop.className
                            "swt:dropdown-content swt:min-w-48 swt:menu swt:bg-base-200 swt:rounded-box swt:z-99 swt:p-2 swt:gap-2 swt:shadow-sm swt:top-110%"
                        prop.style [ style.maxWidth maxWidth ]
                        prop.children [ Html.div recentARCs; actionBar ]
                    ]
            ]
        ]

type Navbar =

    static member MaterialIcon(icon: string, ?styling: bool) =

        let styling = defaultArg styling false

        Html.i [
            prop.className [ "swt:iconify " + icon ]
            if styling then
                prop.style [ style.transform [ transform.rotate 90 ] ]
        ]

    [<ReactComponent>]
    static member Button(icon: ReactElement, tooltip: string, (onClick: MouseEvent -> unit), ?toolTipPosition: string) =

        let toolTipPosition = defaultArg toolTipPosition "swt:tooltip-right"

        Html.div [
            prop.className $"swt:tooltip {toolTipPosition}"
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className [ "swt:btn swt:btn-square swt:btn-ghost" ]
                    prop.children [ icon ]
                    prop.onClick (fun e -> onClick e)
                ]
            ]
        ]

    [<ReactComponent>]
    static member ContextMenu(containerRef, buttons: ARCButton[], ?debug) =

        let toolTipps =
            buttons |> Array.map (fun toolTip -> Html.li [ prop.text toolTip.ToolTip ])

        let icons = buttons |> Array.map (fun icon -> Html.li [ icon.Icon ])

        let buttonElements =
            buttons
            |> Array.map (fun (button: ARCButton) -> ContextMenuItem(Html.li [ prop.text button.ToolTip ], button.Icon))
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
    static member Actionbar(buttons: ARCButton[], maxNumber: int) =

        let selectedElements =
            if buttons.Length > 0 && buttons.Length > maxNumber + 1 then
                Array.take maxNumber buttons
                |> Array.map (fun button -> Navbar.Button(button.Icon, button.ToolTip, button.OnClick))
            else
                buttons
                |> Array.map (fun button -> Navbar.Button(button.Icon, button.ToolTip, button.OnClick))

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

        let restElements =
            let temp =
                if buttons.Length > 0 && buttons.Length <= maxNumber + 1 then
                    Html.div []
                else
                    let containerRef = React.useElementRef ()

                    Html.div [
                        prop.ref containerRef
                        prop.children [
                            Navbar.Button(
                                Navbar.MaterialIcon("swt:fluent--line-horizontal-1-dot-20-regular swt:size-5"),
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

            Html.div [ prop.children temp ]

        let selectedElement = Html.div [ prop.children selectedElements ]

        Html.div [
            prop.className $"swt:flex swt:border swt:border-neutral swt:rounded-lg swt:w-full"
            prop.children [ selectedElement; restElements ]
        ]

    [<ReactComponent>]
    static member Selector
        (
            recentARCs: RecentARC[],
            setRecentARCs,
            buttons: ARCButton[],
            maxNumberRecentElements,
            maxNumberActionBar,
            ?potMaxWidth: int
        ) =

        let maxWidth = defaultArg potMaxWidth 48

        let isOpen, setOpen = React.useState (false)

        if recentARCs.Length > maxNumberRecentElements then
            let tmp = Array.take maxNumberRecentElements recentARCs
            setRecentARCs tmp

        let setArcActivity (arcElement: RecentARC) =
            match arcElement.IsActive with
            | true -> arcElement.SetInActive()
            | false -> arcElement.SetActive()

        let onARCClick (clickedARC) =
            let updated =
                recentARCs
                |> Array.map (fun arc ->
                    if arc = clickedARC then
                        setArcActivity arc
                    else
                        arc.SetInActive()

                    arc
                )

            setRecentARCs updated

        let recentARCElements =
            recentARCs
            |> Array.map (fun arcElement ->
                Html.li [
                    prop.key arcElement.Path
                    prop.className [ "swt:menu-item" ]
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:justify-between"
                            prop.children [
                                Html.span [
                                    prop.className "swt:truncate swt:block swt:min-w-30"
                                    prop.style [ style.maxWidth maxWidth ]
                                    prop.text arcElement.Name
                                ]
                                if arcElement.IsActive then
                                    Html.i [
                                        prop.className
                                            "swt:iconify swt:fluent--checkmark-20-regular swt:size-5 swt:flex-none"
                                    ]
                            ]
                        ]
                    ]
                    prop.onClick (fun _ -> onARCClick arcElement)
                ]
            )

        let dropDownSwitch =
            Html.button [
                prop.onClick (fun _ -> setOpen (not isOpen))
                prop.role.button
                prop.className "swt:btn swt:btn-xs swt:btn-outline swt:flex-nowrap"
                prop.children [
                    Html.div [ prop.text "Placeholder" ]
                    Navbar.MaterialIcon "swt:fluent--arrow-fit-height-24-regular swt:size-5"
                ]
            ]

        Dropdown.Main(
            isOpen,
            setOpen,
            dropDownSwitch,
            recentARCElements,
            Navbar.Actionbar(buttons, maxNumberActionBar),
            potMaxWidth = maxWidth
        )

    [<ReactComponent>]
    static member Main(?left: ReactElement, ?middle: ReactElement, ?right: ReactElement, ?navbarHeight: int) =
        let left = defaultArg left (Html.div [])
        let middle = defaultArg middle (Html.div [])
        let right = defaultArg right (Html.div [])

        Html.div [
            prop.className
                "swt:bg-base-300 swt:text-base-content swt:gap-2 swt:flex swt:items-center swt:w-full swt:h-full swt:p-2"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
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
    static member Entry() =

        let noteAddIcon =
            Navbar.MaterialIcon("swt:fluent--document-add-24-regular swt:size-5")

        let fileOpenIcon =
            Navbar.MaterialIcon("swt:fluent--folder-arrow-up-24-regular swt:size-5")

        let cloudDownloadIcon =
            Navbar.MaterialIcon("swt:fluent--cloud-arrow-down-24-regular swt:size-5")

        let newARCButton = ARCButton(noteAddIcon, "Create a new ARC", fun _ -> ())

        let openARCButton = ARCButton(fileOpenIcon, "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            ARCButton(cloudDownloadIcon, "Download an existing ARC", fun _ -> ())

        let standardButtons = [|
            newARCButton
            openARCButton
            downLoadARCButton
            newARCButton
            openARCButton
        |]

        let testRecentARCs = [|
            RecentARC("Test 1", "/Here", (fun _ -> ()), false)
            RecentARC("Test 2", "/Here/Here", (fun _ -> ()), false)
            RecentARC("Test 3", "/Here/Here/Here", (fun _ -> ()), false)
            RecentARC(
                "Test jfcesjföisjyfnwjtiewhroiajlkfnnalkfjwarkoiewfanflkndslkfjwiajofkcmscnskjfafdölmsalknoisjfamlkcnkj<ycwaklfnewjföosajö",
                "/Here/Here/Here/Here",
                (fun _ -> ()),
                false
            )
        |]

        let recentARCs, setRecentARCs = React.useState (testRecentARCs)

        let selector =
            Navbar.Selector(recentARCs, setRecentARCs, standardButtons, 5, 3, potMaxWidth = 48)

        Navbar.Main(selector)