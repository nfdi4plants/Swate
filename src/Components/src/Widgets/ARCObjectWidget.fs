namespace Swate.Components

open Fable.Core
open Feliz
open WidgetsLocalStorage
open Swate.Components.FileExplorerTypes

[<Erase; Mangle(false)>]
type ARCObjectWidget =

    static member private StoryPrefix = "ARC_OBJECT_WIDGET"
    static member private StorySize = { X = 1180; Y = 760 }
    static member private StoryPosition = { X = 24; Y = 24 }

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
            ?badgeLabel: string,
            ?titleLabel: string,
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

    static member private TryFindItemAndParent(itemId: string, items: FileItem list) =
        let rec loop (parent: FileItem option) (items: FileItem list) =
            items
            |> List.tryPick (fun item ->
                if item.Id = itemId then
                    Some(item, parent)
                else
                    item.Children |> Option.bind (loop (Some item)))

        loop None items

    static member private FlattenFileItemsWithParent(items: FileItem list) =
        let rec loop (parent: FileItem option) (items: FileItem list) =
            items
            |> List.collect (fun item ->
                (item, parent) :: (item.Children |> Option.defaultValue [] |> loop (Some item)))

        loop None items

    static member private TryGetStoryItemKind(item: FileItem) =
        ARCObjectFixture.StoryMeta
        |> Map.tryFind item.Id
        |> Option.map (fun (_, kind, _, _, _) -> kind)
        |> Option.orElseWith (fun () ->
            if item.Children |> Option.exists (List.isEmpty >> not) then
                Some "Group"
            else
                None)

    static member private FilterStoryItemsByKinds(visibleKinds: Set<string>, items: FileItem list) =
        let rec loop (item: FileItem) =
            let filteredChildren =
                item.Children |> Option.map (List.choose loop)

            let hasVisibleChildren =
                filteredChildren |> Option.exists (List.isEmpty >> not)

            let itemKind = ARCObjectWidget.TryGetStoryItemKind(item)

            let isVisibleKind =
                itemKind |> Option.map visibleKinds.Contains |> Option.defaultValue false

            match itemKind with
            | Some "ARC" ->
                Some { item with Children = filteredChildren }
            | Some "Group" ->
                if hasVisibleChildren then
                    Some { item with Children = filteredChildren }
                else
                    None
            | _ ->
                if isVisibleKind || hasVisibleChildren then
                    Some { item with Children = filteredChildren }
                else
                    None

        items |> List.choose loop

    static member private StorySearchItems(items: FileItem list) =
        ARCObjectWidget.FlattenFileItemsWithParent(items)
        |> List.choose (fun (item, parent) ->
            match ARCObjectWidget.TryGetStoryItemKind(item) with
            | Some "Group" -> None
            | Some kind ->
                let subtitle =
                    ARCObjectFixture.StoryMeta
                    |> Map.tryFind item.Id
                    |> Option.map (fun (_, metaKind, role, _, _) ->
                        let parentPart =
                            parent
                            |> Option.map (fun parentItem -> $"Parent: {parentItem.Name}")
                            |> Option.toList

                        String.concat " | " ([ metaKind; role ] @ parentPart))
                    |> Option.defaultValue kind

                Some(item.Name, Some subtitle, item)
            | None -> None)
        |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
        |> List.toArray

    static member private GetExplorerItems(selectedId: string option, items: FileItem list) =
        selectedId
        |> Option.bind (fun itemId -> ARCObjectWidget.TryFindItemAndParent(itemId, items))
        |> Option.map (fun (selectedItem, _parentItem) ->
            let children = selectedItem.Children |> Option.defaultValue []

            if List.isEmpty children then
                ("Current", selectedItem.Name, selectedItem.Id, [ selectedItem ])
            else
                ("Children", selectedItem.Name, selectedItem.Id, children))

    [<ReactComponent>]
    static member ExplorerContent(items: FileItem list, ?selectedItemId: string, ?onItemClick: FileItem -> unit) =
        let onItemClick = defaultArg onItemClick ignore
        let explorerItems = ARCObjectWidget.GetExplorerItems(selectedItemId, items)

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
    static member private StoryNavbar
        (
            selectedTitle: string,
            selectedSubtitle: string,
            selectedKindIndices: Set<int>,
            setSelectedKindIndices: Set<int> -> unit,
            ?rightActions: ReactElement
        )
        =

        ARCObjectWidget.Navbar(
            selectedTitle,
            selectedSubtitle,
            selectedKindIndices,
            setSelectedKindIndices,
            badgeLabel = "Story",
            titleLabel = "ARC Object Navbar",
            ?rightActions = rightActions
        )

    [<ReactComponent>]
    static member private StoryExample() =
        let selectedId, setSelectedId = React.useState ARCObjectFixture.StoryItemIdStudy
        let selectedKindIndices, setSelectedKindIndices =
            React.useState (ARCObjectWidget.DefaultKindFilterIndices())
        let items =
            React.useMemo ((fun () -> ARCObjectFixture.StoryItems()), [||])

        let visibleKinds = ARCObjectWidget.SelectedKindLabels(selectedKindIndices)

        let filteredItems = ARCObjectWidget.FilterStoryItemsByKinds(visibleKinds, items)

        let visibleSelectedId =
            Some selectedId
            |> Option.filter (fun itemId -> ARCObjectWidget.TryFindItemAndParent(itemId, filteredItems).IsSome)

        let selectedMeta =
            visibleSelectedId
            |> Option.bind (fun itemId -> ARCObjectFixture.StoryMeta |> Map.tryFind itemId)

        let selectedMetadataRows =
            visibleSelectedId
            |> Option.bind (fun itemId -> ARCObjectFixture.StoryMetadataRows |> Map.tryFind itemId)

        let selectedTitle =
            selectedMeta
            |> Option.map (fun (title, _, _, _, _) -> title)
            |> Option.defaultValue "No visible selection"

        let selectedSubtitle =
            selectedMeta
            |> Option.map (fun (_, kind, role, _, _) -> $"{kind} | {role}")
            |> Option.defaultValue "Selection"

        let searchItems = ARCObjectWidget.StorySearchItems(filteredItems)

        let searchAction =
            ARCObjectWidget.SearchAction(
                searchItems,
                (fun (name, _, _) -> name),
                (fun (_, _, item) -> setSelectedId item.Id),
                itemSubtitle = (fun (_, subtitle, _) -> subtitle)
            )

        let navbar =
            ARCObjectWidget.StoryNavbar(
                selectedTitle,
                selectedSubtitle,
                selectedKindIndices,
                setSelectedKindIndices,
                rightActions = searchAction
            )

        let treePane =
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = filteredItems,
                ?selectedItemId = visibleSelectedId,
                onItemClick = (fun item -> setSelectedId item.Id),
                useDirectoryChevronToggle = true
            )

        let explorerPane =
            ARCObjectWidget.ExplorerContent(
                filteredItems,
                ?selectedItemId = visibleSelectedId,
                onItemClick = fun item -> setSelectedId item.Id
            )

        let detailsPane =
            match selectedMeta with
            | Some(_, kind, role, previewTarget, description) ->
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                    prop.children [
                        Html.div [
                            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-200/40 swt:p-3"
                            prop.children [
                                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Properties" ]
                                Html.dl [
                                    prop.className "swt:grid swt:grid-cols-[auto_1fr] swt:gap-x-3 swt:gap-y-2 swt:text-sm"
                                    prop.children [
                                        Html.dt [ prop.className "swt:font-medium"; prop.text "Kind" ]
                                        Html.dd kind
                                        Html.dt [ prop.className "swt:font-medium"; prop.text "Role" ]
                                        Html.dd role
                                        Html.dt [ prop.className "swt:font-medium"; prop.text "Preview" ]
                                        Html.dd previewTarget
                                    ]
                                ]
                            ]
                        ]
                        match selectedMetadataRows with
                        | Some rows ->
                            Html.div [
                                prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                                prop.children [
                                    Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Metadata" ]
                                    Html.dl [
                                        prop.className "swt:grid swt:grid-cols-[auto_1fr] swt:gap-x-3 swt:gap-y-2 swt:text-sm"
                                        prop.children [
                                            for label, value in rows do
                                                Html.dt [ prop.className "swt:font-medium"; prop.text label ]
                                                Html.dd [
                                                    prop.className "swt:break-words"
                                                    prop.text value
                                                ]
                                        ]
                                    ]
                                ]
                            ]
                        | None -> Html.none
                        Html.div [
                            prop.className "swt:flex-1 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                            prop.children [
                                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Notes" ]
                                Html.p [ prop.className "swt:text-sm swt:opacity-80"; prop.text description ]
                            ]
                         ]
                     ]
                 ]
            | None ->
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                    prop.children [
                        Html.div [
                            prop.className
                                "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                            prop.children [
                                Html.p [
                                    prop.className "swt:text-sm swt:text-center swt:opacity-70"
                                    prop.text "The current selection is hidden by the active kind filter. Adjust the filter or pick a visible ARC object."
                                ]
                            ]
                        ]
                    ]
                ]

        ARCObjectWidget.Main(navbar = navbar, treePane = treePane, explorerPane = explorerPane, detailsPane = detailsPane)

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
                        ARCObjectExplorer.Main("ARC Object Tree", ?content = treePane)
                        ARCObjectExplorer.Main("ARC Object Explorer", ?content = explorerPane)
                        ARCObjectExplorer.Main("ARC Object Details", ?content = detailsPane)
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        React.useEffectOnce (fun _ ->
            Size.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StorySize)
            Position.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StoryPosition)
        )

        let widgets: Map<WidgetType, WidgetDefinition> =
            [ ]
            |> Map.ofList

        Widget.WidgetController(widgets, children = [ ])
