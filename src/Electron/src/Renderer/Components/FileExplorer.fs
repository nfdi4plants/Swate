module Renderer.Components.FileExplorer


open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorerTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core


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

    let rec loopPaths
        (loadedDirectoryPaths: Set<string>)
        (selectedTreeItemPath: string option)
        (parent: FileTreeNode)
        =
        match parent.isDirectory with
        | true ->
            let normalizedParentPath = normalizeNodePath parent.path

            let tmp =
                if loadedDirectoryPaths.Contains normalizedParentPath then
                    let ra = ResizeArray(parent.children.Values)

                    ra.ToArray()
                    |> Array.map (fun entry -> loopPaths loadedDirectoryPaths selectedTreeItemPath entry)
                    |> Array.choose id
                    |> List.ofArray
                else
                    []

            Some {
                FileTree.createFolder parent.name (Some parent.path) FileItemIcon.Folder with
                    Id = parent.path
                    IsExpanded =
                        selectedTreeItemPath
                        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                    IsLFS = parent.isLfs
                    Children = Some tmp
            }
        | false ->
            Some {
                FileTree.createFile parent.name (Some parent.path) FileItemIcon.Document with
                    Id = parent.path
                    IsLFS = parent.isLfs
            }

open FileExplorerHelper

[<Erase; Mangle(false)>]
type FileExplorer =

    [<ReactComponent>]
    static member private EmptyFileTreePlaceholder() =
        Html.div [
            prop.className "swt:p-4 swt:text-center swt:text-gray-500"
            prop.text "No files found."
        ]

    [<ReactComponent>]
    static member FileTree() =

        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
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
                    pageStateCtx.setState None
                | Some path ->
                    let selectedPath = normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                    let! result = Renderer.Components.ARCHelper.openView selectedPath

                    match result with
                    | Ok loaded ->
                        console.log ("[Renderer] Received data, processing...")

                        Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState loaded
                    | Error errorMessage ->
                        let fullErrorMessage = $"Could not open preview for '{item.Name}': {errorMessage}"
                        console.log ($"[Renderer] Error: {fullErrorMessage}")

                        Renderer.Components.ARCHelper.applyViewError pageStateCtx.setState fullErrorMessage
            }
            |> Promise.start

        match fileItem with
        | Some fileItem ->
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                selectedItemId = fileStateCtx.state.Selection.TreePath
            )
        | None -> FileExplorer.EmptyFileTreePlaceholder()
