module Renderer.Components.FileExplorer

open Swate.Components
open Swate.Components.FileExplorerTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Fable.Core.JsInterop
open Feliz
open ARCtrl


module private FileExplorerHelper =

    [<RequireQualifiedAccess>]
    type ArcCreateKind =
        | Study
        | Assay
        | Workflow
        | Run

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

    let arcCreateKindDefaultIdentifier =
        function
        | ArcCreateKind.Study -> "NewStudy"
        | ArcCreateKind.Assay -> "NewAssay"
        | ArcCreateKind.Workflow -> "NewWorkflow"
        | ArcCreateKind.Run -> "NewRun"

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
        | ArcCreateKind.Run -> ArcRun.init identifier |> ArcFiles.Run

    let promptForIdentifier kind =
        let label = arcCreateKindLabel kind
        let defaultIdentifier = arcCreateKindDefaultIdentifier kind
        let prompted: string = emitJsExpr ($"Identifier for new {label}", defaultIdentifier) "window.prompt($0, $1)"

        if isNull prompted then
            None
        else
            let identifier = prompted.Trim()
            if System.String.IsNullOrWhiteSpace identifier then None else Some identifier

open FileExplorerHelper

[<ReactComponent>]
let EmptyFileTreePlaceholder () =
    Html.div [
        prop.className "swt:p-4 swt:text-center swt:text-gray-500"
        prop.text "No files found."
    ]

[<ReactComponent>]
let FileTree () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    match fileStateCtx.state.FileTree with
    | [||] -> EmptyFileTreePlaceholder()
    | _ ->

        let fileTree = fileStateCtx.state.FileTree |> toFileTreeNode

        let fileItem = loopPaths fileStateCtx.state.Selection.TreePath fileTree

        let setError (errorMsg: string option) =
            match errorMsg with
            | Some msg -> pageStateCtx.setState (Some(Renderer.Types.PageState.ErrorPage msg))
            | None -> pageStateCtx.setState (None)

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(setError, Renderer.Components.ARCHelper.runToggleLfsMark)

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None ->
                    let errorMessage = $"File '{item.Name}' has no path."
                    fileStateCtx.setSelection ArcSelection.empty

                    Renderer.Components.ARCHelper.applyPreviewError
                        pageStateCtx.setState
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage
                        errorMessage
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

                    let! result = Renderer.Components.ARCHelper.openPreview selectedPath

                    match result with
                    | Ok loaded ->
                        console.log ("[Renderer] Received data, processing...")

                        Renderer.Components.ARCHelper.applyLoadedPreview
                            pageStateCtx.setState
                            arcObjectCtx.setArcFileState
                            arcObjectCtx.setPreviewState
                            arcObjectCtx.setStatusMessage
                            loaded
                    | Error errorMessage ->
                        let fullErrorMessage = $"Could not open preview for '{item.Name}': {errorMessage}"
                        console.log ($"[Renderer] Error: {fullErrorMessage}")

                        Renderer.Components.ARCHelper.applyPreviewError
                            pageStateCtx.setState
                            arcObjectCtx.setArcFileState
                            arcObjectCtx.setPreviewState
                            arcObjectCtx.setStatusMessage
                            fullErrorMessage
            }
            |> Promise.start

        let createArcEntry kind (item: FileItem) =
            if item.IsDirectory then
                match promptForIdentifier kind with
                | None -> ()
                | Some identifier ->
                    promise {
                        let arcFile = createArcFile kind identifier

                        match FileContentDTO.fromArcFile arcFile with
                        | None ->
                            let label = arcCreateKindLabel kind
                            let errorMessage = $"Creating {label} files is not supported in Electron yet."

                            Renderer.Components.ARCHelper.applyPreviewError
                                pageStateCtx.setState
                                arcObjectCtx.setArcFileState
                                arcObjectCtx.setPreviewState
                                arcObjectCtx.setStatusMessage
                                errorMessage
                        | Some request ->
                            let requestedPath = normalizePath request.path

                            let alreadyExists =
                                fileStateCtx.state.FileTree
                                |> Array.exists (fun entry -> normalizePath entry.path = requestedPath)

                            if alreadyExists then
                                let label = arcCreateKindLabel kind
                                let errorMessage = $"{label} '{identifier}' already exists."

                                Renderer.Components.ARCHelper.applyPreviewError
                                    pageStateCtx.setState
                                    arcObjectCtx.setArcFileState
                                    arcObjectCtx.setPreviewState
                                    arcObjectCtx.setStatusMessage
                                    errorMessage
                            else
                                let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

                                match saveResult with
                                | Ok() ->
                                    let! openResult = Api.ipcArcVaultApi.openFile (unbox null) request.path

                                    match openResult with
                                    | Ok loadedFile ->
                                        let selectedPath = normalizePath loadedFile.path
                                        fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                                        loadedFile
                                        |> Renderer.Components.ARCHelper.previewLoadResultOfDto
                                        |> Renderer.Components.ARCHelper.applyLoadedPreview
                                            pageStateCtx.setState
                                            arcObjectCtx.setArcFileState
                                            arcObjectCtx.setPreviewState
                                            arcObjectCtx.setStatusMessage
                                    | Error exn ->
                                        let label = arcCreateKindLabel kind
                                        let errorMessage =
                                            $"Created {label} '{identifier}' but could not open it: {exn.Message}"

                                        Renderer.Components.ARCHelper.applyPreviewError
                                            pageStateCtx.setState
                                            arcObjectCtx.setArcFileState
                                            arcObjectCtx.setPreviewState
                                            arcObjectCtx.setStatusMessage
                                            errorMessage
                                | Error exn ->
                                    let label = arcCreateKindLabel kind
                                    let errorMessage = $"Could not create {label} '{identifier}': {exn.Message}"

                                    Renderer.Components.ARCHelper.applyPreviewError
                                        pageStateCtx.setState
                                        arcObjectCtx.setArcFileState
                                        arcObjectCtx.setPreviewState
                                        arcObjectCtx.setStatusMessage
                                        errorMessage
                    }
                    |> Promise.start

        let arcCreateContextMenuItems (item: FileItem) =
            if item.IsDirectory then
                [
                    ArcCreateKind.Study
                    ArcCreateKind.Assay
                    ArcCreateKind.Workflow
                    ArcCreateKind.Run
                ]
                |> List.map (fun kind -> {
                    Label = $"Add {arcCreateKindLabel kind}"
                    Icon = arcCreateKindIcon kind
                    OnClick = fun () -> createArcEntry kind item
                    Disabled = None
                })
            else
                []

        let contextMenuItems (item: FileItem) =
            arcCreateContextMenuItems item
            @ FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        match fileItem with

        | Some fileItem ->
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                selectedItemId = fileStateCtx.state.Selection.TreePath
            )
        | None -> EmptyFileTreePlaceholder()
