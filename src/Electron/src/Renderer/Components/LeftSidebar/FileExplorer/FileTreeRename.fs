namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

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

    let private enqueueRenameError
        (enqueueError: ErrorModalRequest -> unit)
        (errorMessage: string)
        =
        enqueueError (
            ErrorModalRequest.create (
                errorMessage,
                title = "Could not rename item"
            )
        )

    let private applyRenameError (config: ConfirmRenameConfig) (errorMessage: string) =
        enqueueRenameError config.enqueueError errorMessage

    let private tryRemapActiveArcFilePath
        (sourcePath: string)
        (targetPath: string)
        (pageState: Renderer.Types.PageState option)
        =
        match pageState with
        | Some(Renderer.Types.PageState.ArcFilePage arcFile) ->
            arcFile.TryGetRelativePath()
            |> Option.bind (fun arcFilePath ->
                tryRemapSelectionPath sourcePath targetPath (Some arcFilePath)
            )
        | _ -> None

    let requestRenameItem
        (setPendingRenameDraft: ArcRenameDraft option -> unit)
        (enqueueError: ErrorModalRequest -> unit)
        (item: FileItem)
        =
        match tryBuildRenameDraft item with
        | Ok renameDraft -> setPendingRenameDraft (Some renameDraft)
        | Error validationError -> enqueueRenameError enqueueError validationError

    let confirmRenameItem (config: ConfirmRenameConfig) (newName: string) =
        match config.pendingRenameDraft with
        | None -> config.closeRenameModal ()
        | Some renameDraft ->
            match validateRenameName newName with
            | Error validationError -> applyRenameError config validationError
            | Ok normalizedNewName ->
                let targetPath = buildRenamedSiblingPath renameDraft.SourcePath normalizedNewName

                if PathHelpers.pathsEqual targetPath renameDraft.SourcePath then
                    config.closeRenameModal ()
                else
                    config.setIsRenaming true

                    promise {
                        let! renameResult =
                            config.renamePath {
                                relativePath = renameDraft.SourcePath
                                newName = normalizedNewName
                            }

                        match renameResult with
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
                                    applyRenameError
                                        config
                                        $"Renamed item, but could not refresh the open ARC file preview: {reloadError}"
                            | None -> ()

                            config.refreshGitStatus ()
                            config.closeRenameModal ()
                        | Error renameError -> applyRenameError config renameError.Message
                    }
                    |> Promise.catch (fun promiseError -> applyRenameError config promiseError.Message)
                    |> Promise.map (fun _ -> config.setIsRenaming false)
                    |> Promise.start
