namespace Swate.Components

open Feliz
open Fable.Core

[<Erase; Mangle(false); ReactComponent>]
type Selector =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SelectorItem(arcPointer: ARCPointer, onARCClick: ARCPointer -> unit, ?potMaxWidth) =

        let maxWidth = defaultArg potMaxWidth 48

        React.useMemo (
            (fun _ ->
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
                                        prop.className
                                            "swt:iconify swt:fluent--checkmark-20-regular swt:size-5 swt:flex-none"
                                    ]
                            ]
                        ]
                    ]
                    prop.onClick (fun _ -> onARCClick (arcPointer))
                ]
            ),
            [| arcPointer.name, arcPointer.path |]
        )

    [<ReactComponent>]
    static member Main
        (
            arcPointers: ARCPointer[],
            setRecentARCs,
            maxNumberRecentElements,
            ?actionbar: ReactElement,
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
            latestARCs
            |> Array.map (fun arcPointer -> Selector.SelectorItem(arcPointer, onARCClick))

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
                            Html.div [ prop.text "Select an ARC" ]
                            Actionbar.MaterialIcon "swt:fluent--arrow-fit-height-24-regular swt:size-5"
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
            ?actionbar = actionbar
        )

    [<ReactComponent>]
    static member Entry(maxNumber, ?debug: bool) =

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

        Selector.Main(recentARCs, setRecentARCs, maxNumber, potMaxWidth = 48, ?debug = debug)

    [<ReactComponent>]
    static member ActionbarInSelectorEntry(maxNumber, ?maxNumberActionbar, ?debug: bool) =

        let maxNumberActionbar = defaultArg maxNumberActionbar 3

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

        let actionbar = Actionbar.Entry(maxNumberActionbar)

        Selector.Main(
            recentARCs,
            setRecentARCs,
            maxNumber,
            potMaxWidth = 48,
            actionbar = actionbar,
            ?debug = debug
        )

    [<ReactComponent>]
    static member NavbarSelectorEntry(maxNumber, ?maxNumberActionbar, ?debug: bool) =

        let maxNumberActionbar = defaultArg maxNumberActionbar 3

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

        let actionbar = Actionbar.Entry(maxNumberActionbar)

        let selector =
            Selector.Main(
                recentARCs,
                setRecentARCs,
                maxNumber,
                potMaxWidth = 48,
                actionbar = actionbar,
                ?debug = debug
            )

        Navbar.Main(selector, ?debug = debug)