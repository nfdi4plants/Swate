module Main.IPC.GitLfs

open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

open Main.Git.GitLfsService

// Cancellation tracking - in-memory store for cancellation flags keyed by request ID
let cancellations = System.Collections.Generic.Dictionary<string, bool>()

let private createProgressReporter (window: BrowserWindow) (requestId: string) =
    let rendererApi =
        Remoting.createIpc ()
        |> Remoting.withWindow window
        |> Remoting.buildProxySender<IGitLfsProgressRendererApi>

    fun msg ->
        rendererApi.gitLfsProgressUpdate {
            RequestId = requestId
            Message = msg
        }

let runChannel (window: BrowserWindow) (request: GitLfsRequest) =
    promise {
        cancellations.[request.RequestId] <- false

        try
            let onProgress = createProgressReporter window request.RequestId

            let cancelCheck () =
                match cancellations.TryGetValue(request.RequestId) with
                | true, value -> value
                | _ -> false

            let! result = run request onProgress cancelCheck
            return result
        finally
            cancellations.Remove(request.RequestId) |> ignore
    }

let cancelChannel (requestId: string) =
    promise {
        if cancellations.ContainsKey requestId then
            cancellations.[requestId] <- true

        return Ok "Cancellation requested"
    }
