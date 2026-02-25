module Main.IPC.GitLfs

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS
open Node.Api
open Fable.Electron


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
        | Pull, None ->
            [ "lfs"; "pull" ]
        | Pull, Some file ->
            [ "lfs"; "pull"; "--include"; file ]
        | Fetch, None ->
            [ "lfs"; "fetch" ]
        | Fetch, Some file ->
            [ "lfs"; "fetch"; "--include"; file ]
        | Install, _ ->
            [ "lfs"; "install" ]

    let validateRepoPath repoPath =
        Fs.existsSync (Path.join (repoPath, ".git"))

    let runGitLfs
    (request: GitLfsRequest)
    (onProgress: string -> unit)
    (cancelCheck: unit -> bool)
    : Promise<GitLfsResult> =
    promise {
        if isRunning then
            return { Success = false; Output = ""; Error = "Another Git process is running" }

        if not (validateRepoPath request.RepoPath) then
            return { Success = false; Output = ""; Error = "Not a git repository" }

        isRunning <- true

        let runProcess args =
            Promise.create (fun resolve _ ->
                let proc = ChildProcess.spawn ("git", ResizeArray args, createObj [ "cwd" ==> request.RepoPath; "shell" ==> false ])
                let mutable output = ""
                let mutable errorOut = ""
                let mutable finished = false

                proc.stdout.on("data", fun d -> let msg = string d in output <- output + msg; onProgress msg) |> ignore
                proc.stderr.on("data", fun d -> let msg = string d in errorOut <- errorOut + msg; onProgress msg) |> ignore

                let timeoutId =
                    match request.TimeoutMs with
                    | Some ms -> Some(Timers.setTimeout((fun () -> if not finished then proc.kill("SIGTERM") |> ignore), ms))
                    | None -> None

                let cancelInterval =
                    Timers.setInterval((fun () -> if not finished && cancelCheck () then proc.kill("SIGTERM") |> ignore), 300)

                proc.on("close", fun code ->
                    finished <- true
                    timeoutId |> Option.iter Timers.clearTimeout
                    Timers.clearInterval cancelInterval
                    resolve { Success = (code = 0.); Output = output; Error = errorOut }
                ) |> ignore

                proc.on("error", fun err ->
                    finished <- true
                    resolve { Success = false; Output = ""; Error = string err }
                ) |> ignore
            )


        let! result = runProcess (toArgs request)
        if not result.Success then
            isRunning <- false
            return result

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