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
                                    prop.title arcPointer.name
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
            onARCClick: ARCPointer -> unit,
            ?actionbar: (unit -> unit) -> ReactElement,
            ?potMaxWidth: int,
            ?onOpenSelector,
            ?debug: bool
        ) =

        let debug = defaultArg debug false

        let isOpen, setOpen = React.useState (false)

        let onOpenSelector shallBeOpen =
            setOpen shallBeOpen
            onOpenSelector
            |> Option.iter(fun f -> f())

        let actionbar =
            actionbar
            |> Option.map (fun actions -> actions (fun () -> onOpenSelector false))
            |> Option.defaultValue Html.none

        let onARCClick arcPointer =
            onOpenSelector false
            onARCClick arcPointer

        let recentARCElements =
            arcPointers
            |> Array.map (fun arcPointer -> Selector.SelectorItem(arcPointer, onARCClick))

        let dropDownSwitch =
            React.useMemo (
                (fun _ ->
                    Html.button [
                        prop.onClick (fun _ ->
                            onOpenSelector (not isOpen)
                        )
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

        Dropdown.Main(isOpen, setOpen, dropDownSwitch, React.Fragment [ React.Fragment recentARCElements; actionbar ])

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

        let onARCClick (arcPointer:ARCPointer) =
            console.log($"arcPointer: {arcPointer.path}")

        Selector.Main(recentARCs, onARCClick, maxNumber, potMaxWidth = 48, ?debug = debug)

    [<ReactComponent>]
    static member ActionbarInSelectorEntry(maxNumber, ?maxNumberActionbar, ?debug: bool) =

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

        let actionbar = fun _ -> Actionbar.Entry(maxNumberActionbar)

        let onARCClick (arcPointer:ARCPointer) =
            console.log($"arcPointer: {arcPointer.path}")

        Selector.Main(recentARCs, onARCClick, potMaxWidth = 48, actionbar = actionbar, ?debug = debug)

    [<ReactComponent>]
    static member NavbarSelectorEntry(?maxNumberActionbar, ?debug: bool) =

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

        let actionbar = fun _ -> Actionbar.Entry(maxNumberActionbar, ?debug = debug)

        let onARCClick (arcPointer:ARCPointer) =
            console.log($"arcPointer: {arcPointer.path}")

        let selector =
            Selector.Main(recentARCs, onARCClick, potMaxWidth = 48, actionbar = actionbar, ?debug = debug)

        Navbar.Main(selector, ?debug = debug)