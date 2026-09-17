module Main.FileImportCoordinator

open Fable.Core
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main.Bindings.Abort
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

type ActiveFileImport = {
    State: ActiveFileImportState
    AbortController: IAbortController
    Completion: JS.Promise<Result<ImportExternalFilesResult, exn>>
}

let publishState (window: BrowserWindow) activeImport =
    Remoting.createIpc ()
    |> Remoting.withWindow window
    |> Remoting.buildProxySender<IFileImportRendererApi>
    |> fun sender -> sender.fileImportStateUpdate (activeImport |> Option.map _.State)

let run
    (window: BrowserWindow)
    requestId
    canStart
    getActiveImport
    setActiveImport
    (operation: IAbortSignal -> JS.Promise<Result<ImportExternalFilesResult, exn>>)
    =
    match canStart (), getActiveImport () with
    | Error error, _ -> JS.Constructors.Promise.resolve (Error error)
    | Ok(), Some _ ->
        JS.Constructors.Promise.resolve (Error(exn "Another file import is already running in this window."))
    | Ok(), None ->
        let abortController = AbortController.create ()

        let completion = promise {
            try
                return! operation abortController.signal
            finally
                setActiveImport None
                publishState window None
        }

        let activeImport = {
            State = {
                requestId = requestId
                phase = FileImportPhase.Copying
            }
            AbortController = abortController
            Completion = completion
        }

        setActiveImport (Some activeImport)
        publishState window (Some activeImport)
        completion
