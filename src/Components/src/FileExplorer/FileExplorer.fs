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

        let handleDirectoryToggle (item: FileItem) (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()
            dispatch (FileExplorerLogic.ToggleExpanded item.Id)

        let handleDirectoryClick (item: FileItem) (isExpanded: bool) (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()

            match directoryInteractionMode with
            | DirectoryInteractionMode.SingleClickToggle ->
                dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                handleItemClick item
            | DirectoryInteractionMode.OpenOnDoubleClickCloseOnSingleClick ->
                if isExpanded then
                    dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                    handleItemClick item
                elif ev.detail >= 2 then
                    dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                else
                    handleItemClick item
            | DirectoryInteractionMode.ToggleOnSingleClickSelectOnDoubleClick ->
                if ev.detail >= 2 then
                    if item.Selectable then
                        handleItemClick item
                else
                    dispatch (FileExplorerLogic.ToggleExpanded item.Id)

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
                if item.IsDirectory then
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

        let rec renderItem item =
            let isSelected = model.SelectedId = Some item.Id
            let selectedClass = if isSelected then "swt:bg-base-300" else ""
            let isExpanded = model.ExpandedIds.Contains item.Id

            match item.Children with
            | Some children ->
                Html.li [
                    prop.key item.Id
                    prop.custom ("data-file-item-id", item.Id)
                    prop.children [
                        if useDirectoryChevronToggle then
                            Html.details [
                                if isExpanded then
                                    prop.custom ("open", true)
                                prop.children [
                                    Html.summary [
                                        prop.custom ("data-file-item-id", item.Id)
                                        prop.className ("swt:list-none swt:px-2 swt:py-1 " + selectedClass)
                                        prop.onClick (fun ev ->
                                            ev.preventDefault ()
                                            ev.stopPropagation ()
                                        )
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2"
                                                        prop.children [
                                                            Html.button [
                                                                prop.type'.button
                                                                prop.className
                                                                    "swt:flex swt:h-5 swt:w-5 swt:shrink-0 swt:items-center swt:justify-center swt:rounded swt:bg-transparent swt:p-0 hover:swt:bg-base-200"
                                                                prop.ariaLabel (
                                                                    if isExpanded then
                                                                        $"Collapse {item.Name}"
                                                                    else
                                                                        $"Expand {item.Name}"
                                                                )
                                                                prop.onClick (handleDirectoryToggle item)
                                                                prop.children [
                                                                    Html.span [
                                                                        prop.className "swt:text-xs swt:font-mono"
                                                                        prop.text (if isExpanded then "v" else ">")
                                                                    ]
                                                                ]
                                                            ]
                                                            Html.button [
                                                                prop.type'.button
                                                                prop.className
                                                                    "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2 swt:bg-transparent swt:border-0 swt:p-0 swt:text-left"
                                                                prop.onClick (handleDirectorySelection item)
                                                                prop.children [
                                                                    Html.i [
                                                                        prop.className (iconClassName [ "swt:iconify"; "swt:shrink-0" ] item)
                                                                    ]
                                                                    Html.span [
                                                                        prop.className "swt:truncate"
                                                                        prop.text item.Name
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]

                                                    // LFS badge and size if applicable
                                                    if item.IsLFS = Some true then
                                                        Html.div [
                                                            prop.className "swt:flex swt:gap-2 swt:items-center"
                                                            prop.children [
                                                                Html.button [
                                                                    prop.className "swt:btn swt:btn-xs"
                                                                    prop.disabled (item.Downloaded = Some true)
                                                                    prop.text "LFS"
                                                                    prop.onClick (fun e ->
                                                                        e.stopPropagation ()

                                                                        dispatch (
                                                                            FileExplorerLogic.ToggleLFSDownload item.Id
                                                                        )
                                                                    )
                                                                ]
                                                                match item.SizeFormatted with
                                                                | Some size ->
                                                                    Html.span [
                                                                        prop.className "swt:badge swt:badge-sm"
                                                                        prop.text size
                                                                    ]
                                                                | None -> Html.none
                                                            ]
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    if isExpanded then
                                        Html.ul [
                                            prop.className "swt:ml-4"
                                            prop.children (children |> List.map renderItem)
                                        ]
                                ]
                            ]
                        else
                            Html.details [
                                if isExpanded then
                                    prop.custom ("open", true)
                                prop.children [
                                    Html.summary [
                                        prop.custom ("data-file-item-id", item.Id)
                                        prop.className ("swt:px-2 swt:py-1 swt:cursor-pointer " + selectedClass)
                                        prop.onClick (handleDirectoryClick item isExpanded)
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "swt:flex swt:items-center swt:gap-2"
                                                        prop.children [
                                                            Html.i [ prop.className (iconClassName [ "swt:iconify" ] item) ]
                                                            Html.span item.Name
                                                        ]
                                                    ]

                                                    // LFS badge and size if applicable
                                                    if item.IsLFS = Some true then
                                                        Html.div [
                                                            prop.className "swt:flex swt:gap-2 swt:items-center"
                                                            prop.children [
                                                                Html.button [
                                                                    prop.className "swt:btn swt:btn-xs"
                                                                    prop.disabled (item.Downloaded = Some true)
                                                                    prop.text "LFS"
                                                                    prop.onClick (fun e ->
                                                                        e.stopPropagation ()

                                                                        dispatch (
                                                                            FileExplorerLogic.ToggleLFSDownload item.Id
                                                                        )
                                                                    )
                                                                ]
                                                                match item.SizeFormatted with
                                                                | Some size ->
                                                                    Html.span [
                                                                        prop.className "swt:badge swt:badge-sm"
                                                                        prop.text size
                                                                    ]
                                                                | None -> Html.none
                                                            ]
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    if isExpanded then
                                        Html.ul [
                                            prop.className "swt:ml-4"
                                            prop.children (children |> List.map renderItem)
                                        ]
                                ]
                            ]
                    ]
                ]
            | None ->
                Html.li [
                    prop.key item.Id
                    prop.custom ("data-file-item-id", item.Id)
                    prop.children [
                        Html.a [
                            prop.custom ("data-file-item-id", item.Id)
                            prop.className (
                                "swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-between "
                                + selectedClass
                            )
                            prop.onClick (fun _ -> handleItemClick item)
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.i [ prop.className (iconClassName [ "swt:iconify" ] item) ]
                                        Html.span item.Name
                                    ]
                                ]

                                // LFS badge for files
                                if item.IsLFS = Some true then
                                    Html.div [
                                        prop.className "swt:flex swt:gap-2 swt:items-center"
                                        prop.children [
                                            Html.button [
                                                prop.className "swt:btn swt:btn-xs"
                                                prop.disabled (item.Downloaded = Some true)
                                                prop.text "LFS"
                                                prop.onClick (fun e ->
                                                    e.stopPropagation ()
                                                    dispatch (FileExplorerLogic.ToggleLFSDownload item.Id)
                                                )
                                            ]
                                            match item.SizeFormatted with
                                            | Some size ->
                                                Html.span [ prop.className "swt:badge swt:badge-sm"; prop.text size ]
                                            | None -> Html.none
                                        ]
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
                    prop.className "swt:menu swt:w-full"
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
                    Children =
                        Some [
                            FileTree.createFile "Project-final.psd" None FileItemIcon.Document
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
