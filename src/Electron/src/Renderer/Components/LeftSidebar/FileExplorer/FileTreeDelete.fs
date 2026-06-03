namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Helper

module FileTreeDeleteWorkflow =

    type ConfirmDeleteConfig = {
        pendingDeleteItem: FileItem option
        closeDeleteModal: unit -> unit
        setIsDeleting: bool -> unit
        enqueueError: ErrorModalRequest -> unit
    }

    let private applyDeleteError (config: ConfirmDeleteConfig) (errorMessage: string) =
        config.enqueueError (
            ErrorModalRequest.create (
                errorMessage,
                title = "Could not delete item"
            )
        )

    let requestDeleteItem (setPendingDeleteItem: FileItem option -> unit) (item: FileItem) =
        if canDeleteItem item then
            setPendingDeleteItem (Some item)

    let tryGetRelativePath (item: FileItem) : string option =
        item.Path
        |> Option.map PathHelpers.normalizeCanonicalRelativePath

    let confirmDeleteItem (config: ConfirmDeleteConfig) =
        match config.pendingDeleteItem |> Option.bind tryGetRelativePath with
        | None -> config.closeDeleteModal ()
        | Some deletePath when ArcEntityPathRules.isDeletePathAllowed deletePath |> not ->
            config.closeDeleteModal ()
        | Some deletePath ->
            config.setIsDeleting true

            promise {
                let! deleteResult = Api.ipcArcVaultApi.deletePath deletePath

                match deleteResult with
                | Ok() -> config.closeDeleteModal ()
                | Error exn -> applyDeleteError config exn.Message
            }
            |> Promise.catch (fun exn -> applyDeleteError config exn.Message)
            |> Promise.map (fun _ -> config.setIsDeleting false)
            |> Promise.start

    let deleteContextMenuItems (requestDeleteItem: FileItem -> unit) (item: FileItem) =
        FileExplorerContextMenuItem.whenItem
            canDeleteItem
            "Delete"
            "swt:fluent--delete-24-regular"
            requestDeleteItem
            item
