module Renderer.Components.Helper.NoteFileSystemHelper

open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOTypes

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

let writeNoteWithAssets (request: FileContentDTO) = promise {
    match! Api.ipcArcVaultApi.writeFile request with
    | Error error -> return Error error
    | Ok() -> return! ensureAssetsFolder request.path
}
