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
        withArcFileRequest arcFile (fun request ->
            promise {
                let! setResult = Api.ipcArcVaultApi.setArcFileInMemory request

                match setResult with
                | Error exn -> return Error exn
                | Ok() -> return! Api.ipcArcVaultApi.saveArcFile ()
            }
        )

    let setArcFileInMemory (arcFile: ArcFiles) : JS.Promise<Result<unit, exn>> =
        withArcFileRequest arcFile Api.ipcArcVaultApi.setArcFileInMemory

    let saveArcFileAndOpen (arcFile: ArcFiles) =
        withArcFileRequest arcFile (fun request ->
            promise {
                let! setResult = Api.ipcArcVaultApi.setArcFileInMemory request

                match setResult with
                | Error exn -> return Error exn
                | Ok() ->
                    let! saveResult = Api.ipcArcVaultApi.saveArcFile ()

                    match saveResult with
                    | Error exn -> return Error exn
                    | Ok() ->
                        let! openResult = Api.ipcArcVaultApi.openFile request.path
                        return openResult
            }
        )
