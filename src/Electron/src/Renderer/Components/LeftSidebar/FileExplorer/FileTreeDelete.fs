namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Helper
open FileTreeDialogWorkflow

module FileTreeDeleteWorkflow =

    type ConfirmDeleteConfig = {
        pendingDeleteItem: FileItem option
        closeDeleteModal: unit -> unit
        setIsDeleting: bool -> unit
        enqueueError: ErrorModalRequest -> unit
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

            run
                config.setIsDeleting
                applyError
                (fun () -> promise {
                    match! Api.ipcArcVaultApi.deletePath deletePath with
                    | Ok() ->
                        config.closeDeleteModal ()
                        return Ok()
                    | Error exn -> return Error exn.Message
                })
