namespace Swate.Components


open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components.FileExplorerTypes

// ---------------------------------------------------------------------------
[<Mangle(false); Erase>]
type FileExplorer =

    [<ReactComponent>]
    static member FileExplorer
        (
            ?initialItems: FileItem list,
            ?onItemClick: FileItem -> unit,
            ?onContextMenu: FileItem -> Swate.Components.FileExplorerTypes.ContextMenuItem list,
            ?selectedItemId: string option,
            ?onDirectoryArrowToggle: FileItem -> bool -> unit,
            ?directoryInteractionMode: DirectoryInteractionMode,
            ?useDirectoryChevronToggle: bool,
            ?showBreadcrumbs: bool,
            ?getItemIconClass: FileItem -> string option
        ) =
        let reducer model msg = FileExplorerLogic.update msg model

        let initialModel = FileExplorerLogic.init (defaultArg initialItems [])
        let directoryInteractionMode = defaultArg directoryInteractionMode DirectoryInteractionMode.SingleClickToggle
        let useDirectoryChevronToggle = defaultArg useDirectoryChevronToggle false
        let showBreadcrumbs = defaultArg showBreadcrumbs true
        let getItemIconClass = defaultArg getItemIconClass (fun _ -> None)
        let includeSelectedDirectoryInVisiblePath =
            directoryInteractionMode = DirectoryInteractionMode.SingleClickToggle

        let model, dispatch = React.useReducer (reducer, initialModel)
        let containerRef = React.useElementRef ()

        React.useEffect (
            (fun () ->
                dispatch (
                    FileExplorerLogic.UpdateItems(
                        defaultArg initialItems [],
                        selectedItemId,
                        includeSelectedDirectoryInVisiblePath
                    )
                )),
            [| box initialItems; box selectedItemId; box includeSelectedDirectoryInVisiblePath |]
        )

        let handleItemClick item =
            dispatch (FileExplorerLogic.SelectItem item.Id)
            onItemClick |> Option.iter (fun fn -> fn item)

        let handleDirectorySelection (item: FileItem) (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()
            handleItemClick item

        let handleDirectoryArrowToggle (item: FileItem) (isExpanded: bool) =
            let willExpand = not isExpanded
            dispatch (FileExplorerLogic.ToggleExpanded item.Id)
            onDirectoryArrowToggle |> Option.iter (fun fn -> fn item willExpand)

        let copyPathToClipboard (path: string) =
            promise {
                try
                    let windowObj: obj = Browser.Dom.window
                    do! windowObj?navigator?clipboard?writeText (path)
                with ex ->
                    Browser.Dom.console.warn ($"Could not copy file path: {path}", ex)
            }
            |> Promise.start

        let defaultContextMenuItems (item: FileItem) : ContextMenuItem list =
            let canExpandDirectory =
                match item.Children with
                | Some children -> not (List.isEmpty children)
                | None -> true

            [
                if not item.IsDirectory then
                    {
                        Label = "Open"
                        Icon = "swt:fluent--open-24-regular"
                        OnClick = fun () -> handleItemClick item
                        Disabled = None
                    }
                match item.Path with
                | Some path -> {
                    Label = "Copy Path"
                    Icon = "swt:fluent--copy-24-regular"
                    OnClick = fun () -> copyPathToClipboard path
                    Disabled = None
                  }
                | None -> ()
                if item.IsDirectory && canExpandDirectory then
                    let isExpanded = model.ExpandedIds.Contains item.Id

                    {
                        Label = if isExpanded then "Collapse" else "Expand"
                        Icon =
                            if isExpanded then
                                "swt:fluent--folder-open-24-regular"
                            else
                                "swt:fluent--folder-24-regular"
                        OnClick = fun () -> dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                        Disabled = None
                    }
            ]

        let getContextMenuItems (item: FileItem) =
            let customItems =
                onContextMenu |> Option.map (fun fn -> fn item) |> Option.defaultValue []

            defaultContextMenuItems item @ customItems

        let iconClassName (baseClasses: string list) (item: FileItem) =
            [
                yield! baseClasses
                yield item.Icon |> FileItemIcon.className
                yield! item.IconTone |> Option.map FileItemIconTone.className |> Option.toList
                yield! getItemIconClass item |> Option.toList
            ]

        let renderLfsStatusPill (item: FileItem) =
            let isDownloaded = item.Downloaded = Some true
            let statusLabel = "LFS"
            let statusAccessibilityText =
                if isDownloaded then
                    "LFS Downloaded"
                else
                    match item.SizeFormatted with
                    | Some size -> $"LFS Not Downloaded - {size}"
                    | None -> "LFS Not Downloaded"
            let showSizeSegment = not isDownloaded && item.SizeFormatted.IsSome
            let statusClassName =
                if isDownloaded then
                    "swt:badge-success"
                else
                    "swt:badge-info swt:text-info-content"
            let statusIconClassName =
                if isDownloaded then
                    "swt:fluent--checkmark-circle-24-regular"
                else
                    "swt:fluent--cloud-arrow-down-24-regular"
            let statusShapeClass =
                if showSizeSegment then
                    "swt:rounded-none swt:border-0"
                else
                    "swt:rounded-full"
            let statusBadge =
                Html.span [
                    prop.className
                        $"swt:badge swt:badge-sm swt:cursor-default swt:gap-0.5 {statusClassName} {statusShapeClass}"
                    prop.custom (
                        "data-lfs-download-status",
                        if isDownloaded then
                            "downloaded"
                        else
                            "not-downloaded"
                    )
                    prop.children [
                        Html.i [
                            prop.className $"swt:iconify {statusIconClassName} swt:size-3"
                        ]
                        Html.span [
                            prop.text statusLabel
                        ]
                    ]
                ]
            let badgeSegments =
                [
                    statusBadge
                    match item.SizeFormatted, isDownloaded with
                    | Some size, false ->
                        Html.span [
                            prop.className
                                "swt:badge swt:badge-sm swt:rounded-none swt:border-0 swt:cursor-default swt:bg-base-200 swt:text-info-content"
                            prop.text size
                        ]
                    | _ -> ()
                ]

            Html.span [
                prop.className "swt:inline-flex swt:overflow-hidden swt:rounded-full swt:border swt:border-base-300"
                prop.ariaLabel statusAccessibilityText
                prop.title statusAccessibilityText
                prop.onClick (fun e ->
                    e.preventDefault ()
                    e.stopPropagation ()
                )
                prop.children badgeSegments
            ]

        let toComponentMenuItem (item: Swate.Components.FileExplorerTypes.ContextMenuItem) =
            let isDisabled = defaultArg item.Disabled false
            let className = if isDisabled then "swt:opacity-50" else ""

            Swate.Components.ContextMenuItem(
                text = Html.span [ prop.className className; prop.text item.Label ],
                icon =
                    Html.i [
                        prop.className [
                            "swt:iconify " + item.Icon
                            if isDisabled then
                                "swt:opacity-50"
                        ]
                    ],
                onClick =
                    (fun _ ->
                        if not isDisabled then
                            item.OnClick()
                    )
            )

        let contextMenu =
            ContextMenu.ContextMenu(
                (fun data ->
                    let item = data |> unbox<FileItem>
                    getContextMenuItems item |> List.map toComponentMenuItem
                ),
                ref = containerRef,
                onSpawn =
                    (fun e ->
                        let target = e.target :?> Browser.Types.HTMLElement

                        match target.closest ("[data-file-item-id]"), containerRef.current with
                        | Some trigger, Some container when container.contains (trigger) ->
                            let trigger = trigger :?> Browser.Types.HTMLElement
                            let itemId: string = !!trigger?dataset?fileItemId

                            match FileTree.findItem itemId model.Items with
                            | Some item ->
                                let menuItems = getContextMenuItems item
                                if List.isEmpty menuItems then None else Some(box item)
                            | None -> None
                        | _ -> None
                    )
            )

        let selectedPathIds =
            model.BreadcrumbPath
            |> List.map _.Id
            |> Set.ofList

        let rec renderItem item =
            let isSelected = model.SelectedId = Some item.Id
            let isInSelectedPath = selectedPathIds.Contains item.Id
            let isHighlighted = isSelected || isInSelectedPath
            let rowHighlightClass =
                if isHighlighted then
                    "swt:bg-base-300 swt:active:bg-base-300"
                else
                    "swt:hover:bg-base-300 swt:active:bg-base-300"
            let selectedNameClass =
                if isSelected then
                    "swt:font-semibold swt:text-primary"
                else
                    ""
            let isExpanded = model.ExpandedIds.Contains item.Id
            let directoryToggleIconClass =
                if isExpanded then
                    "swt:iconify swt:fluent--caret-down-24-filled swt:size-4 swt:shrink-0"
                else
                    "swt:iconify swt:fluent--caret-right-24-filled swt:size-4 swt:shrink-0"
            let canExpandDirectory =
                match item.Children with
                | Some children -> not (List.isEmpty children)
                | None -> true

            if item.IsDirectory then
                let directoryNameClick = handleDirectorySelection item

                Html.li [
                    prop.key item.Id
                    prop.custom ("data-file-item-id", item.Id)
                    prop.className "swt:w-full"
                    prop.children [
                        Html.div [
                            prop.custom ("data-file-item-id", item.Id)
                            prop.className [
                                "swt:group swt:w-full swt:px-2 swt:py-1 swt:cursor-default"
                                rowHighlightClass
                            ]
                            prop.style [
                                style.display.flex
                                style.width (length.percent 100)
                            ]
                            if not useDirectoryChevronToggle then
                                prop.onClick (handleDirectorySelection item)
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:w-full swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.type'.button
                                            prop.className
                                                "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2 swt:bg-transparent swt:border-0 swt:p-0 swt:text-left swt:cursor-default"
                                            prop.onClick directoryNameClick
                                            prop.children [
                                                Html.i [
                                                    prop.className (iconClassName [ "swt:iconify"; "swt:shrink-0" ] item)
                                                ]
                                                Html.span [
                                                    prop.className [
                                                        "swt:truncate"
                                                        selectedNameClass
                                                    ]
                                                    prop.text item.Name
                                                ]
                                            ]
                                        ]

                                        if item.IsLFS = Some true || canExpandDirectory then
                                            Html.div [
                                                prop.className "swt:ml-auto swt:shrink-0 swt:flex swt:items-center swt:gap-2"
                                                prop.children [
                                                    // LFS badge and size if applicable
                                                    if item.IsLFS = Some true then
                                                        Html.div [
                                                            prop.className "swt:flex swt:items-center"
                                                            prop.children [ renderLfsStatusPill item ]
                                                        ]

                                                    if canExpandDirectory then
                                                        Html.button [
                                                            prop.type'.button
                                                            prop.className [
                                                                "swt:flex swt:min-h-0 swt:h-5 swt:w-5 swt:shrink-0 swt:items-center swt:justify-center swt:rounded swt:border-0 swt:bg-transparent swt:p-0 swt:cursor-default"
                                                                rowHighlightClass
                                                            ]
                                                            prop.ariaLabel (
                                                                if isExpanded then
                                                                    $"Collapse {item.Name}"
                                                                else
                                                                    $"Expand {item.Name}"
                                                            )
                                                            prop.onClick (fun e ->
                                                                e.preventDefault ()
                                                                e.stopPropagation ()
                                                                handleDirectoryArrowToggle item isExpanded
                                                            )
                                                            prop.children [
                                                                Html.i [ prop.className directoryToggleIconClass ]
                                                            ]
                                                        ]
                                                    ]
                                            ]
                                    ]
                                ]
                            ]
                        ]

                        if isExpanded then
                            match item.Children with
                            | Some children ->
                                Html.ul [
                                    prop.className "swt:ml-4"
                                    prop.children (children |> List.map renderItem)
                                ]
                            | None -> Html.none
                    ]
                ]
            else
                Html.li [
                    prop.key item.Id
                    prop.custom ("data-file-item-id", item.Id)
                    prop.children [
                        Html.a [
                            prop.custom ("data-file-item-id", item.Id)
                            prop.className [
                                "swt:group swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-between swt:cursor-default"
                                rowHighlightClass
                            ]
                            prop.onClick (fun _ -> handleItemClick item)
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.i [ prop.className (iconClassName [ "swt:iconify" ] item) ]
                                        Html.span [
                                            prop.className selectedNameClass
                                            prop.text item.Name
                                        ]
                                    ]
                                ]

                                // LFS badge for files
                                if item.IsLFS = Some true then
                                    Html.div [
                                        prop.className "swt:flex swt:items-center"
                                        prop.children [ renderLfsStatusPill item ]
                                    ]
                            ]
                        ]
                    ]
                ]

        Html.div [
            prop.ref containerRef
            prop.className "swt:w-full"
            prop.children [
                if showBreadcrumbs && not (List.isEmpty model.BreadcrumbPath) then
                    Breadcrumbs.Breadcrumbs(model.BreadcrumbPath, fun id -> dispatch (FileExplorerLogic.NavigateTo id))
                Html.ul [
                    prop.testId "file-explorer-container"
                    prop.className "swt:w-full swt:list-none swt:m-0 swt:p-0"
                    prop.children (model.Items |> List.map renderItem)
                ]
                contextMenu
            ]
        ]



module FileExplorerExample =
    [<ReactComponent>]
    let Example () =
        let initialItems: FileItem list = [
            FileTree.createFile "resume.pdf" None FileItemIcon.Document
            {
                FileTree.createFolder "My Files" None FileItemIcon.Folder with
                    IsExpanded = false
                    IsLFS = Some true
                    Downloaded = Some false
                    SizeFormatted = Some "2 KB"
                    Children =
                        Some [
                            FileTree.createFile "Project-final.psd" None FileItemIcon.Document
                            |> fun file -> {
                                file with
                                    IsLFS = Some true
                                    Downloaded = Some true
                                    SizeFormatted = Some "6 MB"
                            }
                            {
                                FileTree.createFolder "Subfolder" None FileItemIcon.Folder with
                                    IsExpanded = false
                                    Children =
                                        Some [
                                            FileTree.createFile "nested-file-1.txt" None FileItemIcon.Document
                                            FileTree.createFile "nested-file-2.md" None FileItemIcon.Document
                                            {
                                                FileTree.createFolder "NestedFolder" None FileItemIcon.Folder with
                                                    IsExpanded = false
                                                    Children =
                                                        Some [
                                                            FileTree.createFile "Project-2-final.psd" None FileItemIcon.Document
                                                            FileTree.createFile "Project-3-final.psd" None FileItemIcon.Document
                                                        ]
                                            }
                                        ]
                            }
                        ]
            }
            {
                FileTree.createFolder "Empty Folder" None FileItemIcon.Folder with
                    IsExpanded = false
                    Children = Some []
            }
            FileTree.createFile "notes.txt" None FileItemIcon.Document
        ]

        let handleItemClick (item: FileItem) =
            Browser.Dom.console.log ("Clicked:", item.Name)

        let handleContextMenu (item: FileItem) = [
            {
                Label = "Rename"
                Icon = "edit"
                OnClick = fun () -> Browser.Dom.console.log ("Rename", item.Name)
                Disabled = None
            }
            {
                Label = "Delete"
                Icon = "delete"
                OnClick = fun () -> Browser.Dom.console.log ("Delete", item.Name)
                Disabled = None
            }
        ]

        Html.div [
            prop.className "swt:p-4"
            prop.children [
                Html.h2 [
                    prop.className "swt:text-2xl swt:font-bold swt:mb-4"
                    prop.text "File Explorer Demo"
                ]
                FileExplorer.FileExplorer(
                    initialItems = initialItems,
                    onItemClick = handleItemClick,
                    onContextMenu = handleContextMenu
                )
            ]
        ]
