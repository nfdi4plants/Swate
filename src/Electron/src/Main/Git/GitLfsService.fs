module Main.Git.GitLfsService

open System
open System.Collections.Generic
open Fable.Core
open Thoth.Json.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Main.Bindings.Node
open Main.Bindings.SimpleGit
open Main.Git.GitLfsAdapter
open Main.Git.GitAuthAdapter

/// Default timeout for interactive Git LFS commands launched by the Electron main process.
[<Literal>]
let DefaultTimeoutMs = 30000

/// Describes whether an outbound Git push needs a separate LFS object upload before the git ref push.
[<RequireQualifiedAccess>]
type OutboundPushPlan =
    | SkipLfsUpload
    | UploadLfsObjects of lfsObjectIds: string[]

let private maxLfsPointerProbeBytes = 1024L
let mutable private cachedSystemInstalled = false

[<Literal>]
let private lfsLsFilesTimeoutMs = 15000

let private gitLfsLsFileInfoDecoder : Decoder<GitLfsLsFileInfo option> =
    Decode.object (fun get ->
        let name = get.Optional.Field "name" Decode.string
        let size = get.Optional.Field "size" Decode.float
        let checkout = get.Optional.Field "checkout" Decode.bool
        let downloaded = get.Optional.Field "downloaded" Decode.bool
        let oidType = get.Optional.Field "oid_type" Decode.string
        let oid = get.Optional.Field "oid" Decode.string
        let version = get.Optional.Field "version" Decode.string

        match name, size, checkout, downloaded, oidType, oid, version with
        | Some name, Some size, Some checkout, Some downloaded, Some oidType, Some oid, Some version ->
            Some {
                name = PathHelpers.normalizeSeparators name
                size = size
                checkout = checkout
                downloaded = downloaded
                ``oid_type`` = oidType
                oid = oid
                version = version
            }
        | _ ->
            None
    )

let private lsFilesResponseDecoder : Decoder<GitLfsLsFileInfo[]> =
    Decode.object (fun get ->
        get.Optional.Field "files" (Decode.array gitLfsLsFileInfoDecoder)
        |> Option.defaultValue [||]
        |> Array.choose id
    )

let private parseLsFiles (stdoutText: string) : GitLfsLsFileInfo[] =
    ARCtrl.Json.Decode.fromJsonString lsFilesResponseDecoder stdoutText

let private indexByRelativePath (files: GitLfsLsFileInfo[]) : Dictionary<string, GitLfsLsFileInfo> =
    let filesByRelativePath = Dictionary<string, GitLfsLsFileInfo>()

    files
    |> Array.iter (fun info ->
        if not (String.IsNullOrWhiteSpace info.name) then
            let relativePath = PathHelpers.normalizeSeparators info.name
            filesByRelativePath.[relativePath] <- { info with name = relativePath }
    )

    filesByRelativePath

/// Chooses the most useful text from a Git LFS adapter result for user-facing errors.
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

/// Builds a GitLfsRequest with a generated request id and default timeout.
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

/// Runs a Git LFS adapter request and normalizes unsuccessful adapter results to Result.Error.
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

/// Runs Git LFS without progress or cancellation hooks.
let runSilently (request: GitLfsRequest) : JS.Promise<Result<GitLfsResult, exn>> =
    run request ignore (fun () -> false)

/// Tracks a repository-relative path in Git LFS. GitService uses this during automatic large-file staging.
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

/// Runs `git lfs install` for a specific repository.
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

/// Runs system-level Git LFS install from the Main IPC endpoint and updates the installation cache on success.
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

/// Probes whether `git lfs` is available on PATH. The positive result is cached for the Main process lifetime.
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

/// Checks whether `.gitattributes` marks a path for Git LFS.
let isTrackedByAttributes (repoRoot: string) (relativePath: string) =
    gitLfs.IsTrackedByAttributes repoRoot relativePath

/// Tries to read `git lfs ls-files -j` metadata keyed by repository-relative path.
/// Fail-open behavior: command or parse failures yield an empty dictionary.
let tryGetLsFilesByRelativePath (repoRoot: string) : JS.Promise<Dictionary<string, GitLfsLsFileInfo>> =
    promise {
        let normalizedRepoRoot = PathHelpers.normalizePath repoRoot

        try
            let! commandResult =
                runGitCaptured {
                    WorkingDirectory = Some normalizedRepoRoot
                    Arguments = [| "lfs"; "ls-files"; "-j" |]
                    Environment = None
                    StandardInput = None
                    TimeoutMs = Some lfsLsFilesTimeoutMs
                }

            if commandResult.ExitCode <> 0 || commandResult.TimedOut then
                return Dictionary<string, GitLfsLsFileInfo>()
            else
                let stdoutText =
                    commandResult.StdoutText
                    |> Option.ofObj
                    |> Option.defaultValue String.Empty
                    |> _.Trim()

                if String.IsNullOrWhiteSpace stdoutText then
                    return Dictionary<string, GitLfsLsFileInfo>()
                else
                    return stdoutText |> parseLsFiles |> indexByRelativePath
        with _ ->
            return Dictionary<string, GitLfsLsFileInfo>()
    }

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

/// Collects redacted Git LFS diagnostics after an LFS upload failure so push errors are actionable.
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

/// Appends optional LFS diagnostic sections to the original push failure message.
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

let private parseLsRemoteTipIds (output: string) =
    output
    |> splitNonEmptyLines
    |> Array.choose (fun line ->
        let columns = line.Split('\t', 2, StringSplitOptions.None)
        if columns.Length = 2 then Some columns.[0] else None
    )
    |> Array.distinct

let private buildRevListStandardInput (refSpec: string) (remoteTipIds: string[]) =
    if remoteTipIds.Length = 0 then
        $"{refSpec}\n"
    else
        String.concat "\n" [| yield refSpec; yield "--not"; yield! remoteTipIds; yield "" |]

let private tryParsePointerOid (content: string) =
    let lines =
        content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
        |> Array.map _.Trim()
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace line))

    let tryGetOidLine () =
        lines
        |> Array.tryPick (fun line ->
            let prefix = "oid sha256:"

            if line.StartsWith(prefix, StringComparison.Ordinal) && line.Length = prefix.Length + 64 then
                let oid = line.Substring(prefix.Length)
                let isLowerHex = oid |> Seq.forall (fun c -> Char.IsDigit c || ('a' <= c && c <= 'f'))
                if isLowerHex then Some oid else None
            else
                None
        )

    let hasSizeLine =
        lines
        |> Array.exists (fun line ->
            let prefix = "size "
            line.StartsWith(prefix, StringComparison.Ordinal)
            && line.Length > prefix.Length
            && (line.Substring(prefix.Length) |> Seq.forall Char.IsDigit)
        )

    if
        lines.Length >= 3
        && lines.[0].Equals("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal)
        && hasSizeLine
    then
        tryGetOidLine ()
    else
        None

let private tryReadBatchPointerOids (stdoutBuffer: obj) =
    let totalLength = bufferLength stdoutBuffer

    let findNewlineIndex startIndex =
        let mutable currentIndex = startIndex
        let mutable foundIndex = -1

        while foundIndex < 0 && currentIndex < totalLength do
            if bufferByteAt stdoutBuffer currentIndex = 10 then
                foundIndex <- currentIndex
            else
                currentIndex <- currentIndex + 1

        foundIndex

    let pointerOids = ResizeArray<string>()
    let mutable index = 0
    let mutable malformed = false

    while not malformed && index < totalLength do
        let headerEnd = findNewlineIndex index

        if headerEnd < 0 then
            malformed <- true
        else
            let header = bufferSubarray stdoutBuffer index headerEnd |> bufferToUtf8String
            let parts = header.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries)

            if parts.Length <> 3 || not (parts.[1].Equals("blob", StringComparison.Ordinal)) then
                malformed <- true
            else
                let success, contentByteLength = Int32.TryParse(parts.[2])
                let contentStart = headerEnd + 1
                let contentEnd = contentStart + contentByteLength

                if not success || contentByteLength < 0 || contentEnd > totalLength then
                    malformed <- true
                else
                    let content =
                        bufferSubarray stdoutBuffer contentStart contentEnd
                        |> bufferToUtf8String

                    match tryParsePointerOid content with
                    | Some pointerOid -> pointerOids.Add pointerOid
                    | None -> ()

                    index <- contentEnd

                    if index < totalLength && bufferByteAt stdoutBuffer index = 10 then
                        index <- index + 1

    if malformed then None else Some(pointerOids.ToArray() |> Array.distinct)

let private extractSpawnFailureMessage (result: GitSpawnResult) =
    let stderrText = result.StderrText |> Option.ofObj |> Option.defaultValue String.Empty |> _.Trim()
    let stdoutText = result.StdoutText |> Option.ofObj |> Option.defaultValue String.Empty |> _.Trim()

    if result.TimedOut then
        if not (String.IsNullOrWhiteSpace stderrText) then
            stderrText
        else
            "Git command timed out."
    elif not (String.IsNullOrWhiteSpace stderrText) then
        stderrText
    elif not (String.IsNullOrWhiteSpace stdoutText) then
        stdoutText
    else
        "Git command failed."

let private spawnFailure (result: GitSpawnResult) = exn (extractSpawnFailureMessage result)

let private isUnsupportedOptionFailure (result: GitSpawnResult) =
    let diagnosticText = $"{result.StdoutText}\n{result.StderrText}".ToLowerInvariant()

    result.ExitCode <> 0
    && (
        diagnosticText.Contains("unknown option")
        || diagnosticText.Contains("unknown flag")
        || diagnosticText.Contains("unrecognized option")
        || diagnosticText.Contains("invalid option")
    )

let private filterResolvableRemoteTipIds
    (runSpawnedGit: GitSpawnRequest -> JS.Promise<GitSpawnResult>)
    (repoPath: string)
    (remoteTipIds: string[])
    : JS.Promise<Result<string[], exn>> =
    promise {
        let distinctRemoteTipIds = remoteTipIds |> Array.distinct

        if distinctRemoteTipIds.Length = 0 then
            return Ok [||]
        else
            let! batchCheckResult =
                runSpawnedGit {
                    WorkingDirectory = Some repoPath
                    Arguments = [| "cat-file"; "--batch-check" |]
                    Environment = None
                    StandardInput = Some(String.concat "\n" [| yield! distinctRemoteTipIds; yield "" |])
                    TimeoutMs = Some DefaultTimeoutMs
                }

            if batchCheckResult.ExitCode <> 0 then
                return Error(spawnFailure batchCheckResult)
            else
                let lines = batchCheckResult.StdoutText |> splitNonEmptyLines
                let statusByObjectId = System.Collections.Generic.Dictionary<string, bool>()
                let mutable malformed = false

                for line in lines do
                    if not malformed then
                        let parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries)

                        if parts.Length < 2 then
                            malformed <- true
                        else
                            let isMissing = parts.[1].Equals("missing", StringComparison.Ordinal)
                            statusByObjectId.[parts.[0]] <- not isMissing

                if malformed || (distinctRemoteTipIds |> Array.exists (fun objectId -> not (statusByObjectId.ContainsKey objectId))) then
                    return Error(exn "Failed to parse git cat-file --batch-check output while filtering remote tips.")
                else
                    return
                        distinctRemoteTipIds
                        |> Array.filter (fun objectId -> statusByObjectId.[objectId])
                        |> Ok
    }

let private getLfsObjectIdsFromObjectIds
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (objectIds: string[])
    (git: ISimpleGit)
    : JS.Promise<Result<string[], 'Failure>> =
    promise {
        let lfsObjectIds = ResizeArray<string>()
        let mutable failure: 'Failure option = None

        for objectId in objectIds do
            if failure.IsNone then
                let! objectTypeResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-t"; objectId |]) git

                match objectTypeResult with
                | Error currentFailure ->
                    failure <- Some currentFailure
                | Ok objectType when objectType.Trim().Equals("blob", StringComparison.Ordinal) ->
                    // These object IDs come from outbound history, not working tree paths, so query the git object store directly.
                    let! objectSizeResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-s"; objectId |]) git

                    match objectSizeResult with
                    | Error currentFailure ->
                        failure <- Some currentFailure
                    | Ok objectSizeText ->
                        let success, objectSize = Int64.TryParse(objectSizeText.Trim())

                        if success && objectSize <= maxLfsPointerProbeBytes then
                            let! contentResult = runSimpleGitRaw (fun currentGit -> currentGit.raw [| "cat-file"; "-p"; objectId |]) git

                            match contentResult with
                            | Error currentFailure ->
                                failure <- Some currentFailure
                            | Ok content ->
                                match tryParsePointerOid content with
                                | Some lfsObjectId -> lfsObjectIds.Add lfsObjectId
                                | None -> ()
                | Ok _ ->
                    ()

        match failure with
        | Some currentFailure -> return Error currentFailure
        | None -> return Ok(lfsObjectIds.ToArray() |> Array.distinct)
    }

let private tryGetOptimizedLfsObjectIds
    (runSpawnedGit: GitSpawnRequest -> JS.Promise<GitSpawnResult>)
    (repoPath: string)
    (refSpec: string)
    (remoteTipIds: string[])
    : JS.Promise<Result<string[] option, exn>> =
    promise {
        let! revListResult =
            runSpawnedGit {
                WorkingDirectory = Some repoPath
                Arguments =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        $"--filter=blob:limit={maxLfsPointerProbeBytes}"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]
                Environment = None
                StandardInput = Some(buildRevListStandardInput refSpec remoteTipIds)
                TimeoutMs = Some DefaultTimeoutMs
            }

        if revListResult.ExitCode <> 0 then
            return
                if isUnsupportedOptionFailure revListResult then
                    Ok None
                else
                    Error(spawnFailure revListResult)
        else
            let candidateBlobIds = revListResult.StdoutText |> splitNonEmptyLines |> Array.distinct

            if candidateBlobIds.Length = 0 then
                return Ok(Some [||])
            else
                let! catFileResult =
                    runSpawnedGit {
                        WorkingDirectory = Some repoPath
                        Arguments = [| "cat-file"; "--batch"; "--buffer" |]
                        Environment = None
                        StandardInput = Some(String.concat "\n" [| yield! candidateBlobIds; yield "" |])
                        TimeoutMs = Some DefaultTimeoutMs
                    }

                if catFileResult.ExitCode <> 0 then
                    return
                        if isUnsupportedOptionFailure catFileResult then
                            Ok None
                        else
                            Error(spawnFailure catFileResult)
                else
                    return
                        match tryReadBatchPointerOids catFileResult.StdoutBuffer with
                        | Some pointerOids -> Ok(Some pointerOids)
                        | None -> Error(exn "Failed to parse git cat-file --batch output while planning Git LFS upload.")
    }

let private getOutboundObjectIdsFromRemoteTips
    (runSpawnedGit: GitSpawnRequest -> JS.Promise<GitSpawnResult>)
    (repoPath: string)
    (refSpec: string)
    (remoteTipIds: string[])
    : JS.Promise<Result<string[] option, exn>> =
    promise {
        let! revListResult =
            runSpawnedGit {
                WorkingDirectory = Some repoPath
                Arguments = [| "rev-list"; "--objects"; "--no-object-names"; "--stdin"; "--ignore-missing" |]
                Environment = None
                StandardInput = Some(buildRevListStandardInput refSpec remoteTipIds)
                TimeoutMs = Some DefaultTimeoutMs
            }

        if revListResult.ExitCode = 0 then
            return Ok(Some(revListResult.StdoutText |> splitNonEmptyLines |> Array.distinct))
        else
            return
                if isUnsupportedOptionFailure revListResult then
                    Ok None
                else
                    Error(spawnFailure revListResult)
    }

/// Determines whether an upcoming git push references LFS pointer objects that must be uploaded explicitly.
/// Tests call this directly because the planning logic has several fallbacks for older git versions.
let planOutboundPush
    (runSimpleGitRaw: (ISimpleGit -> JS.Promise<string>) -> ISimpleGit -> JS.Promise<Result<string, 'Failure>>)
    (runSpawnedGit: GitSpawnRequest -> JS.Promise<GitSpawnResult>)
    (mapSpawnFailure: exn -> 'Failure)
    (runStatus: ISimpleGit -> JS.Promise<Result<StatusResult, 'Failure>>)
    (repoPath: string)
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
            let! remoteTipIdsResult =
                runSimpleGitRaw (fun currentGit -> currentGit.raw [| "ls-remote"; "--refs"; remoteName |]) git

            match remoteTipIdsResult with
            | Error failure ->
                return Error failure
            | Ok remoteTipOutput ->
                let remoteTipIds = parseLsRemoteTipIds remoteTipOutput
                let! resolvableRemoteTipIdsResult =
                    filterResolvableRemoteTipIds runSpawnedGit repoPath remoteTipIds

                match resolvableRemoteTipIdsResult with
                | Error failure ->
                    return Error(mapSpawnFailure failure)
                | Ok remoteTipIds ->
                    let! optimizedLfsObjectIdsResult =
                        tryGetOptimizedLfsObjectIds runSpawnedGit repoPath refSpec remoteTipIds

                    match optimizedLfsObjectIdsResult with
                    | Error failure ->
                        return Error(mapSpawnFailure failure)
                    | Ok(Some [||]) ->
                        return Ok OutboundPushPlan.SkipLfsUpload
                    | Ok(Some lfsObjectIds) ->
                        return Ok(OutboundPushPlan.UploadLfsObjects lfsObjectIds)
                    | Ok None ->
                        let! remoteTruthObjectIdsResult =
                            getOutboundObjectIdsFromRemoteTips runSpawnedGit repoPath refSpec remoteTipIds

                        match remoteTruthObjectIdsResult with
                        | Error failure ->
                            return Error(mapSpawnFailure failure)
                        | Ok(Some [||]) ->
                            return Ok OutboundPushPlan.SkipLfsUpload
                        | Ok(Some objectIds) ->
                            let! lfsObjectIdsResult = getLfsObjectIdsFromObjectIds runSimpleGitRaw objectIds git

                            return
                                lfsObjectIdsResult
                                |> Result.map (fun lfsObjectIds ->
                                    if lfsObjectIds.Length = 0 then
                                        OutboundPushPlan.SkipLfsUpload
                                    else
                                        OutboundPushPlan.UploadLfsObjects lfsObjectIds
                                )
                        | Ok None ->
                            let! remoteRefsResult = getKnownRemoteRefs runSimpleGitRaw remoteName git

                            match remoteRefsResult with
                            | Error failure ->
                                return Error failure
                            | Ok remoteRefs ->
                                let! outboundObjectIdsResult = getOutboundObjectIds runSimpleGitRaw refSpec remoteRefs git

                                match outboundObjectIdsResult with
                                | Error failure ->
                                    return Error failure
                                | Ok [||] ->
                                    return Ok OutboundPushPlan.SkipLfsUpload
                                | Ok objectIds ->
                                    let! lfsObjectIdsResult = getLfsObjectIdsFromObjectIds runSimpleGitRaw objectIds git

                                    return
                                        lfsObjectIdsResult
                                        |> Result.map (fun lfsObjectIds ->
                                            if lfsObjectIds.Length = 0 then
                                                OutboundPushPlan.SkipLfsUpload
                                            else
                                                OutboundPushPlan.UploadLfsObjects lfsObjectIds
                                        )
    }

/// Uploads the exact LFS object ids needed for a push, falling back to refspec upload when the local git-lfs is older.
/// Authentication is supplied by GitAuthAdapter and is passed only for this spawned command.
let uploadObjects
    (runSpawnedGit: GitSpawnRequest -> JS.Promise<GitSpawnResult>)
    (commandAuth: GitCommandAuthentication)
    (repoPath: string)
    (remoteName: string)
    (refSpec: string)
    (lfsObjectIds: string[])
    : JS.Promise<Result<unit, exn>> =
    promise {
        if lfsObjectIds.Length = 0 then
            return Ok()
        else
            let! exactUploadResult =
                runSpawnedGit {
                    WorkingDirectory = Some repoPath
                    Arguments =
                        [|
                            yield! commandAuth.ConfigArgs
                            "lfs"
                            "push"
                            "--object-id"
                            remoteName
                            "--stdin"
                        |]
                    Environment = Some commandAuth.Environment
                    StandardInput = Some(String.concat "\n" [| yield! lfsObjectIds; yield "" |])
                    TimeoutMs = Some DefaultTimeoutMs
                }

            if exactUploadResult.ExitCode = 0 then
                return Ok()
            elif isUnsupportedOptionFailure exactUploadResult then
                let! fallbackResult =
                    runSpawnedGit {
                        WorkingDirectory = Some repoPath
                        Arguments =
                            [|
                                yield! commandAuth.ConfigArgs
                                "lfs"
                                "push"
                                remoteName
                                refSpec
                            |]
                        Environment = Some commandAuth.Environment
                        StandardInput = None
                        TimeoutMs = Some DefaultTimeoutMs
                    }

                return
                    if fallbackResult.ExitCode = 0 then
                        Ok()
                    else
                        Error(exn (extractSpawnFailureMessage fallbackResult))
            else
                return Error(exn (extractSpawnFailureMessage exactUploadResult))
    }
