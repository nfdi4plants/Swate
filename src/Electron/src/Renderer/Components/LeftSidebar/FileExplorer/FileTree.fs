namespace Renderer.Components.LeftSidebar.FileExplorer

open Renderer.Components.Helper.ArcViewHelper
open Renderer.Components.FileExplorerDeleteHelper
open Swate.Components
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open ARCtrl
open Renderer.Components.LeftSidebar.FileExplorer.Modals
open Types
open Helper
open FileTreeMaterialization

module private FileTreeHelper =

    type FileTreeDialog =
        | CreateDialog of ArcExplorerNodeKind
        | FileSystemCreateDialog of FileSystemCreateDraft
        | AssignNoteDialog of ExistingTargetRef
        | RenameDialog of ArcRenameDraft
        | DeleteDialog of FileItem

    type AssignNoteDialogState = {
        Target: ExistingTargetRef option
        AvailableNotes: ResizeArray<AssignableNoteRef>
        AvailableAssets: ResizeArray<AssignableNoteAssetRef>
    }

    let createAssignNoteDialogState activeDialog fileEntries selectedNote =
        let target =
            match activeDialog with
            | Some(AssignNoteDialog target) -> Some target
            | _ -> None

        {
            Target = target
            AvailableNotes = FileTreeAssignNoteHelper.createAssignableNoteOptions fileEntries
            AvailableAssets = FileTreeAssignNoteHelper.createAssignableNoteAssetOptions fileEntries selectedNote
        }

    let saveArcFileAndOpen (arcFile: ArcFiles) : JS.Promise<Result<FileContentDTO, exn>> = promise {
        match FileContentDTO.fromArcFile arcFile with
        | None -> return Error(exn "Saving this file type is not supported in Electron yet.")
        | Some request ->
            match! Api.ipcArcVaultApi.addArcFile request with
            | Error saveError -> return Error saveError
            | Ok() -> return! Api.ipcArcVaultApi.openFile request.path
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

        let arcScopeId =
            appStateCtx
            |> Option.map PathHelpers.normalizePath
            |> Option.bind (fun path ->
                if System.String.IsNullOrWhiteSpace path then
                    None
                else
                    Some path
            )

        let activeDialog, setActiveDialog = React.useState<FileTreeDialog option> None
        let isDialogBusy, setIsDialogBusy = React.useState false

        let selectedAssignableNote, setSelectedAssignableNote =
            React.useState<AssignableNoteRef option> None

        let selectedAssetDestinations, setSelectedAssetDestinations =
            React.useStateWithUpdater<Map<string, AssignNoteAssetDestination>> Map.empty

        // The file watcher emits the initial tree too; only later tree updates should refresh open previews.
        let hasObservedFileTreeUpdateRef = React.useRef false

        React.useEffect (
            (fun () ->
                let filePaths = fileStateCtx.state.FileTree |> Array.map (fun entry -> entry.path)

                if FileExplorerDeleteHelper.isSelectionMissing filePaths fileStateCtx.state.Selection.TreePath then
                    fileStateCtx.setSelection ArcSelection.empty

                    if FileExplorerDeleteHelper.shouldResetPageStateAfterSelectionRemoval pageStateCtx.state then
                        pageStateCtx.setState None
            ),
            [|
                box fileStateCtx.state.FileTree
                box fileStateCtx.state.Selection.TreePath
                box pageStateCtx.state
            |]
        )

        let fileTree: FileTreeNode option =
            React.useMemo (
                (fun () ->
                    match fileStateCtx.state.FileTree with
                    | [||] -> None
                    | _ ->
                        fileStateCtx.state.FileTree
                        |> toFileTreeNode
                        |> collapseSingleChildSameName
                        |> Some
                ),
                [| box fileStateCtx.state.FileTree |]
            )

        let materializedState, setMaterializedState =
            React.useStateWithUpdater FileTreeMaterialization.empty

        let reconciledMaterializedState =
            reconcileMaterializedState arcScopeId fileStateCtx.state.Selection.TreePath fileTree materializedState

        React.useEffect (
            (fun () ->
                setMaterializedState (fun current ->
                    if reconciledMaterializedState = current then
                        current
                    else
                        reconciledMaterializedState
                )
            ),
            [|
                box arcScopeId
                box fileTree
                box fileStateCtx.state.Selection.TreePath
            |]
        )

        let fileItem =
            fileTree
            |> Option.map (
                FileTreeMaterialization.toMaterializedFileItemTree Helper.createItem reconciledMaterializedState.Paths
            )

        let openSelectedPreview (itemName: string) (selectedPath: string) = promise {
            let! result = openView selectedPath

            match result with
            | Ok pageState ->
                console.log ("[Renderer] Received data, processing...")
                pageStateCtx.setState (Some pageState)
            | Error errorMessage ->
                let fullErrorMessage = $"Could not open preview for '{itemName}': {errorMessage}"
                console.log ($"[Renderer] Error: {fullErrorMessage}")
                pageStateCtx.setState (Some(Renderer.Types.PageState.ErrorPage fullErrorMessage))
        }

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None ->
                    errorModal.enqueue (
                        ErrorModalRequest.create ($"File '{item.Name}' has no path.", title = "Preview failed")
                    )
                | Some path when item.IsDirectory ->
                    let selectedPath = PathHelpers.normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
                    pageStateCtx.setState None
                | Some path ->
                    let selectedPath = PathHelpers.normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                    if Swate.Components.Page.FileExplorer.Helper.needsLfsDownload item then
                        pageStateCtx.setState None
                    else
                        do! openSelectedPreview item.Name selectedPath
            }
            |> Promise.start

        let reloadSelectedPreviewAfterFileTreeUpdate () =
            if
                FileExplorerDeleteHelper.shouldClearPageStateForLfsPointerSelection
                    fileStateCtx.state.FileTree
                    fileStateCtx.state.Selection.TreePath
                    pageStateCtx.state
            then
                pageStateCtx.setState None
            else
                match
                    FileExplorerDeleteHelper.tryGetReloadableSelectedFilePath
                        fileStateCtx.state.FileTree
                        fileStateCtx.state.Selection.TreePath
                        pageStateCtx.state
                with
                | None -> ()
                | Some selectedPath ->
                    promise {
                        let! result = openView selectedPath

                        match result with
                        | Ok pageState -> pageStateCtx.setState (Some pageState)
                        | Error errorMessage ->
                            pageStateCtx.setState (
                                Some(
                                    Renderer.Types.PageState.ErrorPage
                                        $"Could not reload preview for '{selectedPath}': {errorMessage}"
                                )
                            )
                    }
                    |> Promise.catch (fun exn ->
                        pageStateCtx.setState (
                            Some(
                                Renderer.Types.PageState.ErrorPage
                                    $"Could not reload preview for '{selectedPath}': {exn.Message}"
                            )
                        )
                    )
                    |> Promise.start

        React.useEffect (
            (fun () ->
                if hasObservedFileTreeUpdateRef.current then
                    reloadSelectedPreviewAfterFileTreeUpdate ()
                else
                    hasObservedFileTreeUpdateRef.current <- true
            ),
            [| box fileStateCtx.state.FileTree |]
        )

        let handleExpansionChange (item: FileItem) (willExpand: bool) =
            if willExpand then
                match item.Path with
                | Some path -> setMaterializedState (fun _ -> materialize path reconciledMaterializedState)
                | None -> ()

        let openDialog dialog =
            setIsDialogBusy false
            setActiveDialog (Some dialog)

        let closeDialog () =
            setIsDialogBusy false
            setSelectedAssignableNote None
            setSelectedAssetDestinations (fun _ -> Map.empty)
            setActiveDialog None

        let selectAssignableNote note =
            setSelectedAssignableNote note
            setSelectedAssetDestinations (fun _ -> Map.empty)

        let setAssetDestination assetPath destination =
            setSelectedAssetDestinations (fun current ->
                match destination with
                | Some destination -> current |> Map.add assetPath destination
                | None -> current |> Map.remove assetPath
            )

        let openCreateModal kind =
            match kind with
            | ArcExplorerNodeKind.Note -> pageStateCtx.setState (Some Renderer.Types.PageState.NotesDraftPage)
            | _ -> openDialog (CreateDialog kind)

        let openFileSystemCreateModal kind item =
            if canCreateFileSystemItemIn item then
                openDialog (FileSystemCreateDialog { Parent = item; Kind = kind })

        let requestDeleteItem =
            FileTreeDeleteWorkflow.requestDeleteItem (Option.iter (DeleteDialog >> openDialog))

        let requestRenameItem =
            FileTreeRenameWorkflow.requestRenameItem (Option.iter (RenameDialog >> openDialog)) errorModal.enqueue

        let requestAssignNoteItem item =
            match FileTreeAssignNoteHelper.tryGetNoteAssignmentTarget item with
            | Some target -> openDialog (AssignNoteDialog target)
            | None ->
                FileTreeAssignNoteHelper.enqueueAssignNoteError
                    errorModal.enqueue
                    "Notes can only be assigned to study or assay folders."

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
            errorModal.enqueue (ErrorModalRequest.create (errorMessage, title = "Could not create ARC file"))

        let applyFileSystemCreateError errorMessage =
            errorModal.enqueue (ErrorModalRequest.create (errorMessage, title = "Could not create file or folder"))

        let reloadPreviewByPath (path: string) : JS.Promise<Result<unit, string>> = promise {
            let! openResult = openView path

            match openResult with
            | Ok pageState ->
                pageStateCtx.setState (Some pageState)
                return Ok()
            | Error errorMessage -> return Error errorMessage
        }

        let (activeCreateKind, activeFileSystemCreateDraft, activeRenameDraft, activeDeleteItem) =
            match activeDialog with
            | Some(CreateDialog kind) -> Some kind, None, None, None
            | Some(FileSystemCreateDialog draft) -> None, Some draft, None, None
            | Some(AssignNoteDialog _) -> None, None, None, None
            | Some(RenameDialog renameDraft) -> None, None, Some renameDraft, None
            | Some(DeleteDialog item) -> None, None, None, Some item
            | None -> None, None, None, None

        let assignNoteDialogState =
            React.useMemo (
                (fun _ -> createAssignNoteDialogState activeDialog fileStateCtx.state.FileTree selectedAssignableNote),
                [|
                    box activeDialog
                    box fileStateCtx.state.FileTree
                    box selectedAssignableNote
                |]
            )

        let confirmDeleteItem () =
            if not isDialogBusy then
                FileTreeDeleteWorkflow.confirmDeleteItem {
                    pendingDeleteItem = activeDeleteItem
                    closeDeleteModal = closeDialog
                    setIsDeleting = setIsDialogBusy
                    enqueueError = errorModal.enqueue
                }

        let createArcEntry kind (identifier: string) =
            if not isDialogBusy then
                let existingPaths =
                    fileStateCtx.state.FileTree |> Array.map (fun entry -> entry.path)

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

                            let pageState = Renderer.Types.PageState.fromFileContentDTO createdArcFileDto
                            pageStateCtx.setState (Some pageState)

                            closeDialog ()
                    }
                    |> Promise.catch (fun exn ->
                        setIsDialogBusy false
                        applyCreateError exn.Message
                    )
                    |> Promise.start

        let createFileSystemItem (name: string) =
            if not isDialogBusy then
                match activeFileSystemCreateDraft with
                | None -> closeDialog ()
                | Some draft ->
                    match tryGetItemRelativePath draft.Parent with
                    | None -> applyFileSystemCreateError "Could not resolve the selected folder path."
                    | Some parentPath ->
                        setIsDialogBusy true

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
                                        let pageState = Renderer.Types.PageState.fromFileContentDTO dto
                                        pageStateCtx.setState (Some pageState)
                                    | Error _ ->
                                        let dto = FileContentDTO.create FileContentType.PlainText "" selectedPath

                                        let pageState = Renderer.Types.PageState.fromFileContentDTO dto
                                        pageStateCtx.setState (Some pageState)
                                | FileSystemItemKind.Folder -> pageStateCtx.setState None

                                closeDialog ()
                        }
                        |> Promise.catch (fun exn -> applyFileSystemCreateError exn.Message)
                        |> Promise.map (fun _ -> setIsDialogBusy false)
                        |> Promise.start

        let renameContextMenuItems =
            FileTreeContextMenu.renameContextMenuItems requestRenameItem

        let itemActions item = [
            yield!
                rootFolderContextMenuItems
                    NoteConversion.notesRootFolder
                    "Create new item in"
                    "swt:fluent--note-add-24-regular"
                    (fun () -> openCreateModal ArcExplorerNodeKind.Note)
                    item
            yield! renameContextMenuItems item
        ]

        let contextMenuConfig: FileTreeContextMenu.ContextMenuConfig = {
            openItem = openPreview
            arcRootPath = appStateCtx
            openCreateModal = openCreateModal
            openFileSystemCreateModal = openFileSystemCreateModal
            requestAssignNoteItem = requestAssignNoteItem
            requestRenameItem = requestRenameItem
            requestDeleteItem = requestDeleteItem
            pathActionConfig = {
                openPathInFileExplorer = Api.ipcArcVaultApi.showPathInFileExplorer
                openPathWithDefaultApplication = Api.ipcArcVaultApi.openPathWithDefaultApplication
                enqueueError = errorModal.enqueue
            }
            enqueueError = errorModal.enqueue
            runToggleLfsMark = Renderer.Components.Helper.GitLfsHelper.runToggleLfsMark
            runDownloadLfsFile = Renderer.Components.Helper.GitLfsHelper.runDownloadLfsFile
            runFreeLocalLfsCopy = Renderer.Components.Helper.GitLfsHelper.runFreeLocalLfsCopy
        }

        let createContextMenuItems =
            FileTreeContextMenu.createContextMenuItems contextMenuConfig arcScopeId

        let rootContextMenu rootItem =
            let rootMenuItem = {
                rootItem with
                    Path = Some ""
                    IsDirectory = true
            }

            Swate.Components.Primitive.ContextMenu.ContextMenu.ContextMenu(
                (fun _ ->
                    FileTreeContextMenu.rootContextMenuItems contextMenuConfig rootMenuItem
                    |> List.map (fun item -> item.ToPrimitiveContextMenuItem())
                ),
                ref = rootContextMenuRef,
                onSpawn = (fun _ -> Some(box ()))
            )

        let getItemStatusAction =
            Renderer.Components.FileExplorerLfs.createLfsPillAction
                errorModal.enqueue
                arcScopeId
                Renderer.Components.Helper.GitLfsHelper.runDownloadLfsFile
                Renderer.Components.Helper.GitLfsHelper.runFreeLocalLfsCopy

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
                    }
                    newName

        let confirmAssignNote () =
            if not isDialogBusy then
                match assignNoteDialogState.Target, selectedAssignableNote with
                | None, _ -> closeDialog ()
                | _, None -> FileTreeAssignNoteHelper.enqueueAssignNoteError errorModal.enqueue "Select a note."
                | Some target, Some note ->
                    FileTreeAssignNoteHelper.assignNoteToTarget
                        {
                            selectedTreePath = fileStateCtx.state.Selection.TreePath
                            pageState = pageStateCtx.state
                            closeDialog = closeDialog
                            setIsAssigning = setIsDialogBusy
                            setSelection = fileStateCtx.setSelection
                            refreshGitStatus = gitStateCtx.refresh
                            reloadPreviewByPath = reloadPreviewByPath
                            movePath = Api.ipcArcVaultApi.movePath
                            enqueueError = errorModal.enqueue
                        }
                        target
                        note
                        (assignNoteDialogState.AvailableAssets |> Seq.toList)
                        selectedAssetDestinations

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

        let activeFileSystemCreateKind =
            activeFileSystemCreateDraft
            |> Option.map _.Kind
            |> Option.defaultValue FileSystemItemKind.File

        let fileSystemCreateModal =
            CreateFileSystemItemModal.Main(
                isOpen = activeFileSystemCreateDraft.IsSome,
                kind = activeFileSystemCreateKind,
                parentName = (activeFileSystemCreateDraft |> Option.map _.Parent.Name),
                close = closeDialog,
                submit = createFileSystemItem,
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

        let assignNoteModal =
            FileTreeAssignNoteModal.Main(
                isOpen = assignNoteDialogState.Target.IsSome,
                itemName = (assignNoteDialogState.Target |> Option.map _.Name),
                selectedNote = selectedAssignableNote,
                setSelectedNote = selectAssignableNote,
                availableNotes = assignNoteDialogState.AvailableNotes,
                availableAssets = assignNoteDialogState.AvailableAssets,
                assetDestinations = selectedAssetDestinations,
                setAssetDestination = setAssetDestination,
                close = closeDialog,
                submit = confirmAssignNote,
                isAssigning = isDialogBusy
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
                            onDirectoryExpansionChange = handleExpansionChange,
                            onContextMenu = createContextMenuItems,
                            getItemIconClass = getItemIconClass,
                            canCreateItem = canCreateFromItem,
                            onCreateItem = createFromItem,
                            getItemActions = itemActions,
                            getItemStatusAction = getItemStatusAction,
                            canDeleteItem = canDeleteItem,
                            onDeleteItem = requestDeleteItem,
                            selectedItemId = fileStateCtx.state.Selection.TreePath,
                            includeDefaultContextMenuItems = false,
                            delegateHorizontalScrollToParent = true
                        )
                    ]
                ]
                rootContextMenu rootItem
                arcCreateModal
                fileSystemCreateModal
                assignNoteModal
                renameModal
                deleteConfirmModal
            ]
        | None ->
            React.Fragment [
                FileTree.EmptyFileTreePlaceholder()
                arcCreateModal
                fileSystemCreateModal
                assignNoteModal
                renameModal
                deleteConfirmModal
            ]
