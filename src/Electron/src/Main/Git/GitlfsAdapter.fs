module Main.Git.GitLfsAdapter

open Fable.Core.JsInterop
open Fable.Core.JS
open Swate.Electron.Shared.GitTypes

let private childProcessDynamic: obj = importAll "node:child_process"


type IGitLfs =
    abstract Run:
        request: GitLfsRequest -> onProgress: (string -> unit) -> cancel: (unit -> bool) -> Promise<GitLfsResult>

    abstract IsTrackedByAttributes: repoRoot: string -> relativePath: string -> bool


type NodeGitLfsAdapter() =
    let mutable isRunning = false

    let toArgs (request: GitLfsRequest) =
        match request.Command, request.FilePath with
        | Pull, None -> Ok [ "lfs"; "pull" ]
        | Pull, Some file -> Ok [ "lfs"; "pull"; "--include"; file ]
        | Fetch, None -> Ok [ "lfs"; "fetch" ]
        | Fetch, Some file -> Ok [ "lfs"; "fetch"; "--include"; file ]
        | Install, _ -> Ok [ "lfs"; "install" ]
        | Track, Some file -> Ok [ "lfs"; "track"; "--"; file ]
        | Untrack, Some file -> Ok [ "lfs"; "untrack"; "--"; file ]
        | Status, Some file -> Ok [ "lfs"; "ls-files"; "--name-only"; "--"; file ]
        | Track, None
        | Untrack, None
        | Status, None -> Error "FilePath is required for this Git LFS command"

    let validateRepoPath (repoPath: string) =
        try
            let output: string =
                childProcessDynamic?execFileSync (
                    "git",
                    [| "rev-parse"; "--is-inside-work-tree" |],
                    createObj [
                        "cwd" ==> repoPath
                        "encoding" ==> "utf8"
                        "stdio" ==> "pipe"
                        "shell" ==> false
                    ]
                )
                |> unbox<string>

            output.Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase)
        with _ ->
            false

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
                match toArgs request with
                | Error err ->
                    return {
                        Success = false
                        Output = ""
                        Error = err
                    }
                | Ok args ->
                    isRunning <- true

                    let! (result: GitLfsResult) = runProcess args

                    if not result.Success then
                        isRunning <- false
                        return result
                    else
                        match request.Command, request.FilePath with
                        | Pull, Some file
                        | Fetch, Some file ->
                            let! checkoutResult = runProcess [ "checkout"; "--"; file ]
                            isRunning <- false
                            return checkoutResult
                        | _ ->
                            isRunning <- false
                            return result
        }

    let isTrackedByAttributes (repoRoot: string) (relativePath: string) =
        if not (validateRepoPath repoRoot) || System.String.IsNullOrWhiteSpace relativePath then
            false
        else
            try
                let output: string =
                    childProcessDynamic?execFileSync (
                        "git",
                        [| "check-attr"; "filter"; "--"; relativePath |],
                        createObj [
                            "cwd" ==> repoRoot
                            "encoding" ==> "utf8"
                            "stdio" ==> "pipe"
                            "shell" ==> false
                        ]
                    )
                    |> unbox<string>

                output.Contains(": filter: lfs")
            with _ ->
                false

    interface IGitLfs with
        member _.Run request onProgress cancel = runGitLfs request onProgress cancel

        member _.IsTrackedByAttributes repoRoot relativePath =
            isTrackedByAttributes repoRoot relativePath



let gitLfsAdapter () : IGitLfs = NodeGitLfsAdapter() :> IGitLfs

// Adapter instance
let gitLfs = gitLfsAdapter ()