namespace Swate.Components.Composite.FolderedDraggableList

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.JsBindings
open Swate.Components.Composite.FolderedDraggableList.Types

module private FolderedDraggableListHelper =

    let effectiveColor folder item =
        item.Color |> Option.orElse folder.Color

    let toggleExpanded allowMultipleOpen folderId (expanded: Set<string>) =
        if expanded.Contains folderId then expanded.Remove folderId
        elif allowMultipleOpen then expanded.Add folderId
        else Set.singleton folderId

    let colorSwatch color =
        match color with
        | Some color when color <> "" ->
            Html.span [
                prop.className "swt:size-2.5 swt:shrink-0 swt:rounded-full swt:border swt:border-base-300"
                prop.custom ("data-foldered-color-swatch", "true")
                prop.style [ style.backgroundColor color ]
            ]
        | _ -> Html.none

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
                    prop.className "swt:badge swt:badge-xs swt:shrink-0"
                    prop.text badge
                ]
            | _ -> Html.none
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
                "swt:btn swt:btn-sm swt:h-auto swt:min-h-8 swt:w-fit swt:max-w-full swt:shrink swt:min-w-0 swt:justify-start swt:gap-2 swt:overflow-hidden swt:px-3 swt:py-1.5 swt:text-xs swt:normal-case"
                if item.Disabled then
                    "swt:btn-disabled swt:cursor-not-allowed"
                else
                    "swt:btn-outline swt:bg-base-100 swt:cursor-grab active:swt:cursor-grabbing"
                if draggable.isDragging then
                    "swt:opacity-60 swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
                match item.Tooltip with
                | Some tooltip when tooltip <> "" -> "swt:tooltip swt:tooltip-right"
                | _ -> ()
            ]
            prop.ariaLabel $"Drag {item.Label}"
            match item.Tooltip with
            | Some tooltip when tooltip <> "" ->
                prop.custom ("data-tip", tooltip)
                prop.title tooltip
            | _ -> ()
            if defaultArg debug false then
                prop.testId $"foldered-draggable-item-{item.Id}"
            prop.children [ renderItemContent renderProps ]
        ]

    [<ReactComponent>]
    static member private Folder<'payload>
        (
            folder: FolderedDraggableFolder<'payload>,
            isExpanded: bool,
            onToggle: unit -> unit,
            dragId: FolderedDraggableFolder<'payload> -> FolderedDraggableItem<'payload> -> string,
            renderItemContent: FolderedDraggableItemRenderFn<'payload>,
            ?debug: bool,
            ?key: string
        ) =
        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
            if defaultArg debug false then
                prop.testId $"foldered-draggable-folder-{folder.Id}"
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.className
                        "swt:btn swt:btn-sm swt:btn-ghost swt:min-h-8 swt:h-auto swt:w-full swt:justify-start swt:gap-2 swt:px-2 swt:py-1 swt:text-left"
                    prop.custom ("aria-expanded", isExpanded)
                    prop.ariaLabel (
                        if isExpanded then
                            $"Collapse {folder.Name}"
                        else
                            $"Expand {folder.Name}"
                    )
                    prop.onClick (fun _ -> onToggle ())
                    prop.children [
                        Html.i [
                            prop.className [
                                "swt:iconify swt:size-4 swt:shrink-0"
                                if isExpanded then
                                    "swt:fluent--chevron-down-20-regular"
                                else
                                    "swt:fluent--chevron-right-20-regular"
                            ]
                        ]
                        FolderedDraggableListHelper.colorSwatch folder.Color
                        Html.i [
                            prop.className [
                                "swt:iconify swt:size-4 swt:shrink-0"
                                if isExpanded then
                                    "swt:fluent--folder-open-24-regular"
                                else
                                    "swt:fluent--folder-24-regular"
                            ]
                        ]
                        Html.span [
                            prop.className "swt:min-w-0 swt:truncate swt:font-medium"
                            prop.text folder.Name
                        ]
                        Html.span [
                            prop.className "swt:badge swt:badge-xs swt:ml-auto swt:shrink-0"
                            prop.text (string folder.Items.Length)
                        ]
                    ]
                ]
                if isExpanded then
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:pl-6"
                        prop.children [
                            for item in folder.Items do
                                FolderedDraggableList.Item(
                                    folder,
                                    item,
                                    dragId,
                                    renderItemContent,
                                    ?debug = debug,
                                    key = $"{folder.Id}:{item.Id}"
                                )
                        ]
                    ]
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
            ?allowMultipleOpen: bool,
            ?renderItemContent: FolderedDraggableItemRenderFn<'payload>,
            ?className: string,
            ?debug: bool
        ) =
        let allowMultipleOpen = defaultArg allowMultipleOpen true
        let debug = defaultArg debug false

        let localExpanded, setLocalExpanded =
            React.useState (defaultArg defaultExpandedFolderIds Set.empty)

        let expanded = expandedFolderIds |> Option.defaultValue localExpanded

        let setExpanded next =
            onExpandedFolderIdsChange |> Option.iter (fun fn -> fn next)

            if expandedFolderIds.IsNone then
                setLocalExpanded next

        let renderItemContent =
            renderItemContent
            |> Option.defaultValue FolderedDraggableListHelper.defaultItemContent

        Html.section [
            prop.className [
                "swt:flex swt:min-w-0 swt:flex-col swt:gap-3"
                match className with
                | Some className -> className
                | None -> ()
            ]
            if debug then
                prop.testId "foldered-draggable-list"
            prop.children [
                for folder in folders do
                    let isExpanded = expanded.Contains folder.Id

                    FolderedDraggableList.Folder(
                        folder,
                        isExpanded,
                        (fun () ->
                            expanded
                            |> FolderedDraggableListHelper.toggleExpanded allowMultipleOpen folder.Id
                            |> setExpanded
                        ),
                        dragId,
                        renderItemContent,
                        debug = debug,
                        key = folder.Id
                    )
            ]
        ]
