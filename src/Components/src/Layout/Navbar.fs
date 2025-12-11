namespace Swate.Components

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types

type RecentArc(name: string, path: string, onClick: MouseEvent -> unit, isActive: bool) =

    member val Name = name with get, set
    member val Path = path with get, set
    member val OnClick = onClick with get, set
    member val IsActive = isActive with get, set

    member this.SetActive() = this.IsActive <- true
    member this.SetInActive() = this.IsActive <- false

type Dropdown =

    [<ReactComponent>]
    static member Main(isOpen, setIsOpen, toggle: ReactElement, recentARCs: ReactElement[], actionBar: ReactElement) =
        let ref = React.useElementRef ()
        React.useListener.onClickAway (ref, fun _ -> setIsOpen false)

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
                        prop.className [
                            "swt:dropdown-content swt:min-w-48 swt:menu swt:bg-base-200 swt:rounded-box swt:menu swt:z-[99] swt:p-2 swt:shadow-sm swt:!top-[110%] swt:rounded-box"
                        ]
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
    static member ContextMenu(containerRef, buttons: ReactElement[], ?debug) =

        let buttonElements =
            buttons |> Array.map (fun button -> ContextMenuItem(button)) |> List.ofArray

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
    static member Actionbar(elements: ReactElement[], maxNumber: int) =

        let selectedElements =
            if elements.Length > 0 && elements.Length > maxNumber + 1 then
                Array.take maxNumber elements
            else
                elements

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
                if elements.Length > 0 && elements.Length <= maxNumber + 1 then
                    Html.div []
                else
                    let containerRef = React.useElementRef ()

                    Html.div [
                        prop.ref containerRef
                        prop.children [
                            Navbar.Button(
                                Navbar.MaterialIcon("swt:fluent--line-horizontal-1-dot-20-filled swt:size-5"),
                                "Tooltip",
                                (fun e ->
                                    match containerRef.current with
                                    | Some container -> fireOpenContextEvent container e.clientX e.clientY
                                    | None -> ()
                                )
                            )
                            Navbar.ContextMenu(containerRef, elements.[maxNumber..])
                        ]
                    ]

            Html.div [ prop.children temp ]

        let selectedElement = Html.div [ prop.children selectedElements ]

        Html.div [
            prop.className "swt:flex swt:border swt:border-neutral swt:rounded-lg"
            prop.children [ selectedElement; restElements ]
        ]

    [<ReactComponent>]
    static member Selector(recentARCs: RecentArc[], setRecentARCs, buttons: ReactElement[], maxNumber) =

        let isOpen, setOpen = React.useState (false)

        let setArcActivity (arcElement: RecentArc) =
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
                    prop.className [
                        "swt:menu-item"
                        if arcElement.IsActive then
                            "swt:bg-primary"
                    ]
                    prop.text arcElement.Name
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
                    Navbar.MaterialIcon "swt:iconify swt:fluent--arrow-fit-height-24-regular swt:size-5"
                ]
            ]

        Dropdown.Main(isOpen, setOpen, dropDownSwitch, recentARCElements, Navbar.Actionbar(buttons, maxNumber))

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

        let newARCButton = Navbar.Button(noteAddIcon, "Create a new ARC", fun _ -> ())

        let openARCButton = Navbar.Button(fileOpenIcon, "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            Navbar.Button(cloudDownloadIcon, "Download an existing ARC", (fun _ -> ()))

        let standardButtons = [|
            newARCButton
            openARCButton
            downLoadARCButton
            newARCButton
            newARCButton
        |]

        let testRecentARCs = [|
            RecentArc("Test 1", "/Here", (fun _ -> ()), false)
            RecentArc("Test 2", "/Here/Here", (fun _ -> ()), false)
            RecentArc("Test 3", "/Here/Here/Here", (fun _ -> ()), false)
        |]

        let recentARCs, setRecentARCs = React.useState (testRecentARCs)

        let selector = Navbar.Selector(recentARCs, setRecentARCs, standardButtons, 3)

        Navbar.Main(selector)