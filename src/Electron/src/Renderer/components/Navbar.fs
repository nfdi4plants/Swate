module Renderer.components.Navbar

open Fable.Core

open ARCtrl
open Swate.Electron.Shared


let saveArcFileWithPreview (arcFile: ArcFiles) : JS.Promise<Result<IPCTypes.PreviewData, string>> =
    promise {
        match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
        | None ->
            return Error "Saving this file type is not supported in Electron yet."
        | Some request ->
            let! saveResult = Api.saveArcFile request

            match saveResult with
            | Ok previewData -> return Ok previewData
            | Error exn -> return Error exn.Message
    }

let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, string>> =
    promise {
        let! saveResult = saveArcFileWithPreview arcFile
        return saveResult |> Result.map ignore
    }

let onSaveClick arcFileState setPreviewData setDidSelectFile _ =
    match arcFileState with
    | None -> ()
    | Some arcFile ->
        promise {
            let! result = saveArcFileWithPreview arcFile

            match result with
            | Ok updatedPreview ->
                setPreviewData (Some updatedPreview)
                setDidSelectFile true
            | Error errorMsg ->
                setPreviewData (Some (IPCTypes.PreviewData.Error $"Save failed: {errorMsg}"))
        }
        |> Promise.start
