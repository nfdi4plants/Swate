module Swate.Components.ARCObjectWidgetStory

open Feliz
open Swate.Components.FileExplorerTypes

let private tryFindItemAndParent (itemId: string) (items: FileItem list) =
    let rec loop (parent: FileItem option) (items: FileItem list) =
        items
        |> List.tryPick (fun item ->
            if item.Id = itemId then
                Some(item, parent)
            else
                item.Children |> Option.bind (loop (Some item)))

    loop None items

let private flattenFileItemsWithParent(items: FileItem list) =
    let rec loop (parent: FileItem option) (items: FileItem list) =
        items
        |> List.collect (fun item ->
            (item, parent) :: (item.Children |> Option.defaultValue [] |> loop (Some item)))

    loop None items

let private tryGetStoryItemKind(item: FileItem) =
    ARCObjectFixture.StoryMeta
    |> Map.tryFind item.Id
    |> Option.map (fun (_, kind, _, _, _) -> kind)
    |> Option.orElseWith (fun () ->
        if item.Children |> Option.exists (List.isEmpty >> not) then
            Some "Group"
        else
            None)

let private filterStoryItemsByKinds(visibleKinds: Set<string>, items: FileItem list) =
    let rec loop (item: FileItem) =
        let filteredChildren =
            item.Children |> Option.map (List.choose loop)

        let hasVisibleChildren =
            filteredChildren |> Option.exists (List.isEmpty >> not)

        let itemKind = tryGetStoryItemKind item

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

let private storySearchItems(items: FileItem list) =
    flattenFileItemsWithParent items
    |> List.choose (fun (item, parent) ->
        match tryGetStoryItemKind item with
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

[<ReactComponent>]
let private StoryExample() =
    let selectedId, setSelectedId = React.useState ARCObjectFixture.StoryItemIdStudy

    let selectedKindIndices, setSelectedKindIndices =
        React.useState (ARCObjectWidget.DefaultKindFilterIndices())

    let items =
        React.useMemo ((fun () -> ARCObjectFixture.StoryItems()), [||])

    let visibleKinds = ARCObjectWidget.SelectedKindLabels(selectedKindIndices)

    let filteredItems = filterStoryItemsByKinds(visibleKinds, items)

    let visibleSelectedId =
        Some selectedId
        |> Option.filter (fun itemId -> tryFindItemAndParent itemId filteredItems |> Option.isSome)

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

    let searchItems = storySearchItems filteredItems

    let searchAction =
        ARCObjectWidget.SearchAction(
            searchItems,
            (fun (name, _, _) -> name),
            (fun (_, _, item) -> setSelectedId item.Id),
            itemSubtitle = (fun (_, subtitle, _) -> subtitle)
        )

    let navbar =
        ARCObjectWidget.Navbar(
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
let Entry() =
    let isOpen, setIsOpen = React.useState false

    Html.div [
        prop.className "swt:min-h-screen swt:bg-base-200 swt:p-6"
        prop.children [
            Html.button [
                prop.type'.button
                prop.className "swt:btn swt:btn-primary"
                prop.text "Open ARC Object"
                prop.onClick (fun _ -> setIsOpen true)
            ]
            if isOpen then
                Html.div [
                    prop.className "swt:mt-6"
                    prop.children [ StoryExample() ]
                ]
        ]
    ]
