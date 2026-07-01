module Renderer.Components.MainContent.Helper


open Fable.Core
open Feliz
open Renderer.Components.Helper.FileSystemHelper
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

let private tryCreateArcFileSaveRequest (arcFile: ArcFiles) : Result<FileContentDTO, exn> =
    match FileContentDTO.fromArcFile arcFile with
    | Some request -> Ok request
    | None -> Error(exn "Saving this file type is not supported in Electron yet.")

let private withArcFileRequest
    (arcFile: ArcFiles)
    (execute: FileContentDTO -> JS.Promise<Result<'T, exn>>)
    : JS.Promise<Result<'T, exn>> =
    promise {
        match tryCreateArcFileSaveRequest arcFile with
        | Error saveError -> return Error saveError
        | Ok request -> return! execute request
    }

let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, exn>> =
    withArcFileRequest
        arcFile
        (fun request -> promise {
            let! setResult = Api.ipcArcVaultApi.setArcFileInMemory request

            match setResult with
            | Error exn -> return Error exn
            | Ok() -> return! Api.ipcArcVaultApi.saveArcFile ()
        })

let setArcFileInMemory (arcFile: ArcFiles) : JS.Promise<Result<unit, exn>> =
    withArcFileRequest arcFile Api.ipcArcVaultApi.setArcFileInMemory

let saveArcFileAndOpen (arcFile: ArcFiles) =
    withArcFileRequest
        arcFile
        (fun request -> promise {
            let! saveResult = Api.ipcArcVaultApi.addArcFile request

            match saveResult with
            | Error exn -> return Error exn
            | Ok() ->
                let! openResult = Api.ipcArcVaultApi.openFile request.path
                return openResult
        })

[<Hook>]
let useNoteImageFilePickerAdapter (pendingImageAssetsRef: IRefValue<ExternalAssetLink list>) =
    React.useMemo (
        (fun _ ->
            createAssetFilePickerAdapter
                Api.ipcArcVaultApi.pickExternalFilePaths
                NoteConversion.noteAssetsFolderName
                (fun asset -> pendingImageAssetsRef.current <- pendingImageAssetsRef.current @ [ asset ])
        ),
        [||]
    )

let writeNoteMarkdownFile (request: FileContentDTO) (assets: ExternalAssetLink list) =
    writeFileWithOptionalExternalAssetLinks
        Api.ipcArcVaultApi.writeFile
        Api.ipcArcVaultApi.createFileSystemItem
        Api.ipcArcVaultApi.copyExternalFilesToArc
        NoteConversion.tryGetNoteFolderRelativePath
        NoteConversion.noteAssetsFolderName
        request
        assets

let writeMarkdownNote path content assets =
    writeNoteMarkdownFile (FileContentDTO.create FileContentType.Markdown content path) assets

let requestFromNotesPayload (payload: NotesSubmitPayload) =
    let targetPath = PathHelpers.normalizePath payload.Intent.RelativePath

    FileContentDTO.create (FileContentDTO.inferTextFileTypeFromPath targetPath) payload.Intent.Content targetPath

let availableExistingNoteTargets fileTree =
    createAvailableArcEntityTargets
        [
            (ARCtrl.ArcPathHelper.StudiesFolderName,
             ARCtrl.ArcPathHelper.StudyFileName,
             fun name -> {
                 Name = name
                 Kind = NotesTargetKind.Study
             })
            (ARCtrl.ArcPathHelper.AssaysFolderName,
             ARCtrl.ArcPathHelper.AssayFileName,
             fun name -> {
                 Name = name
                 Kind = NotesTargetKind.Assay
             })
        ]
        fileTree
