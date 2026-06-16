module Renderer.Components.MainContent.NoteTargetConflictHelper

open System
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types

let targetExistsOnDisk (targetPath: string) = promise {
    let! openResult = Api.ipcArcVaultApi.openFile targetPath

    match openResult with
    | Ok _ -> return true
    | Error _ -> return false
}

let showOverwriteConflictModal (errorModalCtx: ErrorModalActionsContext) (targetPath: string) overwrite =
    let modalId = Guid.NewGuid().ToString()

    errorModalCtx.enqueue (
        ErrorModalRequest.create (
            $"A note already exists at '{targetPath}'. Rename this note and try again, or overwrite the target note.",
            title = "Note already exists",
            dismissLabel = "Rename note",
            actions = [
                ErrorModalAction.create (
                    "Overwrite target",
                    (fun () ->
                        errorModalCtx.dismissById modalId
                        overwrite ()
                    ),
                    iconClassName = "swt:fluent--document-arrow-right-24-regular",
                    style = ErrorModalActionStyle.Error
                )
            ],
            id = modalId
        )
    )
