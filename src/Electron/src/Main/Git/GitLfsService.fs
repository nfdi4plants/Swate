module Main.Git.GitLfsService

open System
open System.Text.RegularExpressions
open Fable.Core
open Swate.Electron.Shared.GitTypes
open Main.Bindings.SimpleGit
open Main.Git.GitLfsAdapter

[<Literal>]
let DefaultTimeoutMs = 30000

let private redactPattern =
    Regex(@"(Authorization:\s*)(Bearer\s+|Basic\s+)[^\s'""]+", RegexOptions.IgnoreCase)

let private credentialUrlPattern =
    Regex("(https?://)([^\\s/@]+(?::[^\\s/@]*)?@)", RegexOptions.IgnoreCase)

let extractFailureMessage (result: GitLfsResult) =
    let errorText = result.Error |> Option.ofObj |> Option.defaultValue String.Empty |> _.Trim()
    let outputText = result.Output |> Option.ofObj |> Option.defaultValue String.Empty |> _.Trim()

    if not (String.IsNullOrWhiteSpace errorText) then
        errorText
    elif not (String.IsNullOrWhiteSpace outputText) then
        outputText
    else
        "Git LFS command failed."

let private redactDiagnosticText (text: string) =
    if String.IsNullOrWhiteSpace text then
        text
    else
        text
        |> fun value -> redactPattern.Replace(value, "$1[REDACTED]")
        |> fun value -> credentialUrlPattern.Replace(value, "$1[REDACTED]@")

let createRequest
    (repoPath: string)
    (command: GitLfsCommand)
    (filePath: string option)
    (timeoutMs: int option)
    : GitLfsRequest =
    {
        RequestId = Guid.NewGuid().ToString()
        RepoPath = repoPath
        Command = command
        FilePath = filePath
        TimeoutMs = Some(defaultArg timeoutMs DefaultTimeoutMs)
    }

let run
    (request: GitLfsRequest)
    (onProgress: string -> unit)
    (cancelCheck: unit -> bool)
    : JS.Promise<Result<GitLfsResult, exn>> =
    promise {
        try
            let! result = gitLfs.Run request onProgress cancelCheck

            if result.Success then
                return Ok result
            else
                return Error(exn (extractFailureMessage result))
        with ex ->
            return Error ex
    }

let runSilently (request: GitLfsRequest) : JS.Promise<Result<GitLfsResult, exn>> =
    run request ignore (fun () -> false)

let track (repoPath: string) (relativePath: string) : JS.Promise<Result<unit, string>> =
    promise {
        let! result =
            createRequest repoPath Track (Some relativePath) None
            |> runSilently

        return
            match result with
            | Ok _ -> Ok()
            | Error exn -> Error exn.Message
    }

let install (repoPath: string) : JS.Promise<Result<unit, string>> =
    promise {
        let! result =
            createRequest repoPath Install None None
            |> runSilently

        return
            match result with
            | Ok _ -> Ok()
            | Error exn -> Error exn.Message
    }

let isTrackedByAttributes (repoRoot: string) (relativePath: string) =
    gitLfs.IsTrackedByAttributes repoRoot relativePath

let private formatDiagnosticsSection (title: string) (content: string option) =
    match content |> Option.map _.Trim() with
    | Some value when not (String.IsNullOrWhiteSpace value) -> Some $"{title}:\n{redactDiagnosticText value}"
    | _ -> None

let private tryRunRawDiagnosticCommand
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (getFailureMessage: 'Failure -> string option)
    (git: ISimpleGit)
    (args: string[])
    : JS.Promise<string option> =
    promise {
        let! result = runSimpleGitRaw (fun currentGit -> currentGit.raw args) git

        return
            match result with
            | Ok output when not (String.IsNullOrWhiteSpace output) -> Some output
            | Ok _ -> None
            | Error failure ->
                getFailureMessage failure
                |> Option.filter (fun message -> not (String.IsNullOrWhiteSpace message))
                |> Option.map (fun message -> $"Unavailable: {message}")
    }

let collectPushDiagnostics
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (getFailureMessage: 'Failure -> string option)
    (remoteName: string)
    (git: ISimpleGit)
    : JS.Promise<string option> =
    promise {
        let! pushRemoteUrl =
            tryRunRawDiagnosticCommand runSimpleGitRaw getFailureMessage git [| "remote"; "get-url"; "--push"; remoteName |]

        let! lfsVersion =
            tryRunRawDiagnosticCommand runSimpleGitRaw getFailureMessage git [| "lfs"; "version" |]

        let! lfsEnv =
            tryRunRawDiagnosticCommand runSimpleGitRaw getFailureMessage git [| "lfs"; "env" |]

        let! lfsLogsLast =
            tryRunRawDiagnosticCommand runSimpleGitRaw getFailureMessage git [| "lfs"; "logs"; "last" |]

        return
            [
                formatDiagnosticsSection "Git Push Remote" pushRemoteUrl
                formatDiagnosticsSection "Git LFS Version" lfsVersion
                formatDiagnosticsSection "Git LFS Env" lfsEnv
                formatDiagnosticsSection "Git LFS Logs Last" lfsLogsLast
            ]
            |> List.choose id
            |> function
                | [] -> None
                | sections -> Some(String.concat "\n\n" sections)
    }

let appendPushDiagnostics (message: string) (diagnostics: string option) =
    match diagnostics |> Option.map _.Trim() with
    | Some value when not (String.IsNullOrWhiteSpace value) ->
        $"{message}\n\nLFS diagnostics:\n{value}"
    | _ ->
        message

let private resolvePushRefSpec
    (runStatus: ISimpleGit -> JS.Promise<Result<StatusResult, 'Failure>>)
    (remoteBranchName: string option)
    (git: ISimpleGit)
    : JS.Promise<string> =
    promise {
        match remoteBranchName with
        | Some branchName -> return branchName
        | None ->
            let! statusResult = runStatus git

            match statusResult with
            | Ok status ->
                let currentBranch =
                    status.current
                    |> Option.bind Option.ofObj
                    |> Option.map _.Trim()
                    |> Option.filter (fun branch -> not (String.IsNullOrWhiteSpace branch))
                    |> Option.defaultValue "HEAD"

                return currentBranch
            | Error _ ->
                return "HEAD"
    }

let pushObjects
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (runStatus: ISimpleGit -> JS.Promise<Result<StatusResult, 'Failure>>)
    (remoteName: string)
    (branchName: string option)
    (git: ISimpleGit)
    : JS.Promise<Result<unit, 'Failure>> =
    promise {
        let! refSpec = resolvePushRefSpec runStatus branchName git

        return!
            runSimpleGitRaw
                (fun currentGit -> promise {
                    let! _ = currentGit.raw [| "lfs"; "push"; remoteName; refSpec |]
                    return String.Empty
                })
                git
            |> Promise.map (
                function
                | Ok _ -> Ok()
                | Error failure -> Error failure
            )
    }
