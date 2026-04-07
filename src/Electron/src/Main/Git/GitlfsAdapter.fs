module Main.Git.GitLfsAdapter

open Fable.Core.JsInterop
open Fable.Core.JS
open Swate.Electron.Shared.GitTypes
open Main.Bindings.Node

[<Literal>]
let private repoValidationTimeoutMs = 5000

let tryExecGitText
    (workingDirectory: string option)
    (timeoutMs: int)
    (args: string[])
    : Promise<string option> =
    promise {
        let! output =
            Fable.Core.JS.Constructors.Promise.Create(fun resolve _reject ->
                let options =
                    createObj [
                        "encoding" ==> "utf8"
                        "stdio" ==> "pipe"
                        "shell" ==> false
                        "timeout" ==> timeoutMs

                        match workingDirectory with
                        | Some value -> "cwd" ==> value
                        | None -> ()
                    ]

                childProcessDynamic?execFile (
                    "git",
                    args,
                    options,
                    fun (error: obj) (stdout: obj) (_stderr: obj) ->
                        if error |> Option.ofObj |> Option.isSome then
                            resolve None
                        else
                            stdout
                            |> Option.ofObj
                            |> Option.map string
                            |> resolve
                )
                |> ignore)

        return output
    }


type IGitLfs =
    abstract Run:
        request: GitLfsRequest -> onProgress: (string -> unit) -> cancel: (unit -> bool) -> Promise<GitLfsResult>

    abstract Install:
        timeoutMs: int option -> onProgress: (string -> unit) -> cancel: (unit -> bool) -> Promise<GitLfsResult>

    abstract IsTrackedByAttributes: repoRoot: string -> relativePath: string -> bool


type NodeGitLfsAdapter() =
    let activeLockKeys = System.Collections.Generic.HashSet<string>()

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

    let normalizeLockKey (workingDirectory: string option) =
        workingDirectory
        |> Option.defaultValue "__system__"
        |> _.Trim()
        |> _.ToLowerInvariant()

    let validateRepoPath (repoPath: string) : Promise<bool> =
        promise {
            let! output =
                tryExecGitText
                    (Some repoPath)
                    repoValidationTimeoutMs
                    [| "rev-parse"; "--is-inside-work-tree" |]

            return
                output
                |> Option.exists (fun text ->
                    text.Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase)
                )
        }

    let validateRepoPathSync (repoPath: string) =
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

    let runProcess
        (args: string list)
        (workingDirectory: string option)
        (timeoutMs: int option)
        (onProgress: string -> unit)
        (cancelCheck: unit -> bool)
        : Promise<GitLfsResult> =
        promise {
            let lockKey = normalizeLockKey workingDirectory

            if activeLockKeys.Contains lockKey then
                return {
                    Success = false
                    Output = ""
                    Error = "Another Git LFS process is running for this repository."
                }
            else
                activeLockKeys.Add lockKey |> ignore

                try
                    let! result =
                        Fable.Core.JS.Constructors.Promise.Create(fun resolve _ ->
                            let spawnOptions =
                                createObj [
                                    "shell" ==> false

                                    match workingDirectory with
                                    | Some value -> "cwd" ==> value
                                    | None -> ()
                                ]

                            let proc: obj =
                                childProcessDynamic?spawn ("git", (args |> List.toArray), spawnOptions)

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
                                timeoutMs
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

                    return result
                finally
                    activeLockKeys.Remove lockKey |> ignore
        }

    let runGitLfs
        (request: GitLfsRequest)
        (onProgress: string -> unit)
        (cancelCheck: unit -> bool)
        : Promise<GitLfsResult> =
        promise {
            let! isValidRepo = validateRepoPath request.RepoPath

            if not isValidRepo then
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
                    let! (result: GitLfsResult) =
                        runProcess args (Some request.RepoPath) request.TimeoutMs onProgress cancelCheck

                    if not result.Success then
                        return result
                    else
                        match request.Command, request.FilePath with
                        | Pull, Some file
                        | Fetch, Some file ->
                            return!
                                runProcess
                                    [ "checkout"; "--"; file ]
                                    (Some request.RepoPath)
                                    request.TimeoutMs
                                    onProgress
                                    cancelCheck
                        | _ ->
                            return result
        }

    let installGitLfs
        (timeoutMs: int option)
        (onProgress: string -> unit)
        (cancelCheck: unit -> bool)
        : Promise<GitLfsResult> =
        promise {
            return! runProcess [ "lfs"; "install" ] None timeoutMs onProgress cancelCheck
        }

    let isTrackedByAttributes (repoRoot: string) (relativePath: string) =
        if not (validateRepoPathSync repoRoot) || System.String.IsNullOrWhiteSpace relativePath then
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

        member _.Install timeoutMs onProgress cancel =
            installGitLfs timeoutMs onProgress cancel

        member _.IsTrackedByAttributes repoRoot relativePath =
            isTrackedByAttributes repoRoot relativePath



let gitLfsAdapter () : IGitLfs = NodeGitLfsAdapter() :> IGitLfs

// Adapter instance
let gitLfs = gitLfsAdapter ()
