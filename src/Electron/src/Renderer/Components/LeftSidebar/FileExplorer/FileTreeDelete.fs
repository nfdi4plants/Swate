namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Fable.Core
open Swate.Components.Shared
open Helper
open FileTreeDialogWorkflow

module FileTreeDeleteWorkflow =

    type ConfirmDeleteConfig = {
        pendingDeleteItem: FileItem option
        closeDeleteModal: unit -> unit
        setIsDeleting: bool -> unit
        enqueueError: ErrorModalRequest -> unit
        /// Returns the paths of the running Download and Free actions when the user confirms.
        /// A function keeps the check current, because an action can start while the modal is open.
        getLfsActivePaths: unit -> string list
        deletePath: string -> JS.Promise<Result<unit, exn>>
    }

    let requestDeleteItem (setPendingDeleteItem: FileItem option -> unit) (item: FileItem) =
        if
            item.Path
            |> Option.map PathHelpers.normalizeCanonicalRelativePath
            |> Option.exists ArcEntityPathRules.isDeletePathAllowed
        then
            setPendingDeleteItem (Some item)

    let confirmDeleteItem (config: ConfirmDeleteConfig) =
        match
            config.pendingDeleteItem
            |> Option.bind _.Path
            |> Option.map PathHelpers.normalizeCanonicalRelativePath
        with
        | None -> config.closeDeleteModal ()
        | Some deletePath when ArcEntityPathRules.isDeletePathAllowed deletePath |> not -> config.closeDeleteModal ()
        | Some deletePath ->
            let applyError message =
                config.enqueueError (ErrorModalRequest.create (message, title = "Could not delete item"))

            let isLocked =
                config.pendingDeleteItem
                |> Option.exists (FileTreeContextMenu.isLockedByLfsActivity (config.getLfsActivePaths ()))

            if isLocked then
                config.closeDeleteModal ()

                applyError
                    $"Swate cannot delete '{deletePath}' while a large file download or free runs on a file the delete would remove. Wait until it finishes, then try again."
            else
                run
                    config.setIsDeleting
                    applyError
                    (fun () -> promise {
                        match! config.deletePath deletePath with
                        | Ok() ->
                            config.closeDeleteModal ()
                            return Ok()
                        | Error exn -> return Error exn.Message
                    })
