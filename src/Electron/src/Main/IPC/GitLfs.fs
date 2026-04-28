module Main.IPC.GitLfs

open Fable.Electron
open Fable.Electron.Main
open Swate.Electron.Shared.GitTypes

open Main.Git.GitLfsService

[<Literal>]
let GitLfsProgressChannel = "git-lfs:progress"

// Cancellation tracking - in-memory store for cancellation flags keyed by request ID
let cancellations = System.Collections.Generic.Dictionary<string, bool>()

let runChannel (event: IpcMainInvokeEvent) (request: GitLfsRequest) =
    promise {
        cancellations.[request.RequestId] <- false

        try
            let onProgress msg =
                event.sender.send (GitLfsProgressChannel, [| box request.RequestId; box msg |])

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
