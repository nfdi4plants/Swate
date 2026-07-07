namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Helper

module FileTreeDeleteWorkflow =

    type ConfirmDeleteConfig = {
        pendingDeleteItem: FileItem option
        closeDeleteModal: unit -> unit
        enqueueError: ErrorModalRequest -> unit
    }

    let private applyDeleteError (config: ConfirmDeleteConfig) (errorMessage: string) =
        config.enqueueError (ErrorModalRequest.create (errorMessage, title = "Could not delete item"))

    let requestDeleteItem (setPendingDeleteItem: FileItem option -> unit) (item: FileItem) =
        if canDeleteItem item then
            setPendingDeleteItem (Some item)

    let tryGetRelativePath (item: FileItem) : string option =
        item.Path |> Option.map PathHelpers.normalizeCanonicalRelativePath

    let confirmDeleteItem (config: ConfirmDeleteConfig) : JS.Promise<unit> =
        match config.pendingDeleteItem |> Option.bind tryGetRelativePath with
        | None -> promise { config.closeDeleteModal () }
        | Some deletePath when ArcEntityPathRules.isDeletePathAllowed deletePath |> not -> promise {
            config.closeDeleteModal ()
          }
        | Some deletePath ->
            promise {
                let! deleteResult = Api.ipcArcVaultApi.deletePath deletePath

                match deleteResult with
                | Ok() -> config.closeDeleteModal ()
                | Error exn -> applyDeleteError config exn.Message
            }
            |> Promise.catch (fun exn -> applyDeleteError config exn.Message)
