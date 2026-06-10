namespace Swate.Components.Page.ARCObjectExplorer

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.Select
open Swate.Components.Primitive.Select.Types
open Swate.Components.Primitive.ComboBox
open Swate.Components.Primitive.Navbar
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Page.ARCObjectExplorer.Types

module private ARCObjectWidgetHelper =

    let widgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-[72rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    let iconClassName (baseClasses: string list, item: FileItem) = [
        yield! baseClasses
        yield item.Icon |> FileItemIcon.className
        yield! item.IconTone |> Option.map FileItemIconTone.className |> Option.toList
    ]

    let rec private tryFindItemWithAncestors (itemId: string, items: FileItem list) =
        let rec loop (ancestorsRev: FileItem list) (currentItems: FileItem list) =
            currentItems
            |> List.tryPick (fun item ->
                if item.Id = itemId then
                    Some(item, List.rev ancestorsRev)
                else
                    item.Children |> Option.bind (loop (item :: ancestorsRev))
            )

        loop [] items

    let private shouldShowInExplorer (item: FileItem) =
        item.Selectable && item.ItemType <> "empty"

    let private collectVisibleDescendants (selectedItem: FileItem) =
        let rec loop (visibleAncestorsRev: string list) (currentItems: FileItem list) =
            currentItems
            |> List.collect (fun item ->
                let shouldShow = shouldShowInExplorer item

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

                current @ loop nextVisibleAncestorsRev (item.Children |> Option.defaultValue [])
            )

        selectedItem.Children |> Option.defaultValue [] |> loop []

    let private contextItems (ancestors: FileItem list, selectedItem: FileItem) =
        (ancestors @ [ selectedItem ])
        |> List.map (fun item -> {
            Item = item
            IsCurrent = item.Id = selectedItem.Id
        })

    let private fallbackSectionLabel (depth: int) =
        match depth with
        | 0 -> "Current"
        | 1 -> "Children"
        | 2 -> "Grandchildren"
        | depth -> $"Level {depth}"

    let private sectionObjectLabel (depth: int, items: ARCObjectExplorerVisibleItem list) =
        let labels =
            items
            |> List.map (fun item -> item.Item.ItemType)
            |> List.filter (fun itemType -> System.String.IsNullOrWhiteSpace itemType |> not)
            |> List.distinct

        match labels with
        | [] -> fallbackSectionLabel depth
        | labels -> String.concat " / " labels

    let private sectionDescription (depth: int, objectLabel: string) =
        match depth with
        | 0 -> $"Selected {objectLabel} object."
        | 1 -> $"Visible {objectLabel} objects directly under the selected ARC object."
        | _ -> $"Visible {objectLabel} objects at this hierarchy level."

    let private sections (selectedItem: FileItem, descendants: ARCObjectExplorerVisibleItem list) =
        if List.isEmpty descendants then
            let label =
                sectionObjectLabel (
                    0,
                    [
                        {
                            Item = selectedItem
                            Depth = 0
                            Lineage = []
                        }
                    ]
                )

            [
                {
                    Label = label
                    Description = sectionDescription (0, label)
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
                let label = sectionObjectLabel (depth, items)

                {
                    Label = label
                    Description = sectionDescription (depth, label)
                    Items = items
                }
            )

    let getExplorerItems (selectedId: string option, items: FileItem list) =
        selectedId
        |> Option.bind (fun itemId -> tryFindItemWithAncestors (itemId, items))
        |> Option.map (fun (selectedItem, ancestors) ->
            let descendants = collectVisibleDescendants selectedItem

            {
                SourceName = selectedItem.Name
                SourceId = selectedItem.Id
                ContextItems = contextItems (ancestors, selectedItem)
                Sections = sections (selectedItem, descendants)
            }
        )

module ARCObjectWidgetData =

    let getExplorerItems (selectedId: string option, items: FileItem list) =
        ARCObjectWidgetHelper.getExplorerItems (selectedId, items)

[<Erase; Mangle(false)>]
type ARCObjectWidget =

    [<ReactComponent>]
    static member private KindFilterTrigger(kindFilterOptions: SelectItem<string>[], selectedKindIndices: Set<int>) =
        let selectedLabels =
            selectedKindIndices
            |> Seq.sort
            |> Seq.choose (fun index ->
                kindFilterOptions
                |> Array.tryItem index
                |> Option.map (fun option -> option.label)
            )
            |> Array.ofSeq

        let summary =
            if selectedLabels.Length = 0 then
                "No kinds"
            elif selectedLabels.Length = kindFilterOptions.Length then
                "All kinds"
            else
                let truncated = selectedLabels |> Array.truncate 2 |> String.concat ", "

                if selectedLabels.Length > 2 then
                    $"{truncated} +{selectedLabels.Length - 2}"
                else
                    truncated

        Html.button [
            prop.title "Filter visible ARC object kinds"
            prop.type'.button
            prop.tabIndex -1
            prop.className "swt:btn swt:btn-sm swt:btn-outline swt:gap-2 swt:pointer-events-none swt:max-w-[18rem]"
            prop.children [
                Icons.Filter("swt:size-4")
                Html.span [
                    prop.className "swt:max-w-48 swt:truncate"
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

        let visibleItems =
            if showInitialResults then
                items |> Array.truncate 10
            else
                items

        let filterFn =
            fun (props: {| item: 'a; search: string |}) ->
                let search = props.search.Trim()

                if System.String.IsNullOrWhiteSpace search then
                    true
                else
                    (itemToString props.item).IndexOf(search, System.StringComparison.OrdinalIgnoreCase)
                    >= 0

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
                    onSelect item
                ),
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
                        data.setActiveIndex None
                ),
            noResultsRenderer =
                (fun () ->
                    Html.li [
                        prop.className "swt:p-4 swt:text-sm swt:opacity-70"
                        prop.text "No matching ARC objects found."
                    ]
                )
        )

    [<ReactComponent>]
    static member SearchActionForExplorerItems
        (items: (string * string option * FileItem)[], onSelectItem: FileItem -> unit, ?placeholder: string)
        =
        ARCObjectWidget.SearchAction(
            items,
            (fun (name, _, _) -> name),
            (fun (_, _, item) -> onSelectItem item),
            itemSubtitle = (fun (_, subtitle, _) -> subtitle),
            ?placeholder = placeholder
        )

    [<ReactComponent>]
    static member Navbar
        (
            selectedTitle: string,
            selectedSubtitle: string,
            kindFilterOptions: SelectItem<string>[],
            selectedKindIndices: Set<int>,
            setSelectedKindIndices: Set<int> -> unit,
            ?rightActions: ReactElement
        ) =

        Html.div [
            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100"
            prop.children [
                Swate.Components.Primitive.Navbar.Navbar.Main(
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
                    right =
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-end swt:gap-2"
                            prop.children [
                                Select.Select(
                                    kindFilterOptions,
                                    selectedKindIndices,
                                    setSelectedKindIndices,
                                    triggerRenderFn =
                                        (fun _ ->
                                            ARCObjectWidget.KindFilterTrigger(kindFilterOptions, selectedKindIndices)
                                        ),
                                    middleware = [|
                                        FloatingUI.Middleware.flip ()
                                        FloatingUI.Middleware.shift ()
                                        FloatingUI.Middleware.offset (4)
                                    |],
                                    dropdownPlacement = FloatingUI.Placement.BottomEnd
                                )

                                match rightActions with
                                | Some actions -> actions
                                | None -> Html.none
                            ]
                        ]
                )
            ]
        ]

    [<ReactComponent>]
    static member ExplorerContent
        (
            items: FileItem list,
            ?selectedItemId: string,
            ?onItemClick: FileItem -> unit,
            ?tileIconSizeClass: string,
            ?contextIconSizeClass: string,
            ?compactEntityTiles: bool,
            ?stickyContextBreadcrumb: bool
        ) =
        let onItemClick = defaultArg onItemClick ignore
        let tileIconSizeClass = defaultArg tileIconSizeClass "swt:text-2xl"
        let contextIconSizeClass = defaultArg contextIconSizeClass "swt:text-base"
        let compactEntityTiles = defaultArg compactEntityTiles false
        let stickyContextBreadcrumb = defaultArg stickyContextBreadcrumb false
        let explorerItems = ARCObjectWidgetHelper.getExplorerItems (selectedItemId, items)

        let itemTypeLabel (item: FileItem) =
            if System.String.IsNullOrWhiteSpace item.ItemType then
                "Object"
            else
                item.ItemType

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

            let tileMinHeightClass =
                if compactEntityTiles then
                    "swt:min-h-16"
                else
                    "swt:min-h-20"

            Html.button [
                prop.type'.button
                prop.className [
                    $"swt:flex swt:w-full swt:min-w-0 swt:flex-col swt:items-center swt:justify-center swt:gap-2 swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-2 {tileMinHeightClass} swt:text-center swt:transition-colors hover:swt:border-primary/60 hover:swt:bg-base-200/60"
                    if isCurrentTarget then
                        "swt:border-primary swt:bg-primary/10"
                ]
                prop.onClick (fun _ -> onItemClick item)
                prop.children [
                    Html.i [
                        prop.className (
                            ARCObjectWidgetHelper.iconClassName ([ "swt:iconify"; tileIconSizeClass ], item)
                        )
                    ]
                    Html.div [
                        prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-1 swt:w-full"
                        prop.children [
                            Html.span [
                                prop.className "swt:text-sm swt:font-medium swt:truncate"
                                prop.text item.Name
                            ]
                            Html.span [
                                prop.className "swt:text-xs swt:opacity-60 swt:truncate"
                                prop.text (tileSubtitle sourceName entry)
                            ]
                        ]
                    ]
                ]
            ]

        let contextItem (entry: ARCObjectExplorerContextItem) =
            let sharedChildren = [
                Html.i [
                    prop.className (
                        ARCObjectWidgetHelper.iconClassName ([ "swt:iconify"; contextIconSizeClass ], entry.Item)
                    )
                ]
                Html.div [
                    prop.className "swt:flex swt:min-w-0 swt:flex-col swt:leading-tight swt:text-left"
                    prop.children [
                        Html.span [
                            prop.className
                                "swt:text-[0.65rem] swt:uppercase swt:tracking-wide swt:opacity-60 swt:truncate"
                            prop.text (itemTypeLabel entry.Item)
                        ]
                        Html.span [ prop.className "swt:truncate"; prop.text entry.Item.Name ]
                    ]
                ]
            ]

            if entry.Item.Selectable then
                Html.button [
                    prop.type'.button
                    prop.className [
                        "swt:flex swt:min-w-0 swt:max-w-[14rem] swt:flex-none swt:items-center swt:gap-2 swt:rounded-full swt:border swt:px-3 swt:py-2 swt:text-sm swt:transition-colors"
                        if entry.IsCurrent then
                            "swt:border-primary swt:bg-primary/10"
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
                            "swt:border-primary swt:bg-primary/10"
                        else
                            "swt:border-base-300 swt:bg-base-100 swt:opacity-80"
                    ]
                    prop.children sharedChildren
                ]

        let sectionView sourceName sourceId (section: ARCObjectExplorerSection) =
            let tileGridClass =
                if compactEntityTiles then
                    "swt:grid swt:grid-cols-[repeat(auto-fill,minmax(8rem,1fr))] swt:gap-2 swt:overflow-x-hidden swt:pb-2"
                else
                    "swt:grid swt:grid-cols-[repeat(auto-fill,minmax(10rem,1fr))] swt:gap-2 swt:overflow-x-hidden swt:pb-2"

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
                        prop.className tileGridClass
                        prop.children [
                            for visibleItem in section.Items do
                                iconTile sourceName visibleItem (visibleItem.Item.Id = sourceId)
                        ]
                    ]
                ]
            ]

        Html.div [
            prop.className
                "swt:flex swt:flex-col swt:gap-4 swt:h-full swt:min-h-0 swt:min-w-0 swt:overflow-y-auto swt:overflow-x-hidden"
            prop.children [
                match explorerItems with
                | Some explorerItems ->
                    let selectedContextItem =
                        explorerItems.ContextItems |> List.tryFind (fun entry -> entry.IsCurrent)

                    let contextContainerClasses = [
                        "swt:flex swt:flex-col swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-4"
                        if stickyContextBreadcrumb then
                            "swt:sticky swt:top-0 swt:z-10 swt:pb-2 swt:gap-2"
                        else
                            "swt:gap-3"
                    ]

                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-4"
                        prop.children [
                            Html.div [
                                prop.className contextContainerClasses
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex swt:flex-col swt:gap-1"
                                        prop.children [
                                            match selectedContextItem with
                                            | Some selectedItem ->
                                                Html.div [
                                                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className
                                                                "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                                            prop.text "Selected"
                                                        ]
                                                        Html.span [
                                                            prop.className "swt:badge swt:badge-sm swt:badge-outline"
                                                            prop.text (itemTypeLabel selectedItem.Item)
                                                        ]
                                                        Html.span [
                                                            prop.className
                                                                "swt:text-sm swt:font-semibold swt:wrap-break-word"
                                                            prop.text selectedItem.Item.Name
                                                        ]
                                                    ]
                                                ]
                                            | None -> Html.none
                                            if not stickyContextBreadcrumb then
                                                Html.p [
                                                    prop.className "swt:text-sm swt:opacity-70"
                                                    prop.text
                                                        "Follow the parent chain left to right to understand where the current ARC object sits in the visible hierarchy."
                                                ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                        prop.children [
                                            for index, contextEntry in explorerItems.ContextItems |> List.indexed do
                                                if index > 0 then
                                                    Html.span [
                                                        prop.className
                                                            "swt:flex swt:flex-none swt:items-center swt:opacity-40"
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
    static member Main
        (?navbar: ReactElement, ?treePane: ReactElement, ?explorerPane: ReactElement, ?detailsPane: ReactElement)
        =
        Html.div [
            prop.className ARCObjectWidgetHelper.widgetContainerClass
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
                            prop.text
                                "Select an object in the tree to inspect its parent chain and visible descendants, or the selected object itself when it is a leaf."
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
                        ARCObjectPanel.Main("", ?content = explorerPane)
                        ARCObjectPanel.Main("ARC Object Details", ?content = detailsPane)
                    ]
                ]
            ]
        ]
