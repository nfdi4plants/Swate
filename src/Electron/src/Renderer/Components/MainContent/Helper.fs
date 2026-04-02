module Renderer.Components.MainContent.Helper


open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

module MainContentHelper =


    let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, exn>> = promise {

        let request = FileContentDTO.fromArcFile arcFile

        match request with
        | None -> return Error(exn "Saving this file type is not supported in Electron yet.")
        | Some request ->

            let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

            return saveResult
    }

    let saveArcFileAndOpen (arcFile: ArcFiles) = promise {

        let request = FileContentDTO.fromArcFile arcFile

        match request with
        | None -> return Error(exn "Saving this file type is not supported in Electron yet.")
        | Some request ->

            let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

            match saveResult with
            | Error exn -> return Error exn
            | Ok() ->
                let! openResult = Api.ipcArcVaultApi.openFile (unbox null) request.path

                return openResult
    }