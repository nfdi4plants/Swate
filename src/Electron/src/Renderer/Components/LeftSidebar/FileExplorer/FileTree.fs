namespace Renderer.Components.LeftSidebar.FileExplorer

open Renderer.Components.ARCHelper
open Renderer.Components.FileExplorerDeleteHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Types
open Helper

module private FileTreeHelper =
    let stagePendingArcFileSave (arcFile: ArcFiles option) : JS.Promise<Result<unit, exn>> = promise {
        let pendingSaveRequestResult =
            match arcFile with
            | None -> Ok None
            | Some nextArcFile ->
                match FileContentDTO.fromArcFile nextArcFile with
                | Some request -> Ok(Some request)
                | None -> Error(exn "Saving this file type is not supported in Electron yet.")

        match pendingSaveRequestResult with
        | Error saveError -> return Error saveError
        | Ok pendingSaveRequest ->
            let! result = Api.ipcArcVaultApi.setPendingArcFileSave pendingSaveRequest
            return result
    }

open FileTreeHelper

[<Erase; Mangle(false)>]
type private DeleteConfirmModal =

    [<ReactComponent>]
    static member Dialog
        (
            isOpen: bool,
            itemName: string option,
            close: unit -> unit,
            submit: unit -> unit,
            ?isDeleting: bool
        ) =

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let displayName = itemName |> Option.defaultValue "this item"
        let isDeleting = defaultArg isDeleting false

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> submit ())
                        prop.children [
                            if isDeleting then
                                Html.span [ prop.text "Deleting..." ]
                            else
                                Html.span [ prop.text "Delete" ]
                        ]
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Delete Item",
            description = Html.text $"Permanently delete '{displayName}'?",
            children = Html.none,
            footer = footer,
            debug = "arc-delete"
        )

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
        let errorModal = ErrorModal.Context.useErrorModalCtx ()
        let arcScopeId = useCurrentArcScopeId ()

        let pendingCreateKind, setPendingCreateKind =
            React.useState<ArcExplorerNodeKind option> None

        let pendingArcFileSave, setPendingArcFileSave = React.useState<ArcFiles option> None
        let pendingDeleteItem, setPendingDeleteItem = React.useState<FileItem option> None
        let isDeleting, setIsDeleting = React.useState false

        let effectiveFileTree =
            React.useMemo (
                (fun () -> withPendingArcFileEntry fileStateCtx.state.FileTree pendingArcFileSave),
                [|
                    box fileStateCtx.state.FileTree
                    box pendingArcFileSave
                |]
            )

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

        let fileTree =
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

                    match tryFindPendingArcFileByPath selectedPath pendingArcFileSave with
                    | Some pendingArcFile ->
                        pageStateCtx.setState (Some(Renderer.Types.PageState.ArcFilePage pendingArcFile))
                    | None ->
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

        let openCreateModal kind = setPendingCreateKind (Some kind)

        let closeDeleteModal () =
            setPendingDeleteItem None

        let requestDeleteItem (item: FileItem) =
            if canDeleteItem item then
                setPendingDeleteItem (Some item)

        let rootPath = fileTree |> Option.map (fun tree -> tree.path)

        let inlineCreateKindForItem item =
            match rootPath with
            | Some path -> tryGetInlineArcCreateKind path item
            | None -> None

        let canCreateFromItem item =
            inlineCreateKindForItem item |> Option.isSome

        let createFromItem item =
            inlineCreateKindForItem item |> Option.iter openCreateModal

        let applyDeleteError (errorMessage: string) =
            errorModal.enqueue (
                ErrorModalRequest.create (
                    errorMessage,
                    title = "Could not delete item",
                    ?scopeId = arcScopeId
                )
            )

        let applyCreateError errorMessage =
            Renderer.Components.ARCHelper.applyViewError pageStateCtx.setState errorMessage

        let confirmDeleteItem () =
            match pendingDeleteItem |> Option.bind tryGetItemRelativePath with
            | None -> closeDeleteModal ()
            | Some deletePath when ArcDeletePathRules.isDeletePathAllowed deletePath |> not ->
                closeDeleteModal ()
            | Some deletePath ->
                setIsDeleting true

                promise {
                    let pendingPath = tryGetArcFilePendingPath pendingArcFileSave
                    let shouldClearPendingDraft =
                        FileExplorerDeleteHelper.isPendingPathAffectedByDelete deletePath pendingPath

                    let! deleteResult = Api.ipcArcVaultApi.deletePath deletePath

                    match deleteResult with
                    | Ok() ->
                        if shouldClearPendingDraft then
                            setPendingArcFileSave None

                            match! stagePendingArcFileSave None with
                            | Ok() -> ()
                            | Error exn -> applyDeleteError exn.Message

                        closeDeleteModal ()
                    | Error exn -> applyDeleteError exn.Message

                    setIsDeleting false
                }
                |> Promise.start

        let createArcEntry kind (identifier: string) =
            let existingPaths = effectiveFileTree |> Array.map (fun entry -> entry.path)

            match tryBuildArcCreateDraft kind identifier existingPaths with
            | Error errorMessage -> applyCreateError errorMessage
            | Ok draft ->
                fileStateCtx.setSelection (ArcSelection.forTreePath (Some draft.Path))
                setPendingArcFileSave (Some draft.ArcFile)
                pageStateCtx.setState (Some(Renderer.Types.PageState.ArcFilePage draft.ArcFile))

                promise {
                    match! stagePendingArcFileSave (Some draft.ArcFile) with
                    | Ok() -> ()
                    | Error exn ->
                        errorModal.enqueue (
                            ErrorModalRequest.create (
                                exn.Message,
                                title = "Could not stage ARC file save",
                                ?scopeId = arcScopeId
                            )
                        )
                }
                |> Promise.start

                closeCreateModal ()

        let arcCreateContextMenuItems (item: FileItem) =
            if item.IsDirectory then
                arcCreateKinds
                |> List.sortBy arcCreateKindSortOrder
                |> List.map (fun kind -> {
                    Label = $"Add {ArcExplorerNodeKind.label kind}"
                    Icon = arcCreateKindIcon kind
                    OnClick = fun () -> openCreateModal kind
                    Disabled = None
                })
            else
                []

        let deleteContextMenuItems (item: FileItem) =
            if canDeleteItem item then
                [
                    {
                        Label = "Delete"
                        Icon = "swt:fluent--delete-24-regular"
                        OnClick = fun () -> requestDeleteItem item
                        Disabled = None
                    }
                ]
            else
                []

        let baseContextMenuItems (item: FileItem) =
            arcCreateContextMenuItems item @ deleteContextMenuItems item

        let createContextMenuItems =
            Renderer.Components.FileExplorerLfs.createContextMenuItems
                errorModal.enqueue
                arcScopeId
                baseContextMenuItems

        let activeCreateKind =
            pendingCreateKind |> Option.defaultValue ArcExplorerNodeKind.Study

        let arcCreateModal =
            CreateArcFileModal.Main(
                isOpen = pendingCreateKind.IsSome,
                kind = activeCreateKind,
                close = closeCreateModal,
                submit = createArcEntry
            )

        let deleteConfirmModal =
            DeleteConfirmModal.Dialog(
                isOpen = pendingDeleteItem.IsSome,
                itemName = (pendingDeleteItem |> Option.map _.Name),
                close = closeDeleteModal,
                submit = confirmDeleteItem,
                isDeleting = isDeleting
            )

        match fileItem with
        | Some rootItem ->
            let visibleItems = rootItem.Children |> Option.defaultValue []

            React.Fragment [
                Html.div [
                    prop.className "swt:w-full"
                    prop.children [
                        Swate.Components.FileExplorer.FileExplorer.FileExplorer(
                            initialItems = visibleItems,
                            onItemClick = openPreview,
                            onDirectoryArrowToggle = handleDirectoryArrowToggle,
                            onContextMenu = createContextMenuItems,
                            canCreateItem = canCreateFromItem,
                            onCreateItem = createFromItem,
                            canDeleteItem = canDeleteItem,
                            onDeleteItem = requestDeleteItem,
                            selectedItemId = fileStateCtx.state.Selection.TreePath,
                            showBreadcrumbs = false
                        )
                    ]
                ]
                arcCreateModal
                deleteConfirmModal
            ]
        | None ->
            React.Fragment [
                FileTree.EmptyFileTreePlaceholder()
                arcCreateModal
                deleteConfirmModal
            ]
