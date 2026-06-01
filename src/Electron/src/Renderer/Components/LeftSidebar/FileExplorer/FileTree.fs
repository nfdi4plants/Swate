namespace Renderer.Components.LeftSidebar.FileExplorer

open Renderer.Components.ARCHelper
open Renderer.Components.FileExplorerDeleteHelper
open Swate.Components
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open ARCtrl
open Types
open Helper
open Renderer.Components.LeftSidebar.FileExplorer.Modals

module private FileTreeHelper =

    let saveArcFileAndOpen (arcFile: ArcFiles) : JS.Promise<Result<FileContentDTO, exn>> = promise {
        match FileContentDTO.fromArcFile arcFile with
        | None -> return Error(exn "Saving this file type is not supported in Electron yet.")
        | Some request ->
            match! Api.ipcArcVaultApi.addArcFile request with
            | Error saveError -> return Error saveError
            | Ok() ->
                return! Api.ipcArcVaultApi.openFile request.path
    }

open FileTreeHelper

[<Erase; Mangle(false)>]
type FileTree =

    [<ReactComponent>]
    static member private EmptyFileTreePlaceholder() =
        Html.div [
            prop.className "swt:p-4 swt:text-center swt:text-gray-500"
            prop.text "No files found."
        ]

    [<ReactComponent>]
    static member FileTree(rootContextMenuRef: IRefValue<Browser.Types.HTMLElement option>) =

        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
        let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
        let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
        let errorModal = useErrorModalCtx ()
        let arcScopeId = useCurrentArcScopeId ()

        let pendingCreateKind, setPendingCreateKind =
            React.useState<ArcExplorerNodeKind option> None

        let pendingFileSystemCreateDraft, setPendingFileSystemCreateDraft =
            React.useState<FileSystemCreateDraft option> None

        let pendingRenameDraft, setPendingRenameDraft = React.useState<ArcRenameDraft option> None
        let isCreatingFileSystemItem, setIsCreatingFileSystemItem = React.useState false
        let isRenaming, setIsRenaming = React.useState false
        let pendingDeleteItem, setPendingDeleteItem = React.useState<FileItem option> None
        let isDeleting, setIsDeleting = React.useState false
        let hasObservedFileTreeUpdateRef = React.useRef false

        let effectiveFileTree =
            React.useMemo ((fun () -> fileStateCtx.state.FileTree), [| box fileStateCtx.state.FileTree |])

        React.useEffect (
            (fun () ->
                let filePaths =
                    effectiveFileTree
                    |> Array.map (fun entry -> entry.path)

                if FileExplorerDeleteHelper.isSelectionMissing filePaths fileStateCtx.state.Selection.TreePath then
                    fileStateCtx.setSelection ArcSelection.empty

                    if FileExplorerDeleteHelper.shouldResetPageStateAfterSelectionRemoval pageStateCtx.state then
                        pageStateCtx.setState None
            ),
            [|
                box effectiveFileTree
                box fileStateCtx.state.Selection.TreePath
                box pageStateCtx.state
            |]
        )

        let fileTree : FileTreeNode option =
            React.useMemo (
                (fun () ->
                    match effectiveFileTree with
                    | [||] -> None
                    | _ ->
                        effectiveFileTree
                        |> toFileTreeNode
                        |> collapseSingleChildSameNameDirectories
                        |> Some
                ),
                [| box effectiveFileTree |]
            )

        let requiredLoadedDirectories =
            React.useMemo (
                (fun () ->
                    match fileTree with
                    | Some tree -> requiredLoadedDirectoryPaths fileStateCtx.state.Selection.TreePath tree
                    | None -> Set.empty
                ),
                [|
                    box fileTree
                    box fileStateCtx.state.Selection.TreePath
                |]
            )

        let loadedDirectoryPaths, setLoadedDirectoryPaths =
            React.useStateWithUpdater requiredLoadedDirectories

        React.useEffect ((fun () -> setLoadedDirectoryPaths (fun _ -> requiredLoadedDirectories)), [| box fileTree |])

        React.useEffect (
            (fun () ->
                setLoadedDirectoryPaths (fun current ->
                    let next = Set.union current requiredLoadedDirectories

                    if next = current then current else next
                )
            ),
            [| box requiredLoadedDirectories |]
        )

        let fileItem =
            fileTree
            |> Option.bind (loopPaths loadedDirectoryPaths fileStateCtx.state.Selection.TreePath)

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (
                            $"File '{item.Name}' has no path.",
                            title = "Preview failed",
                            ?scopeId = arcScopeId
                        )
                    )
                | Some path when item.IsDirectory ->
                    if not item.IsExpanded then
                        setLoadedDirectoryPaths (fun current ->
                            let normalizedPath = PathHelpers.normalizePath path

                            if current.Contains normalizedPath then
                                current
                            else
                                current.Add normalizedPath
                        )

                    let selectedPath = PathHelpers.normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
                    pageStateCtx.setState None
                | Some path ->
                    let selectedPath = PathHelpers.normalizePath path
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

        let reloadSelectedPreviewAfterFileTreeUpdate () =
            match
                FileExplorerDeleteHelper.tryGetReloadableSelectedFilePath
                    fileStateCtx.state.FileTree
                    fileStateCtx.state.Selection.TreePath
                    pageStateCtx.state
            with
            | None -> ()
            | Some selectedPath ->
                promise {
                    let! result = Renderer.Components.ARCHelper.openView selectedPath

                    match result with
                    | Ok loaded -> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState loaded
                    | Error errorMessage ->
                        Renderer.Components.ARCHelper.applyViewError
                            pageStateCtx.setState
                            $"Could not reload preview for '{selectedPath}': {errorMessage}"
                }
                |> Promise.catch (fun exn ->
                    Renderer.Components.ARCHelper.applyViewError
                        pageStateCtx.setState
                        $"Could not reload preview for '{selectedPath}': {exn.Message}")
                |> Promise.start

        React.useEffect (
            (fun () ->
                if hasObservedFileTreeUpdateRef.current then
                    reloadSelectedPreviewAfterFileTreeUpdate ()
                else
                    hasObservedFileTreeUpdateRef.current <- true),
            [| box fileStateCtx.state.FileTree |]
        )

        let handleDirectoryArrowToggle (item: FileItem) (willExpand: bool) =
            if willExpand then
                match item.Path with
                | Some path ->
                    setLoadedDirectoryPaths (fun current ->
                        let normalizedPath = PathHelpers.normalizePath path

                        if current.Contains normalizedPath then
                            current
                        else
                            current.Add normalizedPath
                    )
                | None -> ()

        let closeCreateModal () = setPendingCreateKind None

        let closeFileSystemCreateModal () =
            setPendingFileSystemCreateDraft None

        let openCreateModal kind = setPendingCreateKind (Some kind)

        let openFileSystemCreateModal kind item =
            if canCreateFileSystemItemIn item then
                setPendingFileSystemCreateDraft (Some { Parent = item; Kind = kind })

        let closeDeleteModal () =
            setPendingDeleteItem None

        let closeRenameModal () =
            setPendingRenameDraft None

        let requestDeleteItem =
            FileTreeDeleteWorkflow.requestDeleteItem setPendingDeleteItem

        let requestRenameItem =
            FileTreeRenameWorkflow.requestRenameItem setPendingRenameDraft errorModal.enqueue arcScopeId

        let rootPath = fileTree |> Option.map (fun (tree: FileTreeNode) -> tree.path)

        let inlineCreateKindForItem item =
            match rootPath with
            | Some path -> tryGetInlineArcCreateKind path item
            | None -> None

        let canCreateFromItem item =
            inlineCreateKindForItem item |> Option.isSome

        let createFromItem item =
            inlineCreateKindForItem item |> Option.iter openCreateModal

        let applyCreateError errorMessage =
            errorModal.enqueue (ErrorModalRequest.create (errorMessage, title = "Could not create ARC file", ?scopeId = arcScopeId))

        let applyFileSystemCreateError errorMessage =
            errorModal.enqueue (
                ErrorModalRequest.create (errorMessage, title = "Could not create file or folder", ?scopeId = arcScopeId)
            )

        let reloadPreviewByPath (path: string) : JS.Promise<Result<unit, string>> =
            promise {
                let! openResult = Renderer.Components.ARCHelper.openView path

                match openResult with
                | Ok loaded ->
                    Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState loaded
                    return Ok()
                | Error errorMessage -> return Error errorMessage
            }

        let confirmDeleteItem () =
            FileTreeDeleteWorkflow.confirmDeleteItem {
                pendingDeleteItem = pendingDeleteItem
                closeDeleteModal = closeDeleteModal
                setIsDeleting = setIsDeleting
                enqueueError = errorModal.enqueue
                arcScopeId = arcScopeId
            }

        let createArcEntry kind (identifier: string) =
            let existingPaths = effectiveFileTree |> Array.map (fun entry -> entry.path)

            match tryBuildArcCreateDraft kind identifier existingPaths with
            | Error errorMessage -> applyCreateError errorMessage
            | Ok draft ->
                promise {
                    let! createResult = saveArcFileAndOpen draft.ArcFile

                    match createResult with
                    | Error exn -> applyCreateError exn.Message
                    | Ok createdArcFileDto ->
                        let selectedPath = PathHelpers.normalizePath createdArcFileDto.path
                        fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                        createdArcFileDto
                        |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                        |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState

                        closeCreateModal ()
                }
                |> Promise.catch (fun exn -> applyCreateError exn.Message)
                |> Promise.start

        let createFileSystemItem (name: string) =
            match pendingFileSystemCreateDraft with
            | None -> closeFileSystemCreateModal ()
            | Some draft ->
                match tryGetItemRelativePath draft.Parent with
                | None -> applyFileSystemCreateError "Could not resolve the selected folder path."
                | Some parentPath ->
                    setIsCreatingFileSystemItem true

                    promise {
                        let! createResult =
                            Api.ipcArcVaultApi.createFileSystemItem {
                                parentPath = parentPath
                                name = name
                                kind = draft.Kind
                            }

                        match createResult with
                        | Error exn -> applyFileSystemCreateError exn.Message
                        | Ok createdPath ->
                            let selectedPath = PathHelpers.normalizePath createdPath
                            fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                            match draft.Kind with
                            | FileSystemItemKind.File ->
                                let! openResult = Api.ipcArcVaultApi.openFile selectedPath

                                match openResult with
                                | Ok dto ->
                                    dto
                                    |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                                    |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState
                                | Error _ ->
                                    FileContentDTO.create ARCtrl.Contract.DTOType.PlainText "" selectedPath
                                    |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                                    |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState
                            | FileSystemItemKind.Folder ->
                                setLoadedDirectoryPaths (fun current -> current.Add selectedPath)
                                pageStateCtx.setState None

                            closeFileSystemCreateModal ()
                    }
                    |> Promise.catch (fun exn -> applyFileSystemCreateError exn.Message)
                    |> Promise.map (fun _ -> setIsCreatingFileSystemItem false)
                    |> Promise.start

        let renameContextMenuItems =
            FileTreeContextMenu.renameContextMenuItems requestRenameItem

        let contextMenuConfig: FileTreeContextMenu.ContextMenuConfig = {
                openItem = openPreview
                arcRootPath = appStateCtx
                openCreateModal = openCreateModal
                openFileSystemCreateModal = openFileSystemCreateModal
                requestRenameItem = requestRenameItem
                requestDeleteItem = requestDeleteItem
                pathActionConfig = {
                    openPathInFileExplorer = Api.ipcArcVaultApi.showPathInFileExplorer
                    openPathWithDefaultApplication = Api.ipcArcVaultApi.openPathWithDefaultApplication
                    enqueueError = errorModal.enqueue
                    arcScopeId = arcScopeId
                }
                enqueueError = errorModal.enqueue
                arcScopeId = arcScopeId
                runToggleLfsMark = Renderer.Components.ARCHelper.runToggleLfsMark
                runFreeLocalLfsCopy = Renderer.Components.ARCHelper.runFreeLocalLfsCopy
            }

        let createContextMenuItems =
            FileTreeContextMenu.createContextMenuItems contextMenuConfig

        let rootContextMenu rootItem =
            let rootMenuItem = { rootItem with Path = Some ""; IsDirectory = true }

            Swate.Components.Primitive.ContextMenu.ContextMenu.ContextMenu(
                (fun _ ->
                    FileTreeContextMenu.rootContextMenuItems contextMenuConfig rootMenuItem
                    |> List.map Swate.Components.Page.FileExplorer.Helper.toPrimitiveContextMenuItem),
                ref = rootContextMenuRef,
                onSpawn = (fun _ -> Some(box ()))
            )

        let confirmRenameItem (newName: string) =
            FileTreeRenameWorkflow.confirmRenameItem
                {
                    pendingRenameDraft = pendingRenameDraft
                    selectedTreePath = fileStateCtx.state.Selection.TreePath
                    pageState = pageStateCtx.state
                    closeRenameModal = closeRenameModal
                    setIsRenaming = setIsRenaming
                    setSelection = fileStateCtx.setSelection
                    refreshGitStatus = gitStateCtx.refresh
                    reloadPreviewByPath = reloadPreviewByPath
                    renamePath = Api.ipcArcVaultApi.renamePath
                    enqueueError = errorModal.enqueue
                    arcScopeId = arcScopeId
                }
                newName

        let activeCreateKind =
            pendingCreateKind |> Option.defaultValue ArcExplorerNodeKind.Study

        let arcCreateModal =
            CreateArcFileModal.Main(
                isOpen = pendingCreateKind.IsSome,
                kind = activeCreateKind,
                close = closeCreateModal,
                submit = createArcEntry
            )

        let activeFileSystemCreateKind =
            pendingFileSystemCreateDraft
            |> Option.map _.Kind
            |> Option.defaultValue FileSystemItemKind.File

        let fileSystemCreateModal =
            CreateFileSystemItemModal.Main(
                isOpen = pendingFileSystemCreateDraft.IsSome,
                kind = activeFileSystemCreateKind,
                parentName = (pendingFileSystemCreateDraft |> Option.map _.Parent.Name),
                close = closeFileSystemCreateModal,
                submit = createFileSystemItem,
                isCreating = isCreatingFileSystemItem
            )

        let deleteConfirmModal =
            FileTreeDeleteModal.Main(
                isOpen = pendingDeleteItem.IsSome,
                itemName = (pendingDeleteItem |> Option.map _.Name),
                close = closeDeleteModal,
                submit = confirmDeleteItem,
                isDeleting = isDeleting
            )

        let renameModal =
            FileTreeRenameModal.Main(
                isOpen = pendingRenameDraft.IsSome,
                itemName = (pendingRenameDraft |> Option.map (fun draft -> draft.Item.Name)),
                initialName = (pendingRenameDraft |> Option.map _.InitialName),
                close = closeRenameModal,
                submit = confirmRenameItem,
                isRenaming = isRenaming
            )

        match fileItem with
        | Some rootItem ->
            let visibleItems = rootItem.Children |> Option.defaultValue []

            React.Fragment [
                Html.div [
                    prop.className "swt:w-full"
                    prop.children [
                        Swate.Components.Page.FileExplorer.FileExplorer.FileExplorer(
                            initialItems = visibleItems,
                            onItemClick = openPreview,
                            onDirectoryArrowToggle = handleDirectoryArrowToggle,
                            onContextMenu = createContextMenuItems,
                            getItemIconClass = getItemIconClass,
                            canCreateItem = canCreateFromItem,
                            onCreateItem = createFromItem,
                            getItemActions = renameContextMenuItems,
                            canDeleteItem = canDeleteItem,
                            onDeleteItem = requestDeleteItem,
                            selectedItemId = fileStateCtx.state.Selection.TreePath,
                            showBreadcrumbs = false,
                            includeDefaultContextMenuItems = false
                        )
                    ]
                ]
                rootContextMenu rootItem
                arcCreateModal
                fileSystemCreateModal
                renameModal
                deleteConfirmModal
            ]
        | None ->
            React.Fragment [
                FileTree.EmptyFileTreePlaceholder()
                arcCreateModal
                fileSystemCreateModal
                renameModal
                deleteConfirmModal
            ]
