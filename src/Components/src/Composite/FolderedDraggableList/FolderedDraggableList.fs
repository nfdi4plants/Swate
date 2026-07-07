namespace Swate.Components.Composite.FolderedDraggableList

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Primitive.Popover
open Swate.Components.Composite.FolderedDraggableList.Types

module private FolderedDraggableListHelper =

    let fallbackColor = "#2563eb"

    let currentOrFallback color =
        match color with
        | Some c when c <> "" -> c
        | _ -> fallbackColor

    [<Emit("$0.target && $0.target.closest && $0.target.closest('[data-folder-color-control]') !== null")>]
    let isFolderColorControlEvent (_event: Browser.Types.Event) : bool = jsNative

    let effectiveColor (folder: FolderedDraggableFolder<'payload>) (item: FolderedDraggableItem<'payload>) =
        item.Color |> Option.orElse folder.Color

    let expandedFolderId folders (expanded: Set<string>) =
        folders
        |> List.tryFind (fun folder -> expanded.Contains folder.Id)
        |> Option.map (fun folder -> folder.Id)

    let toggleExpanded folderId expandedFolderId =
        match expandedFolderId with
        | Some expandedFolderId when expandedFolderId = folderId -> Set.empty
        | _ -> Set.singleton folderId

    let appendItemToFolder targetFolderId item folders =
        folders
        |> List.map (fun folder ->
            if folder.Id = targetFolderId then
                {
                    folder with
                        Items = folder.Items @ [ item ]
                }
            else
                folder
        )

    let canAcceptExternalDrop
        (expandedFolder: FolderedDraggableFolder<'payload> option)
        (tryCreateItemFromExternalDrop: FolderedDraggableExternalDropHandler<'payload> option)
        (onFoldersChange: (FolderedDraggableFolder<'payload> list -> unit) option)
        =
        expandedFolder.IsSome
        && tryCreateItemFromExternalDrop.IsSome
        && onFoldersChange.IsSome

    let isShelfDrop shelfDropId (event: DndKit.IDndKitEvent) =
        not (isNull event.over) && string event.over.id = shelfDropId

    let activeId (event: DndKit.IDndKitEvent) =
        if isNull event.active then "" else string event.active.id

    let activeData (event: DndKit.IDndKitEvent) =
        if isNull event.active || isNull event.active.data then
            null
        else
            event.active.data.current

    let colorSwatch color =
        match color with
        | Some color when color <> "" ->
            Html.span [
                prop.className "swt:size-2.5 swt:shrink-0 swt:rounded-full swt:border swt:border-base-300"
                prop.custom ("data-foldered-color-swatch", "true")
                prop.style [ style.backgroundColor color ]
            ]
        | _ -> Html.none

    let colorPickerContent ariaLabel (draftColor: string) (setDraftColor: string -> unit) onSetColor =
        Html.div [
            prop.className "swt:flex swt:items-center swt:gap-2 swt:p-2"
            prop.children [
                Html.input [
                    prop.custom ("type", "color")
                    prop.className "swt:h-8 swt:w-10 swt:cursor-pointer swt:rounded swt:border swt:border-base-300"
                    prop.value draftColor
                    prop.ariaLabel ariaLabel
                    prop.onChange (fun (color: string) -> setDraftColor color)
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-xs swt:btn-primary swt:min-h-0 swt:py-0"
                    prop.text "Select"
                    prop.onClick (fun _ -> onSetColor (Some draftColor))
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:min-h-0 swt:py-0"
                    prop.text "Clear"
                    prop.onClick (fun _ ->
                        setDraftColor fallbackColor
                        onSetColor None
                    )
                ]
            ]
        ]

    let defaultItemContent (render: FolderedDraggableItemRender<'payload>) =
        React.Fragment [
            colorSwatch render.DragData.EffectiveColor
            Html.span [
                prop.className "swt:min-w-0 swt:truncate swt:text-left"
                prop.text render.Item.Label
            ]
            match render.Item.Badge with
            | Some badge when badge <> "" ->
                Html.span [
                    prop.className "swt:badge swt:badge-sm swt:shrink-0"
                    prop.text badge
                ]
            | _ -> Html.none
        ]

    let itemShellClass isDisabled isDragging = [
        "swt:btn swt:btn-sm swt:h-auto swt:min-h-10 swt:w-fit swt:max-w-56 swt:shrink-0 swt:min-w-0 swt:justify-start swt:gap-2 swt:overflow-hidden swt:px-3 swt:py-2 swt:text-sm swt:normal-case"
        if isDisabled then
            "swt:btn-disabled swt:cursor-not-allowed"
        else
            "swt:btn-outline swt:bg-base-100 swt:cursor-grab swt:shadow-sm active:swt:cursor-grabbing"
        if isDragging then
            "swt:pointer-events-none swt:opacity-0"
    ]

[<Erase; Mangle(false)>]
type FolderedDraggableList =

    [<ReactComponent>]
    static member private Item<'payload>
        (
            folder: FolderedDraggableFolder<'payload>,
            item: FolderedDraggableItem<'payload>,
            dragId: FolderedDraggableFolder<'payload> -> FolderedDraggableItem<'payload> -> string,
            renderItemContent: FolderedDraggableItemRenderFn<'payload>,
            onActiveDragChange: FolderedDraggableItemRender<'payload> option -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let dragData: FolderedDraggableData<'payload> = {
            FolderId = folder.Id
            FolderName = folder.Name
            FolderColor = folder.Color
            ItemId = item.Id
            ItemLabel = item.Label
            ItemColor = item.Color
            EffectiveColor = FolderedDraggableListHelper.effectiveColor folder item
            Payload = item.Payload
        }

        let draggable =
            DndKit.useDraggable (
                {|
                    id = dragId folder item
                    data = dragData
                    disabled = item.Disabled
                |}
            )

        let renderProps: FolderedDraggableItemRender<'payload> = {
            Folder = folder
            Item = item
            DragData = dragData
            IsDragging = draggable.isDragging
        }

        React.useEffect (
            (fun () ->
                if draggable.isDragging then
                    onActiveDragChange (Some renderProps)
                else
                    onActiveDragChange None
            ),
            [| box draggable.isDragging |]
        )

        Html.button [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.type'.button
            prop.disabled item.Disabled
            if not item.Disabled then
                prop.ref draggable.setNodeRef
                yield! prop.spread (!!draggable.attributes)
                yield! prop.spread (!!draggable.listeners)
            prop.className [
                yield! FolderedDraggableListHelper.itemShellClass item.Disabled draggable.isDragging
                match item.Tooltip with
                | Some tooltip when tooltip <> "" -> "swt:tooltip swt:tooltip-right"
                | _ -> ()
            ]
            prop.ariaLabel $"Drag {item.Label}"
            if draggable.isDragging then
                prop.tabIndex -1
            match item.Tooltip with
            | Some tooltip when tooltip <> "" ->
                prop.custom ("data-tip", tooltip)
                prop.title tooltip
            | _ -> ()
            if defaultArg debug false && not draggable.isDragging then
                prop.testId $"foldered-draggable-item-{item.Id}"
            prop.children [ renderItemContent renderProps ]
        ]

    [<ReactComponent>]
    static member private FolderColorButton<'payload>
        (folder: FolderedDraggableFolder<'payload>, onSetColor: string option -> unit, ?key: string)
        =
        let draftColor, setDraftColor =
            React.useState (FolderedDraggableListHelper.currentOrFallback folder.Color)

        React.useEffect (
            (fun () -> setDraftColor (FolderedDraggableListHelper.currentOrFallback folder.Color)),
            [| box folder.Color |]
        )

        let trigger =
            Html.button [
                match key with
                | Some key -> prop.key key
                | None -> ()
                prop.type'.button
                prop.custom ("data-folder-color-control", "true")
                prop.custom ("data-foldered-color-swatch", "true")
                prop.className
                    "swt:size-3 swt:shrink-0 swt:cursor-pointer swt:rounded-full swt:border swt:border-base-300 swt:bg-base-100 swt:p-0 swt:shadow-sm"
                match folder.Color with
                | Some color when color <> "" -> prop.style [ style.backgroundColor color ]
                | _ -> ()
                prop.ariaLabel $"Set color for folder {folder.Name}"
            ]

        let content =
            FolderedDraggableListHelper.colorPickerContent
                $"Choose color for folder {folder.Name}"
                draftColor
                setDraftColor
                onSetColor

        Popover.Popover(
            children =
                React.Fragment [
                    Popover.Trigger(
                        trigger,
                        className = "swt:inline-flex swt:shrink-0",
                        props = [ prop.custom ("data-folder-color-control", "true") ]
                    )
                    Popover.Content(
                        children =
                            Html.div [
                                prop.className "swt:flex swt:flex-col swt:gap-2"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex swt:items-start swt:justify-between swt:gap-2"
                                        prop.children [
                                            Html.div [ prop.className "swt:flex-1"; prop.children content ]
                                            Popover.Close()
                                        ]
                                    ]
                                ]
                            ]
                    )
                ]
        )

    [<ReactComponent>]
    static member private Folder<'payload>
        (
            folder: FolderedDraggableFolder<'payload>,
            isExpanded: bool,
            onToggle: unit -> unit,
            ?onSetColor: string option -> unit,
            ?debug: bool,
            ?key: string
        ) =
        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className "swt:relative swt:h-32 swt:w-36 swt:min-w-36 swt:flex-none"
            match folder.Color with
            | Some color when color <> "" -> prop.custom ("data-foldered-folder-color", color)
            | _ -> ()
            prop.custom ("aria-expanded", isExpanded)
            if defaultArg debug false then
                prop.testId $"foldered-draggable-folder-{folder.Id}"
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.className [
                        "swt:btn swt:h-full swt:w-full swt:flex-col swt:items-stretch swt:justify-between swt:gap-2 swt:overflow-hidden swt:rounded-lg swt:border swt:p-3 swt:text-left swt:normal-case swt:shadow-sm swt:transition-all"
                        if isExpanded then
                            "swt:border-primary swt:bg-primary/10 swt:text-primary swt:ring-2 swt:ring-primary/20"
                        else
                            "swt:border-base-300 swt:bg-base-100 hover:swt:border-primary/60 hover:swt:bg-base-200"
                    ]
                    match folder.Color with
                    | Some color when color <> "" && not isExpanded -> prop.style [ style.borderColor color ]
                    | _ -> ()
                    prop.custom ("aria-expanded", isExpanded)
                    prop.title folder.Name
                    prop.ariaLabel (
                        if isExpanded then
                            $"Collapse {folder.Name}"
                        else
                            $"Expand {folder.Name}"
                    )
                    prop.onClick (fun _ -> onToggle ())
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-start swt:justify-between swt:gap-2"
                            prop.children [
                                Html.i [
                                    prop.className [
                                        "swt:iconify swt:size-14 swt:shrink-0"
                                        if isExpanded then
                                            "swt:fluent--folder-open-24-regular"
                                        else
                                            "swt:fluent--folder-24-regular"
                                    ]
                                    match folder.Color with
                                    | Some color when color <> "" -> prop.style [ style.color color ]
                                    | _ -> ()
                                ]
                                Html.span [
                                    prop.className "swt:badge swt:badge-sm swt:shrink-0"
                                    prop.text (string folder.Items.Length)
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
                            prop.children [
                                Html.span [
                                    prop.className "swt:w-full swt:truncate swt:text-sm swt:font-semibold"
                                    prop.text folder.Name
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:gap-2"
                                    prop.children [
                                        match onSetColor with
                                        | Some _ ->
                                            Html.span [
                                                prop.className "swt:size-3 swt:shrink-0"
                                                prop.custom ("aria-hidden", true)
                                            ]
                                        | None -> FolderedDraggableListHelper.colorSwatch folder.Color
                                        Html.span [
                                            prop.className "swt:text-xs swt:font-normal swt:text-base-content/70"
                                            prop.text (
                                                if folder.Items.Length = 1 then
                                                    "1 item"
                                                else
                                                    $"{folder.Items.Length} items"
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                match onSetColor with
                | Some setColor ->
                    Html.div [
                        prop.className "swt:absolute swt:bottom-3 swt:left-3 swt:z-10 swt:flex swt:items-center"
                        prop.children [
                            FolderedDraggableList.FolderColorButton(folder, setColor, key = $"{folder.Id}:color")
                        ]
                    ]
                | None -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member FolderedDraggableList<'payload>
        (
            folders: FolderedDraggableFolder<'payload> list,
            dragId: FolderedDraggableFolder<'payload> -> FolderedDraggableItem<'payload> -> string,
            ?expandedFolderIds: Set<string>,
            ?defaultExpandedFolderIds: Set<string>,
            ?onExpandedFolderIdsChange: Set<string> -> unit,
            ?renderItemContent: FolderedDraggableItemRenderFn<'payload>,
            ?shelfDropId: string,
            ?tryCreateItemFromExternalDrop: FolderedDraggableExternalDropHandler<'payload>,
            ?onFoldersChange: FolderedDraggableFolder<'payload> list -> unit,
            ?onSetFolderColor: string -> string option -> unit,
            ?className: string,
            ?debug: bool
        ) =
        let debug = defaultArg debug false
        let generatedShelfDropId = React.useId ()

        let shelfDropId =
            defaultArg shelfDropId $"foldered-draggable-list-shelf-{generatedShelfDropId}"

        let localExpanded, setLocalExpanded =
            React.useState (defaultArg defaultExpandedFolderIds Set.empty)

        let activeDrag, setActiveDrag =
            React.useState (None: FolderedDraggableItemRender<'payload> option)

        let expanded = expandedFolderIds |> Option.defaultValue localExpanded
        let expandedFolderId = FolderedDraggableListHelper.expandedFolderId folders expanded

        let expandedFolder =
            folders |> List.tryFind (fun folder -> Some folder.Id = expandedFolderId)

        let canAcceptExternalDrop =
            FolderedDraggableListHelper.canAcceptExternalDrop
                expandedFolder
                tryCreateItemFromExternalDrop
                onFoldersChange

        let shelfDroppable =
            DndKit.useDroppable (
                {|
                    id = shelfDropId
                    disabled = not canAcceptExternalDrop
                |}
            )

        let setExpanded next =
            onExpandedFolderIdsChange |> Option.iter (fun fn -> fn next)

            if expandedFolderIds.IsNone then
                setLocalExpanded next

        let renderItemContent =
            renderItemContent
            |> Option.defaultValue FolderedDraggableListHelper.defaultItemContent

        DndKit.useDndMonitor (
            {|
                onDragCancel = fun (_: DndKit.IDndKitEvent) -> setActiveDrag None
                onDragEnd =
                    fun (event: DndKit.IDndKitEvent) ->
                        setActiveDrag None

                        match expandedFolder, tryCreateItemFromExternalDrop, onFoldersChange with
                        | Some targetFolder, Some tryCreateItemFromExternalDrop, Some onFoldersChange when
                            FolderedDraggableListHelper.isShelfDrop shelfDropId event
                            ->
                            let drop: FolderedDraggableExternalDrop<'payload> = {
                                TargetFolder = targetFolder
                                ActiveId = FolderedDraggableListHelper.activeId event
                                ActiveData = FolderedDraggableListHelper.activeData event
                                Folders = folders
                            }

                            match tryCreateItemFromExternalDrop drop with
                            | Some item ->
                                folders
                                |> FolderedDraggableListHelper.appendItemToFolder targetFolder.Id item
                                |> onFoldersChange
                            | None -> ()
                        | _ -> ()
            |}
        )

        Html.section [
            prop.className [
                "swt:flex swt:min-w-0 swt:flex-col swt:gap-4"
                match className with
                | Some className -> className
                | None -> ()
            ]
            if debug then
                prop.testId "foldered-draggable-list"
            prop.children [
                Html.div [
                    prop.className
                        "swt:flex swt:min-w-0 swt:flex-row swt:flex-nowrap swt:gap-3 swt:overflow-x-auto swt:overflow-y-hidden swt:pb-2"
                    if debug then
                        prop.testId "foldered-draggable-folder-row"
                    prop.children [
                        for folder in folders do
                            let isExpanded = Some folder.Id = expandedFolderId

                            let onSetColor =
                                onSetFolderColor
                                |> Option.map (fun setFolderColor -> fun color -> setFolderColor folder.Id color)

                            FolderedDraggableList.Folder(
                                folder,
                                isExpanded,
                                (fun () ->
                                    expandedFolderId
                                    |> FolderedDraggableListHelper.toggleExpanded folder.Id
                                    |> setExpanded
                                ),
                                ?onSetColor = onSetColor,
                                debug = debug,
                                key = folder.Id
                            )
                    ]
                ]
                Html.div [
                    prop.ref shelfDroppable.setNodeRef
                    prop.className [
                        "swt:min-h-24 swt:min-w-0 swt:rounded-lg swt:border swt:border-dashed swt:p-3 swt:transition-colors"
                        if canAcceptExternalDrop && shelfDroppable.isOver then
                            "swt:border-primary swt:bg-primary/10"
                        else
                            "swt:border-base-300 swt:bg-base-200/40"
                    ]
                    if debug then
                        prop.testId "foldered-draggable-item-shelf"
                    prop.children [
                        Html.div [
                            prop.className
                                "swt:relative swt:flex swt:min-h-12 swt:min-w-0 swt:flex-row swt:flex-nowrap swt:items-center swt:gap-2 swt:overflow-x-auto swt:overflow-y-hidden swt:pb-1"
                            if debug then
                                prop.testId "foldered-draggable-item-row"
                            prop.children [
                                match expandedFolder with
                                | Some folder ->
                                    for item in folder.Items do
                                        FolderedDraggableList.Item(
                                            folder,
                                            item,
                                            dragId,
                                            renderItemContent,
                                            setActiveDrag,
                                            debug = debug,
                                            key = $"{folder.Id}:{item.Id}"
                                        )
                                | None -> ()
                            ]
                        ]
                    ]
                ]
                DndKit.DragOverlay(
                    dropAnimation = {| duration = 0; easing = "linear" |},
                    children =
                        match activeDrag with
                        | Some render ->
                            Html.div [
                                prop.className [
                                    yield! FolderedDraggableListHelper.itemShellClass render.Item.Disabled false
                                    "swt:pointer-events-none swt:shadow-xl swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
                                ]
                                if debug then
                                    prop.testId "foldered-draggable-drag-overlay"
                                prop.children [ renderItemContent { render with IsDragging = true } ]
                            ]
                        | None -> Html.none
                )
            ]
        ]
