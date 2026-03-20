module Main.Git.GitProvisioningService

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals
open Main.Git.GitTokenProvider

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

type ExistingPathKind =
    | Directory
    | File
    | Symlink
    | Other

type CloneTargetState =
    | Missing
    | ExistingEmpty
    | ExistingNonEmpty

let private tryGetNodeErrorCode (error: exn) : string option =
    try
        error?code |> unbox<string> |> Option.ofObj
    with _ ->
        None

let private createPathAccessError (pathValue: string) (error: exn) =
    let code =
        match tryGetNodeErrorCode error with
        | Some codeValue -> codeValue
        | None -> "UNKNOWN"

    exn $"Path '{pathValue}' could not be accessed ({code}): {error.Message}"

let private classifyStatsObject (stats: obj) : ExistingPathKind =
    try
        if stats?isSymbolicLink() |> unbox<bool> then
            ExistingPathKind.Symlink
        elif stats?isDirectory() |> unbox<bool> then
            ExistingPathKind.Directory
        elif stats?isFile() |> unbox<bool> then
            ExistingPathKind.File
        else
            ExistingPathKind.Other
    with _ ->
        ExistingPathKind.Other

let private statAsync (pathValue: string) : JS.Promise<obj> =
    fsPromisesDynamic?stat(pathValue) |> unbox<JS.Promise<obj>>

let private lstatAsync (pathValue: string) : JS.Promise<obj> =
    fsPromisesDynamic?lstat(pathValue) |> unbox<JS.Promise<obj>>

let private tryGetExistingPathKindWithStats
    (getStatsAsync: string -> JS.Promise<obj>)
    (pathValue: string)
    : JS.Promise<Result<ExistingPathKind option, exn>> =
    promise {
        try
            let! stats = getStatsAsync pathValue
            return Ok(Some(classifyStatsObject stats))
        with error ->
            match tryGetNodeErrorCode error with
            | Some "ENOENT" -> return Ok None
            | _ -> return Error(createPathAccessError pathValue error)
    }

let private tryGetExistingPathKind (pathValue: string) : JS.Promise<Result<ExistingPathKind option, exn>> =
    promise {
        let! statResult = tryGetExistingPathKindWithStats statAsync pathValue

        match statResult with
        | Ok None ->
            // `stat` follows symbolic links; for broken links this yields ENOENT even though the entry exists.
            // Probe with `lstat` to correctly detect a link at the path.
            let! lstatResult = tryGetExistingPathKindWithStats lstatAsync pathValue

            match lstatResult with
            | Ok (Some ExistingPathKind.Symlink) -> return Ok(Some ExistingPathKind.Symlink)
            | _ -> return lstatResult
        | _ ->
            return statResult
    }

let private tryGetExistingPathKindNoFollow (pathValue: string) : JS.Promise<Result<ExistingPathKind option, exn>> =
    tryGetExistingPathKindWithStats lstatAsync pathValue

let validateAndNormalizeTargetPathWithResolver
    (resolvePath: string -> string)
    (targetPath: string)
    : Result<string, exn> =
    let candidate =
        targetPath
        |> Option.ofObj
        |> Option.map _.Trim()
        |> Option.defaultValue String.Empty

    if String.IsNullOrWhiteSpace candidate then
        Error(exn "Target path must not be empty.")
    elif candidate.Contains("\u0000") then
        Error(exn "Target path contains invalid null characters.")
    else
        try
            Ok(resolvePath candidate)
        with error ->
            Error(exn $"Target path could not be normalized: {error.Message}")

let classifyCloneTargetState (directoryExists: bool) (entryCount: int) : CloneTargetState =
    if directoryExists && entryCount > 0 then
        CloneTargetState.ExistingNonEmpty
    elif directoryExists then
        CloneTargetState.ExistingEmpty
    else
        CloneTargetState.Missing

let ensureCloneTargetIsEmpty (state: CloneTargetState) : Result<unit, exn> =
    match state with
    | CloneTargetState.ExistingNonEmpty ->
        Error(exn "Target path already exists and is not empty.")
    | CloneTargetState.ExistingEmpty
    | CloneTargetState.Missing ->
        Ok ()

let shouldRetryWithoutAuth (failure: GitService.GitFailure) =
    let message =
        failure.Message
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> fun text -> text.ToLowerInvariant()

    let contains (term: string) = message.Contains(term)

    match failure.Kind with
    | GitService.GitFailureKind.Forbidden ->
        true
    | GitService.GitFailureKind.Unauthorized ->
        // Treat generic local filesystem permission errors conservatively (no cleanup+retry).
        // Allow SSH publickey failures, and common HTTP 401 auth signals.
        let hasHttpAuthSignal =
            contains "unauthorized"
            || contains "authentication failed"
            || contains "401"
            || contains "could not read username"
            || contains "no access token available"

        let isSshPublicKeyFailure =
            contains "permission denied" && contains "publickey"

        hasHttpAuthSignal || isSshPublicKeyFailure
    | GitService.GitFailureKind.Network
    | GitService.GitFailureKind.Timeout
    | GitService.GitFailureKind.Canceled
    | GitService.GitFailureKind.Unknown ->
        false

let buildCloneBranchOptions (branch: string option) : string[] =
    match branch with
    | None -> [||]
    | Some branchName -> [| "--branch"; branchName |]

let private createFailure kind message : GitService.GitFailure = {
    Kind = kind
    Message = message
}

let private toFailure (error: exn) : GitService.GitFailure =
    GitInternals.toFailure GitService.classifyFailureKind createFailure error

let private errorResult (error: exn) : GitService.GitResult<'T> =
    GitInternals.errorResult GitService.classifyFailureKind createFailure error

let private runSimpleGit (operation: ISimpleGit -> JS.Promise<'T>) (git: ISimpleGit) : JS.Promise<GitService.GitResult<'T>> =
    GitInternals.runSimpleGit toFailure operation git

let private resolveAbsolutePath (pathValue: string) =
    pathDynamic?resolve(pathValue) |> unbox<string>

let private dirname (pathValue: string) =
    pathDynamic?dirname(pathValue) |> unbox<string>

let private mkdirRecursiveAsync (directoryPath: string) : JS.Promise<unit> =
    promise {
        let mkdirPromise =
            fsPromisesDynamic?mkdir(directoryPath, createObj [ "recursive" ==> true ])
            |> unbox<JS.Promise<obj>>

        let! _ = mkdirPromise
        return ()
    }

let private removeRecursiveAsync (targetPath: string) : JS.Promise<unit> =
    promise {
        let rmPromise =
            fsPromisesDynamic?rm(targetPath, createObj [ "recursive" ==> true; "force" ==> true ])
            |> unbox<JS.Promise<obj>>

        let! _ = rmPromise
        return ()
    }

let validateCloneRetryCleanupEntries (entries: string[]) : Result<string option, exn> =
    let safeEntries = if isNull entries then [||] else entries

    if safeEntries.Length = 0 then
        Ok None
    else
        let gitEntries =
            safeEntries
            |> Array.filter (fun entry -> entry.Equals(".git", StringComparison.OrdinalIgnoreCase))

        if gitEntries.Length = 0 then
            Error(
                exn
                    "Refusing to clean clone target for auth-fallback retry because it contains unexpected files (no .git directory detected)."
            )
        elif safeEntries.Length = 1 then
            Ok(Some gitEntries.[0])
        else
            Error(
                exn
                    "Refusing to clean clone target for auth-fallback retry because it contains unexpected files in addition to the .git directory."
            )

let private cleanupCloneTargetForRetry
    (targetPath: string)
    : JS.Promise<Result<unit, exn>> =
    promise {
        try
            // Safety guard:
            // This cleanup is only used to enable a single unauthenticated clone retry after an auth failure.
            // It is intentionally conservative to avoid deleting unrelated user data if the target path is
            // concurrently modified by another process between attempts.
            let! kindResult = tryGetExistingPathKindNoFollow targetPath

            match kindResult with
            | Error statError ->
                return Error statError
            | Ok None ->
                // Target is missing; nothing to clean.
                return Ok ()
            | Ok (Some ExistingPathKind.Directory) ->
                let! entries = fsPromisesDynamic?readdir(targetPath) |> unbox<JS.Promise<string[]>>

                match validateCloneRetryCleanupEntries entries with
                | Error entryError ->
                    return Error entryError
                | Ok None ->
                    // An empty directory is a valid clone destination; avoid deleting it unnecessarily.
                    return Ok ()
                | Ok (Some gitEntryName) ->
                    // Only remove the git metadata, and only when the directory contains nothing else.
                    // This avoids deleting concurrently-created unrelated files and reduces TOCTOU impact.
                    let gitDirPath = pathDynamic?join(targetPath, gitEntryName) |> unbox<string>
                    let! gitKindResult = tryGetExistingPathKindNoFollow gitDirPath

                    match gitKindResult with
                    | Error gitKindError ->
                        return Error gitKindError
                    | Ok (Some ExistingPathKind.Directory) ->
                        do! removeRecursiveAsync gitDirPath
                        return Ok ()
                    | Ok (Some ExistingPathKind.Symlink) ->
                        return Error(exn "Refusing to clean clone target for retry because .git is a symbolic link/junction.")
                    | Ok (Some ExistingPathKind.File)
                    | Ok (Some ExistingPathKind.Other) ->
                        return Error(exn "Refusing to clean clone target for retry because .git is not a directory.")
                    | Ok None ->
                        // Directory contents indicated .git, but it is missing now; treat as race and refuse.
                        return
                            Error(
                                exn
                                    "Refusing to clean clone target for retry because .git could not be found (directory contents changed concurrently)."
                            )
            | Ok (Some ExistingPathKind.Symlink) ->
                return Error(exn "Refusing to clean clone target for retry because target path is a symbolic link/junction.")
            | Ok (Some ExistingPathKind.File)
            | Ok (Some ExistingPathKind.Other) ->
                return Error(exn "Refusing to clean clone target for retry because target path is not a directory.")

        with error ->
            return Error(exn $"Failed to clean clone target before retry: {error.Message}")
    }

let private getCloneTargetState (targetPath: string) : JS.Promise<Result<CloneTargetState, exn>> =
    promise {
        let! kindResult = tryGetExistingPathKind targetPath

        match kindResult with
        | Error statError ->
            return Error statError
        | Ok None ->
            return Ok CloneTargetState.Missing
        | Ok (Some ExistingPathKind.Directory) ->
            let! entries = fsPromisesDynamic?readdir(targetPath) |> unbox<JS.Promise<obj[]>>
            let entryCount = if isNull entries then 0 else entries.Length
            return Ok(classifyCloneTargetState true entryCount)
        | Ok (Some ExistingPathKind.Symlink) ->
            return Error(exn "Target path exists and is a symbolic link/junction (not a directory).")
        | Ok (Some ExistingPathKind.File)
        | Ok (Some ExistingPathKind.Other) ->
            return Error(exn "Target path exists and is not a directory.")
    }

let private validateOptionalBranch (branch: string option) : Result<string option, exn> =
    let normalizedBranch = branch |> Option.bind Option.ofObj

    match normalizedBranch with
    | None -> Ok None
    | Some branchName ->
        GitService.ensureValidBranchLikeName "Branch name" branchName
        |> Result.map Some

let private createNonEmptyTargetFailure () : GitService.GitResult<'T> =
    Error {
        Kind = GitService.GitFailureKind.Unknown
        Message = "Target path already exists and is not empty."
    }

let private createExistingRepositoryFailure () : GitService.GitResult<'T> =
    Error {
        Kind = GitService.GitFailureKind.Unknown
        Message = "Target path is already a git repository."
    }

let private cloneWithGit
    (git: ISimpleGit)
    (remoteUrl: string)
    (targetPath: string)
    (branch: string option)
    : JS.Promise<GitService.GitResult<string>> =
    runSimpleGit
        (fun cloneGit -> promise {
            let branchOptions = buildCloneBranchOptions branch

            // baseDir is target parent; passing absolute target path here preserves explicit destination semantics.
            if branchOptions.Length = 0 then
                let! _ = cloneGit.clone(remoteUrl, targetPath)
                return targetPath
            else
                let! _ = cloneGit.clone(remoteUrl, targetPath, !^branchOptions)
                return targetPath
        })
        git

let initRepository (targetPath: string) : JS.Promise<GitService.GitResult<string>> =
    promise {
        match validateAndNormalizeTargetPathWithResolver resolveAbsolutePath targetPath with
        | Error validationError ->
            return errorResult validationError
        | Ok normalizedTargetPath ->
            try
                let! preInitResult =
                    promise {
                        let! kindResult = tryGetExistingPathKind normalizedTargetPath

                        match kindResult with
                        | Error statError ->
                            return errorResult statError
                        | Ok (Some ExistingPathKind.Directory) ->
                            let existingGit =
                                createOptions normalizedTargetPath standardTimeout None
                                |> createGit

                            let! existingRepoResult = runSimpleGit (fun git -> git.checkIsRepo()) existingGit

                            match existingRepoResult with
                            | Error failure ->
                                return Error failure
                            | Ok true ->
                                return createExistingRepositoryFailure ()
                            | Ok false ->
                                return Ok ()
                        | Ok (Some ExistingPathKind.Symlink) ->
                            return errorResult (exn "Target path exists and is a symbolic link/junction (not a directory).")
                        | Ok (Some ExistingPathKind.File)
                        | Ok (Some ExistingPathKind.Other) ->
                            return errorResult (exn "Target path exists and is not a directory.")
                        | Ok None ->
                            do! mkdirRecursiveAsync normalizedTargetPath
                            return Ok ()
                    }

                match preInitResult with
                | Error failure ->
                    return Error failure
                | Ok () ->
                    let initGit =
                        createOptions normalizedTargetPath standardTimeout None
                        |> createGit

                    let! initResult =
                        runSimpleGit
                            (fun git -> promise {
                                let! _ = git.init()
                                return normalizedTargetPath
                            })
                            initGit

                    return initResult
            with error ->
                return errorResult error
    }

let cloneRepository
    (remoteUrl: string)
    (targetPath: string)
    (branch: string option)
    (progress: GitService.GitProgressCallback option)
    : JS.Promise<GitService.GitResult<string>> =
    promise {
        let remoteUrlCandidate = remoteUrl |> Option.ofObj |> Option.defaultValue String.Empty

        match GitService.ensureAllowedRemoteUrl remoteUrlCandidate with
        | Error remoteError ->
            return errorResult remoteError
        | Ok safeRemoteUrl ->
            match tryExtractHostFromRemoteUrl safeRemoteUrl with
            | Error hostError ->
                return errorResult hostError
            | Ok host ->
                match validateAndNormalizeTargetPathWithResolver resolveAbsolutePath targetPath with
                | Error pathError ->
                    return errorResult pathError
                | Ok normalizedTargetPath ->
                    match validateOptionalBranch branch with
                    | Error branchError ->
                        return errorResult branchError
                    | Ok safeBranch ->
                        try
                            let targetParent = dirname normalizedTargetPath
                            let! parentEnsureResult =
                                promise {
                                    let! parentKindResult = tryGetExistingPathKind targetParent

                                    match parentKindResult with
                                    | Error parentError ->
                                        return Error parentError
                                    | Ok (Some ExistingPathKind.Directory) ->
                                        return Ok ()
                                    | Ok (Some ExistingPathKind.Symlink) ->
                                        return Error(exn "Target parent path exists and is a symbolic link/junction (not a directory).")
                                    | Ok (Some ExistingPathKind.File)
                                    | Ok (Some ExistingPathKind.Other) ->
                                        return Error(exn "Target parent path exists and is not a directory.")
                                    | Ok None ->
                                        do! mkdirRecursiveAsync targetParent
                                        return Ok ()
                                }

                            match parentEnsureResult with
                            | Error parentError ->
                                return errorResult parentError
                            | Ok () ->
                                let! cloneTargetStateResult = getCloneTargetState normalizedTargetPath

                                match cloneTargetStateResult with
                                | Error cloneTargetError ->
                                    return errorResult cloneTargetError
                                | Ok cloneTargetState ->
                                    match ensureCloneTargetIsEmpty cloneTargetState with
                                    | Error _ ->
                                        return createNonEmptyTargetFailure ()
                                    | Ok () ->
                                        let cloneOptions = createOptions targetParent syncTimeout progress

                                        let! tokenResult =
                                            promise {
                                                try
                                                    let! tokenOption = tryGetAccessToken host
                                                    return Ok tokenOption
                                                with tokenError ->
                                                    return Error tokenError
                                            }

                                        match tokenResult with
                                        | Error tokenError ->
                                            return errorResult tokenError
                                        | Ok tokenOption ->
                                            match tokenOption |> Option.filter (fun token -> not (String.IsNullOrWhiteSpace token)) with
                                            | Some token ->
                                                let authenticatedGitResult =
                                                    try
                                                        Ok(applyAuth createGit cloneOptions host token)
                                                    with authError ->
                                                        Error authError

                                                match authenticatedGitResult with
                                                | Error authError ->
                                                    return errorResult authError
                                                | Ok authenticatedGit ->
                                                    let! authenticatedCloneResult =
                                                        cloneWithGit authenticatedGit safeRemoteUrl normalizedTargetPath safeBranch

                                                    match authenticatedCloneResult with
                                                    | Ok _ ->
                                                        return authenticatedCloneResult
                                                    | Error failure when shouldRetryWithoutAuth failure ->
                                                        let! cleanupResult =
                                                            cleanupCloneTargetForRetry normalizedTargetPath

                                                        match cleanupResult with
                                                        | Error cleanupError ->
                                                            return errorResult cleanupError
                                                        | Ok () ->
                                                            let unauthenticatedGit = createGit cloneOptions
                                                            return! cloneWithGit unauthenticatedGit safeRemoteUrl normalizedTargetPath safeBranch
                                                    | Error _ ->
                                                        return authenticatedCloneResult
                                            | None ->
                                                let unauthenticatedGit = createGit cloneOptions
                                                return! cloneWithGit unauthenticatedGit safeRemoteUrl normalizedTargetPath safeBranch
                        with error ->
                            return errorResult error
    }
