module Renderer.Components.FileExplorer


open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open ARCtrl


module private FileExplorerHelper =

    type ArcCreateDraft = { ArcFile: ArcFiles; Path: string }

    let private normalizeNodePath (path: string) = normalizePath path

    let tryGetArcFileRelativePath (arcFile: ArcFiles) =
        arcFile.TryGetRelativePath() |> Option.map normalizePath

    let tryPendingArcFileEntry (arcFile: ArcFiles) =
        tryGetArcFileRelativePath arcFile
        |> Option.map (fun path -> FileEntry.create (getFileName path, path, false))

    let withPendingArcFileEntry (fileTree: FileEntry[]) (pendingArcFile: ArcFiles option) =
        match pendingArcFile |> Option.bind tryPendingArcFileEntry with
        | Some pendingEntry when fileTree |> Array.exists (fun entry -> PathHelpers.pathsEqual entry.path pendingEntry.path) |> not ->
            Array.append fileTree [| pendingEntry |]
        | _ -> fileTree

    let tryFindPendingArcFileByPath (path: string) (pendingArcFile: ArcFiles option) =
        pendingArcFile
        |> Option.filter (fun arcFile ->
            tryGetArcFileRelativePath arcFile
            |> Option.exists (fun pendingPath -> PathHelpers.pathsEqual pendingPath path)
        )

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

            {
                FileTree.createFolder parent.name (Some parent.path) FileItemIcon.Folder with
                    Id = parent.path
                    IsExpanded =
                        selectedTreeItemPath
                        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                    Children = children
            }
            |> Renderer.Components.FileExplorerLfs.withFileTreeNodeLfsState parent
            |> Some
        | false ->
            {
                FileTree.createFile parent.name (Some parent.path) FileItemIcon.Document with
                    Id = parent.path
            }
            |> Renderer.Components.FileExplorerLfs.withFileTreeNodeLfsState parent
            |> Some

    let arcCreateKindIcon =
        function
        | ArcExplorerNodeKind.Study -> "swt:fluent--document-table-24-regular"
        | ArcExplorerNodeKind.Assay -> "swt:fluent--beaker-24-regular"
        | ArcExplorerNodeKind.Workflow -> "swt:fluent--flowchart-24-regular"
        | ArcExplorerNodeKind.Run -> "swt:fluent--play-24-regular"
        | kind -> failwithf "ARC node kind '%s' cannot be created from the file explorer." (ArcExplorerNodeKind.label kind)

    let arcCreateKinds = [
        ArcExplorerNodeKind.Study
        ArcExplorerNodeKind.Assay
        ArcExplorerNodeKind.Workflow
        ArcExplorerNodeKind.Run
    ]

    let arcCreateKindSortOrder =
        function
        | ArcExplorerNodeKind.Study -> 10
        | ArcExplorerNodeKind.Assay -> 20
        | ArcExplorerNodeKind.Workflow -> 30
        | ArcExplorerNodeKind.Run -> 40
        | _ -> 1000

    let arcCreateKindDefaultIdentifier =
        function
        | ArcExplorerNodeKind.Study -> "New Study"
        | ArcExplorerNodeKind.Assay -> "New Assay"
        | ArcExplorerNodeKind.Workflow -> "New Workflow"
        | ArcExplorerNodeKind.Run -> "New Run"
        | kind -> failwithf "ARC node kind '%s' cannot be created from the file explorer." (ArcExplorerNodeKind.label kind)

    let isArcCreateIdentifierValid (identifier: string) =
        let identifier = identifier.Trim()

        (System.String.IsNullOrWhiteSpace identifier |> not)
        && ARCtrl.Helper.Identifier.tryCheckValidCharacters identifier

    let arcCreateIdentifierError =
        "Identifier is required and may only contain letters, digits, spaces, underscores, or dashes."

    let tryCreateArcFile kind identifier =
        match kind with
        | ArcExplorerNodeKind.Study ->
            let study = ArcStudy.init identifier
            study.InitTable($"{identifier} Table") |> ignore
            Ok(ArcFiles.Study(study, []))
        | ArcExplorerNodeKind.Assay ->
            let assay = ArcAssay.init identifier
            assay.InitTable($"{identifier} Table") |> ignore
            Ok(ArcFiles.Assay assay)
        | ArcExplorerNodeKind.Workflow -> ArcWorkflow.init identifier |> ArcFiles.Workflow |> Ok
        | ArcExplorerNodeKind.Run ->
            let run = ArcRun.init identifier
            run.InitTable($"{identifier} Table") |> ignore
            Ok(ArcFiles.Run run)
        | kind -> Error $"Creating {ArcExplorerNodeKind.label kind} files is not supported from the file explorer."

    let tryGetInlineArcCreateKind (rootPath: string) (item: FileItem) =
        if not item.IsDirectory then
            None
        else
            match item.Path with
            | Some path when getPathDepth path = getPathDepth rootPath + 1 ->
                match PathHelpers.getNameFromPath path |> fun name -> name.ToLowerInvariant() with
                | "studies" -> Some ArcExplorerNodeKind.Study
                | "assays" -> Some ArcExplorerNodeKind.Assay
                | "workflows" -> Some ArcExplorerNodeKind.Workflow
                | "runs" -> Some ArcExplorerNodeKind.Run
                | _ -> None
            | _ -> None

    let tryBuildArcCreateDraft kind (identifier: string) (existingPaths: string seq) =
        let identifier = identifier.Trim()
        let label = ArcExplorerNodeKind.label kind

        if isArcCreateIdentifierValid identifier |> not then
            Error arcCreateIdentifierError
        else
            match tryCreateArcFile kind identifier with
            | Error errorMessage -> Error errorMessage
            | Ok arcFile ->
                match FileContentDTO.fromArcFile arcFile with
                | None -> Error $"Creating {label} files is not supported in Electron yet."
                | Some request ->
                    let requestedPath = normalizePath request.path

                    let alreadyExists =
                        existingPaths
                        |> Seq.exists (fun path -> PathHelpers.pathsEqual (normalizePath path) requestedPath)

                    if alreadyExists then
                        Error $"{label} '{identifier}' already exists."
                    else
                        Ok {
                            ArcFile = arcFile
                            Path = requestedPath
                        }

open FileExplorerHelper

[<Erase; Mangle(false)>]
type private ArcCreateModal =

    [<ReactComponent>]
    static member Main
        (isOpen: bool, kind: ArcExplorerNodeKind, close: unit -> unit, submit: ArcExplorerNodeKind -> string -> unit)
        =

        let identifier, setIdentifier = React.useState (arcCreateKindDefaultIdentifier kind)

        React.useEffect (
            (fun () -> setIdentifier (arcCreateKindDefaultIdentifier kind)),
            [| box kind |]
        )

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let label = ArcExplorerNodeKind.label kind
        let isValid = isArcCreateIdentifierValid identifier

        let submitIfValid () =
            if isValid then
                submit kind identifier

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled (not isValid)
                        prop.onClick (fun _ -> submitIfValid ())
                        prop.text $"Create {label}"
                    ]
                ]
            ]

        let content =
            Html.fieldSet [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [
                        prop.className "swt:fieldset-legend"
                        prop.text "Identifier"
                    ]
                    Html.label [
                        prop.className "swt:input swt:w-full"
                        prop.children [
                            Html.input [
                                prop.autoFocus true
                                prop.value identifier
                                prop.onChange setIdentifier
                                prop.onKeyDown (key.enter, fun _ -> submitIfValid ())
                            ]
                        ]
                    ]
                    Html.p [
                        prop.hidden isValid
                        prop.className "swt:text-error swt:text-sm"
                        prop.text arcCreateIdentifierError
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text $"Add {label}",
            description = Html.text $"Create a new {label.ToLowerInvariant()} in the current ARC.",
            children = content,
            footer = footer,
            debug = "arc-create"
        )

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
        let pendingCreateKind, setPendingCreateKind = React.useState<ArcExplorerNodeKind option> None
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
                [| box fileStateCtx.state.FileTree; box pendingArcFileSave |]
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
                        |> Some),
                [| box effectiveFileTree |]
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
                            current.Add normalizedPath)
                | None -> ()

        let closeCreateModal () =
            setPendingCreateKind None

        let openCreateModal kind =
            setPendingCreateKind (Some kind)

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
            let existingPaths =
                effectiveFileTree |> Array.map (fun entry -> entry.path)

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

        let contextMenuItems =
            Renderer.Components.FileExplorerLfs.createContextMenuItems errorModal.enqueue arcScopeId arcCreateContextMenuItems

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
                        Html.div [
                            prop.testId "left-sidebar-file-explorer-arc-name"
                            prop.className "swt:mb-2 swt:px-2 swt:text-sm swt:font-semibold swt:truncate"
                            prop.text arcName
                        ]
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
        | None ->
            React.Fragment [
                FileExplorer.EmptyFileTreePlaceholder()
                arcCreateModal
            ]
