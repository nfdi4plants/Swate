namespace Swate.Components.Page.FileExplorer

open Swate.Components.Page.FileExplorer.Types
open Fable.Core
open Fable.Core.JsInterop
open Feliz


module private FileExplorerHelper =

    type IntersectionObserverEntry =
        abstract isIntersecting: bool

    type IntersectionObserver =
        abstract observe: Browser.Types.Element -> unit
        abstract disconnect: unit -> unit

    [<Emit("new IntersectionObserver($0)")>]
    let createIntersectionObserver
        (_callback: IntersectionObserverEntry[] -> IntersectionObserver -> unit)
        : IntersectionObserver =
        jsNative

    let rec collectDirectoryIds (items: FileItem list) =
        items
        |> List.fold
            (fun directoryIds item ->
                let nestedDirectoryIds =
                    item.Children |> Option.map collectDirectoryIds |> Option.defaultValue Set.empty

                if item.IsDirectory then
                    directoryIds |> Set.add item.Id |> Set.union nestedDirectoryIds
                else
                    directoryIds |> Set.union nestedDirectoryIds
            )
            Set.empty

    let rec collectDirectoryChildCounts (items: FileItem list) =
        items
        |> List.fold
            (fun counts item ->
                let counts =
                    if item.IsDirectory then
                        counts
                        |> Map.add item.Id (item.Children |> Option.map List.length |> Option.defaultValue 0)
                    else
                        counts

                item.Children
                |> Option.map (fun children ->
                    collectDirectoryChildCounts children
                    |> Map.fold (fun state directoryId childCount -> Map.add directoryId childCount state) counts
                )
                |> Option.defaultValue counts
            )
            Map.empty

    let tryGetEventTargetElement (e: Browser.Types.Event) : Browser.Types.Element option =
        let targetObj: obj = box e.target

        if isNullOrUndefined targetObj then
            None
        elif isNullOrUndefined targetObj?closest then
            let parentElement: obj = targetObj?parentElement

            if isNullOrUndefined parentElement then
                None
            else
                Some(unbox<Browser.Types.Element> parentElement)
        else
            Some(unbox<Browser.Types.Element> targetObj)

    let private copyPathToClipboard (path: string) =
        promise {
            try
                let windowObj: obj = Browser.Dom.window
                do! windowObj?navigator?clipboard?writeText (path)
            with ex ->
                Browser.Dom.console.warn ($"Could not copy file path: {path}", ex)
        }
        |> Promise.start

    let private defaultContextMenuItems
        (item: FileItem)
        (isExpanded: bool)
        (selectItem: FileItem -> unit)
        (getCopyPath: FileItem -> string option)
        (getCopyRelativePath: FileItem -> string option)
        (setExpanded: FileItem -> bool -> unit)
        : Swate.Components.Page.FileExplorer.Types.ContextMenuItem list =
        let canExpandDirectory =
            match item.Children with
            | Some children -> not (List.isEmpty children)
            | None -> true

        [
            if not item.IsDirectory then
                ContextMenuItem.create "Open" "swt:fluent--open-24-regular" (fun () -> selectItem item)

            match item.Path with
            | Some _ ->
                match getCopyPath item with
                | Some path ->
                    ContextMenuItem.create
                        "Copy Path"
                        "swt:fluent--copy-24-regular"
                        (fun () -> copyPathToClipboard path)
                | None -> ()

                match getCopyRelativePath item with
                | Some path ->
                    ContextMenuItem.create
                        "Copy Relative Path"
                        "swt:fluent--copy-24-regular"
                        (fun () -> copyPathToClipboard path)
                | None -> ()
            | None -> ()

            if item.IsDirectory && canExpandDirectory then
                ContextMenuItem.create
                    (if isExpanded then "Collapse" else "Expand")
                    (if isExpanded then
                         "swt:fluent--folder-open-24-regular"
                     else
                         "swt:fluent--folder-24-regular")
                    (fun () -> setExpanded item (not isExpanded))
        ]

    let getContextMenuItems
        (item: FileItem)
        (isExpanded: bool)
        (selectItem: FileItem -> unit)
        (onContextMenu: (FileItem -> Swate.Components.Page.FileExplorer.Types.ContextMenuItem list) option)
        (getCopyPath: FileItem -> string option)
        (getCopyRelativePath: FileItem -> string option)
        (includeDefaultContextMenuItems: bool)
        (setExpanded: FileItem -> bool -> unit)
        =
        let defaultItems =
            if includeDefaultContextMenuItems then
                defaultContextMenuItems item isExpanded selectItem getCopyPath getCopyRelativePath setExpanded
            else
                []

        let customItems =
            onContextMenu |> Option.map (fun fn -> fn item) |> Option.defaultValue []

        defaultItems @ customItems

// ---------------------------------------------------------------------------
[<Mangle(false); Erase>]
type FileExplorer =

    [<ReactComponent>]
    static member private LoadMoreControl
        (directoryId: string, boundary: int, onLoadMore: unit -> unit, onIntersectionChange: bool -> unit)
        =
        let sentinelRef = React.useElementRef ()

        React.useEffect (
            (fun () ->
                match sentinelRef.current with
                | None -> FsReact.createDisposable ignore
                | Some sentinel ->
                    let observer =
                        FileExplorerHelper.createIntersectionObserver (fun entries _ ->
                            entries
                            |> Array.tryLast
                            |> Option.iter (fun entry -> onIntersectionChange entry.isIntersecting)
                        )

                    observer.observe sentinel
                    FsReact.createDisposable observer.disconnect
            ),
            [| box boundary |]
        )

        Html.li [
            prop.ref sentinelRef
            prop.key $"{directoryId}-{boundary}"
            prop.testId $"file-explorer-load-more-{directoryId}"
            prop.className "swt:list-none swt:py-1 swt:pl-1"
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.className
                        "swt:btn swt:btn-ghost swt:btn-xs swt:h-auto swt:min-h-7 swt:gap-1 swt:px-2 swt:text-base-content/70"
                    prop.onClick (fun event ->
                        event.stopPropagation ()
                        onLoadMore ()
                    )
                    prop.children [
                        Html.span [
                            prop.className "swt:iconify swt:fluent--chevron-down-20-regular swt:size-4"
                            prop.ariaHidden true
                        ]
                        Html.span "Load more"
                    ]
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member FileExplorer
        (
            ?initialItems: FileItem list,
            ?onItemClick: FileItem -> unit,
            ?onContextMenu: FileItem -> Swate.Components.Page.FileExplorer.Types.ContextMenuItem list,
            ?canCreateItem: FileItem -> bool,
            ?onCreateItem: FileItem -> unit,
            ?getItemActions: FileItem -> Swate.Components.Page.FileExplorer.Types.ContextMenuItem list,
            ?getItemStatusAction: FileItem -> Swate.Components.Page.FileExplorer.Types.ContextMenuItem option,
            ?canDeleteItem: FileItem -> bool,
            ?onDeleteItem: FileItem -> unit,
            ?selectedItemId: string option,
            ?onDirectoryExpansionChange: FileItem -> bool -> unit,
            ?onExpansionChange: FileItem -> bool -> unit,
            ?onDirectoryArrowToggle: FileItem -> bool -> unit,
            ?directoryInteractionMode: DirectoryInteractionMode,
            ?directoryChevronToggleOnly: bool,
            ?directoryChevronToggleOnlyForItem: FileItem -> bool,
            ?delegateHorizontalScrollToParent: bool,
            ?truncateOverflowingItemNames: bool,
            ?getItemIconClass: FileItem -> string option,
            ?getCopyPath: FileItem -> string option,
            ?getCopyRelativePath: FileItem -> string option,
            ?includeDefaultContextMenuItems: bool,
            ?childRenderBatchSize: int,
            ?hasMoreChildren: FileItem -> bool,
            ?onLoadMoreChildren: FileItem -> unit,
            ?automaticallyLoadChildren: bool
        ) =
        let reducer model msg = FileExplorerLogic.update msg model

        let initialModel = FileExplorerLogic.init (defaultArg initialItems [])

        let directoryInteractionMode =
            defaultArg directoryInteractionMode DirectoryInteractionMode.SingleClickToggle

        let directoryChevronToggleOnly = defaultArg directoryChevronToggleOnly false

        let directoryChevronToggleOnlyForItem =
            defaultArg directoryChevronToggleOnlyForItem (fun _ -> directoryChevronToggleOnly)

        let delegateHorizontalScrollToParent =
            defaultArg delegateHorizontalScrollToParent false

        let truncateOverflowingItemNames = defaultArg truncateOverflowingItemNames false

        let getItemIconClass = defaultArg getItemIconClass (fun _ -> None)
        let getCopyPath = defaultArg getCopyPath (fun item -> item.Path)
        let getCopyRelativePath = defaultArg getCopyRelativePath (fun _ -> None)
        let includeDefaultContextMenuItems = defaultArg includeDefaultContextMenuItems true
        let hasMoreChildren = defaultArg hasMoreChildren (fun _ -> false)
        let automaticallyLoadChildren = defaultArg automaticallyLoadChildren true
        let canCreateItem = defaultArg canCreateItem (fun (_: FileItem) -> false)
        let getItemActions = defaultArg getItemActions (fun (_: FileItem) -> [])
        let getItemStatusAction = defaultArg getItemStatusAction (fun (_: FileItem) -> None)
        let canDeleteItem = defaultArg canDeleteItem (fun (_: FileItem) -> false)

        let childRenderBatchSize =
            childRenderBatchSize
            |> Option.map (fun batchSize ->
                if batchSize < 1 then
                    invalidArg (nameof childRenderBatchSize) "Child render batch size must be at least one."

                batchSize
            )

        let includeSelectedDirectoryInVisiblePath =
            directoryInteractionMode = DirectoryInteractionMode.SingleClickToggle

        let model, dispatch = React.useReducer (reducer, initialModel)
        let containerRef = React.useElementRef ()

        let visibleChildCounts, setVisibleChildCounts =
            React.useStateWithUpdater (Map.empty<string, int>)

        // Permit only one increment while a directory's sentinel remains visible. In particular, browser
        // scroll anchoring must not keep a recreated sentinel visible and cascade through every batch.
        let directoriesAwaitingSentinelExit = React.useRef Set.empty<string>
        let previousDirectoryChildCounts = React.useRef Map.empty<string, int>

        let onDirectoryExpansionChange =
            onDirectoryExpansionChange
            |> Option.orElse onExpansionChange
            |> Option.orElse onDirectoryArrowToggle

        let scrollContainerClassName =
            if truncateOverflowingItemNames then
                "swt:w-full swt:min-w-0 swt:overflow-x-hidden"
            elif delegateHorizontalScrollToParent then
                "swt:w-max swt:min-w-full"
            else
                "swt:w-full swt:overflow-x-auto"

        let listClassName =
            if truncateOverflowingItemNames then
                "swt:w-full swt:min-w-0 swt:list-none swt:m-0 swt:p-0"
            else
                "swt:w-full swt:min-w-max swt:list-none swt:m-0 swt:p-0"

        React.useEffect (
            (fun () ->
                dispatch (
                    FileExplorerLogic.UpdateItems(
                        defaultArg initialItems [],
                        selectedItemId,
                        includeSelectedDirectoryInVisiblePath
                    )
                )
            ),
            [|
                box initialItems
                box selectedItemId
                box includeSelectedDirectoryInVisiblePath
            |]
        )

        React.useEffect (
            (fun () ->
                match childRenderBatchSize with
                | None -> ()
                | Some _ ->
                    let currentDirectoryIds = FileExplorerHelper.collectDirectoryIds model.Items
                    let currentChildCounts = FileExplorerHelper.collectDirectoryChildCounts model.Items

                    let directoriesWithNewChildren =
                        currentChildCounts
                        |> Map.toSeq
                        |> Seq.choose (fun (directoryId, childCount) ->
                            let previousCount =
                                previousDirectoryChildCounts.current
                                |> Map.tryFind directoryId
                                |> Option.defaultValue 0

                            if childCount > previousCount then
                                Some directoryId
                            else
                                None
                        )
                        |> Set.ofSeq

                    setVisibleChildCounts (fun counts ->
                        counts
                        |> Map.filter (fun directoryId _ -> currentDirectoryIds.Contains directoryId)
                    )

                    directoriesAwaitingSentinelExit.current <-
                        directoriesAwaitingSentinelExit.current
                        |> Set.filter (fun directoryId ->
                            currentDirectoryIds.Contains directoryId
                            && not (directoriesWithNewChildren.Contains directoryId)
                        )

                    previousDirectoryChildCounts.current <- currentChildCounts
            ),
            [| box model.Items; box childRenderBatchSize |]
        )

        let setExpanded (item: FileItem) (willExpand: bool) =
            let isExpanded = model.ExpandedIds.Contains item.Id

            if isExpanded <> willExpand then
                if not willExpand && childRenderBatchSize.IsSome then
                    setVisibleChildCounts (Map.remove item.Id)
                    directoriesAwaitingSentinelExit.current <- directoriesAwaitingSentinelExit.current.Remove item.Id

                dispatch (FileExplorerLogic.SetExpanded(item.Id, willExpand))
                onDirectoryExpansionChange |> Option.iter (fun fn -> fn item willExpand)

        let selectItem (item: FileItem) =
            dispatch (FileExplorerLogic.SelectItem item.Id)
            onItemClick |> Option.iter (fun fn -> fn item)

        let handleDirectorySelection (item: FileItem) (canExpand: bool) (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()

            if
                directoryInteractionMode = DirectoryInteractionMode.SingleClickToggle
                && canExpand
                && not (directoryChevronToggleOnlyForItem item)
            then
                setExpanded item (not (model.ExpandedIds.Contains item.Id))

            selectItem item

        let contextMenu =
            Swate.Components.Primitive.ContextMenu.ContextMenu.ContextMenu(
                (fun data ->
                    let item = data |> unbox<FileItem>
                    let isExpanded = model.ExpandedIds.Contains item.Id

                    FileExplorerHelper.getContextMenuItems
                        item
                        isExpanded
                        selectItem
                        onContextMenu
                        getCopyPath
                        getCopyRelativePath
                        includeDefaultContextMenuItems
                        setExpanded
                    |> List.map (fun x -> x.ToPrimitiveContextMenuItem())
                ),
                ref = containerRef,
                onSpawn =
                    (fun e ->
                        let trigger =
                            e
                            |> FileExplorerHelper.tryGetEventTargetElement
                            |> Option.bind (fun target -> target.closest ("[data-file-item-id]"))

                        match trigger, containerRef.current with
                        | Some trigger, Some container when container.contains (trigger) ->
                            let trigger = trigger :?> Browser.Types.HTMLElement
                            let itemId: string = !!trigger?dataset?fileItemId

                            match FileTree.findItem itemId model.Items with
                            | Some item ->
                                let menuItems =
                                    FileExplorerHelper.getContextMenuItems
                                        item
                                        (model.ExpandedIds.Contains item.Id)
                                        selectItem
                                        onContextMenu
                                        getCopyPath
                                        getCopyRelativePath
                                        includeDefaultContextMenuItems
                                        setExpanded

                                if List.isEmpty menuItems then None else Some(box item)
                            | None -> None
                        | _ -> None
                    )
            )

        let selectedPathIds = model.SelectedPath |> List.map _.Id |> Set.ofList

        let getVisibleChildCount (directory: FileItem) (children: FileItem list) =
            let childCount = List.length children

            match childRenderBatchSize with
            | None -> childCount
            | Some batchSize ->
                let storedCount =
                    visibleChildCounts |> Map.tryFind directory.Id |> Option.defaultValue batchSize

                let selectionRequiredCount =
                    children
                    |> List.tryFindIndex (fun child -> selectedPathIds.Contains child.Id)
                    |> Option.map (fun selectedIndex -> ((selectedIndex / batchSize) + 1) * batchSize)
                    |> Option.defaultValue 0

                min childCount (max storedCount selectionRequiredCount)

        let exposeNextChildBatch (directory: FileItem) childCount visibleCount =
            match childRenderBatchSize with
            | None -> ()
            | Some batchSize ->
                setVisibleChildCounts (fun counts ->
                    let nextCount = min childCount (visibleCount + batchSize)

                    if nextCount = visibleCount then
                        counts
                    else
                        counts |> Map.add directory.Id nextCount
                )

        let loadMoreChildren (directory: FileItem) childCount visibleCount =
            if visibleCount < childCount then
                exposeNextChildBatch directory childCount visibleCount
            elif hasMoreChildren directory then
                onLoadMoreChildren |> Option.iter (fun loadMore -> loadMore directory)

        let handleLoadMoreIntersection (directory: FileItem) childCount visibleCount isIntersecting =
            if isIntersecting then
                if not (directoriesAwaitingSentinelExit.current.Contains directory.Id) then
                    directoriesAwaitingSentinelExit.current <- directoriesAwaitingSentinelExit.current.Add directory.Id

                    if automaticallyLoadChildren then
                        loadMoreChildren directory childCount visibleCount
            else
                directoriesAwaitingSentinelExit.current <- directoriesAwaitingSentinelExit.current.Remove directory.Id

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

            let canExpand =
                match item.Children with
                | Some children -> not (List.isEmpty children)
                | None -> true

            let itemActions = getItemActions item
            let statusAction = getItemStatusAction item

            if item.IsDirectory then
                let childrenTree =
                    if isExpanded then
                        match item.Children with
                        | Some children ->
                            let childCount = List.length children
                            let visibleChildCount = getVisibleChildCount item children

                            let renderedChildren =
                                children |> List.truncate visibleChildCount |> List.map renderItem

                            Some(
                                Html.ul [
                                    prop.className "swt:ml-4"
                                    prop.children [
                                        yield! renderedChildren

                                        if visibleChildCount < childCount || hasMoreChildren item then
                                            FileExplorer.LoadMoreControl(
                                                item.Id,
                                                visibleChildCount,
                                                (fun () -> loadMoreChildren item childCount visibleChildCount),
                                                handleLoadMoreIntersection item childCount visibleChildCount
                                            )
                                    ]
                                ]
                            )
                        | None -> None
                    else
                        None

                FileExplorerItem.DirectoryRow(
                    item,
                    rowHighlightClass,
                    selectedNameClass,
                    isExpanded,
                    directoryChevronToggleOnlyForItem item,
                    canExpand,
                    getItemIconClass,
                    handleDirectorySelection item canExpand,
                    (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        setExpanded item (not isExpanded)
                    ),
                    ?onCreateItem = onCreateItem,
                    canCreateItem = canCreateItem,
                    itemActions = itemActions,
                    ?onDeleteItem = onDeleteItem,
                    canDeleteItem = canDeleteItem,
                    ?statusAction = statusAction,
                    ?children = childrenTree
                )
            else
                FileExplorerItem.FileRow(
                    item,
                    rowHighlightClass,
                    selectedNameClass,
                    getItemIconClass,
                    (fun () -> selectItem item),
                    itemActions = itemActions,
                    ?onDeleteItem = onDeleteItem,
                    canDeleteItem = canDeleteItem,
                    ?statusAction = statusAction
                )

        Html.div [
            prop.ref containerRef
            prop.className "swt:w-full"
            prop.children [
                Html.div [
                    prop.testId "file-explorer-scroll-container"
                    prop.className scrollContainerClassName
                    prop.children [
                        Html.ul [
                            prop.testId "file-explorer-container"
                            prop.className listClassName
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
                                                            FileTree.createFile
                                                                "Project-2-final.psd"
                                                                None
                                                                FileItemIcon.Document
                                                            FileTree.createFile
                                                                "Project-3-final.psd"
                                                                None
                                                                FileItemIcon.Document
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
            ContextMenuItem.create
                "Rename"
                "swt:fluent--rename-24-regular"
                (fun () -> Browser.Dom.console.log ("Rename", item.Name))
            ContextMenuItem.create
                "Delete"
                "swt:fluent--delete-24-regular"
                (fun () -> Browser.Dom.console.log ("Delete", item.Name))
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
