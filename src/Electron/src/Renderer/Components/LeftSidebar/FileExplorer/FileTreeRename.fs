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
        pageState: Renderer.Types.PageState option
        closeRenameModal: unit -> unit
        setIsRenaming: bool -> unit
        setSelection: ArcSelection -> unit
        refreshGitStatus: unit -> unit
        reloadPreviewByPath: string -> JS.Promise<Result<unit, string>>
        renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>
        enqueueError: ErrorModalRequest -> unit
    }

    let private tryRemapActiveArcFilePath
        (sourcePath: string)
        (targetPath: string)
        (pageState: Renderer.Types.PageState option)
        =
        match pageState with
        | Some(Renderer.Types.PageState.ArcFilePage(arcFile, _)) ->
            arcFile.TryGetRelativePath()
            |> Option.bind (fun arcFilePath -> tryRemapSelectionPath sourcePath targetPath (Some arcFilePath))
        | _ -> None

    let requestRenameItem
        (setPendingRenameDraft: ArcRenameDraft option -> unit)
        (enqueueError: ErrorModalRequest -> unit)
        (item: FileItem)
        =
        match tryBuildRenameDraft item with
        | Ok renameDraft -> setPendingRenameDraft (Some renameDraft)
        | Error validationError ->
            FileTreeDialogWorkflow.enqueueError "Could not rename item" enqueueError validationError

    let confirmRenameItem (config: ConfirmRenameConfig) (newName: string) =
        let applyError = enqueueError "Could not rename item" config.enqueueError

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
                                tryRemapSelectionPath renameDraft.SourcePath targetPath config.selectedTreePath
                                |> Option.iter (fun remappedSelectionPath ->
                                    config.setSelection (ArcSelection.forTreePath (Some remappedSelectionPath))
                                )

                                match tryRemapActiveArcFilePath renameDraft.SourcePath targetPath config.pageState with
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
