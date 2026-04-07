module Main.Git.GitLfsService

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Swate.Electron.Shared.GitTypes
open Main.Bindings.SimpleGit
open Main.Git.GitLfsAdapter
open Main.Git.GitAuthAdapter

[<Literal>]
let DefaultTimeoutMs = 30000

[<RequireQualifiedAccess>]
type OutboundPushPlan =
    | SkipLfsUpload
    | UploadLfsObjects of refSpec: string

let private lfsPointerOidPattern = Regex("^oid sha256:[0-9a-f]{64}$", RegexOptions.Multiline)
let private lfsPointerSizePattern = Regex("^size \\d+$", RegexOptions.Multiline)
let private maxLfsPointerProbeBytes = 1024L
let mutable private cachedSystemInstalled = false

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
        redactToken text

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

let installSystem () : JS.Promise<Result<unit, string>> =
    promise {
        try
            let! result = gitLfs.Install (Some DefaultTimeoutMs) ignore (fun () -> false)

            return
                if result.Success then
                    cachedSystemInstalled <- true
                    Ok()
                else
                    Error(extractFailureMessage result)
        with ex ->
            return Error ex.Message
    }

let isSystemInstalled () : JS.Promise<bool> =
    promise {
        if cachedSystemInstalled then
            return true
        else
            let! output = tryExecGitText None DefaultTimeoutMs [| "lfs"; "version" |]
            let isInstalled = output |> Option.exists (fun text -> not (String.IsNullOrWhiteSpace text))

            if isInstalled then
                cachedSystemInstalled <- true

            return isInstalled
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

let private splitNonEmptyLines (text: string) =
    text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
    |> Array.map _.Trim()
    |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace line))

let private shouldSkipLfsUploadFromPushDryRun (output: string) =
    let statusLines =
        output
        |> splitNonEmptyLines
        |> Array.filter (fun line ->
            not (line.StartsWith("To ", StringComparison.Ordinal))
            && not (line.Equals("Done", StringComparison.OrdinalIgnoreCase))
        )

    statusLines.Length = 0
    || statusLines |> Array.forall (fun line -> line.StartsWith("=", StringComparison.Ordinal))
    // Rejected dry runs still mean there is no successful outbound push to upload LFS objects for.
    || statusLines |> Array.exists (fun line -> line.StartsWith("!", StringComparison.Ordinal))

let private getKnownRemoteRefs
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (remoteName: string)
    (git: ISimpleGit)
    : JS.Promise<Result<string[], 'Failure>> =
    promise {
        let! result =
            runSimpleGitRaw
                (fun currentGit -> currentGit.raw [| "for-each-ref"; "--format=%(refname)"; $"refs/remotes/{remoteName}" |])
                git

        return
            result
            |> Result.map (fun output ->
                output
                |> splitNonEmptyLines
                |> Array.filter (fun refName -> not (refName.EndsWith("/HEAD", StringComparison.Ordinal)))
            )
    }

let private getOutboundObjectIds
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (refSpec: string)
    (remoteRefs: string[])
    (git: ISimpleGit)
    : JS.Promise<Result<string[], 'Failure>> =
    promise {
        let args =
            [|
                "rev-list"
                "--objects"
                refSpec
                yield!
                    remoteRefs
                    |> Array.map (fun remoteRef -> $"^{remoteRef}")
            |]

        let! result = runSimpleGitRaw (fun currentGit -> currentGit.raw args) git

        return
            result
            |> Result.map (fun output ->
                output
                |> splitNonEmptyLines
                |> Array.choose (fun line ->
                    let separatorIndex = line.IndexOf(' ')

                    if separatorIndex <= 0 then
                        None
                    else
                        Some(line.Substring(0, separatorIndex))
                )
                |> Array.distinct
            )
    }

let private isLfsPointerContent (content: string) =
    let normalized =
        content.Replace("\r\n", "\n").Trim()

    normalized.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal)
    && lfsPointerOidPattern.IsMatch normalized
    && lfsPointerSizePattern.IsMatch normalized

/// Checks whether any of the given git object IDs (from a push dry-run) are LFS pointer files.
/// This determines if `git lfs push` is needed before the actual `git push`.
/// Uses `git cat-file -t` to check blob type and `git cat-file -s` to check size,
/// then reads small blobs to detect LFS pointer content (version + oid + size headers).
let private containsOutboundLfsPointerObjects
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (objectIds: string[])
    (git: ISimpleGit)
    : JS.Promise<Result<bool, 'Failure>> =
    promise {
        let mutable hasLfsPointer = false
        let mutable failure: 'Failure option = None

        for objectId in objectIds do
            if not hasLfsPointer && failure.IsNone then
                let! objectTypeResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-t"; objectId |]) git

                match objectTypeResult with
                | Error currentFailure ->
                    failure <- Some currentFailure
                | Ok objectType when objectType.Trim().Equals("blob", StringComparison.Ordinal) ->
                    // Use `git cat-file -s` instead of fs.statSync because we need the size of the staged blob
                    // in git's object store, not the working tree file. The on-disk file may differ from what is
                    // staged (partial staging, .gitattributes filters, CRLF normalization).
                    let! objectSizeResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-s"; objectId |]) git

                    match objectSizeResult with
                    | Error currentFailure ->
                        failure <- Some currentFailure
                    | Ok objectSizeText ->
                        let success, objectSize = Int64.TryParse(objectSizeText.Trim())

                        if (not success) || objectSize <= maxLfsPointerProbeBytes then
                            let! contentResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-p"; objectId |]) git

                            match contentResult with
                            | Error currentFailure ->
                                failure <- Some currentFailure
                            | Ok content when isLfsPointerContent content ->
                                hasLfsPointer <- true
                            | Ok _ ->
                                ()
                | Ok _ ->
                    ()

        match failure with
        | Some currentFailure -> return Error currentFailure
        | None -> return Ok hasLfsPointer
    }

let planOutboundPush
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (runStatus: ISimpleGit -> JS.Promise<Result<StatusResult, 'Failure>>)
    (remoteName: string)
    (branchName: string option)
    (git: ISimpleGit)
    : JS.Promise<Result<OutboundPushPlan, 'Failure>> =
    promise {
        let! refSpec = resolvePushRefSpec runStatus branchName git
        let! dryRunResult =
            runSimpleGitRaw
                (fun currentGit -> currentGit.raw [| "push"; "--porcelain"; "--dry-run"; remoteName; refSpec |])
                git

        match dryRunResult with
        | Error failure ->
            return Error failure
        | Ok dryRunOutput when shouldSkipLfsUploadFromPushDryRun dryRunOutput ->
            return Ok OutboundPushPlan.SkipLfsUpload
        | Ok _ ->
            let! remoteRefsResult = getKnownRemoteRefs runSimpleGitRaw remoteName git

            match remoteRefsResult with
            | Error failure ->
                return Error failure
            | Ok remoteRefs ->
                let! outboundObjectIdsResult = getOutboundObjectIds runSimpleGitRaw refSpec remoteRefs git

                match outboundObjectIdsResult with
                | Error failure ->
                    return Error failure
                | Ok objectIds when objectIds.Length = 0 ->
                    return Ok OutboundPushPlan.SkipLfsUpload
                | Ok objectIds ->
                    let! hasOutboundLfsPointersResult =
                        containsOutboundLfsPointerObjects runSimpleGitRaw objectIds git

                    return
                        hasOutboundLfsPointersResult
                        |> Result.map (fun hasOutboundLfsPointers ->
                            if hasOutboundLfsPointers then
                                OutboundPushPlan.UploadLfsObjects refSpec
                            else
                                OutboundPushPlan.SkipLfsUpload
                        )
    }

let uploadObjects
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (remoteName: string)
    (refSpec: string)
    (git: ISimpleGit)
    : JS.Promise<Result<unit, 'Failure>> =
    promise {
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
