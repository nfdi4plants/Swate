module Renderer.Components.FileExplorer


open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorerTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open ARCtrl


module FileExplorerCreate =

    [<RequireQualifiedAccess>]
    type ArcCreateKind =
        | Study
        | Assay
        | Workflow
        | Run

    type ArcCreateDraft = { ArcFile: ArcFiles; Path: string }

    let rec loopPaths (selectedTreeItemPath: string option) (parent: FileTreeNode) =
        match parent.isDirectory with
        | true ->
            let tmp =
                let ra = ResizeArray(parent.children.Values)

                ra.ToArray()
                |> Array.map (fun entry -> loopPaths selectedTreeItemPath entry)
                |> Array.choose id
                |> List.ofArray

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

    let arcCreateKindLabel =
        function
        | ArcCreateKind.Study -> "Study"
        | ArcCreateKind.Assay -> "Assay"
        | ArcCreateKind.Workflow -> "Workflow"
        | ArcCreateKind.Run -> "Run"

    let arcCreateKindIcon =
        function
        | ArcCreateKind.Study -> "swt:fluent--document-table-24-regular"
        | ArcCreateKind.Assay -> "swt:fluent--beaker-24-regular"
        | ArcCreateKind.Workflow -> "swt:fluent--flowchart-24-regular"
        | ArcCreateKind.Run -> "swt:fluent--play-24-regular"

    let arcCreateKinds = [
        ArcCreateKind.Study
        ArcCreateKind.Assay
        ArcCreateKind.Workflow
        ArcCreateKind.Run
    ]

    let arcCreateKindSortOrder =
        function
        | ArcCreateKind.Study -> 10
        | ArcCreateKind.Assay -> 20
        | ArcCreateKind.Workflow -> 30
        | ArcCreateKind.Run -> 40

    let arcCreateKindDefaultIdentifier =
        function
        | ArcCreateKind.Study -> "NewStudy"
        | ArcCreateKind.Assay -> "NewAssay"
        | ArcCreateKind.Workflow -> "NewWorkflow"
        | ArcCreateKind.Run -> "NewRun"

    let isArcCreateIdentifierValid (identifier: string) =
        let identifier = identifier.Trim()

        (System.String.IsNullOrWhiteSpace identifier |> not)
        && ARCtrl.Helper.Identifier.tryCheckValidCharacters identifier

    let arcCreateIdentifierError =
        "Identifier is required and may only contain letters, digits, spaces, underscores, or dashes."

    let createArcFile kind identifier =
        match kind with
        | ArcCreateKind.Study ->
            let study = ArcStudy.init identifier
            study.InitTable($"{identifier} Table") |> ignore
            ArcFiles.Study(study, [])
        | ArcCreateKind.Assay ->
            let assay = ArcAssay.init identifier
            assay.InitTable($"{identifier} Table") |> ignore
            ArcFiles.Assay assay
        | ArcCreateKind.Workflow -> ArcWorkflow.init identifier |> ArcFiles.Workflow
        | ArcCreateKind.Run ->
            let run = ArcRun.init identifier
            run.InitTable($"{identifier} Table") |> ignore
            ArcFiles.Run run

    let tryBuildArcCreateDraft kind (identifier: string) (existingPaths: string seq) =
        let identifier = identifier.Trim()
        let label = arcCreateKindLabel kind

        if isArcCreateIdentifierValid identifier |> not then
            Error arcCreateIdentifierError
        else
            let arcFile = createArcFile kind identifier

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

open FileExplorerCreate

[<ReactComponent>]
let EmptyFileTreePlaceholder () =
    Html.div [
        prop.className "swt:p-4 swt:text-center swt:text-gray-500"
        prop.text "No files found."
    ]

[<ReactComponent>]
let private ArcCreateModal
    (kind: ArcCreateKind option)
    (identifier: string)
    (setIdentifier: string -> unit)
    (close: unit -> unit)
    (submit: ArcCreateKind -> string -> unit)
    =

    let setIsOpen isOpen =
        if not isOpen then
            close ()

    match kind with
    | None -> BaseModal.BaseModal(false, setIsOpen, Html.none)
    | Some kind ->
        let label = arcCreateKindLabel kind
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
            isOpen = true,
            setIsOpen = setIsOpen,
            header = Html.text $"Add {label}",
            description = Html.text $"Create a new {label.ToLowerInvariant()} in the current ARC.",
            children = content,
            footer = footer,
            debug = "arc-create"
        )

[<ReactComponent>]
let FileTree () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let pendingCreateKind, setPendingCreateKind =
        React.useState<ArcCreateKind option> None

    let createIdentifier, setCreateIdentifier = React.useState ""
    let errorModal = ErrorModal.Context.useErrorModal ()
    let arcScopeId = useCurrentArcScopeId ()

    match fileStateCtx.state.FileTree with
    | [||] -> EmptyFileTreePlaceholder()
    | _ ->

        let fileTree = fileStateCtx.state.FileTree |> toFileTreeNode

        let fileItem = loopPaths fileStateCtx.state.Selection.TreePath fileTree

        let setError (errorMsg: string option) =
            match errorMsg with
            | Some msg ->
                errorModal.enqueue (
                    ErrorModalRequest.create (msg, title = "Git LFS update failed", ?scopeId = arcScopeId)
                )
            | None -> ()

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(setError, Renderer.Components.ARCHelper.runToggleLfsMark)

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

        let closeCreateModal () =
            setPendingCreateKind None
            setCreateIdentifier ""

        let openCreateModal kind =
            setCreateIdentifier (arcCreateKindDefaultIdentifier kind)
            setPendingCreateKind (Some kind)

        let applyCreateError errorMessage =
            Renderer.Components.ARCHelper.applyViewError
                pageStateCtx.setState
                arcObjectCtx.setArcFileState
                arcObjectCtx.setPreviewState
                arcObjectCtx.setStatusMessage
                errorMessage

        let createArcEntry kind (identifier: string) =
            let existingPaths =
                fileStateCtx.state.FileTree |> Array.map (fun entry -> entry.path)

            match tryBuildArcCreateDraft kind identifier existingPaths with
            | Error errorMessage -> applyCreateError errorMessage
            | Ok draft ->
                fileStateCtx.setSelection (ArcSelection.forTreePath (Some draft.Path))

                draft.ArcFile
                |> Renderer.Components.ARCHelper.viewLoadResultOfArcFile
                |> Renderer.Components.ARCHelper.applyLoadedView
                    pageStateCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage

                arcObjectCtx.setPendingArcFileSave (Some draft.ArcFile)
                closeCreateModal ()

        let arcCreateContextMenuItems (item: FileItem) =
            if item.IsDirectory then
                arcCreateKinds
                |> List.sortBy arcCreateKindSortOrder
                |> List.map (fun kind -> {
                    Label = $"Add {arcCreateKindLabel kind}"
                    Icon = arcCreateKindIcon kind
                    OnClick = fun () -> openCreateModal kind
                    Disabled = None
                })
            else
                []

        let contextMenuSortOrder (item: ContextMenuItem) =
            match item.Label with
            | "Add Study" -> 10
            | "Add Assay" -> 20
            | "Add Workflow" -> 30
            | "Add Run" -> 40
            | "Mark Git LFS"
            | "Unmark Git LFS" -> 100
            | "Git LFS: marked"
            | "Git LFS: not marked" -> 110
            | _ -> 1000

        let sortContextMenuItems (items: ContextMenuItem list) =
            items |> List.sortBy (fun item -> contextMenuSortOrder item, item.Label)

        let contextMenuItems (item: FileItem) =
            arcCreateContextMenuItems item
            @ FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)
            |> sortContextMenuItems

        match fileItem with

        | Some fileItem ->
            React.Fragment [
                Swate.Components.FileExplorer.FileExplorer(
                    initialItems = [ fileItem ],
                    onItemClick = openPreview,
                    onContextMenu = contextMenuItems,
                    selectedItemId = fileStateCtx.state.Selection.TreePath
                )
                ArcCreateModal pendingCreateKind createIdentifier setCreateIdentifier closeCreateModal createArcEntry
            ]
        | None -> EmptyFileTreePlaceholder()