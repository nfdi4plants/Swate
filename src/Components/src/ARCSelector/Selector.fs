namespace Swate.Components

open Feliz
open Fable.Core

open SelectorTypes
open Swate.Components.Types.Actionbar

module SelectorHelper =

    let normalizePath = PathHelpers.normalizePath

    let comparePaths =
        fun (path1: string) (path2: string) -> normalizePath path1 = normalizePath path2

[<Erase; Mangle(false); ReactComponent>]
type Selector =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SelectorItem
        (
            arcPointer: ARCPointer,
            onClick: ARCPointer -> unit,
            ?rmvRecentArc: ARCPointer -> unit,
            ?isCurrentlyOpenArcPath: bool,
            ?potMaxWidth,
            ?testId: string
        ) =

        let maxWidth = defaultArg potMaxWidth 48

        Html.li [
            prop.key arcPointer.path
            prop.className [
                "swt:menu-item"
                if arcPointer.isActive then
                    "swt:font-bold"
            ]
            if testId.IsSome then
                prop.testId testId.Value
            prop.children [
                Html.div [
                    prop.className "swt:flex"
                    prop.children [
                        Html.span [
                            prop.className "swt:truncate swt:block swt:min-w-30"
                            prop.style [ style.maxWidth maxWidth ]
                            prop.text arcPointer.name
                            prop.title arcPointer.path
                        ]
                        Html.div [
                            prop.className "swt:ml-auto swt:flex swt:items-center"
                            prop.children [
                                Html.div [
                                    prop.className "swt:divider swt:divider-horizontal swt:mx-0!"
                                ]
                                match rmvRecentArc with
                                | Some rmvRecentArc ->

                                    Html.button [
                                        prop.className [
                                            "swt:btn swt:btn-ghost swt:btn-square swt:btn-xs"
                                            "swt:hover:btn-error"
                                        ]
                                        prop.onClick (fun e ->
                                            e.stopPropagation ()
                                            rmvRecentArc arcPointer
                                        )
                                        prop.children [
                                            Html.i [
                                                prop.className "swt:iconify swt:fluent--delete-12-regular swt:size-4"
                                            ]
                                        ]
                                    ]
                                | None -> Html.none
                                Html.i [
                                    prop.className [
                                        "swt:iconify swt:fluent--checkmark-24-regular swt:size-4"
                                        match isCurrentlyOpenArcPath with
                                        | Some true -> ""
                                        | _ -> "swt:invisible"
                                    ]
                                ]
                            ]
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
            ?rmvRecentArc: ARCPointer -> unit,
            ?actionbar: ReactElement,
            ?potMaxWidth: int,
            ?onOpenChange: bool -> unit,
            ?debug: bool,
            ?isLoading: bool,
            ?currentlyOpenArcPath: string,
            ?controlRef: IRefValue<SelectorRef>
        ) =

        let debug = defaultArg debug false

        let isOpen, setIsOpen = React.useState (false)

        let currentlyOpenArcName =
            currentlyOpenArcPath
            |> Option.map (fun path ->
                let normalized = SelectorHelper.normalizePath path

                let name =
                    normalized.Split([| "/" |], System.StringSplitOptions.RemoveEmptyEntries)
                    |> Array.last

                name
            )

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

        let RecentARCItems =
            recentARCs
            |> Array.mapi (fun i arcPointer ->
                let isCurrentlyOpenArcPath =
                    currentlyOpenArcPath
                    |> Option.exists (fun path -> SelectorHelper.comparePaths path arcPointer.path)

                let testId = if debug then Some $"selector-arc-item-{i}" else None

                Selector.SelectorItem(
                    arcPointer,
                    onClick,
                    ?testId = testId,
                    isCurrentlyOpenArcPath = isCurrentlyOpenArcPath,
                    ?rmvRecentArc = rmvRecentArc
                )
            )

        let Toggle =
            match isLoading with
            | Some true ->
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-outline swt:flex-nowrap swt:cursor-not-allowed"
                    prop.disabled true
                    prop.children [
                        Html.span [
                            prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                        ]
                        Html.span [ prop.text "Loading..." ]
                    ]
                ]
            | _ ->
                Html.button [
                    prop.onClick (fun _ -> setIsOpen (not isOpen))
                    prop.role.button
                    prop.className "swt:btn swt:btn-sm swt:btn-outline swt:flex-nowrap"
                    if debug then
                        prop.testId "selector-test"
                    prop.children [
                        Html.div [
                            match currentlyOpenArcName with
                            | Some name -> prop.text name
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
                    match RecentARCItems with
                    | [||] ->
                        prop.children [
                            Html.li [
                                prop.className "swt:text-sm swt:text-base-content/80 swt:px-8 swt:py-2 swt:text-center"
                                prop.text "No recent ARCs"
                            ]
                        ]
                    | _ -> prop.children RecentARCItems
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
    static member Entry(?maxNumberActionbar, ?debug: bool) =

        let maxNumberActionbar = defaultArg maxNumberActionbar 3
        let selectorController = React.useRef ({ toggle = fun _ -> () }: SelectorRef)

        let currentlyOpenArcPath, setCurrentlyOpenArcPath =
            React.useState (None: string option)

        let testRecentARCs = [|
            ARCPointer.create ("Test 1", "/Here/Test 1", false)
            ARCPointer.create ("Test 2", "/Here/Test 2", false)
            ARCPointer.create ("Test 3", "/Here/Test 3", false)
            ARCPointer.create (
                "Test jfcesjföisjyfnwjtiewhroiajlkfnnalkfjwarkoiewfanflkndslkfjwiajofkcmscnskjfafdölmsalknoisjfamlkcnkj<ycwaklfnewjföosajö",
                "/Here/Here/Here/Here",
                false
            )
        |]

        let recentARCs, setRecentARCs = React.useState (testRecentARCs)

        let closeDropdown =
            fun (_: Browser.Types.MouseEvent) -> selectorController.current.toggle ()

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
            setCurrentlyOpenArcPath (Some arcPointer.path)

            console.log ($"Clicked on: {arcPointer.path}")

        let rmvRecentArc (arcPointer: ARCPointer) =
            let newRecentARCs =
                recentARCs |> Array.filter (fun arc -> arc.path <> arcPointer.path)

            setRecentARCs newRecentARCs

            if currentlyOpenArcPath = Some arcPointer.path then
                setCurrentlyOpenArcPath None

        Selector.Main(
            recentARCs,
            onClick,
            rmvRecentArc,
            potMaxWidth = 48,
            actionbar = actionbar,
            ?currentlyOpenArcPath = currentlyOpenArcPath,
            ?debug = debug,
            controlRef = selectorController
        )
