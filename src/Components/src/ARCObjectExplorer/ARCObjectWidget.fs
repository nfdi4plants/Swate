namespace Swate.Components

open Fable.Core
open Feliz
open Swate.Components.FileExplorerTypes

type ARCObjectExplorerVisibleItem = {
    Item: FileItem
    Depth: int
    Lineage: string list
}

type ARCObjectExplorerContextItem = {
    Item: FileItem
    IsCurrent: bool
}

type ARCObjectExplorerSection = {
    Label: string
    Description: string
    Items: ARCObjectExplorerVisibleItem list
}

type ARCObjectExplorerItems = {
    SourceName: string
    SourceId: string
    ContextItems: ARCObjectExplorerContextItem list
    Sections: ARCObjectExplorerSection list
}

type ARCObjectWidgetHelper =

    static member private TryFindItemWithAncestors(itemId: string, items: FileItem list) =
        let rec loop (ancestorsRev: FileItem list) (items: FileItem list) =
            items
            |> List.tryPick (fun item ->
                if item.Id = itemId then
                    Some(item, List.rev ancestorsRev)
                else
                    item.Children |> Option.bind (loop (item :: ancestorsRev)))

        loop [] items

    static member private ShouldShowInExplorer(item: FileItem) =
        item.Selectable && item.ItemType <> "empty"

    static member private CollectVisibleDescendants(selectedItem: FileItem) =
        let rec loop (visibleAncestorsRev: string list) (items: FileItem list) =
            items
            |> List.collect (fun item ->
                let shouldShow = ARCObjectWidgetHelper.ShouldShowInExplorer item

                let current =
                    if shouldShow then
                        [
                            {
                                Item = item
                                Depth = visibleAncestorsRev.Length + 1
                                Lineage = List.rev visibleAncestorsRev
                            }
                        ]
                    else
                        []

                let nextVisibleAncestorsRev =
                    if shouldShow then
                        item.Name :: visibleAncestorsRev
                    else
                        visibleAncestorsRev

                current @ loop nextVisibleAncestorsRev (item.Children |> Option.defaultValue []))

        selectedItem.Children
        |> Option.defaultValue []
        |> loop []

    static member private ContextItems(ancestors: FileItem list, selectedItem: FileItem) =
        (ancestors @ [ selectedItem ])
        |> List.map (fun item -> {
            Item = item
            IsCurrent = item.Id = selectedItem.Id
        })

    static member private FallbackSectionLabel(depth: int) =
        match depth with
        | 0 -> "Current"
        | 1 -> "Children"
        | 2 -> "Grandchildren"
        | depth -> $"Level {depth}"

    static member private SectionObjectLabel(depth: int, items: ARCObjectExplorerVisibleItem list) =
        let labels =
            items
            |> List.map (fun item -> item.Item.ItemType)
            |> List.filter (fun itemType -> System.String.IsNullOrWhiteSpace itemType |> not)
            |> List.distinct

        match labels with
        | [] -> ARCObjectWidgetHelper.FallbackSectionLabel depth
        | labels -> String.concat " / " labels

    static member private SectionDescription(depth: int, objectLabel: string) =
        match depth with
        | 0 -> $"Selected {objectLabel} object."
        | 1 -> $"Visible {objectLabel} objects directly under the selected ARC object."
        | _ -> $"Visible {objectLabel} objects at this hierarchy level."

    static member private Sections(selectedItem: FileItem, descendants: ARCObjectExplorerVisibleItem list) =
        if List.isEmpty descendants then
            let label = ARCObjectWidgetHelper.SectionObjectLabel(0, [
                {
                    Item = selectedItem
                    Depth = 0
                    Lineage = []
                }
            ])

            [
                {
                    Label = label
                    Description = ARCObjectWidgetHelper.SectionDescription(0, label)
                    Items = [
                        {
                            Item = selectedItem
                            Depth = 0
                            Lineage = []
                        }
                    ]
                }
            ]
        else
            descendants
            |> List.groupBy (fun item -> item.Depth)
            |> List.sortBy fst
            |> List.map (fun (depth, items) ->
                let label = ARCObjectWidgetHelper.SectionObjectLabel(depth, items)

                {
                    Label = label
                    Description = ARCObjectWidgetHelper.SectionDescription(depth, label)
                    Items = items
                })

    static member GetExplorerItems(selectedId: string option, items: FileItem list) =
        selectedId
        |> Option.bind (fun itemId -> ARCObjectWidgetHelper.TryFindItemWithAncestors(itemId, items))
        |> Option.map (fun (selectedItem, ancestors) ->
            let descendants = ARCObjectWidgetHelper.CollectVisibleDescendants selectedItem

            {
                SourceName = selectedItem.Name
                SourceId = selectedItem.Id
                ContextItems = ARCObjectWidgetHelper.ContextItems(ancestors, selectedItem)
                Sections = ARCObjectWidgetHelper.Sections(selectedItem, descendants)
            })

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

        let tileSubtitle sourceName (entry: ARCObjectExplorerVisibleItem) =
            match entry.Depth with
            | 0 -> "Selected object"
            | 1 -> $"Child of {sourceName}"
            | 2 ->
                match entry.Lineage with
                | parent :: _ -> $"Grandchild via {parent}"
                | [] -> $"Grandchild of {sourceName}"
            | depth ->
                match entry.Lineage with
                | [] -> $"Level {depth} descendant"
                | lineage ->
                    let lineageText = String.concat " / " lineage
                    $"Level {depth} via {lineageText}"

        let iconTile sourceName (entry: ARCObjectExplorerVisibleItem) isCurrentTarget =
            let item = entry.Item

            Html.button [
                prop.type'.button
                prop.className [
                    "swt:flex swt:min-w-[15rem] swt:max-w-[18rem] swt:flex-none swt:flex-col swt:items-center swt:justify-center swt:gap-3 swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:min-h-28 swt:text-center swt:transition-colors hover:swt:border-primary/60 hover:swt:bg-base-200/60"
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
                            Html.span [
                                prop.className "swt:text-xs swt:opacity-60"
                                prop.text (tileSubtitle sourceName entry)
                            ]
                        ]
                    ]
                ]
            ]

        let contextItem (entry: ARCObjectExplorerContextItem) =
            let sharedChildren = [
                Html.i [
                    prop.className [ "swt:iconify swt:text-base"; entry.Item.IconPath ]
                ]
                Html.span [
                    prop.className "swt:truncate"
                    prop.text entry.Item.Name
                ]
            ]

            if entry.Item.Selectable then
                Html.button [
                    prop.type'.button
                    prop.className [
                        "swt:flex swt:min-w-0 swt:max-w-[14rem] swt:flex-none swt:items-center swt:gap-2 swt:rounded-full swt:border swt:px-3 swt:py-2 swt:text-sm swt:transition-colors"
                        if entry.IsCurrent then
                            "swt:border-primary swt:bg-primary/10 swt:text-primary"
                        else
                            "swt:border-base-300 swt:bg-base-100 hover:swt:border-primary/60 hover:swt:bg-base-200/60"
                    ]
                    prop.onClick (fun _ -> onItemClick entry.Item)
                    prop.children sharedChildren
                ]
            else
                Html.div [
                    prop.className [
                        "swt:flex swt:min-w-0 swt:max-w-[14rem] swt:flex-none swt:items-center swt:gap-2 swt:rounded-full swt:border swt:px-3 swt:py-2 swt:text-sm"
                        if entry.IsCurrent then
                            "swt:border-primary swt:bg-primary/10 swt:text-primary"
                        else
                            "swt:border-base-300 swt:bg-base-100 swt:opacity-80"
                    ]
                    prop.children sharedChildren
                ]

        let sectionView sourceName sourceId (section: ARCObjectExplorerSection) =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:justify-between swt:gap-3"
                                prop.children [
                                    Html.h5 [
                                        prop.className "swt:text-sm swt:font-semibold swt:uppercase swt:tracking-wide"
                                        prop.text section.Label
                                    ]
                                    Html.span [
                                        prop.className "swt:text-xs swt:opacity-60"
                                        prop.text (
                                            if section.Items.Length = 1 then
                                                "1 object"
                                            else
                                                $"{section.Items.Length} objects"
                                        )
                                    ]
                                ]
                            ]
                            Html.p [
                                prop.className "swt:text-sm swt:opacity-70"
                                prop.text section.Description
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:gap-3 swt:overflow-x-auto swt:pb-2"
                        prop.children [
                            for visibleItem in section.Items do
                                iconTile
                                    sourceName
                                    visibleItem
                                    (visibleItem.Item.Id = sourceId)
                        ]
                    ]
                ]
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-4 swt:h-full swt:overflow-auto"
            prop.children [
                match explorerItems with
                | Some explorerItems ->
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [
                                prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text "Hierarchy"
                            ]
                            Html.h4 [ prop.className "swt:text-lg swt:font-semibold"; prop.text explorerItems.SourceName ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-4"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-col swt:gap-3 swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-4"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex swt:flex-col swt:gap-1"
                                        prop.children [
                                            Html.h5 [
                                                prop.className "swt:text-sm swt:font-semibold swt:uppercase swt:tracking-wide"
                                                prop.text "Context"
                                            ]
                                            Html.p [
                                                prop.className "swt:text-sm swt:opacity-70"
                                                prop.text "Follow the parent chain left to right to understand where the current ARC object sits in the visible hierarchy."
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "swt:flex swt:items-center swt:gap-2 swt:overflow-x-auto swt:pb-1"
                                        prop.children [
                                            for index, contextEntry in explorerItems.ContextItems |> List.indexed do
                                                if index > 0 then
                                                    Html.span [
                                                        prop.className "swt:flex swt:flex-none swt:items-center swt:opacity-40"
                                                        prop.children [ Icons.ChevronRight() ]
                                                    ]

                                                contextItem contextEntry
                                        ]
                                    ]
                                ]
                            ]
                            for index, section in explorerItems.Sections |> List.indexed do
                                if index > 0 then
                                    Html.div [ prop.className "swt:border-t swt:border-base-300" ]

                                sectionView explorerItems.SourceName explorerItems.SourceId section
                        ]
                    ]
                | None ->
                    Html.div [
                        prop.className
                            "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                        prop.children [
                            Html.p [
                                prop.className "swt:text-sm swt:text-center swt:opacity-70"
                                prop.text "Select an ARC object in the tree to explore its visible descendants."
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
                            prop.text "Select an object in the tree to inspect its parent chain and visible descendants, or the selected object itself when it is a leaf."
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
