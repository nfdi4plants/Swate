module Main.Git.GitService

open System
open System.IO
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Swate.Electron.Shared.GitTypes
open Main.Bindings.Filesystem
open Main.Bindings.SimpleGit
open Main.Git.GitLfsService
open Main.Git.GitTokenProvider
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals

type GitFailureKind =
    | Unauthorized
    | Forbidden
    | Network
    | Timeout
    | Canceled
    | LfsInstallRequired
    | Unknown

type GitFailure = {
    Kind: GitFailureKind
    Message: string
}

type GitResult<'T> = Result<'T, GitFailure>

[<RequireQualifiedAccess>]
type GitBranchRefKind =
    | Local
    | Remote

type GitFileStatusDto = {
    Path: string
    Index: string
    WorkingDir: string
    OriginalPath: string option
}

type GitBranchRefDto = {
    RefName: string
    DisplayLabel: string
    Kind: GitBranchRefKind
    IsCurrent: bool
    IsTracking: bool
}

type GitDiffViewDataDto = {
    Path: string
    PreviousContent: string
    CurrentContent: string
    WordDiffText: string
}

type GitMergeConflictViewDataDto = {
    Path: string
    MergeConflictContent: string
}

type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
    Conflicted: string[]
    IsMergeInProgress: bool
    Files: GitFileStatusDto[]
}

type GitDiffSummaryDto = {
    Changed: int
    Insertions: int
    Deletions: int
}

type GitLfsSettingsDto = { AutoTrackThresholdMb: int }

type GitConfirmMergeResolutionResult = {
    UpdatedStatus: GitStatusDto
    RemainingConflictedPaths: string[]
    NextConflictedPath: string option
}

type GitProgressCallback = GitInternals.GitProgressCallback

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

let private disallowedRemotePrefixes = [| "file://"; "ext::"; "fd::" |]

let private protocolOverridePattern =
    Regex("protocol\\.[^\\s=]+\\.(allow|deny)|(^|\\s)-c\\s+protocol\\.", RegexOptions.IgnoreCase)

let private remoteNamePattern = Regex("^[A-Za-z0-9._/-]+$")
let private invalidBranchCharactersPattern = Regex(@"[~^:?*\[\\\s]")
let private gitLfsThresholdConfigKey = "swate.lfs.autotrackthresholdmb"
let private gitLfsDefaultThresholdMb = 1
let private gitLfsMaximumThresholdMb = 100

let private lfsInstallRequiredTokens =
    [|
        "git lfs is required for files larger than"
        "git lfs is required for this operation"
        "git: 'lfs' is not a git command"
        "git-lfs filter-process"
        "this repository is configured for git lfs but 'git-lfs' was not found"
        "external filter 'git-lfs filter-process' failed"
        "smudge filter lfs failed"
        "clean filter 'lfs' failed"
    |]

let classifyFailureKind (message: string) =
    let normalizedMessage =
        message
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> fun text -> text.ToLowerInvariant()

    let containsAny (terms: string[]) =
        terms
        |> Array.exists normalizedMessage.Contains

    if containsAny lfsInstallRequiredTokens then
        LfsInstallRequired
    elif containsAny [| "abort"; "cancelled"; "canceled"; "aborterror" |] then
        Canceled
    elif containsAny [| "timed out"; "timeout"; "time out" |] then
        Timeout
    elif containsAny [| "forbidden"; "403" |] then
        Forbidden
    elif
        containsAny [|
            "unauthorized"
            "authentication failed"
            "401"
            "could not read username"
            "no access token available"
            "permission denied"
        |]
    then
        Unauthorized
    elif
        containsAny [|
            "network"
            "could not resolve host"
            "failed to connect"
            "connection reset"
            "connection refused"
            "unable to access"
        |]
    then
        Network
    else
        Unknown

// GitService owns threshold formatting because the threshold is part of git workflow policy, not raw LFS command execution.
let private formatThresholdMb (thresholdMb: int) = $"{thresholdMb} MB"

// GitService builds the install prompt because this message is returned as part of git operation failures shown to the user.
let private buildLfsInstallPromptMessage (thresholdMb: int option) (details: string option) =
    let prompt =
        match thresholdMb with
        | Some value -> $"Git LFS is required for files larger than {formatThresholdMb value}. Install Git LFS now?"
        | None -> "Git LFS is required for this operation. Install Git LFS now?"

    match details |> Option.map _.Trim() with
    | Some value when not (String.IsNullOrWhiteSpace value) -> $"{prompt}\n\n{redactToken value}"
    | _ -> prompt

// GitService shapes the final failure because LFS install-required errors need workflow-specific wording.
let private createFailure kind (message: string) : GitFailure =
    match kind with
    | LfsInstallRequired ->
        let finalMessage =
            if message.IndexOf("Install Git LFS now?", StringComparison.OrdinalIgnoreCase) >= 0 then
                message
            else
                buildLfsInstallPromptMessage None (Some message)

        {
            Kind = kind
            Message = finalMessage
        }
    | _ ->
        { Kind = kind; Message = message }

let private toFailure (error: exn) : GitFailure =
    GitInternals.toFailure classifyFailureKind createFailure error

let private errorResult (error: exn) : GitResult<'T> =
    GitInternals.errorResult classifyFailureKind createFailure error

let private tryGetNodeErrorCode (error: exn) : string option =
    try
        error?code |> unbox<string> |> Option.ofObj
    with _ ->
        None

let private valueOrEmptyArray (items: 'T[]) = if isNull items then [||] else items

let private normalizeStatusCode (code: string) =
    let trimmed =
        code
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> _.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        "."
    else
        trimmed

let private isStagedIndexStatus (code: string) =
    let normalized = normalizeStatusCode code
    normalized <> "." && normalized <> "?"

// GitService validates the threshold because it owns the policy that decides when normal git actions must switch into LFS handling.
let private validateLfsThresholdMb (thresholdMb: int) =
    if thresholdMb < 1 then
        Error(exn "Git LFS auto-track threshold must be at least 1 MB.")
    elif thresholdMb > gitLfsMaximumThresholdMb then
        Error(exn $"Git LFS auto-track threshold must not exceed {gitLfsMaximumThresholdMb} MB.")
    else
        Ok thresholdMb

// GitService parses the stored threshold because the setting is interpreted by git workflow code here.
let private tryParseConfiguredThresholdMb (value: string option) =
    match value |> Option.bind Option.ofObj with
    | None -> None
    | Some text ->
        let success, parsed = Int32.TryParse(text.Trim())

        if success && parsed >= 1 && parsed <= gitLfsMaximumThresholdMb then Some parsed else None

// GitService converts the threshold to bytes because file-size decisions happen during stage/commit orchestration here.
let private thresholdMbToBytes (thresholdMb: int) =
    int64 thresholdMb * 1024L * 1024L

// GitService formats the persisted threshold because it owns the config contract for this workflow setting.
let private formatThresholdConfigValue (thresholdMb: int) = string thresholdMb

let private tryGetFileSizeInBytes (absolutePath: string) : JS.Promise<int64 option> =
    promise {
        try
            let! stats = fsPromisesDynamic?stat (absolutePath) |> unbox<JS.Promise<obj>>
            return Some(stats?size |> unbox<float> |> int64)
        with error ->
            match tryGetNodeErrorCode error with
            | Some "ENOENT" -> return None
            | _ -> return raise error
    }

let ensureValidBranchLikeName (label: string) (value: string) =
    let trimmed = value.Trim()
    let containsControlCharacter = trimmed |> Seq.exists Char.IsControl

    if String.IsNullOrWhiteSpace trimmed then
        Error(exn $"{label} must not be empty.")
    elif containsControlCharacter then
        Error(exn $"{label} contains control characters.")
    elif trimmed.StartsWith("-") then
        Error(exn $"{label} must not start with '-'.")
    elif trimmed.StartsWith("/") then
        Error(exn $"{label} must not start with '/'.")
    elif
        trimmed.Split('/')
        |> Array.exists (fun segment -> segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
    then
        Error(exn $"{label} must not contain '.lock' path segments.")
    elif
        trimmed.Contains("..")
        || trimmed.EndsWith(".")
        || trimmed.Contains("@{")
        || trimmed.EndsWith("/")
    then
        Error(exn $"{label} contains invalid git ref segments.")
    elif invalidBranchCharactersPattern.IsMatch(trimmed) then
        Error(exn $"{label} contains invalid characters.")
    else
        Ok trimmed

let ensureValidPathspec (pathSpec: string) =
    let normalized = pathSpec.Replace("\\", "/").Trim()

    if String.IsNullOrWhiteSpace normalized then
        Error(exn "Pathspec must not be empty.")
    elif normalized.StartsWith("/") then
        Error(exn "Absolute pathspecs are not allowed.")
    elif Regex.IsMatch(normalized, "^[A-Za-z]:/") then
        Error(exn "Absolute pathspecs are not allowed.")
    elif
        normalized.Split('/')
        |> Array.exists (fun segment -> segment = "." || segment = "..")
    then
        Error(exn "Pathspec must not contain traversal segments.")
    elif normalized.Contains("\000") then
        Error(exn "Pathspec contains invalid null characters.")
    else
        Ok normalized

let validatePathspecs (pathSpecs: string[]) =
    if isNull pathSpecs || pathSpecs.Length = 0 then
        Error(exn "At least one pathspec is required.")
    else
        pathSpecs
        |> Array.map ensureValidPathspec
        |> Array.fold
            (fun state next ->
                match state, next with
                | Error e, _ -> Error e
                | _, Error e -> Error e
                | Ok acc, Ok value -> Ok(Array.append acc [| value |])
            )
            (Ok [||])

let validateRemoteName (remoteName: string) =
    let normalized =
        remoteName
        |> Option.ofObj
        |> Option.map _.Trim()
        |> Option.filter (fun x -> not (String.IsNullOrWhiteSpace x))
        |> Option.defaultValue "origin"

    if remoteNamePattern.IsMatch normalized then
        Ok normalized
    else
        Error(exn "Remote name contains unsupported characters.")

let ensureAllowedRemoteUrl (remoteUrl: string) =
    let normalized = remoteUrl.Trim()

    if String.IsNullOrWhiteSpace normalized then
        Error(exn "Remote URL is empty.")
    elif protocolOverridePattern.IsMatch normalized then
        Error(exn "Remote URL contains a protocol override attempt.")
    elif
        disallowedRemotePrefixes
        |> Array.exists (fun prefix -> normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    then
        Error(exn "Remote URL uses a blocked protocol.")
    elif normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
        Ok normalized
    elif normalized.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase) then
        Ok normalized
    else
        Error(exn "Only https:// and ssh:// remotes are allowed.")

let private validateOptionalBranchName (branchName: string option) =
    match branchName with
    | None -> Ok None
    | Some branchName -> ensureValidBranchLikeName "Branch name" branchName |> Result.map Some

let private unsupportedGitContentMessage (path: string) = $"Unsupported git content for '{path}'."

let private unsupportedGitContentResult<'T> (path: string) : GitResult<'T> =
    Error {
        Kind = Unknown
        Message = unsupportedGitContentMessage path
    }

[<Emit("$0.length")>]
let private bufferLength (buffer: obj) : int = jsNative

[<Emit("$0[$1]")>]
let private bufferByteAt (buffer: obj) (index: int) : int = jsNative

[<Emit("$0.toString('utf8')")>]
let private bufferToUtf8String (buffer: obj) : string = jsNative

let private readFileBufferAsync (absolutePath: string) : JS.Promise<obj> =
    fsPromisesDynamic?readFile (absolutePath)
    |> unbox<JS.Promise<obj>>

let private explicitlyUnsupportedExtensions =
    set [
        ".xlsx"
        ".xls"
        ".xlsm"
        ".xlsb"
        ".ods"
        ".zip"
        ".gz"
        ".png"
        ".jpg"
        ".jpeg"
        ".gif"
        ".bmp"
        ".ico"
        ".pdf"
        ".dll"
        ".exe"
        ".jar"
    ]

let private isExplicitlyUnsupportedPath (path: string) =
    let extension = pathDynamic?extname (path) |> unbox<string>

    not (String.IsNullOrWhiteSpace extension)
    && explicitlyUnsupportedExtensions.Contains(extension.ToLowerInvariant())

let private isLikelyBinaryBuffer (buffer: obj) =
    let sampleLength = min (bufferLength buffer) 8192

    if sampleLength = 0 then
        false
    else
        let mutable nullByteDetected = false
        let mutable controlCount = 0

        for index in 0 .. sampleLength - 1 do
            let byteValue = bufferByteAt buffer index

            if byteValue = 0 then
                nullByteDetected <- true
            elif (byteValue < 7) || (byteValue > 13 && byteValue < 32) then
                controlCount <- controlCount + 1

        nullByteDetected || (float controlCount / float sampleLength) > 0.3

let private tryResolveArcRelativePath (arcPath: string) (requestedPath: string) =
    match ensureValidPathspec requestedPath with
    | Error validationError -> Error validationError
    | Ok safeRelativePath ->
        let arcRoot = pathDynamic?resolve (arcPath) |> unbox<string>
        let absolutePath = pathDynamic?resolve (arcRoot, safeRelativePath) |> unbox<string>

        let relativePath =
            pathDynamic?relative (arcRoot, absolutePath)
            |> unbox<string>
            |> fun value -> value.Replace("\\", "/")

        if
            String.IsNullOrWhiteSpace relativePath
            || relativePath = "."
            || relativePath = ".."
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || (pathDynamic?isAbsolute (relativePath) |> unbox<bool>)
        then
            Error(exn "Pathspec resolves outside the active ARC root.")
        else
            Ok(safeRelativePath, absolutePath)

let private isMergeInProgress (arcPath: string) =
    let mergeHeadPath = pathDynamic?resolve (arcPath, ".git", "MERGE_HEAD") |> unbox<string>
    existsSync mergeHeadPath

let private toStatusDto (arcPath: string) (status: StatusResult) : GitStatusDto =
    let conflictedPaths = valueOrEmptyArray status.conflicted

    let fileStatuses =
        valueOrEmptyArray status.files
        |> Array.map (fun fileStatus -> {
            Path = fileStatus.path
            Index = fileStatus.index
            WorkingDir = fileStatus.working_dir
            OriginalPath = fileStatus.``from``
        })

    {
        Current = status.current
        Tracking = status.tracking
        Ahead = status.ahead
        Behind = status.behind
        IsClean = status.isClean ()
        Conflicted = conflictedPaths
        IsMergeInProgress = conflictedPaths.Length > 0 || isMergeInProgress arcPath
        Files = fileStatuses
    }

let private ensureCurrentlyConflictedPath (status: GitStatusDto) (requestedPath: string) =
    if
        status.Conflicted
        |> Array.exists (fun conflictedPath -> String.Equals(conflictedPath, requestedPath, StringComparison.Ordinal))
    then
        Ok()
    else
        Error(exn $"File '{requestedPath}' is not currently marked as conflicted.")

let private missingHeadContentMarkers =
    [|
        "does not exist in 'head'"
        "exists on disk, but not in 'head'"
        "bad revision 'head'"
        "invalid object name 'head'"
    |]

let private isMissingHeadContentFailure (failure: GitFailure) =
    let message = failure.Message.ToLowerInvariant()
    missingHeadContentMarkers |> Array.exists message.Contains

let private readWorkingTreeTextIfPresent (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<string option>> =
    promise {
        match tryResolveArcRelativePath arcPath requestedPath with
        | Error validationError -> return errorResult validationError
        | Ok(safePath, absolutePath) ->
            if isExplicitlyUnsupportedPath safePath then
                return unsupportedGitContentResult safePath
            elif not (existsSync absolutePath) then
                return Ok None
            else
                let! buffer = readFileBufferAsync absolutePath

                if isLikelyBinaryBuffer buffer then
                    return unsupportedGitContentResult safePath
                else
                    return Ok(Some(bufferToUtf8String buffer))
    }

let private readHeadTextIfAvailable (git: ISimpleGit) (requestedPath: string) : JS.Promise<GitResult<string option>> =
    promise {
        match ensureValidPathspec requestedPath with
        | Error validationError -> return errorResult validationError
        | Ok safePath ->
            if isExplicitlyUnsupportedPath safePath then
                return unsupportedGitContentResult safePath
            else
                let! result =
                    GitInternals.runSimpleGit
                        toFailure
                        (fun currentGit -> currentGit.raw [| "show"; $"HEAD:{safePath}" |])
                        git

                match result with
                | Ok content -> return Ok(Some content)
                | Error failure when isMissingHeadContentFailure failure -> return Ok None
                | Error failure -> return Error failure
    }

let private quoteDiffPathToken (pathPrefix: string) (path: string option) =
    match path with
    | None -> "/dev/null"
    | Some value ->
        let escapedPath =
            value.Replace("\\", "\\\\").Replace("\"", "\\\"")

        $"\"{pathPrefix}{escapedPath}\""

let private buildSyntheticWordDiffText (previousPath: string option) (currentPath: string option) =
    let previousToken = quoteDiffPathToken "a/" previousPath
    let currentToken = quoteDiffPathToken "b/" currentPath

    [
        yield $"diff --git {previousToken} {currentToken}"

        match previousPath, currentPath with
        | None, Some _ ->
            yield "new file mode 100644"
        | Some _, None ->
            yield "deleted file mode 100644"
        | _ ->
            ()

        match previousPath, currentPath with
        | Some oldPath, Some currentPath when not (String.Equals(oldPath, currentPath, StringComparison.Ordinal)) ->
            yield $"rename from {oldPath}"
            yield $"rename to {currentPath}"
        | _ ->
            ()

        yield $"--- {previousToken}"
        yield $"+++ {currentToken}"
    ]
    |> String.concat "\n"

let private ensureDefaultTrackingBranchForPull
    (remoteName: string)
    (git: ISimpleGit)
    : JS.Promise<unit> =
    promise {
        let! status = git.status ()

        let hasTrackingBranch =
            status.tracking
            |> Option.bind Option.ofObj
            |> Option.exists (fun tracking -> not (String.IsNullOrWhiteSpace tracking))

        let currentBranch =
            status.current
            |> Option.bind Option.ofObj
            |> Option.map _.Trim()
            |> Option.filter (fun branch -> not (String.IsNullOrWhiteSpace branch))

        match hasTrackingBranch, status.detached, currentBranch with
        | true, _, _
        | _, true, _
        | _, _, None ->
            return ()
        | false, false, Some branchName ->
            let upstreamRef = $"{remoteName}/{branchName}"
            let! _ = git.raw [| "branch"; $"--set-upstream-to={upstreamRef}"; branchName |]
            return ()
    }

let private runSimpleGit (operation: ISimpleGit -> JS.Promise<'T>) (git: ISimpleGit) : JS.Promise<GitResult<'T>> =
    GitInternals.runSimpleGit toFailure operation git

// GitService reads the threshold because stage/commit need the value while deciding whether to enforce LFS automatically.
let private getConfiguredLfsThresholdMb (git: ISimpleGit) : JS.Promise<int> =
    promise {
        try
            let! configResult = git.getConfig (gitLfsThresholdConfigKey, "local")

            return
                configResult.value
                |> tryParseConfiguredThresholdMb
                |> Option.defaultValue gitLfsDefaultThresholdMb
        with _ ->
            return gitLfsDefaultThresholdMb
    }

// GitService creates this failure because install-required is surfaced as a git workflow error, not as a raw LFS command result.
let private createLfsInstallRequiredFailure (thresholdMb: int option) (details: string option) : GitFailure = {
    Kind = LfsInstallRequired
    Message = buildLfsInstallPromptMessage thresholdMb details
}

// GitService decides when to attach LFS diagnostics because that choice depends on the surrounding push workflow failure.
let private shouldCollectLfsDiagnostics (failure: GitFailure) =
    let message = failure.Message.ToLowerInvariant()
    message.Contains("gitlab-lfs")
    || message.Contains("git lfs")
    || message.Contains("lfs:")
    || message.Contains("client error")
    || failure.Kind = LfsInstallRequired

// GitService keeps this wrapper because automatic tracking must map LFS command failures into GitFailure values used by the workflow layer.
let private runGitLfsTrackCommand
    (arcPath: string)
    (relativePath: string)
    (thresholdMb: int)
    : JS.Promise<GitResult<unit>> =
    promise {
        let! result = track arcPath relativePath

        return
            match result with
            | Ok () -> Ok()
            | Error message when classifyFailureKind message = LfsInstallRequired ->
                Error(createLfsInstallRequiredFailure (Some thresholdMb) (Some message))
            | Error message ->
                Error(createFailure Unknown message)
    }

// GitService computes oversized staged paths because only the git workflow layer knows which index entries are about to be committed.
let private getOversizedStagedPaths
    (arcPath: string)
    (git: ISimpleGit)
    (thresholdBytes: int64)
    : JS.Promise<GitResult<string[]>> =
    promise {
        let! statusResult = runSimpleGit (fun currentGit -> currentGit.status ()) git

        match statusResult with
        | Error failure -> return Error failure
        | Ok status ->
            let stagedPaths =
                valueOrEmptyArray status.files
                |> Array.filter (fun fileStatus -> isStagedIndexStatus fileStatus.index)
                |> Array.map _.path
                |> Array.distinct

            let oversizedPaths = ResizeArray<string>()
            let mutable failure: GitFailure option = None

            for stagedPath in stagedPaths do
                match failure with
                | Some _ -> ()
                | None ->
                    match tryResolveArcRelativePath arcPath stagedPath with
                    | Error validationError ->
                        failure <- Some(toFailure validationError)
                    | Ok(_, absolutePath) ->
                        let! fileSizeOption = tryGetFileSizeInBytes absolutePath

                        if fileSizeOption |> Option.exists (fun fileSize -> fileSize > thresholdBytes) then
                            oversizedPaths.Add stagedPath

            match failure with
            | Some failure -> return Error failure
            | None -> return Ok(oversizedPaths.ToArray())
    }

// GitService enforces automatic LFS because this is workflow orchestration across status, file-size checks, tracking, and re-staging.
let private ensureLargeStagedFilesUseLfs (arcPath: string) (git: ISimpleGit) : JS.Promise<GitResult<unit>> =
    promise {
        let! thresholdMb = getConfiguredLfsThresholdMb git
        let thresholdBytes = thresholdMbToBytes thresholdMb
        let! oversizedPathsResult = getOversizedStagedPaths arcPath git thresholdBytes

        match oversizedPathsResult with
        | Error failure -> return Error failure
        | Ok oversizedPaths when oversizedPaths.Length = 0 -> return Ok()
        | Ok oversizedPaths ->
            let pathsToTrack =
                oversizedPaths
                |> Array.filter (fun relativePath -> not (isTrackedByAttributes arcPath relativePath))

            let mutable failure: GitFailure option = None

            for pathToTrack in pathsToTrack do
                match failure with
                | Some _ -> ()
                | None ->
                    let! trackResult = runGitLfsTrackCommand arcPath pathToTrack thresholdMb

                    match trackResult with
                    | Ok () -> ()
                    | Error trackFailure -> failure <- Some trackFailure

            match failure with
            | Some failure ->
                return Error failure
            | None ->
                let pathsToRestage =
                    [|
                        if pathsToTrack.Length > 0 then
                            ".gitattributes"

                        yield! oversizedPaths
                    |]
                    |> Array.distinct

                let! restageResult = runSimpleGit (fun currentGit -> currentGit.add pathsToRestage) git

                match restageResult with
                | Ok _ ->
                    return Ok()
                | Error failure when failure.Kind = LfsInstallRequired ->
                    return Error(createLfsInstallRequiredFailure (Some thresholdMb) None)
                | Error failure ->
                    return Error failure
    }

let private createAuthenticatedGit
    (arcPath: string)
    (remoteName: string)
    (progressCallback: GitProgressCallback option)
    =
    promise {
        let probeOptions = createOptions arcPath standardTimeout None
        let probeGit = createGit probeOptions

        let! remoteResult =
            runSimpleGit
                (fun git -> promise {
                    let! remoteUrl = git.raw [| "remote"; "get-url"; remoteName |]
                    return remoteUrl.Trim()
                })
                probeGit

        match remoteResult with
        | Error failure -> return Error failure
        | Ok remoteUrl ->
            match ensureAllowedRemoteUrl remoteUrl with
            | Error validationError -> return errorResult validationError
            | Ok allowedRemoteUrl ->
                match tryExtractHostFromRemoteUrl allowedRemoteUrl with
                | Error hostError -> return errorResult hostError
                | Ok host ->
                    let! tokenOption = tryGetAccessToken host

                    match tokenOption with
                    | Some token when not (String.IsNullOrWhiteSpace token) ->
                        try
                            let operationOptions = createOptions arcPath syncTimeout progressCallback
                            let git = applyAuth createGit operationOptions host token (Some remoteName) (Some allowedRemoteUrl)
                            return Ok git
                        with error ->
                            return errorResult error
                    | _ ->
                        return
                            Error {
                                Kind = Unauthorized
                                Message = $"No access token available for remote host '{host}'."
                            }
    }

let private ensureRepo (git: ISimpleGit) = promise {
    let! isRepo = git.checkIsRepo ()

    if isRepo then
        return Ok()
    else
        return Error(exn "The selected ARC path is not a git repository.")
}

let private withLocalGit (arcPath: string) (operation: ISimpleGit -> JS.Promise<'T>) : JS.Promise<GitResult<'T>> = promise {
    let options = createOptions arcPath standardTimeout None
    let git = createGit options

    let! repoCheckResult = ensureRepo git

    match repoCheckResult with
    | Error repoError -> return errorResult repoError
    | Ok() -> return! runSimpleGit operation git
}

let private withAuthenticatedGit
    (arcPath: string)
    (remoteName: string)
    (progressCallback: GitProgressCallback option)
    (operation: ISimpleGit -> JS.Promise<'T>)
    =
    promise {
        let! gitResult = createAuthenticatedGit arcPath remoteName progressCallback

        match gitResult with
        | Error failure -> return Error failure
        | Ok git -> return! runSimpleGit operation git
    }

let getStatus (arcPath: string) : JS.Promise<GitResult<GitStatusDto>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! status = git.status ()
            return toStatusDto arcPath status
        })

let getBranches (arcPath: string) : JS.Promise<GitResult<GitBranchRefDto[]>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! status = git.status ()
            let statusDto = toStatusDto arcPath status
            let! localBranchSummary = git.branchLocal ()
            let! remoteBranchText = git.raw [| "branch"; "-r"; "--no-color" |]

            let localRefs =
                valueOrEmptyArray localBranchSummary.all
                |> Array.map (fun branchName -> {
                    RefName = branchName
                    DisplayLabel = branchName
                    Kind = GitBranchRefKind.Local
                    IsCurrent = statusDto.Current = Some branchName
                    IsTracking = false
                })

            let remoteRefs =
                remoteBranchText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                |> Array.map _.Trim()
                |> Array.filter (fun branchName ->
                    not (String.IsNullOrWhiteSpace branchName)
                    && not (branchName.Contains " -> ")
                )
                |> Array.map (fun branchName -> {
                    RefName = branchName
                    DisplayLabel = branchName
                    Kind = GitBranchRefKind.Remote
                    IsCurrent = false
                    IsTracking = statusDto.Tracking = Some branchName
                })

            return
                Array.append localRefs remoteRefs
                |> Array.distinctBy _.RefName
                |> Array.sortBy (fun branch ->
                    let kindOrder =
                        match branch.Kind with
                        | GitBranchRefKind.Local -> 0
                        | GitBranchRefKind.Remote -> 1

                    kindOrder, (if branch.IsCurrent then 0 else 1), (if branch.IsTracking then 0 else 1), branch.DisplayLabel.ToLowerInvariant()
                )
        })

// GitService exposes LFS settings because the sidebar consumes them as part of the git workflow configuration surface.
let getLfsSettings (arcPath: string) : JS.Promise<GitResult<GitLfsSettingsDto>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! thresholdMb = getConfiguredLfsThresholdMb git

            return { AutoTrackThresholdMb = thresholdMb }
        })

let getDiffSummary (arcPath: string) : JS.Promise<GitResult<GitDiffSummaryDto>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! diff = git.diffSummary ()

            return {
                Changed = diff.changed
                Insertions = diff.insertions
                Deletions = diff.deletions
            }
        })

let getDiff (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<string>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let diffArgs = [| "diff"; "--"; yield! safePathSpecs |]
                    let! diff = git.raw diffArgs
                    return diff
                })
}

let getWordDiff (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<string>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let diffArgs = [|
                        "diff"
                        "--word-diff=porcelain"
                        "-U0"
                        "--"
                        yield! safePathSpecs
                    |]

                    let! diff = git.raw diffArgs
                    return diff
                })
}

let getDiffViewData (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<GitDiffViewDataDto>> = promise {
    match ensureValidPathspec requestedPath with
    | Error validationError -> return errorResult validationError
    | Ok safeRequestedPath ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! status = git.status ()
                    let statusDto = toStatusDto arcPath status

                    match statusDto.Files |> Array.tryFind (fun file -> String.Equals(file.Path, safeRequestedPath, StringComparison.Ordinal)) with
                    | None ->
                        return raise (exn $"No git status entry found for '{safeRequestedPath}'.")
                    | Some fileStatus ->
                        let previousPathCandidate = fileStatus.OriginalPath |> Option.defaultValue safeRequestedPath

                        let! previousContentResult = readHeadTextIfAvailable git previousPathCandidate

                        let previousContent =
                            match previousContentResult with
                            | Ok content -> content
                            | Error failure -> raise (exn failure.Message)

                        let! currentContentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                        let currentContent =
                            match currentContentResult with
                            | Ok content -> content
                            | Error failure -> raise (exn failure.Message)

                        if previousContent.IsNone && currentContent.IsNone then
                            return raise (exn $"No git diff content found for '{safeRequestedPath}'.")
                        else
                            let diffPaths =
                                [|
                                    yield safeRequestedPath

                                    match fileStatus.OriginalPath with
                                    | Some originalPath when not (String.Equals(originalPath, safeRequestedPath, StringComparison.Ordinal)) ->
                                        yield originalPath
                                    | _ ->
                                        ()
                                |]

                            let! wordDiffResult =
                                runSimpleGit
                                    (fun currentGit ->
                                        currentGit.raw [|
                                            "diff"
                                            "--word-diff=porcelain"
                                            "-U0"
                                            "--find-renames"
                                            "HEAD"
                                            "--"
                                            yield! diffPaths
                                        |]
                                    )
                                    git

                            let previousPathForMetadata =
                                if previousContent.IsSome then
                                    Some previousPathCandidate
                                else
                                    None

                            let currentPathForMetadata =
                                if currentContent.IsSome then
                                    Some safeRequestedPath
                                else
                                    None

                            let wordDiffText =
                                match wordDiffResult with
                                | Ok diff when not (String.IsNullOrWhiteSpace diff) -> diff
                                | _ -> buildSyntheticWordDiffText previousPathForMetadata currentPathForMetadata

                            return {
                                Path = safeRequestedPath
                                PreviousContent = previousContent |> Option.defaultValue ""
                                CurrentContent = currentContent |> Option.defaultValue ""
                                WordDiffText = wordDiffText
                            }
                })
}

let getMergeConflictViewData (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<GitMergeConflictViewDataDto>> = promise {
    match ensureValidPathspec requestedPath with
    | Error validationError -> return errorResult validationError
    | Ok safeRequestedPath ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! status = git.status ()
                    let statusDto = toStatusDto arcPath status

                    match ensureCurrentlyConflictedPath statusDto safeRequestedPath with
                    | Error conflictError ->
                        return raise conflictError
                    | Ok() ->
                        let! contentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                        match contentResult with
                        | Ok(Some content) ->
                            return {
                                Path = safeRequestedPath
                                MergeConflictContent = content
                            }
                        | Ok None ->
                            return raise (exn $"Conflicted file '{safeRequestedPath}' no longer exists.")
                        | Error failure ->
                            return raise (exn failure.Message)
                })
}

let fetch
    (arcPath: string)
    (remoteName: string option)
    (branchName: string option)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<unit>> =
    promise {
        match validateRemoteName (remoteName |> Option.defaultValue "origin") with
        | Error remoteError -> return errorResult remoteError
        | Ok safeRemoteName ->
            match validateOptionalBranchName branchName with
            | Error branchError -> return errorResult branchError
            | Ok safeBranchName ->
                let! result =
                    withAuthenticatedGit
                        arcPath
                        safeRemoteName
                        progressCallback
                        (fun git -> promise {
                            match safeBranchName with
                            | None ->
                                let! _ = git.fetch (safeRemoteName)
                                return ()
                            | Some safeBranch ->
                                let! _ = git.fetch (safeRemoteName, safeBranch)
                                return ()
                        })

                return result
    }

let pull
    (arcPath: string)
    (remoteName: string option)
    (branchName: string option)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<unit>> =
    promise {
        match validateRemoteName (remoteName |> Option.defaultValue "origin") with
        | Error remoteError -> return errorResult remoteError
        | Ok safeRemoteName ->
            match validateOptionalBranchName branchName with
            | Error branchError -> return errorResult branchError
            | Ok safeBranchName ->
                let! result =
                    withAuthenticatedGit
                        arcPath
                        safeRemoteName
                        progressCallback
                        (fun git -> promise {
                            match safeBranchName with
                            | None ->
                                do! ensureDefaultTrackingBranchForPull safeRemoteName git
                                let! _ = git.pull (safeRemoteName)
                                return ()
                            | Some safeBranch ->
                                let! _ = git.pull (safeRemoteName, safeBranch)
                                return ()
                        })

                return result
    }

// Push keeps the LFS pre-upload and diagnostics here because they are part of the overall push workflow, not standalone LFS tooling.
let push
    (arcPath: string)
    (remoteName: string option)
    (branchName: string option)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<unit>> =
    promise {
        match validateRemoteName (remoteName |> Option.defaultValue "origin") with
        | Error remoteError -> return errorResult remoteError
        | Ok safeRemoteName ->
            match validateOptionalBranchName branchName with
            | Error branchError -> return errorResult branchError
            | Ok safeBranchName ->
                let! gitResult = createAuthenticatedGit arcPath safeRemoteName progressCallback

                match gitResult with
                | Error failure -> return Error failure
                | Ok git ->
                    let! lfsPushResult =
                        pushObjects
                            runSimpleGit
                            (fun currentGit -> runSimpleGit (fun gitInstance -> gitInstance.status ()) currentGit)
                            safeRemoteName
                            safeBranchName
                            git

                    let! pushResult =
                        match lfsPushResult with
                        | Error failure -> promise { return Error failure }
                        | Ok () ->
                            runSimpleGit
                                (fun currentGit -> promise {
                                    match safeBranchName with
                                    | None ->
                                        let! _ = currentGit.push (safeRemoteName, "HEAD")
                                        return ()
                                    | Some safeBranch ->
                                        let! _ = currentGit.push (safeRemoteName, safeBranch)
                                        return ()
                                })
                                git

                    match lfsPushResult, pushResult with
                    | Error failure, _ when shouldCollectLfsDiagnostics failure ->
                        let! diagnostics =
                            collectPushDiagnostics
                                runSimpleGit
                                (fun failure -> Some failure.Message)
                                safeRemoteName
                                git

                        return
                            Error {
                                failure with
                                    Message = appendPushDiagnostics failure.Message diagnostics
                            }
                    | _, Error failure when shouldCollectLfsDiagnostics failure ->
                        let! diagnostics =
                            collectPushDiagnostics
                                runSimpleGit
                                (fun failure -> Some failure.Message)
                                safeRemoteName
                                git

                        return
                            Error {
                                failure with
                                    Message = appendPushDiagnostics failure.Message diagnostics
                            }
                    | _, _ ->
                        return pushResult
    }

// GitService writes LFS settings because the threshold is a git workflow policy setting owned by this service.
let setLfsSettings (arcPath: string) (settings: GitLfsSettingsDto) : JS.Promise<GitResult<unit>> =
    promise {
        match validateLfsThresholdMb settings.AutoTrackThresholdMb with
        | Error validationError -> return errorResult validationError
        | Ok thresholdMb ->
            return!
                withLocalGit
                    arcPath
                    (fun git -> promise {
                        let! _ =
                            git.raw [|
                                "config"
                                "--local"
                                gitLfsThresholdConfigKey
                                formatThresholdConfigValue thresholdMb
                            |]

                        return ()
                    })
    }

// Staging keeps automatic LFS enforcement here because the decision must happen immediately after normal git add updates the index.
let stagePaths (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<unit>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! _ = git.add safePathSpecs
                    let! lfsResult = ensureLargeStagedFilesUseLfs arcPath git

                    match lfsResult with
                    | Ok () -> return ()
                    | Error failure -> return raise (exn failure.Message)
                })
}

let unstagePaths (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<unit>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let resetOptions = [| yield "--"; yield! safePathSpecs |]

                    let! _ = git.reset ("mixed", !^resetOptions)
                    return ()
                })
}

// Commit re-checks LFS here as a workflow safeguard in case staged content reached the index outside this service's staging path.
let commit (arcPath: string) (message: string) : JS.Promise<GitResult<string>> = promise {
    let normalizedMessage = message.Trim()

    if String.IsNullOrWhiteSpace normalizedMessage then
        return errorResult (exn "Commit message must not be empty.")
    else
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! lfsResult = ensureLargeStagedFilesUseLfs arcPath git

                    match lfsResult with
                    | Error failure ->
                        return raise (exn failure.Message)
                    | Ok () ->
                        let! result = git.commit (normalizedMessage)

                        return
                            if String.IsNullOrWhiteSpace result.commit then
                                "Committed changes."
                            else
                                result.commit
                })
}

let confirmMergeResolution
    (arcPath: string)
    (requestedPath: string)
    (expectedConflictContent: string)
    (resolvedContent: string)
    : JS.Promise<GitResult<GitConfirmMergeResolutionResult>> =
    promise {
        match tryResolveArcRelativePath arcPath requestedPath with
        | Error validationError -> return errorResult validationError
        | Ok(safeRequestedPath, absolutePath) ->
            if isExplicitlyUnsupportedPath safeRequestedPath then
                return unsupportedGitContentResult safeRequestedPath
            else
                return!
                    withLocalGit
                        arcPath
                        (fun git -> promise {
                            let! statusBeforeWrite = git.status ()
                            let statusDtoBeforeWrite = toStatusDto arcPath statusBeforeWrite

                            match ensureCurrentlyConflictedPath statusDtoBeforeWrite safeRequestedPath with
                            | Error conflictError ->
                                return raise conflictError
                            | Ok() ->
                                let! currentContentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                                let currentConflictContent =
                                    match currentContentResult with
                                    | Ok(Some content) -> content
                                    | Ok None -> raise (exn $"Conflicted file '{safeRequestedPath}' no longer exists.")
                                    | Error failure -> raise (exn failure.Message)

                                if not (String.Equals(currentConflictContent, expectedConflictContent, StringComparison.Ordinal)) then
                                    return
                                        raise (
                                            exn
                                                $"Conflicted file '{safeRequestedPath}' changed on disk since it was opened. Reopen the merge conflict view and retry."
                                        )
                                else
                                    writeFileSync absolutePath resolvedContent TextEncoding.Utf8
                                    let! _ = git.add [| safeRequestedPath |]
                                    let! statusAfterStage = git.status ()
                                    let updatedStatus = toStatusDto arcPath statusAfterStage

                                    if updatedStatus.Conflicted.Length = 0 && updatedStatus.IsMergeInProgress then
                                        let! _ = git.commit "Resolve merge conflicts"
                                        let! statusAfterCommit = git.status ()
                                        let committedStatus = toStatusDto arcPath statusAfterCommit

                                        return {
                                            UpdatedStatus = committedStatus
                                            RemainingConflictedPaths = committedStatus.Conflicted
                                            NextConflictedPath = committedStatus.Conflicted |> Array.tryHead
                                        }
                                    else
                                        return {
                                            UpdatedStatus = updatedStatus
                                            RemainingConflictedPaths = updatedStatus.Conflicted
                                            NextConflictedPath = updatedStatus.Conflicted |> Array.tryHead
                                        }
                        })
    }

let createBranch (arcPath: string) (branchName: string) (startPoint: string option) : JS.Promise<GitResult<unit>> = promise {
    match ensureValidBranchLikeName "Branch name" branchName with
    | Error branchError -> return errorResult branchError
    | Ok safeBranchName ->
        match startPoint with
        | None ->
            return!
                withLocalGit
                    arcPath
                    (fun git -> promise {
                        let! _ = git.checkoutLocalBranch (safeBranchName)
                        return ()
                    })
        | Some value ->
            match ensureValidBranchLikeName "Start point" value with
            | Error startPointError -> return errorResult startPointError
            | Ok safeStartPoint ->
                return!
                    withLocalGit
                        arcPath
                        (fun git -> promise {
                            let! _ = git.checkoutBranch (safeBranchName, safeStartPoint)
                            return ()
                        })
}

let checkoutBranch (arcPath: string) (branchName: string) : JS.Promise<GitResult<unit>> = promise {
    match ensureValidBranchLikeName "Branch name" branchName with
    | Error branchError -> return errorResult branchError
    | Ok safeBranchName ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! localBranches = git.branchLocal ()
                    let branches = valueOrEmptyArray localBranches.all

                    let exists =
                        branches
                        |> Array.exists (fun existing ->
                            String.Equals(existing, safeBranchName, StringComparison.Ordinal)
                        )

                    if not exists then
                        return raise (exn $"Branch '{safeBranchName}' does not exist in the local repository.")

                    let! _ = git.checkout (safeBranchName)
                    return ()
                })
}
