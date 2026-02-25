module Main.IPC.GitLfs

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS
open Fable.Electron

[<Import("existsSync", "node:fs")>]
let private existsSync (path: string) : bool = jsNative

[<Import("join", "node:path")>]
let private pathJoin (parts: string array) : string = jsNative

let private childProcessDynamic: obj = importAll "node:child_process"

type GitLfsCommand =
    | Pull
    | Fetch
    | Install

type GitLfsResult = {
    Success: bool
    Output: string
    Error: string
}

type GitLfsRequest = {
    RequestId: string
    RepoPath: string
    Command: GitLfsCommand
    FilePath: string option
    TimeoutMs: int option
}


type IGitLfs =
    abstract Run:
        request: GitLfsRequest -> onProgress: (string -> unit) -> cancel: (unit -> bool) -> Promise<GitLfsResult>


type NodeGitLfsAdapter() =
    let mutable isRunning = false

    let toArgs (request: GitLfsRequest) =
        match request.Command, request.FilePath with
        | Pull, None -> [ "lfs"; "pull" ]
        | Pull, Some file -> [ "lfs"; "pull"; "--include"; file ]
        | Fetch, None -> [ "lfs"; "fetch" ]
        | Fetch, Some file -> [ "lfs"; "fetch"; "--include"; file ]
        | Install, _ -> [ "lfs"; "install" ]

    let validateRepoPath repoPath =
        existsSync (pathJoin [| repoPath; ".git" |])

    let runGitLfs
        (request: GitLfsRequest)
        (onProgress: string -> unit)
        (cancelCheck: unit -> bool)
        : Promise<GitLfsResult> =
        promise {
            let runProcess (args: string list) =
                Fable.Core.JS.Constructors.Promise.Create(fun resolve _ ->
                    let proc: obj =
                        childProcessDynamic?spawn (
                            "git",
                            (args |> List.toArray),
                            createObj [ "cwd" ==> request.RepoPath; "shell" ==> false ]
                        )

                    let mutable output = ""
                    let mutable errorOut = ""
                    let mutable finished = false

                    proc?stdout?on (
                        "data",
                        fun d ->
                            let msg = string d
                            output <- output + msg
                            onProgress msg
                    )
                    |> ignore

                    proc?stderr?on (
                        "data",
                        fun d ->
                            let msg = string d
                            errorOut <- errorOut + msg
                            onProgress msg
                    )
                    |> ignore

                    let timeoutId =
                        request.TimeoutMs
                        |> Option.map (fun ms ->
                            Fable.Core.JS.setTimeout
                                (fun () ->
                                    if not finished then
                                        proc?kill ("SIGTERM") |> ignore
                                )
                                ms
                        )

                    let cancelInterval =
                        Fable.Core.JS.setInterval
                            (fun () ->
                                if not finished && cancelCheck () then
                                    proc?kill ("SIGTERM") |> ignore
                            )
                            300

                    proc?on (
                        "close",
                        fun code ->
                            finished <- true
                            timeoutId |> Option.iter Fable.Core.JS.clearTimeout
                            Fable.Core.JS.clearInterval cancelInterval

                            let success = if isNull code then false else (unbox<float> code) = 0.

                            resolve {
                                Success = success
                                Output = output
                                Error = errorOut
                            }
                    )
                    |> ignore

                    proc?on (
                        "error",
                        fun err ->
                            finished <- true
                            timeoutId |> Option.iter Fable.Core.JS.clearTimeout
                            Fable.Core.JS.clearInterval cancelInterval

                            resolve {
                                Success = false
                                Output = ""
                                Error = string err
                            }
                    )
                    |> ignore
                )

            if isRunning then
                return {
                    Success = false
                    Output = ""
                    Error = "Another Git process is running"
                }
            elif not (validateRepoPath request.RepoPath) then
                return {
                    Success = false
                    Output = ""
                    Error = "Not a git repository"
                }
            else
                isRunning <- true

                let! result = runProcess (toArgs request)

                if not result.Success then
                    isRunning <- false
                    return result
                else
                    match request.FilePath with
                    | Some file ->
                        let! checkoutResult = runProcess [ "checkout"; "--"; file ]
                        isRunning <- false
                        return checkoutResult
                    | None ->
                        isRunning <- false
                        return result
        }

    interface IGitLfs with
        member _.Run request onProgress cancel = runGitLfs request onProgress cancel


let gitLfsAdapter () : IGitLfs = NodeGitLfsAdapter() :> IGitLfs












// ==========================
// IPC Integration
// ==========================

[<Literal>]
let GitLfsRunChannel = "git-lfs:run"

[<Literal>]
let GitLfsProgressChannel = "git-lfs:progress"

[<Literal>]
let GitLfsCancelChannel = "git-lfs:cancel"



// Adapter instance
let gitLfs = gitLfsAdapter ()

// Cancellation tracking - in-memory store for cancellation flags keyed by request ID
let cancellations = System.Collections.Generic.Dictionary<string, bool>()



// ==========================
// IPC API Contract
// ==========================

type registerGitLfsIpcApi = {
    runChannel: IpcMainEvent -> GitLfsRequest -> JS.Promise<Result<string, exn>>
    cancelChannel: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
}

let registerGitLfsIpc: registerGitLfsIpcApi =

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
                            return Ok result.Output
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
