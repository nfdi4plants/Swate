module Renderer.components.Navbar

open Fable.Core

open ARCtrl
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes


let saveArcFileWithPreview (arcFile: ArcFiles) : JS.Promise<Result<PreviewData, string>> =
    promise {
        match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
        | None ->
            return Microsoft.FSharp.Core.Error "Saving this file type is not supported in Electron yet."
        | Some request ->
            let! saveResult = Api.saveArcFile request

            match saveResult with
            | Ok previewData -> return Ok previewData
            | Microsoft.FSharp.Core.Error exn -> return Microsoft.FSharp.Core.Error exn.Message
    }

let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, string>> =
    promise {
        let! saveResult = saveArcFileWithPreview arcFile
        return saveResult |> Result.map ignore
    }

let onSaveClick arcFileState setPreviewData _ =
    match arcFileState with
    | None -> ()
    | Some arcFile ->
        promise {
            let! result = saveArcFileWithPreview arcFile

            match result with
            | Ok updatedPreview ->
                setPreviewData (Some updatedPreview)
            | Microsoft.FSharp.Core.Error errorMsg ->
                setPreviewData (Some (Error $"Save failed: {errorMsg}"))
        }
        |> Promise.start
