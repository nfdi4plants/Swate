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
            ?canCreateItem: FileItem -> bool,
            ?onCreateItem: FileItem -> unit,
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
        let canCreateItem = defaultArg canCreateItem (fun (_: FileItem) -> false)
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

        let handleDirectorySelection (item: FileItem) (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()
            FileExplorerItemHelper.handleItemClick (item, onItemClick, dispatch)

        let handleDirectoryArrowToggle (item: FileItem) (isExpanded: bool) =
            let willExpand = not isExpanded
            dispatch (FileExplorerLogic.ToggleExpanded item.Id)
            onDirectoryArrowToggle |> Option.iter (fun fn -> fn item willExpand)

        let contextMenu =
            ContextMenu.ContextMenu(
                (fun data ->
                    let item = data |> unbox<FileItem>
                    FileExplorerContextMenu.getContextMenuItems (item, model, onItemClick, onContextMenu, dispatch)
                    |> List.map FileExplorerContextMenu.toComponentMenuItem
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
                                let menuItems =
                                    FileExplorerContextMenu.getContextMenuItems (
                                        item,
                                        model,
                                        onItemClick,
                                        onContextMenu,
                                        dispatch
                                    )

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

            let canExpandDirectory =
                match item.Children with
                | Some children -> not (List.isEmpty children)
                | None -> true

            if item.IsDirectory then
                let childrenTree =
                    if isExpanded then
                        match item.Children with
                        | Some children ->
                            Some(
                                Html.ul [
                                    prop.className "swt:ml-4"
                                    prop.children (children |> List.map renderItem)
                                ]
                            )
                        | None -> None
                    else
                        None

                FileExplorerItem.DirectoryRow (
                    item,
                    rowHighlightClass,
                    selectedNameClass,
                    isExpanded,
                    useDirectoryChevronToggle,
                    canExpandDirectory,
                    getItemIconClass,
                    handleDirectorySelection item,
                    (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        handleDirectoryArrowToggle item isExpanded
                    ),
                    ?onCreateItem = onCreateItem,
                    canCreateItem = canCreateItem,
                    ?children = childrenTree
                )
            else
                FileExplorerItem.FileRow (
                    item,
                    rowHighlightClass,
                    selectedNameClass,
                    getItemIconClass,
                    (fun () -> FileExplorerItemHelper.handleItemClick (item, onItemClick, dispatch))
                )

        Html.div [
            prop.ref containerRef
            prop.className "swt:w-full"
            prop.children [
                if showBreadcrumbs && not (List.isEmpty model.BreadcrumbPath) then
                    Breadcrumbs.Breadcrumbs(model.BreadcrumbPath, fun id -> dispatch (FileExplorerLogic.NavigateTo id))
                Html.div [
                    prop.testId "file-explorer-scroll-container"
                    prop.className "swt:w-full swt:overflow-x-auto"
                    prop.children [
                        Html.ul [
                            prop.testId "file-explorer-container"
                            prop.className "swt:w-full swt:min-w-max swt:list-none swt:m-0 swt:p-0"
                            prop.children (model.Items |> List.map renderItem)
                        ]
                    ]
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
