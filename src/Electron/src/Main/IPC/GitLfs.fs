module Main.IPC.GitLfs

open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.GitTypes

open Main.Git.GitLfsAdapter



// ==========================
// IPC Integration
// ==========================

[<Literal>]
let GitLfsRunChannel = "git-lfs:run"

[<Literal>]
let GitLfsProgressChannel = "git-lfs:progress"

[<Literal>]
let GitLfsCancelChannel = "git-lfs:cancel"





// Cancellation tracking - in-memory store for cancellation flags keyed by request ID
let cancellations = System.Collections.Generic.Dictionary<string, bool>()



// ==========================
// IPC API Contract
// ==========================

/// git lfs file IPC call method :/
let registerGitLfsIpc: IGitLfsApi =

    {
        runChannel =
            fun (event: IpcMainEvent) (request: GitLfsRequest) ->

                promise {
                    try
                        cancellations.[request.RequestId] <- false

                        let onProgress msg =
                            event.sender.send (GitLfsProgressChannel, [| box request.RequestId; box msg |])

                        let cancelCheck () =
                            match cancellations.TryGetValue(request.RequestId) with
                            | true, value -> value
                            | _ -> false

                        let! result = gitLfs.Run request onProgress cancelCheck

                        cancellations.Remove(request.RequestId) |> ignore

                        if result.Success then
                            return Ok result
                        else
                            return Error(System.Exception result.Error)

                    with ex ->
                        return Error ex
                }

        cancelChannel =
            fun (_: IpcMainEvent) (requestId: string) ->

                promise {
                    cancellations.[requestId] <- true
                    return Ok "Cancellation requested"
                }
    }