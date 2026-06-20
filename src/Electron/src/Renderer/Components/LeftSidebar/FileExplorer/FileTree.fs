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

    type FileTreeDialog =
        | CreateDialog of ArcExplorerNodeKind
        | RenameDialog of ArcRenameDraft
        | DeleteDialog of FileItem

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
    static member FileTree() =

        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
        let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
        let errorModal = useErrorModalCtx ()
        let arcScopeId = useCurrentArcScopeId ()

        let activeDialog, setActiveDialog = React.useState<FileTreeDialog option> None
        let isDialogBusy, setIsDialogBusy = React.useState false
        let hasObservedFileTreeUpdateRef = React.useRef false
        let skipNextFileTreeReloadPathRef = React.useRef<string option> None

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

        let openSelectedPreview (itemName: string) (selectedPath: string) =
            promise {
                let! result = Renderer.Components.ARCHelper.openView selectedPath

                match result with
                | Ok loaded ->
                    console.log ("[Renderer] Received data, processing...")
                    Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState loaded
                | Error errorMessage ->
                    let fullErrorMessage = $"Could not open preview for '{itemName}': {errorMessage}"
                    console.log ($"[Renderer] Error: {fullErrorMessage}")
                    Renderer.Components.ARCHelper.applyViewError pageStateCtx.setState fullErrorMessage
            }

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

                    if Swate.Components.Page.FileExplorer.Helper.needsLfsDownload item then
                        skipNextFileTreeReloadPathRef.current <- Some selectedPath
                        console.log ($"[Renderer] Downloading Git LFS content for '{selectedPath}' before preview.")

                        let! downloadResult = Renderer.Components.ARCHelper.runDownloadLfsFile selectedPath

                        match downloadResult with
                        | Error errorMessage ->
                            skipNextFileTreeReloadPathRef.current <- None
                            let fullErrorMessage =
                                $"Could not download Git LFS content for '{item.Name}': {errorMessage}"

                            console.log ($"[Renderer] Error: {fullErrorMessage}")
                            Renderer.Components.ARCHelper.applyViewError pageStateCtx.setState fullErrorMessage
                        | Ok () -> do! openSelectedPreview item.Name selectedPath
                    else
                        do! openSelectedPreview item.Name selectedPath
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
                    match skipNextFileTreeReloadPathRef.current, fileStateCtx.state.Selection.TreePath with
                    | Some pendingPath, Some selectedPath when PathHelpers.pathsEqual pendingPath selectedPath ->
                        skipNextFileTreeReloadPathRef.current <- None
                    | Some _, _ ->
                        skipNextFileTreeReloadPathRef.current <- None
                        reloadSelectedPreviewAfterFileTreeUpdate ()
                    | None, _ -> reloadSelectedPreviewAfterFileTreeUpdate ()
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

        let openDialog dialog =
            setIsDialogBusy false
            setActiveDialog (Some dialog)

        let closeDialog () =
            setIsDialogBusy false
            setActiveDialog None

        let openCreateModal kind =
            openDialog (CreateDialog kind)

        let requestDeleteItem =
            FileTreeDeleteWorkflow.requestDeleteItem (Option.iter (DeleteDialog >> openDialog))

        let requestRenameItem =
            FileTreeRenameWorkflow.requestRenameItem
                (Option.iter (RenameDialog >> openDialog))
                errorModal.enqueue
                arcScopeId

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

        let reloadPreviewByPath (path: string) : JS.Promise<Result<unit, string>> =
            promise {
                let! openResult = Renderer.Components.ARCHelper.openView path

                match openResult with
                | Ok loaded ->
                    Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState loaded
                    return Ok()
                | Error errorMessage -> return Error errorMessage
            }

        let activeCreateKind, activeRenameDraft, activeDeleteItem =
            match activeDialog with
            | Some(CreateDialog kind) -> Some kind, None, None
            | Some(RenameDialog renameDraft) -> None, Some renameDraft, None
            | Some(DeleteDialog item) -> None, None, Some item
            | None -> None, None, None

        let confirmDeleteItem () =
            if not isDialogBusy then
                FileTreeDeleteWorkflow.confirmDeleteItem {
                    pendingDeleteItem = activeDeleteItem
                    closeDeleteModal = closeDialog
                    setIsDeleting = setIsDialogBusy
                    enqueueError = errorModal.enqueue
                    arcScopeId = arcScopeId
                }

        let createArcEntry kind (identifier: string) =
            if not isDialogBusy then
                let existingPaths = effectiveFileTree |> Array.map (fun entry -> entry.path)

                match tryBuildArcCreateDraft kind identifier existingPaths with
                | Error errorMessage -> applyCreateError errorMessage
                | Ok draft ->
                    setIsDialogBusy true

                    promise {
                        let! createResult = saveArcFileAndOpen draft.ArcFile

                        match createResult with
                        | Error exn ->
                            setIsDialogBusy false
                            applyCreateError exn.Message
                        | Ok createdArcFileDto ->
                            let selectedPath = PathHelpers.normalizePath createdArcFileDto.path
                            fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                            createdArcFileDto
                            |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                            |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState

                            closeDialog ()
                    }
                    |> Promise.catch (fun exn ->
                        setIsDialogBusy false
                        applyCreateError exn.Message)
                    |> Promise.start

        let arcCreateContextMenuItems (item: FileItem) =
            inlineCreateKindForItem item
            |> Option.map (fun kind ->
                FileExplorerContextMenuItem.create
                    $"Add {ArcExplorerNodeKind.label kind}"
                    (arcCreateKindIcon kind)
                    (fun () -> openCreateModal kind))
            |> Option.toList

        let deleteContextMenuItems =
            FileTreeDeleteWorkflow.deleteContextMenuItems requestDeleteItem

        let renameContextMenuItems =
            FileTreeRenameWorkflow.renameContextMenuItems requestRenameItem

        let baseContextMenuItems (item: FileItem) =
            arcCreateContextMenuItems item
            @ renameContextMenuItems item
            @ deleteContextMenuItems item

        let createContextMenuItems =
            Renderer.Components.FileExplorerLfs.createContextMenuItems
                errorModal.enqueue
                arcScopeId
                baseContextMenuItems

        let getItemStatusAction =
            Renderer.Components.FileExplorerLfs.createLfsPillAction errorModal.enqueue arcScopeId

        let confirmRenameItem (newName: string) =
            if not isDialogBusy then
                FileTreeRenameWorkflow.confirmRenameItem
                    {
                        pendingRenameDraft = activeRenameDraft
                        selectedTreePath = fileStateCtx.state.Selection.TreePath
                        pageState = pageStateCtx.state
                        closeRenameModal = closeDialog
                        setIsRenaming = setIsDialogBusy
                        setSelection = fileStateCtx.setSelection
                        refreshGitStatus = gitStateCtx.refresh
                        reloadPreviewByPath = reloadPreviewByPath
                        renamePath = Api.ipcArcVaultApi.renamePath
                        enqueueError = errorModal.enqueue
                        arcScopeId = arcScopeId
                    }
                    newName

        let createModalKind =
            activeCreateKind |> Option.defaultValue ArcExplorerNodeKind.Study

        let arcCreateModal =
            CreateArcFileModal.Main(
                isOpen = activeCreateKind.IsSome,
                kind = createModalKind,
                close = closeDialog,
                submit = createArcEntry,
                isCreating = isDialogBusy
            )

        let deleteConfirmModal =
            FileTreeDeleteModal.Main(
                isOpen = activeDeleteItem.IsSome,
                itemName = (activeDeleteItem |> Option.map _.Name),
                close = closeDialog,
                submit = confirmDeleteItem,
                isDeleting = isDialogBusy
            )

        let renameModal =
            FileTreeRenameModal.Main(
                isOpen = activeRenameDraft.IsSome,
                itemName = (activeRenameDraft |> Option.map (fun draft -> draft.Item.Name)),
                initialName = (activeRenameDraft |> Option.map _.InitialName),
                close = closeDialog,
                submit = confirmRenameItem,
                isRenaming = isDialogBusy
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
                            getItemStatusAction = getItemStatusAction,
                            canDeleteItem = canDeleteItem,
                            onDeleteItem = requestDeleteItem,
                            selectedItemId = fileStateCtx.state.Selection.TreePath,
                            useParentHorizontalScroll = true,
                            showBreadcrumbs = false
                        )
                    ]
                ]
                arcCreateModal
                renameModal
                deleteConfirmModal
            ]
        | None ->
            React.Fragment [
                FileTree.EmptyFileTreePlaceholder()
                arcCreateModal
                renameModal
                deleteConfirmModal
            ]
