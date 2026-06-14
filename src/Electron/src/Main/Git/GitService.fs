module Main.Git.GitService

open System
open System.IO
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components.Page.GitSidebarTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Main.Bindings.Node
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Bindings.SimpleGit
open Main.Git.GitLfsAdapter
open Main.Git.GitLfsService
open Main.Git.GitTokenProvider
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals

type GitFailure = {
    Kind: GitFailureKind
    Message: string
}

/// Internal result type returned by Main Git services before IPC maps it to shared DTOs.
type GitResult<'T> = Result<'T, GitFailure>

/// Resolved push target used by push workflow and tests to decide refspec/upstream behavior.
type GitPushTarget = {
    RefSpec: string
    PushBranch: string
    SetUpstream: bool
}

/// Pull result payload. Warning is reserved for recoverable follow-up issues such as LFS hydration failures.
type GitPullResult = { Warning: GitFailure option }

type GitProgressCallback = GitInternals.GitProgressCallback

let private disallowedRemotePrefixes = [| "file://"; "ext::"; "fd::" |]

let private protocolOverridePattern =
    Regex("protocol\\.[^\\s=]+\\.(allow|deny)|(^|\\s)-c\\s+protocol\\.", RegexOptions.IgnoreCase)

let private remoteNamePattern = Regex("^[A-Za-z0-9._/-]+$")
let private invalidBranchCharactersPattern = Regex(@"[~^:?*\[\\\s]")
let private gitLfsThresholdConfigKey = "swate.lfs.autotrackthresholdmb"
let private gitLfsDownloadLargeFilesConfigKey = "swate.lfs.downloadlargefiles"
let private gitLfsDefaultThresholdMb = 1
let private gitLfsMaximumThresholdMb = 100
let private gitLfsDefaultDownloadLargeFiles = true

let private normalizeOptionalGitRef (value: string option) =
    value
    |> Option.bind Option.ofObj
    |> Option.map _.Trim()
    |> Option.filter (fun item -> not (String.IsNullOrWhiteSpace item))

let private lfsInstallRequiredTokens = [|
    "git lfs is required for files larger than"
    "git lfs is required for this operation"
    "git: 'lfs' is not a git command"
    "git-lfs filter-process"
    "this repository is configured for git lfs but 'git-lfs' was not found"
    "external filter 'git-lfs filter-process' failed"
    "smudge filter lfs failed"
    "clean filter 'lfs' failed"
|]

/// Classifies git/simple-git/LFS error text into the shared failure taxonomy used over IPC.
let classifyFailureKind (message: string) =
    let normalizedMessage =
        message
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> fun text -> text.ToLowerInvariant()

    let containsAny (terms: string[]) =
        terms |> Array.exists normalizedMessage.Contains

    if containsAny lfsInstallRequiredTokens then
        GitFailureKind.LfsInstallRequired
    elif
        containsAny [|
            "has already been taken"
            "name already exists"
            "project already exists"
            "repository already exists"
        |]
    then
        GitFailureKind.RemoteProjectAlreadyExists
    elif containsAny [| "abort"; "cancelled"; "canceled"; "aborterror" |] then
        GitFailureKind.Canceled
    elif containsAny [| "timed out"; "timeout"; "time out" |] then
        GitFailureKind.Timeout
    elif containsAny [| "forbidden"; "403" |] then
        GitFailureKind.Forbidden
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
        GitFailureKind.Unauthorized
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
        GitFailureKind.Network
    else
        GitFailureKind.Unknown

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
    | GitFailureKind.LfsInstallRequired ->
        let finalMessage =
            if message.IndexOf("Install Git LFS now?", StringComparison.OrdinalIgnoreCase) >= 0 then
                message
            else
                buildLfsInstallPromptMessage None (Some message)

        { Kind = kind; Message = finalMessage }
    | _ -> { Kind = kind; Message = message }

let private toFailure (error: exn) : GitFailure =
    GitInternals.toFailure classifyFailureKind createFailure error

let private errorResult (error: exn) : GitResult<'T> =
    GitInternals.errorResult classifyFailureKind createFailure error

// These helpers intentionally throw inside promise callbacks so the surrounding
// runSimpleGit/withLocalGit boundary can translate the failure into GitFailure.
let private abortGitPromise<'T> (message: string) : 'T = raise (exn message)

let private abortGitPromiseWith<'T> (error: exn) : 'T = raise error

let private tryGetNodeErrorCode (error: exn) : string option =
    try
        error?code |> unbox<string> |> Option.ofObj
    with _ ->
        None

let private valueOrEmptyArray (items: 'T[]) = if isNull items then [||] else items

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

        if success && parsed >= 1 && parsed <= gitLfsMaximumThresholdMb then
            Some parsed
        else
            None

// GitService parses the download preference because pull behavior is part of the git workflow owned here.
let private tryParseConfiguredDownloadLargeFiles (value: string option) =
    match value |> Option.bind Option.ofObj with
    | None -> None
    | Some text ->
        match text.Trim().ToLowerInvariant() with
        | "true"
        | "1"
        | "yes"
        | "on" -> Some true
        | "false"
        | "0"
        | "no"
        | "off" -> Some false
        | _ -> None

// GitService converts the threshold to bytes because file-size decisions happen during stage/commit orchestration here.
let private thresholdMbToBytes (thresholdMb: int) = int64 thresholdMb * 1024L * 1024L

// GitService formats the persisted threshold because it owns the config contract for this workflow setting.
let private formatThresholdConfigValue (thresholdMb: int) = string thresholdMb

// GitService formats the persisted download preference because it owns the config contract for this workflow setting.
let private formatDownloadLargeFilesConfigValue (downloadLargeFiles: bool) =
    if downloadLargeFiles then "true" else "false"

let private tryGetFileSizeInBytes (absolutePath: string) : JS.Promise<int64 option> = promise {
    try
        let! stats = statAsync absolutePath
        return Some(stats.size |> int64)
    with error ->
        match tryGetNodeErrorCode error with
        | Some "ENOENT" -> return None
        | _ -> return raise error
}

/// Validates local branch names and branch-like start points before passing them to git.
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

/// Validates renderer-provided pathspecs as ARC-relative paths before file or git access.
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

/// Validates a non-empty pathspec array and returns normalized pathspecs for git commands.
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

let private splitGitOutputLines (text: string) =
    text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
    |> Array.map _.Trim()
    |> Array.filter (String.IsNullOrWhiteSpace >> not)

let private withGitPathspecs (command: string[]) (pathSpecs: string[]) = [|
    yield! command
    yield "--"
    yield! pathSpecs
|]

/// Validates a remote name and defaults blank input to `origin`.
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

/// Enforces Swate's remote URL policy before clone/add-remote/auth lookup.
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

let private trimTrailingGitSuffix (path: string) =
    if path.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && path.Length > 4 then
        path.[.. path.Length - 5]
    else
        path

/// Converts a validated git remote URL (https/ssh) into a browser-friendly repository URL.
let tryGetRepositoryWebUrlFromRemoteUrl (remoteUrl: string) : Result<string, exn> =
    match ensureAllowedRemoteUrl remoteUrl with
    | Error remoteUrlError -> Error remoteUrlError
    | Ok safeRemoteUrl ->
        let mutable uri = Unchecked.defaultof<Uri>

        if not (Uri.TryCreate(safeRemoteUrl, UriKind.Absolute, &uri)) then
            Error(exn $"Remote URL '{safeRemoteUrl}' is not a valid absolute URI.")
        elif String.IsNullOrWhiteSpace uri.Host then
            Error(exn "Remote URL is missing a host.")
        else
            let normalizedPath = uri.AbsolutePath.TrimEnd('/') |> trimTrailingGitSuffix

            if String.IsNullOrWhiteSpace normalizedPath || normalizedPath = "/" then
                Error(exn "Remote URL does not contain a repository path.")
            else
                Ok($"https://{uri.Host}{normalizedPath}".TrimEnd('/'))

let private validateOptionalBranchName (branchName: string option) =
    match branchName with
    | None -> Ok None
    | Some branchName -> ensureValidBranchLikeName "Branch name" branchName |> Result.map Some

let private unsupportedGitContentMessage (path: string) =
    $"Unsupported git content for '{path}'."

let private unsupportedGitContentResult<'T> (path: string) : GitResult<'T> =
    Error {
        Kind = GitFailureKind.Unknown
        Message = unsupportedGitContentMessage path
    }

/// Converts the service's unsupported-content sentinel into a DTO that IPC can return without throwing.
let tryGetUnsupportedGitContent (requestedPath: string) (failure: GitFailure) : GitUnsupportedContentDto option =
    if String.Equals(failure.Message, unsupportedGitContentMessage requestedPath, StringComparison.Ordinal) then
        Some {
            Path = requestedPath
            Reason = Some failure.Message
        }
    else
        None

// Read file as a raw buffer for binary detection during merge conflict and diff view loading.
// Binary files cannot be displayed in the text-based diff/merge viewers and need to be routed
// to the unsupported content page instead.
let private readFileBufferAsync (absolutePath: string) : JS.Promise<obj> =
    Main.Bindings.Filesystem.readFileBufferAsync absolutePath

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
    let extension = extname path

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
        let arcRoot = resolve [| arcPath |]
        let absolutePath = resolve [| arcRoot; safeRelativePath |]

        let relativePath =
            relative arcRoot absolutePath |> fun value -> value.Replace("\\", "/")

        if
            String.IsNullOrWhiteSpace relativePath
            || relativePath = "."
            || relativePath = ".."
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || isAbsolute relativePath
        then
            Error(exn "Pathspec resolves outside the active ARC root.")
        else
            Ok(safeRelativePath, absolutePath)

let private createTemporaryLfsBackupPath (absolutePath: string) =
    $"{absolutePath}.swate-lfs-backup-{Guid.NewGuid():N}"

let private restoreTemporaryLfsBackup backupPath absolutePath =
    if existsSync backupPath then
        if existsSync absolutePath then
            unlinkSync absolutePath

        renameSync backupPath absolutePath

let private removeTemporaryLfsBackup backupPath =
    if existsSync backupPath then
        unlinkSync backupPath

let private isPathCleanInStatus (status: StatusResult) (relativePath: string) =
    valueOrEmptyArray status.files
    |> Array.exists (fun fileStatus ->
        String.Equals(fileStatus.path, relativePath, StringComparison.Ordinal)
        || (fileStatus.``from``
            |> Option.exists (fun original -> String.Equals(original, relativePath, StringComparison.Ordinal)))
    )
    |> not

let private ensureBackupMatchesLfsOid (backupPath: string) (listing: GitLfsLsFileInfo) = promise {
    let! pointerTextResult =
        runGitCaptured {
            WorkingDirectory = None
            Arguments = [| "lfs"; "pointer"; "--file"; backupPath |]
            Environment = None
            StandardInput = None
            TimeoutMs = Some 30000
        }

    let generatedPointer =
        $"{pointerTextResult.StdoutText}\n{pointerTextResult.StderrText}"

    return
        if
            pointerTextResult.ExitCode = 0
            && generatedPointer.Contains($"oid sha256:{listing.oid}")
        then
            Ok()
        else
            Error(exn "The temporary LFS backup did not match the expected object. The original file was restored.")
}

let private isMergeInProgress (arcPath: string) =
    let mergeHeadPath = resolve [| arcPath; ".git"; "MERGE_HEAD" |]
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

let private missingHeadContentMarkers = [|
    "does not exist in 'head'"
    "exists on disk, but not in 'head'"
    "bad revision 'head'"
    "invalid object name 'head'"
|]

let private isMissingHeadContentFailure (failure: GitFailure) =
    let message = failure.Message.ToLowerInvariant()
    missingHeadContentMarkers |> Array.exists message.Contains

let private readWorkingTreeTextIfPresent
    (arcPath: string)
    (requestedPath: string)
    : JS.Promise<GitResult<string option>> =
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

let private readHeadTextIfAvailable (git: ISimpleGit) (requestedPath: string) : JS.Promise<GitResult<string option>> = promise {
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
        let escapedPath = value.Replace("\\", "\\\\").Replace("\"", "\\\"")

        $"\"{pathPrefix}{escapedPath}\""

let private buildSyntheticWordDiffText (previousPath: string option) (currentPath: string option) =
    let previousToken = quoteDiffPathToken "a/" previousPath
    let currentToken = quoteDiffPathToken "b/" currentPath

    [
        yield $"diff --git {previousToken} {currentToken}"

        match previousPath, currentPath with
        | None, Some _ -> yield "new file mode 100644"
        | Some _, None -> yield "deleted file mode 100644"
        | _ -> ()

        match previousPath, currentPath with
        | Some oldPath, Some currentPath when not (String.Equals(oldPath, currentPath, StringComparison.Ordinal)) ->
            yield $"rename from {oldPath}"
            yield $"rename to {currentPath}"
        | _ -> ()

        yield $"--- {previousToken}"
        yield $"+++ {currentToken}"
    ]
    |> String.concat "\n"

let private ensureDefaultTrackingBranchForPull (remoteName: string) (git: ISimpleGit) : JS.Promise<unit> = promise {
    let! remoteBranchText = git.raw [| "branch"; "-r"; "--no-color" |]
    let! status = git.status ()

    let currentBranch = normalizeOptionalGitRef status.current
    let currentTracking = normalizeOptionalGitRef status.tracking

    let remoteRefs =
        remoteBranchText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
        |> Array.map _.Trim()
        |> Array.filter (fun branchName ->
            not (String.IsNullOrWhiteSpace branchName) && not (branchName.Contains " -> ")
        )

    match status.detached, currentBranch with
    | true, _
    | _, None -> return ()
    | false, Some branchName ->
        let desiredUpstreamRef = $"{remoteName}/{branchName}"

        let desiredTracking =
            remoteRefs
            |> Array.tryFind (fun branchRef -> String.Equals(branchRef, desiredUpstreamRef, StringComparison.Ordinal))

        match desiredTracking, currentTracking with
        | None, _ -> return ()
        | Some desired, Some current when String.Equals(current, desired, StringComparison.Ordinal) -> return ()
        | Some desired, _ ->
            let! _ = git.raw [| "branch"; $"--set-upstream-to={desired}"; branchName |]
            return ()
}

/// Resolves the branch/refspec to push and whether `--set-upstream` should be used.
/// Exposed for tests because this policy affects new-branch publishing behavior.
let resolvePushTarget
    (requestedBranchName: string option)
    (currentBranch: string option)
    (trackingBranch: string option)
    (isDetached: bool)
    : GitPushTarget =
    let normalizedCurrentBranch = normalizeOptionalGitRef currentBranch
    let normalizedTrackingBranch = normalizeOptionalGitRef trackingBranch

    match requestedBranchName with
    | Some branchName -> {
        RefSpec = branchName
        PushBranch = branchName
        SetUpstream =
            not isDetached
            && normalizedCurrentBranch = Some branchName
            && normalizedTrackingBranch.IsNone
      }
    | None ->
        match isDetached, normalizedCurrentBranch with
        | true, _
        | _, None -> {
            RefSpec = "HEAD"
            PushBranch = "HEAD"
            SetUpstream = false
          }
        | false, Some branchName -> {
            RefSpec = branchName
            PushBranch = branchName
            SetUpstream = normalizedTrackingBranch.IsNone
          }

let private reconcileTrackingBranchForCheckout
    (remoteName: string)
    (branchName: string)
    (git: ISimpleGit)
    : JS.Promise<unit> =
    promise {
        let! remoteBranchText = git.raw [| "branch"; "-r"; "--no-color" |]
        let! status = git.status ()

        let currentTracking = normalizeOptionalGitRef status.tracking

        let remoteRefs =
            remoteBranchText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            |> Array.map _.Trim()
            |> Array.filter (fun branchRef ->
                not (String.IsNullOrWhiteSpace branchRef) && not (branchRef.Contains " -> ")
            )

        let desiredUpstreamRef = $"{remoteName}/{branchName}"

        let desiredTracking =
            remoteRefs
            |> Array.tryFind (fun branchRef -> String.Equals(branchRef, desiredUpstreamRef, StringComparison.Ordinal))

        match desiredTracking, currentTracking with
        | Some desired, Some current when String.Equals(current, desired, StringComparison.Ordinal) -> return ()
        | Some desired, _ ->
            let! _ = git.raw [| "branch"; $"--set-upstream-to={desired}"; branchName |]
            return ()
        | None, Some _ ->
            let! _ = git.raw [| "branch"; "--unset-upstream"; branchName |]
            return ()
        | None, None -> return ()
    }

let private runSimpleGit (operation: ISimpleGit -> JS.Promise<'T>) (git: ISimpleGit) : JS.Promise<GitResult<'T>> =
    GitInternals.runSimpleGit toFailure operation git

// GitService reads the threshold because stage/commit need the value while deciding whether to enforce LFS automatically.
let private getConfiguredLfsThresholdMb (git: ISimpleGit) : JS.Promise<int> = promise {
    try
        let! configResult = git.getConfig (gitLfsThresholdConfigKey, "local")

        return
            configResult.value
            |> tryParseConfiguredThresholdMb
            |> Option.defaultValue gitLfsDefaultThresholdMb
    with _ ->
        return gitLfsDefaultThresholdMb
}

// GitService reads the download preference because pull/sync need it while deciding whether to hydrate LFS content or keep pointers.
let private getConfiguredLfsDownloadLargeFiles (git: ISimpleGit) : JS.Promise<bool> = promise {
    try
        let! configResult = git.getConfig (gitLfsDownloadLargeFilesConfigKey, "local")

        return
            configResult.value
            |> tryParseConfiguredDownloadLargeFiles
            |> Option.defaultValue gitLfsDefaultDownloadLargeFiles
    with _ ->
        return gitLfsDefaultDownloadLargeFiles
}

// GitService creates this failure because install-required is surfaced as a git workflow error, not as a raw LFS command result.
let private createLfsInstallRequiredFailure (thresholdMb: int option) (details: string option) : GitFailure = {
    Kind = GitFailureKind.LfsInstallRequired
    Message = buildLfsInstallPromptMessage thresholdMb details
}

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
            | Ok() -> Ok()
            | Error message when classifyFailureKind message = GitFailureKind.LfsInstallRequired ->
                Error(createLfsInstallRequiredFailure (Some thresholdMb) (Some message))
            | Error message -> Error(createFailure GitFailureKind.Unknown message)
    }

let private getOversizedWorkingTreePaths
    (arcPath: string)
    (selectedPaths: string[])
    (thresholdBytes: int64)
    : JS.Promise<GitResult<string[]>> =
    promise {
        let oversizedPaths = ResizeArray<string>()
        let mutable failure: GitFailure option = None

        for selectedPath in selectedPaths |> Array.distinct do
            match failure with
            | Some _ -> ()
            | None ->
                match tryResolveArcRelativePath arcPath selectedPath with
                | Error validationError -> failure <- Some(toFailure validationError)
                | Ok(_, absolutePath) ->
                    let! fileSizeOption = tryGetFileSizeInBytes absolutePath

                    if fileSizeOption |> Option.exists (fun fileSize -> fileSize > thresholdBytes) then
                        oversizedPaths.Add selectedPath

        match failure with
        | Some failure -> return Error failure
        | None -> return Ok(oversizedPaths.ToArray())
    }

// GitService keeps automatic LFS mutation in the explicit staging flow so only user-selected paths are re-staged.
let private enforceStageTimeLfsTrackingForPaths
    (arcPath: string)
    (selectedPaths: string[])
    (git: ISimpleGit)
    : JS.Promise<GitResult<unit>> =
    promise {
        let! thresholdMb = getConfiguredLfsThresholdMb git
        let thresholdBytes = thresholdMbToBytes thresholdMb
        let! oversizedPathsResult = getOversizedWorkingTreePaths arcPath selectedPaths thresholdBytes

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
                    | Ok() -> ()
                    | Error trackFailure -> failure <- Some trackFailure

            match failure with
            | Some failure -> return Error failure
            | None ->
                if pathsToTrack.Length = 0 then
                    return Ok()
                else
                    let pathsToRestage = [| ".gitattributes"; yield! oversizedPaths |] |> Array.distinct

                    let! restageResult = runSimpleGit (fun currentGit -> currentGit.add pathsToRestage) git

                    match restageResult with
                    | Ok _ -> return Ok()
                    | Error failure when failure.Kind = GitFailureKind.LfsInstallRequired ->
                        return Error(createLfsInstallRequiredFailure (Some thresholdMb) None)
                    | Error failure -> return Error failure
    }

let private tryGetIndexedBlobId (git: ISimpleGit) (relativePath: string) : JS.Promise<GitResult<string option>> = promise {
    let! lsFilesResult =
        runSimpleGit (fun currentGit -> currentGit.raw [| "ls-files"; "--stage"; "--"; relativePath |]) git

    return
        lsFilesResult
        |> Result.bind (fun output ->
            output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            |> Array.tryPick (fun line ->
                let tabIndex = line.IndexOf('\t')

                if tabIndex <= 0 then
                    None
                else
                    let metadata =
                        line.Substring(0, tabIndex).Split(' ', StringSplitOptions.RemoveEmptyEntries)

                    if metadata.Length >= 3 && metadata.[2] = "0" then
                        Some metadata.[1]
                    else
                        None
            )
            |> Ok
        )
}

let private tryGetIndexedBlobSizeInBytes
    (git: ISimpleGit)
    (relativePath: string)
    : JS.Promise<GitResult<int64 option>> =
    promise {
        let! blobIdResult = tryGetIndexedBlobId git relativePath

        match blobIdResult with
        | Error failure -> return Error failure
        | Ok None -> return Ok None
        | Ok(Some blobId) ->
            let! sizeResult = runSimpleGit (fun currentGit -> currentGit.raw [| "cat-file"; "-s"; blobId |]) git

            return
                sizeResult
                |> Result.bind (fun text ->
                    let success, parsedSize = Int64.TryParse(text.Trim())

                    if success then
                        Ok(Some parsedSize)
                    else
                        errorResult (exn $"Could not determine staged blob size for '{relativePath}'.")
                )
    }

let private createCommitLfsValidationFailure (thresholdMb: int) (oversizedPaths: string[]) =
    let listedPaths = oversizedPaths |> Array.truncate 5 |> String.concat ", "

    let suffix = if oversizedPaths.Length > 5 then ", ..." else String.Empty

    {
        Kind = GitFailureKind.Unknown
        Message =
            $"Staged content larger than {formatThresholdMb thresholdMb} must already be staged as Git LFS pointers before commit. Re-stage the affected paths after tracking them with Git LFS or use Swate staging. Affected paths: {listedPaths}{suffix}"
    }

let private validateCommitLfsPolicy (git: ISimpleGit) : JS.Promise<GitResult<unit>> = promise {
    let! thresholdMb = getConfiguredLfsThresholdMb git
    let thresholdBytes = thresholdMbToBytes thresholdMb
    let! statusResult = runSimpleGit (fun currentGit -> currentGit.status ()) git

    match statusResult with
    | Error failure -> return Error failure
    | Ok status ->
        let stagedPaths =
            valueOrEmptyArray status.files
            |> Array.filter (fun fileStatus -> GitStatusCode.isStagedIndexStatus fileStatus.index)
            |> Array.map _.path
            |> Array.distinct

        let oversizedPaths = ResizeArray<string>()
        let mutable failure: GitFailure option = None

        for stagedPath in stagedPaths do
            match failure with
            | Some _ -> ()
            | None ->
                let! stagedSizeResult = tryGetIndexedBlobSizeInBytes git stagedPath

                match stagedSizeResult with
                | Error stagedFailure -> failure <- Some stagedFailure
                | Ok(Some stagedSize) when stagedSize > thresholdBytes -> oversizedPaths.Add stagedPath
                | Ok _ -> ()

        match failure with
        | Some failure -> return Error failure
        | None when oversizedPaths.Count = 0 -> return Ok()
        | None -> return Error(createCommitLfsValidationFailure thresholdMb (oversizedPaths.ToArray()))
}

// GitService keeps the pull preference application here because it changes how the authenticated pull workflow checks out the working tree.
let private applyLfsDownloadPreference (downloadLargeFiles: bool) (git: ISimpleGit) =
    if downloadLargeFiles then
        git
    else
        git.env (GitLfsSkipSmudgeEnvKey, "1")

// GitService keeps explicit post-pull hydration here because it must reuse the authenticated git instance created for the pull workflow.
let private hydratePulledLfsContent (git: ISimpleGit) : JS.Promise<GitResult<unit>> = promise {
    let! result =
        runSimpleGit
            (fun currentGit -> promise {
                let! _ = currentGit.raw [| "lfs"; "pull" |]
                return ()
            })
            git

    return result
}

let private createPullHydrationFailure (failure: GitFailure) = {
    failure with
        Message = $"Git pull completed, but Git LFS download failed: {failure.Message}"
}

type private AuthenticatedGitSession = {
    Git: ISimpleGit
    CommandAuth: GitAuthAdapter.GitCommandAuthentication
}

let private createLocalGitSession arcPath progressCallback =
    let options = createOptions arcPath syncTimeout progressCallback

    {
        Git = createGit options
        CommandAuth = {
            ConfigArgs = [||]
            Environment = createNonInteractiveEnv ()
        }
    }

let private createAuthenticatedGitSession
    (arcPath: string)
    (remoteName: string)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<AuthenticatedGitSession>> =
    promise {
        let probeOptions = createOptions arcPath standardTimeout None
        let probeGit = createGit probeOptions

        let! remoteResult =
            runSimpleGit
                (fun git -> promise {
                    let! remoteUrl = git.raw [| "config"; "--get"; $"remote.{remoteName}.url" |]
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

                            let git =
                                applyAuth
                                    createGit
                                    operationOptions
                                    host
                                    token
                                    (Some remoteName)
                                    (Some allowedRemoteUrl)

                            let commandAuth =
                                createCommandAuthentication host token (Some remoteName) (Some allowedRemoteUrl)

                            return Ok { Git = git; CommandAuth = commandAuth }
                        with error ->
                            return errorResult error
                    | _ ->
                        return
                            Error {
                                Kind = GitFailureKind.Unauthorized
                                Message = $"No access token available for remote host '{host}'."
                            }
    }

let private createOriginLfsRemoteSession
    (arcPath: string)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<AuthenticatedGitSession>> =
    promise {
        let remoteName = "origin"
        let probeOptions = createOptions arcPath standardTimeout None
        let probeGit = createGit probeOptions

        let! remoteResult =
            runSimpleGit
                (fun git -> promise {
                    let! remoteUrl = git.raw [| "config"; "--get"; $"remote.{remoteName}.url" |]
                    return remoteUrl.Trim()
                })
                probeGit

        match remoteResult with
        | Error failure -> return Error failure
        | Ok remoteUrl ->
            match ensureAllowedRemoteUrl remoteUrl with
            | Ok _ ->
                let! sessionResult = createAuthenticatedGitSession arcPath remoteName progressCallback
                return sessionResult
            | Error _ when
                remoteUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                || isAbsolute remoteUrl
                ->
                return Ok(createLocalGitSession arcPath progressCallback)
            | Error validationError -> return errorResult validationError
    }

let private createOriginLfsRemoteGit
    (arcPath: string)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<ISimpleGit>> =
    promise {
        let! sessionResult = createOriginLfsRemoteSession arcPath progressCallback
        return sessionResult |> Result.map _.Git
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

type private RemoteUrlState =
    | RemoteMissing
    | RemoteConfiguredWithoutUrl
    | RemoteConfigured of string

let private tryGetNonEmptyRemoteUrl (remote: RemoteWithRefs) =
    [| remote.refs.push; remote.refs.fetch |]
    |> Array.tryPick (fun url ->
        url
        |> Option.ofObj
        |> Option.map _.Trim()
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
    )

let private getRemoteUrlState (remoteName: string) (git: ISimpleGit) : JS.Promise<RemoteUrlState> = promise {
    let! remoteList = git.getRemotes true

    let remoteUrl =
        match remoteList with
        | U2.Case1 remotes ->
            remotes
            |> Array.tryFind (fun remote -> String.Equals(remote.name, remoteName, StringComparison.Ordinal))
            |> Option.map (fun _ -> RemoteConfiguredWithoutUrl)
        | U2.Case2 remotes ->
            remotes
            |> Array.tryFind (fun remote -> String.Equals(remote.name, remoteName, StringComparison.Ordinal))
            |> Option.map (fun remote ->
                match tryGetNonEmptyRemoteUrl remote with
                | Some url -> RemoteConfigured url
                | None -> RemoteConfiguredWithoutUrl
            )

    return remoteUrl |> Option.defaultValue RemoteMissing
}

let private tryGetPublishRepositoryName (arcPath: string) =
    let projectName =
        arcPath
        |> Option.ofObj
        |> Option.map basename
        |> Option.map _.Trim()
        |> Option.filter (String.IsNullOrWhiteSpace >> not)

    match projectName with
    | Some name -> Ok name
    | None -> Error(exn "Cannot derive a DataHub repository name from the ARC path.")

let private createPublishFailure (projectName: string) (message: string) =
    let details =
        message
        |> Option.ofObj
        |> Option.map _.Trim()
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultValue "DataHub project creation failed without details."

    {
        Kind = classifyFailureKind details
        Message = $"Could not publish local repository '{projectName}' to the active DataHub account: {details}"
    }

let private configurePublishedOriginRemote
    (arcPath: string)
    (remoteState: RemoteUrlState)
    (remoteUrl: string)
    : JS.Promise<GitResult<unit>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            match remoteState with
            | RemoteMissing ->
                let! _ = git.addRemote ("origin", remoteUrl)
                return ()
            | RemoteConfiguredWithoutUrl ->
                let! _ = git.raw [| "remote"; "set-url"; "origin"; remoteUrl |]
                return ()
            | RemoteConfigured _ -> return ()
        })

let private publishOriginRemoteIfMissing
    (arcPath: string)
    (remoteName: string)
    (remoteState: RemoteUrlState)
    : JS.Promise<GitResult<unit>> =
    promise {
        if not (String.Equals(remoteName, "origin", StringComparison.Ordinal)) then
            return
                Error {
                    Kind = GitFailureKind.Unknown
                    Message = $"Remote '{remoteName}' is not configured. Configure the remote before pushing."
                }
        else
            match tryGetPublishRepositoryName arcPath with
            | Error nameError -> return errorResult nameError
            | Ok projectName ->
                let! projectResult = RemoteProvisioning.createProject projectName

                match projectResult with
                | Error message -> return Error(createPublishFailure projectName message)
                | Ok remoteUrl ->
                    match ensureAllowedRemoteUrl remoteUrl with
                    | Error remoteUrlError ->
                        return
                            errorResult (
                                exn
                                    $"DataHub created repository '{projectName}', but returned an unusable Git remote URL: {remoteUrlError.Message}"
                            )
                    | Ok safeRemoteUrl -> return! configurePublishedOriginRemote arcPath remoteState safeRemoteUrl
    }

let private ensurePushRemoteConfigured (arcPath: string) (remoteName: string) : JS.Promise<GitResult<unit>> = promise {
    let! remoteStateResult = withLocalGit arcPath (getRemoteUrlState remoteName)

    match remoteStateResult with
    | Error failure -> return Error failure
    | Ok(RemoteConfigured remoteUrl) ->
        match ensureAllowedRemoteUrl remoteUrl with
        | Ok _ -> return Ok()
        | Error validationError ->
            return
                errorResult (
                    exn
                        $"Remote '{remoteName}' is configured but cannot be used for authenticated push: {validationError.Message}"
                )
    | Ok((RemoteMissing | RemoteConfiguredWithoutUrl) as remoteState) ->
        return! publishOriginRemoteIfMissing arcPath remoteName remoteState
}

let private withAuthenticatedGit
    (arcPath: string)
    (remoteName: string)
    (progressCallback: GitProgressCallback option)
    (operation: ISimpleGit -> JS.Promise<'T>)
    =
    promise {
        let! gitResult = createAuthenticatedGitSession arcPath remoteName progressCallback

        match gitResult with
        | Error failure -> return Error failure
        | Ok session -> return! runSimpleGit operation session.Git
    }

let private requireCleanWorkingTreeForLfsStorageAction (actionLabel: string) (git: ISimpleGit) = promise {
    let! status = git.status ()

    return
        if status.isClean () then
            Ok()
        else
            Error(exn $"{actionLabel} requires a clean working tree. Save, discard, or commit local changes first.")
}

let private rejectCustomLfsStorageForPrune (git: ISimpleGit) = promise {
    let! storageResult = runSimpleGit (fun currentGit -> currentGit.raw [| "config"; "--get"; "lfs.storage" |]) git

    return
        match storageResult with
        | Ok value when not (String.IsNullOrWhiteSpace(value.Trim())) ->
            Error(exn "Git LFS prune is disabled because this repository uses a custom lfs.storage directory.")
        | _ -> Ok()
}

/// Reads status for the active ARC repository, including conflict metadata used by the sidebar and merge UI.
let getStatus (arcPath: string) : JS.Promise<GitResult<GitStatusDto>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! status = git.status ()
            return toStatusDto arcPath status
        })

/// Lists local and remote branch refs for the sidebar branch picker.
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
                |> Array.map (fun branchName ->
                    let isCurrent = statusDto.Current = Some branchName

                    {
                        RefName = branchName
                        DisplayLabel = branchName
                        Kind = GitBranchRefKind.Local
                        IsCurrent = isCurrent
                        // Mark the active local branch when it already tracks an upstream so the sidebar can represent the switched branch itself.
                        IsTracking = isCurrent && statusDto.Tracking.IsSome
                    }
                )

            let remoteRefs =
                remoteBranchText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                |> Array.map _.Trim()
                |> Array.filter (fun branchName ->
                    not (String.IsNullOrWhiteSpace branchName) && not (branchName.Contains " -> ")
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

                    kindOrder,
                    (if branch.IsCurrent then 0 else 1),
                    (if branch.IsTracking then 0 else 1),
                    branch.DisplayLabel.ToLowerInvariant()
                )
        })

/// Exposes persisted LFS workflow settings consumed by the Git sidebar.
let getLfsSettings (arcPath: string) : JS.Promise<GitResult<GitLfsSettingsDto>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! thresholdMb = getConfiguredLfsThresholdMb git
            let! downloadLargeFiles = getConfiguredLfsDownloadLargeFiles git

            return {
                AutoTrackThresholdMb = thresholdMb
                DownloadLargeFiles = downloadLargeFiles
            }
        })

/// Returns aggregate unstaged diff counts for the active ARC repository.
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

/// Returns raw `git diff` text for validated pathspecs. Used by tests and lower-level consumers.
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

/// Returns porcelain word-diff text for validated pathspecs.
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

/// Loads previous/current text plus word-diff metadata for the renderer diff view.
/// Binary or explicitly unsupported files return the unsupported-content sentinel.
let getDiffViewData (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<GitDiffViewDataDto>> = promise {
    match ensureValidPathspec requestedPath with
    | Error validationError -> return errorResult validationError
    | Ok safeRequestedPath ->
        if isExplicitlyUnsupportedPath safeRequestedPath then
            return unsupportedGitContentResult safeRequestedPath
        else
            return!
                withLocalGit
                    arcPath
                    (fun git -> promise {
                        let! status = git.status ()
                        let statusDto = toStatusDto arcPath status

                        match
                            statusDto.Files
                            |> Array.tryFind (fun file ->
                                String.Equals(file.Path, safeRequestedPath, StringComparison.Ordinal)
                            )
                        with
                        | None -> return abortGitPromise $"No git status entry found for '{safeRequestedPath}'."
                        | Some fileStatus ->
                            let previousPathCandidate =
                                fileStatus.OriginalPath |> Option.defaultValue safeRequestedPath

                            let! previousContentResult = readHeadTextIfAvailable git previousPathCandidate

                            let previousContent =
                                match previousContentResult with
                                | Ok content -> content
                                | Error failure -> abortGitPromise failure.Message

                            let! currentContentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                            let currentContent =
                                match currentContentResult with
                                | Ok content -> content
                                | Error failure -> abortGitPromise failure.Message

                            if previousContent.IsNone && currentContent.IsNone then
                                return abortGitPromise $"No git diff content found for '{safeRequestedPath}'."
                            else
                                let diffPaths = [|
                                    yield safeRequestedPath

                                    match fileStatus.OriginalPath with
                                    | Some originalPath when
                                        not (String.Equals(originalPath, safeRequestedPath, StringComparison.Ordinal))
                                        ->
                                        yield originalPath
                                    | _ -> ()
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

/// Loads the current conflicted file content for the merge-resolution view.
let getMergeConflictViewData
    (arcPath: string)
    (requestedPath: string)
    : JS.Promise<GitResult<GitMergeConflictViewDataDto>> =
    promise {
        match ensureValidPathspec requestedPath with
        | Error validationError -> return errorResult validationError
        | Ok safeRequestedPath ->
            if isExplicitlyUnsupportedPath safeRequestedPath then
                return unsupportedGitContentResult safeRequestedPath
            else
                return!
                    withLocalGit
                        arcPath
                        (fun git -> promise {
                            let! status = git.status ()
                            let statusDto = toStatusDto arcPath status

                            match ensureCurrentlyConflictedPath statusDto safeRequestedPath with
                            | Error conflictError -> return abortGitPromiseWith conflictError
                            | Ok() ->
                                let! contentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                                match contentResult with
                                | Ok(Some content) ->
                                    return {
                                        Path = safeRequestedPath
                                        MergeConflictContent = content
                                    }
                                | Ok None ->
                                    return abortGitPromise $"Conflicted file '{safeRequestedPath}' no longer exists."
                                | Error failure -> return abortGitPromise failure.Message
                        })
    }

/// Fetches from a validated remote using the configured token provider and optional progress callback.
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

/// Fetches and runs a merge-tree preflight to classify whether pull is likely safe or requires merge resolution.
/// Renderer workflow uses this before "update from online" actions.
let previewPull
    (arcPath: string)
    (remoteName: string option)
    (branchName: string option)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<GitPullPreflightResult>> =
    promise {
        match validateRemoteName (remoteName |> Option.defaultValue "origin") with
        | Error remoteError -> return errorResult remoteError
        | Ok safeRemoteName ->
            match validateOptionalBranchName branchName with
            | Error branchError -> return errorResult branchError
            | Ok safeBranchName ->
                let! statusResult = withLocalGit arcPath (fun git -> git.status ())

                match statusResult with
                | Error failure -> return Error failure
                | Ok status ->
                    let currentBranch = normalizeOptionalGitRef status.current

                    match status.detached, currentBranch with
                    | true, _
                    | _, None ->
                        return
                            Ok {
                                Status = GitPullPreflightStatus.Indeterminate
                                Message = Some "Cannot preview pull for a detached HEAD."
                            }
                    | false, Some _ ->
                        let upstreamRef =
                            safeBranchName
                            |> Option.map (fun safeBranch -> $"{safeRemoteName}/{safeBranch}")
                            |> Option.orElseWith (fun () -> normalizeOptionalGitRef status.tracking)

                        match upstreamRef with
                        | None ->
                            return
                                Ok {
                                    Status = GitPullPreflightStatus.Indeterminate
                                    Message = Some "No upstream tracking branch is configured for the current branch."
                                }
                        | Some resolvedUpstreamRef ->
                            return!
                                withAuthenticatedGit
                                    arcPath
                                    safeRemoteName
                                    progressCallback
                                    (fun git -> promise {
                                        match safeBranchName with
                                        | None ->
                                            let! _ = git.fetch safeRemoteName
                                            ()
                                        | Some safeBranch ->
                                            let! _ = git.fetch (safeRemoteName, safeBranch)
                                            ()

                                        let! mergeTreeResult =
                                            runGitCaptured {
                                                WorkingDirectory = Some arcPath
                                                Arguments = [|
                                                    "merge-tree"
                                                    "--write-tree"
                                                    "HEAD"
                                                    resolvedUpstreamRef
                                                |]
                                                Environment = None
                                                StandardInput = None
                                                TimeoutMs = Some 30000
                                            }

                                        if mergeTreeResult.ExitCode = 0 then
                                            return {
                                                Status = GitPullPreflightStatus.SafeToPull
                                                Message = None
                                            }
                                        else
                                            let diagnosticText =
                                                $"{mergeTreeResult.StdoutText}\n{mergeTreeResult.StderrText}"

                                            let normalizedDiagnostic = diagnosticText.ToLowerInvariant()

                                            if
                                                normalizedDiagnostic.Contains("conflict")
                                                || normalizedDiagnostic.Contains("merge conflict")
                                            then
                                                return {
                                                    Status = GitPullPreflightStatus.WouldRequireMergeResolution
                                                    Message = Some "Pulling would require merge resolution."
                                                }
                                            else
                                                let trimmedDiagnostic = diagnosticText.Trim()

                                                return {
                                                    Status = GitPullPreflightStatus.Indeterminate
                                                    Message =
                                                        if String.IsNullOrWhiteSpace trimmedDiagnostic then
                                                            Some "Git pull preflight could not be classified safely."
                                                        else
                                                            Some
                                                                $"Git pull preflight could not be classified safely: {trimmedDiagnostic}"
                                                }
                                    })
    }

/// Pulls from a validated remote using token-backed auth and the repository LFS download preference.
/// When large-file download is enabled, LFS content is hydrated after the git pull.
let pull
    (arcPath: string)
    (remoteName: string option)
    (branchName: string option)
    (progressCallback: GitProgressCallback option)
    : JS.Promise<GitResult<GitPullResult>> =
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
                            let! downloadLargeFiles = getConfiguredLfsDownloadLargeFiles git
                            let effectiveGit = applyLfsDownloadPreference downloadLargeFiles git

                            match safeBranchName with
                            | None ->
                                do! ensureDefaultTrackingBranchForPull safeRemoteName effectiveGit
                                let! _ = effectiveGit.pull (safeRemoteName)
                                ()
                            | Some safeBranch ->
                                let! _ = effectiveGit.pull (safeRemoteName, safeBranch)
                                ()

                            if downloadLargeFiles then
                                let! lfsPullResult = hydratePulledLfsContent effectiveGit

                                match lfsPullResult with
                                | Ok() -> return { Warning = None }
                                | Error failure ->
                                    let hydrationFailure = createPullHydrationFailure failure
                                    return abortGitPromise hydrationFailure.Message
                            else
                                return { Warning = None }
                        })

                return result
    }

/// Coordinates LFS upload planning, optional LFS upload, and the final git push.
/// Kept separately testable because it controls when git hooks are skipped after an explicit LFS upload.
let executePushWorkflow
    (pushTarget: GitPushTarget)
    (buildOutboundPlan: unit -> JS.Promise<Result<OutboundPushPlan, GitFailure>>)
    (uploadLfsObjects: string[] -> JS.Promise<GitResult<unit>>)
    (pushToRemote: bool -> GitPushTarget -> JS.Promise<GitResult<unit>>)
    (collectLfsDiagnostics: GitFailure -> JS.Promise<string option>)
    : JS.Promise<GitResult<unit>> =
    promise {
        let! lfsPlanResult = buildOutboundPlan ()

        match lfsPlanResult with
        | Error failure -> return Error failure
        | Ok OutboundPushPlan.SkipLfsUpload -> return! pushToRemote false pushTarget
        | Ok(OutboundPushPlan.UploadLfsObjects lfsObjectIds) ->
            let! lfsUploadResult = uploadLfsObjects lfsObjectIds

            match lfsUploadResult with
            | Ok() -> return! pushToRemote true pushTarget
            | Error failure ->
                let! diagnostics = collectLfsDiagnostics failure

                return
                    Error {
                        failure with
                            Message = appendPushDiagnostics failure.Message diagnostics
                    }
    }

/// Pushes the current or requested branch, uploading referenced LFS objects first when needed.
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
                let! remoteConfigResult = ensurePushRemoteConfigured arcPath safeRemoteName

                match remoteConfigResult with
                | Error failure -> return Error failure
                | Ok() ->
                    let! gitResult = createAuthenticatedGitSession arcPath safeRemoteName progressCallback

                    match gitResult with
                    | Error failure -> return Error failure
                    | Ok session ->
                        let git = session.Git
                        let! pushStatusResult = runSimpleGit (fun currentGit -> currentGit.status ()) git

                        match pushStatusResult with
                        | Error failure -> return Error failure
                        | Ok pushStatus ->
                            let pushTarget =
                                resolvePushTarget
                                    safeBranchName
                                    pushStatus.current
                                    pushStatus.tracking
                                    pushStatus.detached

                            return!
                                executePushWorkflow
                                    pushTarget
                                    (fun () ->
                                        planOutboundPush
                                            runSimpleGit
                                            runGitCaptured
                                            toFailure
                                            (fun currentGit ->
                                                runSimpleGit (fun gitInstance -> gitInstance.status ()) currentGit
                                            )
                                            arcPath
                                            safeRemoteName
                                            (Some pushTarget.RefSpec)
                                            git
                                    )
                                    (fun objectIds -> promise {
                                        let! uploadResult =
                                            GitLfsService.uploadObjects
                                                runGitCaptured
                                                session.CommandAuth
                                                arcPath
                                                safeRemoteName
                                                pushTarget.RefSpec
                                                objectIds

                                        return uploadResult |> Result.mapError toFailure
                                    })
                                    (fun skipLfsHook currentPushTarget ->
                                        runSimpleGit
                                            (fun currentGit -> promise {
                                                let pushGit =
                                                    if skipLfsHook then
                                                        currentGit.env ("GIT_LFS_SKIP_PUSH", "1")
                                                    else
                                                        currentGit

                                                if currentPushTarget.SetUpstream then
                                                    let! _ =
                                                        pushGit.push (
                                                            safeRemoteName,
                                                            currentPushTarget.PushBranch,
                                                            !^[| "--set-upstream" |]
                                                        )

                                                    return ()
                                                else
                                                    let! _ =
                                                        pushGit.push (safeRemoteName, currentPushTarget.PushBranch)

                                                    return ()
                                            })
                                            git
                                    )
                                    (fun _failure ->
                                        collectPushDiagnostics
                                            runSimpleGit
                                            (fun currentFailure -> Some currentFailure.Message)
                                            safeRemoteName
                                            git
                                    )
    }

/// Persists Git workflow LFS settings in local repository config.
let setLfsSettings (arcPath: string) (settings: GitLfsSettingsDto) : JS.Promise<GitResult<unit>> = promise {
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

                    let! _ =
                        git.raw [|
                            "config"
                            "--local"
                            gitLfsDownloadLargeFilesConfigKey
                            formatDownloadLargeFilesConfigValue settings.DownloadLargeFiles
                        |]

                    return ()
                })
}

let pruneLfsCache (arcPath: string) : JS.Promise<GitResult<string>> = promise {
    let! localValidationResult =
        withLocalGit
            arcPath
            (fun git -> promise {
                match! requireCleanWorkingTreeForLfsStorageAction "Cleaning the Git LFS cache" git with
                | Error validationError -> return abortGitPromiseWith validationError
                | Ok() ->
                    match! rejectCustomLfsStorageForPrune git with
                    | Error validationError -> return abortGitPromiseWith validationError
                    | Ok() -> return ()
            })

    match localValidationResult with
    | Error failure -> return Error failure
    | Ok() ->
        let! gitResult = createOriginLfsRemoteGit arcPath None

        match gitResult with
        | Error failure -> return Error failure
        | Ok git ->
            let! result = runSimpleGit (fun currentGit -> currentGit.raw GitLfsService.storagePruneArgs) git
            return result
}

let dedupLfsStorage (arcPath: string) : JS.Promise<GitResult<string>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            match! requireCleanWorkingTreeForLfsStorageAction "Reducing Git LFS duplicate storage" git with
            | Error validationError -> return abortGitPromiseWith validationError
            | Ok() ->
                let! output = git.raw GitLfsService.storageDedupArgs
                return output
        })

/// Stages validated pathspecs and auto-tracks oversized selected files in Git LFS before restaging.
let stagePaths (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<unit>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! _ = git.add safePathSpecs
                    let! lfsResult = enforceStageTimeLfsTrackingForPaths arcPath safePathSpecs git

                    match lfsResult with
                    | Ok() -> return ()
                    | Error failure -> return abortGitPromise failure.Message
                })
}

/// Unstages validated pathspecs with a mixed reset while leaving working tree files unchanged.
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

let private hasHeadCommit (git: ISimpleGit) = promise {
    let! headResult = runSimpleGit (fun currentGit -> currentGit.raw [| "rev-parse"; "--verify"; "HEAD" |]) git
    return Result.isOk headResult
}

let private headPathsForPathspecs (git: ISimpleGit) (pathSpecs: string[]) = promise {
    let! headPathsResult =
        runSimpleGit
            (fun currentGit -> currentGit.raw (withGitPathspecs [| "ls-tree"; "-r"; "--name-only"; "HEAD" |] pathSpecs))
            git

    match headPathsResult with
    | Ok output -> return splitGitOutputLines output
    | Error failure -> return abortGitPromise failure.Message
}

let private discardPathspecsWithOriginals (arcPath: string) (status: StatusResult) (safePathSpecs: string[]) =
    let selectedPaths = safePathSpecs |> Set.ofArray
    let statusDto = toStatusDto arcPath status

    let originalPaths =
        statusDto.Files
        |> Array.choose (fun file ->
            if selectedPaths.Contains file.Path then
                file.OriginalPath
            else
                None
        )
        |> Array.choose (fun path ->
            match ensureValidPathspec path with
            | Ok safePath -> Some safePath
            | Error _ -> None
        )

    Array.append safePathSpecs originalPaths |> Array.distinct

let private requireLfsListingForPath (arcPath: string) (safePath: string) : JS.Promise<GitLfsLsFileInfo> = promise {
    match! GitLfsService.tryFindListingForPath arcPath safePath with
    | Ok listing -> return listing
    | Error message -> return abortGitPromise $"'{safePath}' {message}"
}

let private runLfsRaw (args: string[]) git =
    runSimpleGit (fun currentGit -> currentGit.raw args) git

let private withOriginLfsGitResult arcPath (operation: ISimpleGit -> JS.Promise<GitResult<'T>>) = promise {
    let! gitResult = createOriginLfsRemoteGit arcPath None

    match gitResult with
    | Error failure -> return Error failure
    | Ok git -> return! operation git
}

let private getCleanLfsListingForPath
    (arcPath: string)
    (safePath: string)
    (dirtyMessage: string)
    : JS.Promise<GitResult<GitLfsLsFileInfo>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let! status = git.status ()

            if not (isPathCleanInStatus status safePath) then
                return abortGitPromise dirtyMessage
            else
                return! requireLfsListingForPath arcPath safePath
        })

let private getCleanLfsFileForAction
    (arcPath: string)
    (requestedPath: string)
    (dirtyMessageForSafePath: string -> string)
    : JS.Promise<GitResult<string * string * GitLfsLsFileInfo>> =
    promise {
        match tryResolveArcRelativePath arcPath requestedPath with
        | Error validationError -> return errorResult validationError
        | Ok(safePath, absolutePath) ->
            match! getCleanLfsListingForPath arcPath safePath (dirtyMessageForSafePath safePath) with
            | Error failure -> return Error failure
            | Ok listing -> return Ok(safePath, absolutePath, listing)
    }

let private createMissingLfsAttributesFailure safePath actionDescription =
    exn
        $"'{safePath}' is listed by Git LFS, but the current .gitattributes does not register it. Restore Git LFS tracking for this path before {actionDescription}."

let private createLfsCheckoutFailure arcPath safePath =
    if not (isTrackedByAttributes arcPath safePath) then
        createMissingLfsAttributesFailure safePath "downloading it"
    else
        exn $"Could not download '{safePath}' into the working tree."

let private requireDownloadedLfsFile
    arcPath
    safePath
    absolutePath
    (expectedListing: GitLfsLsFileInfo)
    (git: ISimpleGit)
    =
    promise {
        let! finalListing = requireLfsListingForPath arcPath safePath

        if not finalListing.checkout then
            return abortGitPromiseWith (createLfsCheckoutFailure arcPath safePath)
        else
            let! finalStats = statAsync absolutePath
            let expectedSize = int64 expectedListing.size
            let actualSize = int64 finalStats.size

            if actualSize <> expectedSize then
                return abortGitPromiseWith (createLfsCheckoutFailure arcPath safePath)
            else
                let! finalStatus = git.status ()

                if not (isPathCleanInStatus finalStatus safePath) then
                    return abortGitPromiseWith (createLfsCheckoutFailure arcPath safePath)
                else
                    return ()
    }

let private downloadMissingLfsFile arcPath safePath absolutePath listing : JS.Promise<GitResult<unit>> = promise {
    match! createOriginLfsRemoteSession arcPath None with
    | Error failure -> return Error failure
    | Ok session ->
        match! GitLfsService.downloadObjectFromListing arcPath session.CommandAuth safePath listing with
        | Error error -> return errorResult error
        | Ok() ->
            let! checkoutResult = runLfsRaw (GitLfsService.buildCheckoutArgs safePath) session.Git

            match checkoutResult with
            | Error failure -> return Error failure
            | Ok _ ->
                return!
                    runSimpleGit
                        (fun currentGit -> requireDownloadedLfsFile arcPath safePath absolutePath listing currentGit)
                        session.Git
}

let freeLocalLfsCopy (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<unit>> = promise {
    let! lfsFileResult =
        getCleanLfsFileForAction
            arcPath
            requestedPath
            (fun safePath ->
                $"'{safePath}' has local changes. Save, discard, or commit them before freeing the local LFS copy."
            )

    match lfsFileResult with
    | Error failure -> return Error failure
    | Ok(_, _, listing) when not listing.checkout -> return Ok()
    | Ok(safePath, _, _) when not (isTrackedByAttributes arcPath safePath) ->
        return errorResult (createMissingLfsAttributesFailure safePath "freeing the local LFS copy")
    | Ok(safePath, absolutePath, listing) ->
        return!
            withOriginLfsGitResult
                arcPath
                (fun git -> promise {
                    let! fetchResult = runLfsRaw (GitLfsService.buildFetchRefetchArgs safePath) git

                    match fetchResult with
                    | Error failure -> return Error failure
                    | Ok _ ->
                        let backupPath = createTemporaryLfsBackupPath absolutePath

                        try
                            renameSync absolutePath backupPath

                            match! ensureBackupMatchesLfsOid backupPath listing with
                            | Error validationError ->
                                restoreTemporaryLfsBackup backupPath absolutePath

                                return errorResult validationError
                            | Ok() ->
                                let pointerGit = git.env (GitLfsSkipSmudgeEnvKey, "1")

                                let! checkoutResult =
                                    runSimpleGit
                                        (fun currentGit -> currentGit.raw [| "checkout"; "HEAD"; "--"; safePath |])
                                        pointerGit

                                match checkoutResult with
                                | Error failure ->
                                    restoreTemporaryLfsBackup backupPath absolutePath

                                    return Error failure
                                | Ok _ ->
                                    let! finalStatusResult = runSimpleGit (fun currentGit -> currentGit.status ()) git

                                    match finalStatusResult with
                                    | Error failure ->
                                        restoreTemporaryLfsBackup backupPath absolutePath

                                        return Error failure
                                    | Ok finalStatus when not (isPathCleanInStatus finalStatus safePath) ->
                                        restoreTemporaryLfsBackup backupPath absolutePath

                                        return
                                            errorResult (
                                                exn
                                                    $"Could not replace '{safePath}' with an LFS pointer without changing Git status."
                                            )
                                    | Ok _ ->
                                        removeTemporaryLfsBackup backupPath

                                        return Ok()
                        with ex ->
                            restoreTemporaryLfsBackup backupPath absolutePath

                            return errorResult ex
                })
}

let downloadLfsFile (arcPath: string) (requestedPath: string) : JS.Promise<GitResult<unit>> = promise {
    let! lfsFileResult =
        getCleanLfsFileForAction
            arcPath
            requestedPath
            (fun safePath ->
                $"'{safePath}' has local changes. Save, discard, or commit them before downloading the Git LFS file."
            )

    match lfsFileResult with
    | Error failure -> return Error failure
    | Ok(_, _, listing) when listing.checkout -> return Ok()
    | Ok(safePath, absolutePath, listing) -> return! downloadMissingLfsFile arcPath safePath absolutePath listing
}

/// Discards validated pathspecs by restoring tracked paths from HEAD and cleaning selected untracked files.
let discardPaths (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<unit>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! status = git.status ()
                    let discardPathSpecs = discardPathspecsWithOriginals arcPath status safePathSpecs
                    let! hasHead = hasHeadCommit git

                    if hasHead then
                        let! resetResult =
                            runSimpleGit
                                (fun currentGit -> currentGit.raw (withGitPathspecs [| "reset" |] discardPathSpecs))
                                git

                        match resetResult with
                        | Error failure -> return abortGitPromise failure.Message
                        | Ok _ ->
                            let! headPaths = headPathsForPathspecs git discardPathSpecs

                            if headPaths.Length > 0 then
                                let! restoreResult =
                                    runSimpleGit
                                        (fun currentGit ->
                                            currentGit.raw (withGitPathspecs [| "restore"; "--worktree" |] headPaths)
                                        )
                                        git

                                match restoreResult with
                                | Error failure -> return abortGitPromise failure.Message
                                | Ok _ -> ()
                    else
                        let! rmCachedResult =
                            runSimpleGit
                                (fun currentGit ->
                                    currentGit.raw (
                                        withGitPathspecs
                                            [| "rm"; "--cached"; "-r"; "--ignore-unmatch" |]
                                            discardPathSpecs
                                    )
                                )
                                git

                        match rmCachedResult with
                        | Error failure -> return abortGitPromise failure.Message
                        | Ok _ -> ()

                    let! cleanResult =
                        runSimpleGit
                            (fun currentGit -> currentGit.raw (withGitPathspecs [| "clean"; "-fd" |] discardPathSpecs))
                            git

                    match cleanResult with
                    | Error failure -> return abortGitPromise failure.Message
                    | Ok _ -> return ()
                })
}

/// Commits the current index after validating the commit message and staged LFS policy.
let commit (arcPath: string) (message: string) : JS.Promise<GitResult<string>> = promise {
    let normalizedMessage = message.Trim()

    if String.IsNullOrWhiteSpace normalizedMessage then
        return errorResult (exn "Commit message must not be empty.")
    else
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! lfsResult = validateCommitLfsPolicy git

                    match lfsResult with
                    | Error failure -> return abortGitPromise failure.Message
                    | Ok() ->
                        let! result = git.commit (normalizedMessage)

                        return
                            if String.IsNullOrWhiteSpace result.commit then
                                "Committed changes."
                            else
                                result.commit
                })
}

/// Writes resolved merge-conflict content, stages it, and optionally commits when no conflicts remain.
/// The expected content guard prevents overwriting a file that changed since the renderer opened the conflict view.
let confirmMergeResolution
    (arcPath: string)
    (requestedPath: string)
    (expectedConflictContent: string)
    (resolvedContent: string)
    (autoCommit: bool)
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
                            | Error conflictError -> return abortGitPromiseWith conflictError
                            | Ok() ->
                                let! currentContentResult = readWorkingTreeTextIfPresent arcPath safeRequestedPath

                                let currentConflictContent =
                                    match currentContentResult with
                                    | Ok(Some content) -> content
                                    | Ok None ->
                                        abortGitPromise $"Conflicted file '{safeRequestedPath}' no longer exists."
                                    | Error failure -> abortGitPromise failure.Message

                                if
                                    not (
                                        String.Equals(
                                            currentConflictContent,
                                            expectedConflictContent,
                                            StringComparison.Ordinal
                                        )
                                    )
                                then
                                    return
                                        abortGitPromise
                                            $"Conflicted file '{safeRequestedPath}' changed on disk since it was opened. Reopen the merge conflict view and retry."
                                else
                                    try
                                        writeFileSync absolutePath resolvedContent TextEncoding.Utf8
                                    with error ->
                                        return
                                            abortGitPromise
                                                $"Failed to write resolved content for '{safeRequestedPath}': {error.Message}"

                                    let! addResult =
                                        runSimpleGit (fun currentGit -> currentGit.add [| safeRequestedPath |]) git

                                    match addResult with
                                    | Error failure ->
                                        return
                                            abortGitPromise
                                                $"Resolved content was written to '{safeRequestedPath}', but staging failed: {failure.Message}"
                                    | Ok _ ->
                                        let! statusAfterStage = git.status ()
                                        let updatedStatus = toStatusDto arcPath statusAfterStage

                                        if
                                            autoCommit
                                            && updatedStatus.Conflicted.Length = 0
                                            && updatedStatus.IsMergeInProgress
                                        then
                                            let! commitResult =
                                                runSimpleGit
                                                    (fun currentGit -> currentGit.commit "Resolve merge conflicts")
                                                    git

                                            match commitResult with
                                            | Error failure -> return abortGitPromise failure.Message
                                            | Ok _ ->
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

/// Creates and checks out a new local branch, optionally from a validated start point.
/// Tracking is reconciled against `origin/<branch>` when that remote branch exists.
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
                        do! reconcileTrackingBranchForCheckout "origin" safeBranchName git
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
                            do! reconcileTrackingBranchForCheckout "origin" safeBranchName git
                            return ()
                        })
}

/// Adds a new validated remote to the active ARC repository.
let addRemote (arcPath: string) (remoteName: string) (remoteUrl: string) : JS.Promise<GitResult<unit>> = promise {
    match validateRemoteName remoteName with
    | Error remoteError -> return errorResult remoteError
    | Ok safeRemoteName ->
        match ensureAllowedRemoteUrl remoteUrl with
        | Error remoteUrlError -> return errorResult remoteUrlError
        | Ok safeRemoteUrl ->
            return!
                withLocalGit
                    arcPath
                    (fun git -> promise {
                        let! remoteList = git.getRemotes ()

                        let remoteExists =
                            match remoteList with
                            | U2.Case1 remotes ->
                                remotes
                                |> Array.exists (fun remote ->
                                    String.Equals(remote.name, safeRemoteName, StringComparison.Ordinal)
                                )
                            | U2.Case2 remotes ->
                                remotes
                                |> Array.exists (fun remote ->
                                    String.Equals(remote.name, safeRemoteName, StringComparison.Ordinal)
                                )

                        if remoteExists then
                            return abortGitPromise $"Remote '{safeRemoteName}' already exists."
                        else
                            let! _ = git.addRemote (safeRemoteName, safeRemoteUrl)
                            return ()
                    })
}

/// Resolves the origin remote to a browser-friendly repository URL if one exists and is openable.
let getOriginRepositoryWebUrl (arcPath: string) : JS.Promise<GitResult<string option>> =
    withLocalGit
        arcPath
        (fun git -> promise {
            let originName = "origin"

            let! verboseRemoteList = git.getRemotes true

            let configuredFromVerboseRemotes =
                match verboseRemoteList with
                | U2.Case2 remotes ->
                    remotes
                    |> Array.tryFind (fun remote -> String.Equals(remote.name, originName, StringComparison.Ordinal))
                    |> Option.bind (fun remote ->
                        remote.refs
                        |> Option.ofObj
                        |> Option.bind (fun refs ->
                            [| refs.push; refs.fetch |]
                            |> Array.choose Option.ofObj
                            |> Array.map _.Trim()
                            |> Array.tryFind (String.IsNullOrWhiteSpace >> not)
                        )
                    )
                | U2.Case1 _ -> None

            let! configuredRemoteUrlOption =
                match configuredFromVerboseRemotes with
                | Some configuredRemoteUrl -> promise { return Some configuredRemoteUrl }
                | None -> promise {
                    let! remoteList = git.getRemotes ()

                    let originExists =
                        match remoteList with
                        | U2.Case1 remotes ->
                            remotes
                            |> Array.exists (fun remote ->
                                String.Equals(remote.name, originName, StringComparison.Ordinal)
                            )
                        | U2.Case2 remotes ->
                            remotes
                            |> Array.exists (fun remote ->
                                String.Equals(remote.name, originName, StringComparison.Ordinal)
                            )

                    if not originExists then
                        return None
                    else
                        let! configuredRemoteUrlResult =
                            runSimpleGit
                                (fun currentGit -> currentGit.raw [| "config"; "--get"; "remote.origin.url" |])
                                git

                        match configuredRemoteUrlResult with
                        | Ok configuredRemoteUrl ->
                            let normalizedRemoteUrl = configuredRemoteUrl.Trim()

                            if String.IsNullOrWhiteSpace normalizedRemoteUrl then
                                return None
                            else
                                return Some normalizedRemoteUrl
                        | Error _ -> return None
                  }

            match configuredRemoteUrlOption with
            | None -> return None
            | Some configuredRemoteUrl ->
                match tryGetRepositoryWebUrlFromRemoteUrl configuredRemoteUrl with
                | Ok repositoryWebUrl -> return Some repositoryWebUrl
                | Error _ -> return None
        })

/// Checks out an existing local branch, or creates/checks out a local branch from StartPoint.
/// Renderer passes remote branch switches as Name=<local name>, StartPoint=<remote ref>.
let checkoutBranch (arcPath: string) (request: GitCheckoutBranchRequest) : JS.Promise<GitResult<unit>> = promise {
    match ensureValidBranchLikeName "Branch name" request.Name with
    | Error branchError -> return errorResult branchError
    | Ok safeBranchName ->
        match request.StartPoint with
        | Some startPoint ->
            match ensureValidBranchLikeName "Start point" startPoint with
            | Error startPointError -> return errorResult startPointError
            | Ok safeStartPoint ->
                return!
                    withLocalGit
                        arcPath
                        (fun git -> promise {
                            let! _ = git.checkoutBranch (safeBranchName, safeStartPoint)
                            do! reconcileTrackingBranchForCheckout "origin" safeBranchName git
                            return ()
                        })
        | None ->
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
                            return abortGitPromise $"Branch '{safeBranchName}' does not exist in the local repository."

                        let! _ = git.checkout (safeBranchName)
                        do! reconcileTrackingBranchForCheckout "origin" safeBranchName git
                        return ()
                    })
}
