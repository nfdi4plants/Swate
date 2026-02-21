namespace Renderer

open Fable.Core
open ARCtrl
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

[<RequireQualifiedAccess>]
module ArcFilePersistence =

    let saveArcFileWithPreview (arcFile: ArcFiles) : JS.Promise<Result<PreviewData, string>> =
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
