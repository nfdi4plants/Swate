namespace Swate.Components

open Feliz
open Fable.Core

open SelectorTypes
open Swate.Components.Types.Actionbar

[<Erase; Mangle(false); ReactComponent>]
type Selector =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SelectorItem(arcPointer: ARCPointer, onClick: ARCPointer -> unit, ?potMaxWidth, ?testId: string) =

        let maxWidth = defaultArg potMaxWidth 48

        Html.li [
            prop.key arcPointer.path
            prop.className [ "swt:menu-item" ]
            if testId.IsSome then
                prop.testId testId.Value
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:justify-between"
                    prop.children [
                        Html.span [
                            prop.className "swt:truncate swt:block swt:min-w-30"
                            prop.style [ style.maxWidth maxWidth ]
                            prop.text arcPointer.name
                            prop.title arcPointer.path
                        ]
                        if arcPointer.isActive then
                            Html.i [
                                prop.className "swt:iconify swt:fluent--checkmark-24-regular swt:size-5 swt:flex-none"
                            ]
                    ]
                ]
            ]
            prop.onClick (fun _ -> onClick (arcPointer))
        ]

    [<ReactComponent>]
    static member Main
        (
            recentARCs: ARCPointer[],
            onClick: ARCPointer -> unit,
            ?actionbar: ReactElement,
            ?potMaxWidth: int,
            ?onOpenChange: bool -> unit,
            ?debug: bool,
            ?isLoading: bool,
            ?controlRef: IRefValue<SelectorRef>
        ) =

        let debug = defaultArg debug false

        let isOpen, setIsOpen = React.useState (false)

        let setIsOpen =
            fun b ->
                onOpenChange |> Option.iter (fun f -> f b)
                setIsOpen b

        React.useImperativeHandle (
            (unbox controlRef),
            (fun () -> {
                toggle = fun () -> setIsOpen (not isOpen)
            }),
            [| box isOpen |]
        )

        let onClick =
            fun (arcPointer: ARCPointer) ->
                onClick arcPointer
                setIsOpen false

        let activeArc = recentARCs |> Array.tryFind (fun arc -> arc.isActive)

        let RecentARCItems =
            recentARCs
            |> Array.mapi (fun i arcPointer ->
                if debug then
                    Selector.SelectorItem(arcPointer, onClick, testId = $"selector-arc-item-{i}")
                else
                    Selector.SelectorItem(arcPointer, onClick)
            )

        let Toggle =
            match isLoading with
            | Some true ->
                Html.button [
                    prop.className "swt:btn swt:btn-xs swt:btn-outline swt:flex-nowrap swt:cursor-not-allowed"
                    prop.disabled true
                    prop.children [
                        Html.div [
                            prop.className "swt:animate-spin swt:iconify swt:fluent--sync-24-regular swt:size-5"
                        ]
                        Html.span [ prop.text "Loading..." ]
                    ]
                ]
            | _ ->
                Html.button [
                    prop.onClick (fun _ -> setIsOpen (not isOpen))
                    prop.role.button
                    prop.className "swt:btn swt:btn-xs swt:btn-outline swt:flex-nowrap"
                    if debug then
                        prop.testId "selector-test"
                    prop.children [
                        Html.div [
                            match activeArc with
                            | Some arc -> prop.text arc.name
                            | None -> prop.text "Select an ARC"
                        ]
                        Swate.Components.Actionbar.MaterialIcon "swt:fluent--arrow-fit-height-24-regular swt:size-5"
                    ]
                ]

        let Content =
            React.Fragment [
                Html.div [
                    if debug then
                        prop.testId "selector-dropdown-content"
                    prop.children [ React.Fragment RecentARCItems ]
                ]
                match actionbar with
                | Some actionbar ->
                    Html.div [ prop.className "swt:divider swt:m-0! swt:h-3!" ]

                    Html.div [
                        if debug then
                            prop.testId "selector-actionbar"
                        prop.className "swt:w-full"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:justify-center swt:w-full"
                                prop.children actionbar
                            ]
                        ]
                    ]
                | None -> Html.none
            ]

        Dropdown.Main(
            isOpen,
            setIsOpen,
            Toggle,
            Content,
            contentClassName =
                "swt:w-max swt:max-w-none swt:menu swt:bg-base-200 swt:rounded-box swt:z-99 swt:p-2 swt:shadow-sm swt:top-110% swt:menu-sm",
            closeOnClick = false
        )

    [<ReactComponent>]
    static member Entry(?debug: bool) =

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

        let onClick (arcPointer: ARCPointer) =
            let newRecentARCs =
                recentARCs
                |> Array.map (fun arc ->
                    if arc.path = arcPointer.path then
                        { arc with isActive = true }
                    else
                        { arc with isActive = false }
                )

            setRecentARCs newRecentARCs
            console.log ($"Clicked on: {arcPointer.path}")

        Selector.Main(recentARCs, onClick, potMaxWidth = 48, ?debug = debug)

    [<ReactComponent>]
    static member ActionbarInSelectorEntry(?maxNumberActionbar, ?debug: bool) =

        let maxNumberActionbar = defaultArg maxNumberActionbar 3
        let selectorController = React.useRef ({ toggle = fun _ -> () }: SelectorRef)

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

        let closeDropdown () = selectorController.current.toggle ()

        let actionbarButtons = [|
            ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", closeDropdown)
            ButtonInfo.create (
                "swt:fluent--folder-arrow-up-24-regular swt:size-5",
                "Open an existing ARC",
                closeDropdown
            )

            ButtonInfo.create (
                "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
                "Download an existing ARC",
                closeDropdown
            )

            ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", closeDropdown)
            ButtonInfo.create (
                "swt:fluent--folder-arrow-up-24-regular swt:size-5",
                "Open an existing ARC",
                closeDropdown
            )
        |]

        let actionbar = Actionbar.Main(actionbarButtons, maxNumberActionbar, ?debug = debug)

        let onClick (arcPointer: ARCPointer) =
            let newRecentARCs =
                recentARCs
                |> Array.map (fun arc ->
                    if arc.path = arcPointer.path then
                        { arc with isActive = true }
                    else
                        { arc with isActive = false }
                )

            selectorController.current.toggle ()
            setRecentARCs newRecentARCs

            console.log ($"Clicked on: {arcPointer.path}")

        Selector.Main(
            recentARCs,
            onClick,
            potMaxWidth = 48,
            actionbar = actionbar,
            ?debug = debug,
            controlRef = selectorController
        )

    [<ReactComponent>]
    static member NavbarSelectorEntry(onClick, ?maxNumberActionbar, ?debug: bool) =

        let maxNumberActionbar = defaultArg maxNumberActionbar 3


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

        let actionbar = Actionbar.Entry(maxNumberActionbar, ?debug = debug)

        let selector =
            Selector.Main(recentARCs, onClick, potMaxWidth = 48, actionbar = actionbar, ?debug = debug)

        Navbar.Main(selector, ?debug = debug)