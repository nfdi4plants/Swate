namespace Swate.Components

open Fable.Core
open Feliz
open Swate.Components.FileExplorerTypes


type ARCObjectWidgetHelper =

    static member TryFindItemAndParent(itemId: string, items: FileItem list) =
        let rec loop (parent: FileItem option) (items: FileItem list) =
            items
            |> List.tryPick (fun item ->
                if item.Id = itemId then
                    Some(item, parent)
                else
                    item.Children |> Option.bind (loop (Some item)))

        loop None items

    static member GetExplorerItems(selectedId: string option, items: FileItem list) =
        selectedId
        |> Option.bind (fun itemId -> ARCObjectWidgetHelper.TryFindItemAndParent(itemId, items))
        |> Option.map (fun (selectedItem, _parentItem) ->
            let children = selectedItem.Children |> Option.defaultValue []

            if List.isEmpty children then
                ("Current", selectedItem.Name, selectedItem.Id, [ selectedItem ])
            else
                ("Children", selectedItem.Name, selectedItem.Id, children))

[<Erase; Mangle(false)>]
type ARCObjectWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-[72rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    static member private KindFilterOptions: SelectItem<string>[] = [|
        {| label = "Study"; item = "Study" |}
        {| label = "Assay"; item = "Assay" |}
        {| label = "Workflow"; item = "Workflow" |}
        {| label = "Run"; item = "Run" |}
        {| label = "Table"; item = "Table" |}
        {| label = "DataMap"; item = "DataMap" |}
        {| label = "Note"; item = "Note" |}
        {| label = "Sample"; item = "Sample" |}
    |]

    static member DefaultKindFilterIndices() =
        ARCObjectWidget.KindFilterOptions
        |> Array.mapi (fun index _ -> index)
        |> Set.ofArray

    static member SelectedKindLabels(selectedKindIndices: Set<int>) =
        selectedKindIndices
        |> Seq.sort
        |> Seq.choose (fun index ->
            ARCObjectWidget.KindFilterOptions
            |> Array.tryItem index
            |> Option.map (fun option -> option.item))
        |> Set.ofSeq

    [<ReactComponent>]
    static member private KindFilterTrigger(selectedKindIndices: Set<int>) =
        let selectedLabels =
            selectedKindIndices
            |> Seq.sort
            |> Seq.choose (fun index ->
                ARCObjectWidget.KindFilterOptions
                |> Array.tryItem index
                |> Option.map (fun option -> option.label))
            |> Array.ofSeq

        let summary =
            if selectedLabels.Length = 0 then
                "No kinds"
            elif selectedLabels.Length = ARCObjectWidget.KindFilterOptions.Length then
                "All kinds"
            else
                let truncated =
                    selectedLabels
                    |> Array.truncate 2
                    |> String.concat ", "

                if selectedLabels.Length > 2 then
                    $"{truncated} +{selectedLabels.Length - 2}"
                else
                    truncated

        Html.button [
            prop.title "Filter visible ARC object kinds"
            prop.type'.button
            prop.tabIndex -1
            prop.className
                "swt:btn swt:btn-sm swt:btn-outline swt:gap-2 swt:pointer-events-none swt:max-w-[18rem]"
            prop.children [
                Icons.Filter("swt:size-4")
                Html.span [
                    prop.className "swt:max-w-[12rem] swt:truncate"
                    prop.text summary
                ]
            ]
        ]

    [<ReactComponent>]
    static member SearchAction<'a>
        (
            items: 'a[],
            itemToString: 'a -> string,
            onSelect: 'a -> unit,
            ?itemSubtitle: 'a -> string option,
            ?placeholder: string
        ) =
        let itemSubtitle = defaultArg itemSubtitle (fun _ -> None)
        let placeholder = defaultArg placeholder "Search visible objects..."
        let inputValue, setInputValue = React.useState ""
        let showInitialResults = System.String.IsNullOrWhiteSpace inputValue
        let visibleItems = if showInitialResults then items |> Array.truncate 10 else items

        let filterFn =
            fun (props: {| item: 'a; search: string |}) ->
                let search = props.search.Trim()

                if System.String.IsNullOrWhiteSpace search then
                    true
                else
                    (itemToString props.item).IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0

        let itemRenderer =
            fun
                (props:
                    {|
                        item: 'a
                        index: int
                        isActive: bool
                        props: ResizeArray<IReactProperty>
                    |}) ->
                let subtitle = itemSubtitle props.item

                Html.li [
                    prop.className [
                        "swt:border-l-4 swt:border-transparent swt:list-row swt:rounded-none swt:p-2"
                        if props.isActive then
                            "swt:border-primary! swt:bg-base-content/10"
                    ]
                    prop.role.option
                    prop.ariaSelected props.isActive
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:min-w-0"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-sm swt:font-medium swt:truncate"
                                    prop.text (itemToString props.item)
                                ]
                                match subtitle with
                                | Some subtitle ->
                                    Html.span [
                                        prop.className "swt:text-xs swt:opacity-60 swt:truncate"
                                        prop.text subtitle
                                    ]
                                | None -> Html.none
                            ]
                        ]
                    ]
                    yield! props.props
                ]

        ComboBox.ComboBox<'a>(
            inputValue,
            setInputValue,
            visibleItems,
            filterFn,
            itemToString,
            onChange =
                (fun _ item ->
                    setInputValue ""
                    onSelect item),
            placeholder = placeholder,
            inputLeadingVisual = Icons.MagnifyingClass("swt:opacity-60"),
            labelClassName = "swt:w-72 swt:max-w-[42vw] swt:input-sm",
            itemRenderer = itemRenderer,
            onClick =
                (fun _ data ->
                    data.setIsOpen true

                    if visibleItems.Length > 0 then
                        data.setActiveIndex (Some 0)
                    else
                        data.setActiveIndex None),
            noResultsRenderer =
                (fun () ->
                    Html.li [
                        prop.className "swt:p-4 swt:text-sm swt:opacity-70"
                        prop.text "No matching ARC objects found."
                    ])
        )

    [<ReactComponent>]
    static member Navbar
        (
            selectedTitle: string,
            selectedSubtitle: string,
            selectedKindIndices: Set<int>,
            setSelectedKindIndices: Set<int> -> unit,
            ?rightActions: ReactElement
        ) =

        Html.div [
            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100"
            prop.children [
                Swate.Components.Navbar.Main(
                    left =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:justify-center swt:min-w-0"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                    prop.text selectedSubtitle
                                ]
                                Html.span [
                                    prop.className "swt:text-sm swt:font-medium swt:truncate"
                                    prop.text selectedTitle
                                ]
                            ]
                        ],
                    middle =
                        Html.div [
                            prop.className "swt:flex swt:w-full swt:items-center swt:justify-center swt:gap-2"
                            prop.children [
                                Select.Select(
                                    ARCObjectWidget.KindFilterOptions,
                                    selectedKindIndices,
                                    setSelectedKindIndices,
                                    triggerRenderFn = (fun _ -> ARCObjectWidget.KindFilterTrigger(selectedKindIndices)),
                                    middleware = [|
                                        FloatingUI.Middleware.flip ()
                                        FloatingUI.Middleware.shift ()
                                        FloatingUI.Middleware.offset (4)
                                    |],
                                    dropdownPlacement = FloatingUI.Placement.BottomEnd
                                )
                            ]
                        ],
                    right =
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-end swt:gap-2"
                            prop.children [
                                match rightActions with
                                | Some actions -> actions
                                | None -> Html.none
                            ]
                        ]
                )
            ]
        ]

    [<ReactComponent>]
    static member ExplorerContent(items: FileItem list, ?selectedItemId: string, ?onItemClick: FileItem -> unit) =
        let onItemClick = defaultArg onItemClick ignore
        let explorerItems = ARCObjectWidgetHelper.GetExplorerItems(selectedItemId, items)

        let iconTile (subtitle: string) (item: FileItem) isCurrentTarget =
            Html.button [
                prop.type'.button
                prop.className [
                    "swt:flex swt:flex-col swt:items-center swt:justify-center swt:gap-3 swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:min-h-28 swt:text-center swt:transition-colors hover:swt:border-primary/60 hover:swt:bg-base-200/60"
                    if isCurrentTarget then "swt:border-primary swt:bg-primary/10"
                ]
                prop.onClick (fun _ -> onItemClick item)
                prop.children [
                    Html.i [
                        prop.className [ "swt:iconify swt:text-4xl swt:text-primary"; item.IconPath ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [ prop.className "swt:text-sm swt:font-medium"; prop.text item.Name ]
                            Html.span [ prop.className "swt:text-xs swt:opacity-60"; prop.text subtitle ]
                        ]
                    ]
                ]
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-4 swt:h-full swt:overflow-auto"
            prop.children [
                match explorerItems with
                | Some(relationLabel, sourceName, sourceId, visibleItems) ->
                    let tileSubtitle =
                        match relationLabel with
                        | "Children" -> $"Child of {sourceName}"
                        | "Current" -> "Selected object"
                        | _ -> sourceName

                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [
                                prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text relationLabel
                            ]
                            Html.h4 [ prop.className "swt:text-lg swt:font-semibold"; prop.text sourceName ]
                            Html.p [
                                prop.className "swt:text-sm swt:opacity-70"
                                prop.text (
                                    match relationLabel with
                                    | "Children" -> "Direct children of the selected ARC object."
                                    | "Current" -> "The selected ARC object has no children, so it is shown directly."
                                    | _ -> "Current selection."
                                )
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:grid swt:grid-cols-2 swt:xl:grid-cols-3 swt:gap-3"
                        prop.children [
                            for item in visibleItems do
                                iconTile tileSubtitle item (item.Id = sourceId)
                        ]
                    ]
                | None ->
                    Html.div [
                        prop.className
                            "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                        prop.children [
                            Html.p [
                                prop.className "swt:text-sm swt:text-center swt:opacity-70"
                                prop.text "Select an ARC object in the tree to explore its nearby objects."
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main(?navbar: ReactElement, ?treePane: ReactElement, ?explorerPane: ReactElement, ?detailsPane: ReactElement) =
        Html.div [
            prop.className ARCObjectWidget.WidgetContainerClass
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-base swt:font-bold"
                            prop.text "ARC Object Widget"
                        ]
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text "Select an object in the tree to inspect its children, or the selected object itself when it is a leaf."
                        ]
                    ]
                ]
                match navbar with
                | Some navbar -> navbar
                | None -> Html.none
                Html.div [
                    prop.className
                        "swt:grid swt:grid-cols-1 swt:lg:grid-cols-[minmax(16rem,20rem)_minmax(0,1fr)_minmax(14rem,18rem)] swt:gap-3 swt:flex-1 swt:min-h-0"
                    prop.children [
                        ARCObjectPanel.Main("ARC Object Tree", ?content = treePane)
                        ARCObjectPanel.Main("ARC Object Explorer", ?content = explorerPane)
                        ARCObjectPanel.Main("ARC Object Details", ?content = detailsPane)
                    ]
                ]
            ]
        ]
