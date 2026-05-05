namespace Renderer.Components.LeftSidebar.FileExplorer

open Renderer.Components.ARCHelper
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

        let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
        let errorModal = ErrorModal.Context.useErrorModalCtx ()
        let arcScopeId = useCurrentArcScopeId ()

        let pendingCreateKind, setPendingCreateKind =
            React.useState<ArcExplorerNodeKind option> None

        let pendingArcFileSave, setPendingArcFileSave = React.useState<ArcFiles option> None

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

        let effectiveFileTree =
            React.useMemo (
                (fun () -> withPendingArcFileEntry fileStateCtx.state.FileTree pendingArcFileSave),
                [|
                    box fileStateCtx.state.FileTree
                    box pendingArcFileSave
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

        let toggleLfsMark =
            Renderer.Components.FileExplorerLfs.createToggleLfsMark
                errorModal.enqueue
                arcScopeId
                Renderer.Components.ARCHelper.runToggleLfsMark

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
                            let normalizedPath = normalizePath path

                            if current.Contains normalizedPath then
                                current
                            else
                                current.Add normalizedPath
                        )

                    let selectedPath = normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
                    pageStateCtx.setState None
                | Some path ->
                    let selectedPath = normalizePath path
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
                        let normalizedPath = normalizePath path

                        if current.Contains normalizedPath then
                            current
                        else
                            current.Add normalizedPath
                    )
                | None -> ()

        let closeCreateModal () = setPendingCreateKind None

        let openCreateModal kind = setPendingCreateKind (Some kind)

        let rootPath = fileTree |> Option.map (fun tree -> tree.path)

        let inlineCreateKindForItem item =
            match rootPath with
            | Some path -> tryGetInlineArcCreateKind path item
            | None -> None

        let canCreateFromItem item =
            inlineCreateKindForItem item |> Option.isSome

        let createFromItem item =
            inlineCreateKindForItem item |> Option.iter openCreateModal

        let applyCreateError errorMessage =
            Renderer.Components.ARCHelper.applyViewError pageStateCtx.setState errorMessage

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

        let contextMenuItems (item: FileItem) =
            Renderer.Components.FileExplorerLfs.withLfsContextMenuItems
                item
                toggleLfsMark
                (arcCreateContextMenuItems item)

        let copyArcPathToClipboard (path: string) =
            promise {
                try
                    do! navigator.clipboard.writeText path
                with ex ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (
                            $"Could not copy ARC path: {ex.Message}",
                            title = "Copy path failed",
                            ?scopeId = arcScopeId
                        )
                    )
            }
            |> Promise.start

        let openArcFolderInFileExplorer () =
            promise {
                match! Api.ipcArcVaultApi.openArcFolderInFileExplorer () with
                | Ok() -> ()
                | Error exn ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (exn.Message, title = "Open folder failed", ?scopeId = arcScopeId)
                    )
            }
            |> Promise.start

        let activeCreateKind =
            pendingCreateKind |> Option.defaultValue ArcExplorerNodeKind.Study

        let arcCreateModal =
            ArcCreateModal.Main(
                isOpen = pendingCreateKind.IsSome,
                kind = activeCreateKind,
                close = closeCreateModal,
                submit = createArcEntry
            )

        let arcNameFromRootItem (rootItem: FileItem) =
            match rootItem.Path with
            | Some path ->
                let normalizedPath = normalizePath path

                if System.String.IsNullOrWhiteSpace normalizedPath then
                    rootItem.Name
                else
                    getFileName normalizedPath
            | None -> rootItem.Name

        match fileItem with
        | Some rootItem ->
            let visibleItems = rootItem.Children |> Option.defaultValue []
            let arcName = arcNameFromRootItem rootItem

            React.Fragment [
                Html.div [
                    prop.className "swt:w-full"
                    prop.children [
                        Renderer.Components.FileExplorerArcPath.ArcPathPopover(
                            arcName,
                            appStateCtx,
                            copyArcPathToClipboard,
                            openArcFolderInFileExplorer
                        )
                        Swate.Components.FileExplorer.FileExplorer.FileExplorer(
                            initialItems = visibleItems,
                            onItemClick = openPreview,
                            onDirectoryArrowToggle = handleDirectoryArrowToggle,
                            onContextMenu = contextMenuItems,
                            canCreateItem = canCreateFromItem,
                            onCreateItem = createFromItem,
                            selectedItemId = fileStateCtx.state.Selection.TreePath,
                            showBreadcrumbs = false
                        )
                    ]
                ]
                arcCreateModal
            ]
        | None -> React.Fragment [ FileTree.EmptyFileTreePlaceholder(); arcCreateModal ]