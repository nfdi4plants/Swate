module Renderer.Components.FileExplorer


open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorerTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz


module private FileExplorerHelper =

    let private normalizeNodePath (path: string) = normalizePath path

    let rec private collectSelectedDirectoryPathChain
        (selectedTreeItemPath: string option)
        (node: FileTreeNode)
        (loadedPaths: Set<string>)
        =
        let normalizedNodePath = normalizeNodePath node.path

        let isInSelectedPathChain =
            selectedTreeItemPath
            |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath normalizedNodePath)

        if node.isDirectory then
            let nextLoadedPaths =
                if isInSelectedPathChain then
                    Set.add normalizedNodePath loadedPaths
                else
                    loadedPaths

            node.children.Values
            |> Seq.fold
                (fun state child -> collectSelectedDirectoryPathChain selectedTreeItemPath child state)
                nextLoadedPaths
        else
            loadedPaths

    let requiredLoadedDirectoryPaths (selectedTreeItemPath: string option) (root: FileTreeNode) =
        let rootPathSet =
            if root.isDirectory then
                Set.singleton (normalizeNodePath root.path)
            else
                Set.empty

        collectSelectedDirectoryPathChain selectedTreeItemPath root rootPathSet

    let private mapLfsSize (sizeBytes: float option) =
        let size = sizeBytes |> Option.map int64
        size, (size |> Option.map FileTree.formatSize)

    let rec loopPaths
        (loadedDirectoryPaths: Set<string>)
        (selectedTreeItemPath: string option)
        (parent: FileTreeNode)
        =
        match parent.isDirectory with
        | true ->
            let lfsSize, lfsSizeFormatted = mapLfsSize parent.lfsSizeBytes
            let normalizedParentPath = normalizeNodePath parent.path
            let isDirectoryLoaded = loadedDirectoryPaths.Contains normalizedParentPath
            let hasSourceChildren = parent.children.Count > 0

            let mappedChildren =
                if isDirectoryLoaded then
                    let ra = ResizeArray(parent.children.Values)

                    ra.ToArray()
                    |> Array.map (fun entry -> loopPaths loadedDirectoryPaths selectedTreeItemPath entry)
                    |> Array.choose id
                    |> List.ofArray
                else
                    []

            let children =
                if isDirectoryLoaded then
                    Some mappedChildren
                elif hasSourceChildren then
                    None
                else
                    Some []

            Some {
                FileTree.createFolder parent.name (Some parent.path) FileItemIcon.Folder with
                    Id = parent.path
                    IsExpanded =
                        selectedTreeItemPath
                        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                    IsLFS = parent.isLfs
                    IsLFSPointer = parent.isLfsPointer
                    Downloaded = parent.downloaded
                    Size = lfsSize
                    SizeFormatted = lfsSizeFormatted
                    Children = children
            }
        | false ->
            let lfsSize, lfsSizeFormatted = mapLfsSize parent.lfsSizeBytes
            Some {
                FileTree.createFile parent.name (Some parent.path) FileItemIcon.Document with
                    Id = parent.path
                    IsLFS = parent.isLfs
                    IsLFSPointer = parent.isLfsPointer
                    Downloaded = parent.downloaded
                    Size = lfsSize
                    SizeFormatted = lfsSizeFormatted
            }

open FileExplorerHelper

[<ReactComponent>]
let EmptyFileTreePlaceholder () =
    Html.div [
        prop.className "swt:p-4 swt:text-center swt:text-gray-500"
        prop.text "No files found."
    ]

[<ReactComponent>]
let FileTree () =

    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()
    let errorModal = ErrorModal.Context.useErrorModalCtx ()
    let arcScopeId = useCurrentArcScopeId ()

    let fileTree =
        React.useMemo (
            (fun () ->
                match fileStateCtx.state.FileTree with
                | [||] -> None
                | _ ->
                    fileStateCtx.state.FileTree
                    |> toFileTreeNode
                    |> collapseSingleChildSameNameDirectories
                    |> Some),
            [| box fileStateCtx.state.FileTree |]
        )

    let requiredLoadedDirectories =
        React.useMemo (
            (fun () ->
                match fileTree with
                | Some tree -> requiredLoadedDirectoryPaths fileStateCtx.state.Selection.TreePath tree
                | None -> Set.empty),
            [| box fileTree; box fileStateCtx.state.Selection.TreePath |]
        )

    let loadedDirectoryPaths, setLoadedDirectoryPaths =
        React.useStateWithUpdater requiredLoadedDirectories

    React.useEffect (
        (fun () -> setLoadedDirectoryPaths (fun _ -> requiredLoadedDirectories)),
        [| box fileTree |]
    )

    React.useEffect (
        (fun () ->
            setLoadedDirectoryPaths (fun current ->
                let next = Set.union current requiredLoadedDirectories

                if next = current then
                    current
                else
                    next)),
        [| box requiredLoadedDirectories |]
    )

    let fileItem =
        fileTree
        |> Option.bind (loopPaths loadedDirectoryPaths fileStateCtx.state.Selection.TreePath)

    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg -> errorModal.enqueue (ErrorModalRequest.create(msg, title = "Git LFS update failed", ?scopeId = arcScopeId))
        | None -> ()

    let toggleLfsMark =
        FileExplorerGitLfsHelper.ToggleLfsMark(setError, Renderer.Components.ARCHelper.runToggleLfsMark)

    let contextMenuItems (item: FileItem) =
        FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

    let openPreview (item: FileItem) =
        promise {
            match item.Path with
            | None ->
                errorModal.enqueue (
                    ErrorModalRequest.create($"File '{item.Name}' has no path.", title = "Preview failed", ?scopeId = arcScopeId)
                )
            | Some path when item.IsDirectory ->
                if not item.IsExpanded then
                    setLoadedDirectoryPaths (fun current ->
                        let normalizedPath = normalizePath path

                        if current.Contains normalizedPath then
                            current
                        else
                            current.Add normalizedPath)

                let selectedPath = normalizePath path
                fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                Renderer.Components.ARCHelper.clearArcObjectPreview
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage

                pageStateCtx.setState None
            | Some path ->
                let selectedPath = normalizePath path
                fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                let! result = Renderer.Components.ARCHelper.openView selectedPath

                match result with
                | Ok loaded ->
                    console.log ("[Renderer] Received data, processing...")

                    Renderer.Components.ARCHelper.applyLoadedView
                        pageStateCtx.setState
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage
                        loaded
                | Error errorMessage ->
                    let fullErrorMessage = $"Could not open preview for '{item.Name}': {errorMessage}"
                    console.log ($"[Renderer] Error: {fullErrorMessage}")

                    Renderer.Components.ARCHelper.applyViewError
                        pageStateCtx.setState
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage
                        fullErrorMessage
        }
        |> Promise.start

    let handleDirectoryArrowToggle (item: FileItem) (willExpand: bool) =
        if willExpand then
            match item.Path with
            | Some path ->
                setLoadedDirectoryPaths (fun current ->
                    let normalizedPath = normalizePath path

                    if current.Contains normalizedPath then
                        current
                    else
                        current.Add normalizedPath)
            | None -> ()

    let arcName =
        let fromRootItem = fileItem |> Option.map (fun root -> root.Name)

        let fromAppPath =
            appStateCtx.state
            |> Option.bind (fun path ->
                let normalizedPath = normalizePath path
                if System.String.IsNullOrWhiteSpace normalizedPath then
                    None
                else
                    Some(getFileName normalizedPath)
            )

        fromAppPath
        |> Option.orElse fromRootItem
        |> Option.defaultValue "ARC"

    match fileItem with
    | Some rootItem ->
        let visibleItems = rootItem.Children |> Option.defaultValue []

        Html.div [
            prop.className "swt:w-full"
            prop.children [
                Html.div [
                    prop.testId "left-sidebar-file-explorer-arc-name"
                    prop.className "swt:mb-2 swt:px-2 swt:text-sm swt:font-semibold swt:truncate"
                    prop.text arcName
                ]
                Swate.Components.FileExplorer.FileExplorer(
                    initialItems = visibleItems,
                    onItemClick = openPreview,
                    onDirectoryArrowToggle = handleDirectoryArrowToggle,
                    onContextMenu = contextMenuItems,
                    selectedItemId = fileStateCtx.state.Selection.TreePath,
                    showBreadcrumbs = false
                )
            ]
        ]
    | None -> EmptyFileTreePlaceholder()
