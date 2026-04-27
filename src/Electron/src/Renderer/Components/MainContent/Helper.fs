module Renderer.Components.MainContent.Helper


open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

module MainContentHelper =

    let private tryCreateArcFileSaveRequest (arcFile: ArcFiles) : Result<FileContentDTO, exn> =
        match FileContentDTO.fromArcFile arcFile with
        | Some request -> Ok request
        | None -> Error(exn "Saving this file type is not supported in Electron yet.")

    let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, exn>> = promise {
        match tryCreateArcFileSaveRequest arcFile with
        | Error saveError -> return Error saveError
        | Ok request ->
            let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

            return saveResult
    }

    let setPendingArcFileSave (arcFile: ArcFiles option) : JS.Promise<Result<unit, exn>> = promise {
        let pendingSaveRequestResult =
            match arcFile with
            | None -> Ok None
            | Some nextArcFile -> tryCreateArcFileSaveRequest nextArcFile |> Result.map Some

        match pendingSaveRequestResult with
        | Error saveError -> return Error saveError
        | Ok pendingSaveRequest ->
            let! result = Api.ipcArcVaultApi.setPendingArcFileSave (unbox null) pendingSaveRequest
            return result
    }

    let saveArcFileAndOpen (arcFile: ArcFiles) = promise {
        match tryCreateArcFileSaveRequest arcFile with
        | Error saveError -> return Error saveError
        | Ok request ->
            let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

            match saveResult with
            | Error exn -> return Error exn
            | Ok() ->
                let! openResult = Api.ipcArcVaultApi.openFile (unbox null) request.path

                return openResult
    }
