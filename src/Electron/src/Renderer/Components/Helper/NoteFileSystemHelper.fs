module Renderer.Components.Helper.NoteFileSystemHelper

open System
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper.NotePathHelper

let ensureAssetsFolder (markdownPath: string) = promise {
    match NoteConversion.tryGetNoteFolderRelativePath markdownPath with
    | None -> return Ok()
    | Some noteFolderPath ->
        let! createResult =
            Api.ipcArcVaultApi.createFileSystemItem {
                parentPath = noteFolderPath
                name = NoteConversion.noteAssetsFolderName
                kind = FileSystemItemKind.Folder
            }

        let isAlreadyExistsError (error: exn) =
            error.Message.ToLowerInvariant().Contains("already exists")

        match createResult with
        | Ok _ -> return Ok()
        | Error error when isAlreadyExistsError error -> return Ok()
        | Error error -> return Error error
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

let shouldRunOrShowOverwriteModal
    (errorModalCtx: ErrorModalActionsContext)
    (targetPath: string)
    overwrite
    =
    promise {
        let conflictPath = noteTargetConflictPath targetPath
        let! existsResult = Api.ipcArcVaultApi.pathExists conflictPath

        match existsResult with
        | Error error -> return Error error
        | Ok true ->
            showOverwriteConflictModal errorModalCtx conflictPath overwrite
            return Ok false
        | Ok false -> return Ok true
    }

let writeNoteWithAssets (request: FileContentDTO) = promise {
    match! Api.ipcArcVaultApi.writeFile request with
    | Error error -> return Error error
    | Ok() -> return! ensureAssetsFolder request.path
}
