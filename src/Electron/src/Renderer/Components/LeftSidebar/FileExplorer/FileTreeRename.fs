namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeDialogWorkflow
open Renderer

module FileTreeRenameWorkflow =

    type ConfirmRenameConfig = {
        pendingRenameDraft: ArcRenameDraft option
        selectedTreePath: string option
        openArcFile: ArcFiles option
        closeRenameModal: unit -> unit
        setIsRenaming: bool -> unit
        setSelection: ArcSelection -> unit
        refreshGitStatus: unit -> unit
        reloadPreviewByPath: string -> JS.Promise<Result<unit, string>>
        renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>
        enqueueError: ErrorModalRequest -> unit
    }

    let private tryRemapActiveArcFilePath (sourcePath: string) (targetPath: string) (openArcFile: ArcFiles option) =
        openArcFile
        |> Option.bind (fun arcFile -> arcFile.TryGetRelativePath())
        |> Option.bind (PathHelpers.tryRemapPathPrefix sourcePath targetPath)

    let requestRenameItem
        (setPendingRenameDraft: ArcRenameDraft option -> unit)
        (enqueueError: ErrorModalRequest -> unit)
        (item: FileItem)
        =
        match tryBuildRenameDraft item with
        | Ok renameDraft -> setPendingRenameDraft (Some renameDraft)
        | Error validationError ->
            enqueueError (ErrorModalRequest.create (validationError, title = "Could not rename item"))

    let confirmRenameItem (config: ConfirmRenameConfig) (newName: string) =
        let applyError message =
            config.enqueueError (ErrorModalRequest.create (message, title = "Could not rename item"))

        match config.pendingRenameDraft with
        | None -> config.closeRenameModal ()
        | Some renameDraft ->
            match validateRenameName newName with
            | Error validationError -> applyError validationError
            | Ok normalizedNewName ->
                let targetPath = buildRenamedSiblingPath renameDraft.SourcePath normalizedNewName

                if PathHelpers.pathsEqual targetPath renameDraft.SourcePath then
                    config.closeRenameModal ()
                else
                    run
                        config.setIsRenaming
                        applyError
                        (fun () -> promise {
                            let! renameResult =
                                config.renamePath {
                                    relativePath = renameDraft.SourcePath
                                    newName = normalizedNewName
                                }

                            match renameResult with
                            | Error renameError -> return Error renameError.Message
                            | Ok() ->
                                config.selectedTreePath
                                |> Option.bind (PathHelpers.tryRemapPathPrefix renameDraft.SourcePath targetPath)
                                |> Option.iter (fun remappedSelectionPath ->
                                    config.setSelection (ArcSelection.forTreePath (Some remappedSelectionPath))
                                )

                                match
                                    tryRemapActiveArcFilePath renameDraft.SourcePath targetPath config.openArcFile
                                with
                                | Some remappedArcFilePath ->
                                    let! reloadResult = config.reloadPreviewByPath remappedArcFilePath

                                    match reloadResult with
                                    | Ok() -> ()
                                    | Error reloadError ->
                                        applyError
                                            $"Renamed item, but could not refresh the open ARC file preview: {reloadError}"
                                | None -> ()

                                config.refreshGitStatus ()
                                config.closeRenameModal ()
                                return Ok()
                        })
