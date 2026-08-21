module Renderer.Components.MainContent.Helper


open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

let private tryCreateArcFileSaveRequest (arcFile: ArcFiles) : Result<FileContentDTO, exn> =
    match FileContentDTO.fromArcFile arcFile with
    | Some request -> Ok request
    | None -> Error(exn "Saving this file type is not supported in Electron yet.")

let withArcFileRequest
    (arcFile: ArcFiles)
    (execute: FileContentDTO -> JS.Promise<Result<'T, exn>>)
    : JS.Promise<Result<'T, exn>> =
    promise {
        match tryCreateArcFileSaveRequest arcFile with
        | Error saveError -> return Error saveError
        | Ok request -> return! execute request
    }

let addArcFileAndOpen (arcFile: ArcFiles) : JS.Promise<Result<FileContentDTO, exn>> =
    withArcFileRequest
        arcFile
        (fun request -> promise {
            match! Api.ipcArcVaultApi.addArcFile request with
            | Error exn -> return Error exn
            | Ok() -> return! Api.ipcArcVaultApi.openFile request.path
        })

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
