module Main.Git.GitProvisioningService

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Electron.Shared.GitTypes
open Main.Bindings.Node
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals
open Main.Git.GitTokenProvider

let private gitLfsDownloadLargeFilesConfigKey = "swate.lfs.downloadlargefiles"

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

let private formatDownloadLargeFilesConfigValue (downloadLargeFiles: bool) =
    if downloadLargeFiles then "true" else "false"

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
        Kind = GitFailureKind.Unknown
        Message = "Target path already exists and is not empty."
    }

let private createExistingRepositoryFailure () : GitService.GitResult<'T> =
    Error {
        Kind = GitFailureKind.Unknown
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

// Skip the LFS smudge filter during clone to avoid downloading all LFS objects upfront.
// The user's "Download Large Files" preference controls whether LFS content is hydrated
// after clone via a separate `git lfs pull` step (see hydrateClonedLfsContent).
let private applyCloneSkipSmudge (git: ISimpleGit) =
    git.env (GitLfsSkipSmudgeEnvKey, "1")

let private hydrateClonedLfsContent (git: ISimpleGit) : JS.Promise<GitService.GitResult<unit>> =
    runSimpleGit
        (fun currentGit -> promise {
            let! _ = currentGit.raw [| "lfs"; "pull" |]
            return ()
        })
        git

let private persistCloneDownloadPreference
    (repoPath: string)
    (downloadLargeFiles: bool)
    : JS.Promise<GitService.GitResult<unit>> =
    let repoOptions = createOptions repoPath standardTimeout None
    let repoGit = createGit repoOptions

    runSimpleGit
        (fun currentGit -> promise {
            let! _ =
                currentGit.raw [|
                    "config"
                    "--local"
                    gitLfsDownloadLargeFilesConfigKey
                    formatDownloadLargeFilesConfigValue downloadLargeFiles
                |]

            return ()
        })
        repoGit

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
    (downloadLargeFiles: bool)
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
                let! isLfsInstalled =
                    if downloadLargeFiles then
                        GitLfsService.isSystemInstalled ()
                    else
                        promise { return true }

                if downloadLargeFiles && not isLfsInstalled then
                    return
                        Error {
                            Kind = GitFailureKind.LfsInstallRequired
                            Message = "Git LFS is required for this operation. Install Git LFS now?"
                        }
                else
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

                                            let hydrateIfRequested tokenOption cloneResult =
                                                promise {
                                                    match cloneResult with
                                                    | Error _ ->
                                                        return cloneResult
                                                    | Ok _ ->
                                                        let! persistResult =
                                                            persistCloneDownloadPreference normalizedTargetPath downloadLargeFiles

                                                        match persistResult with
                                                        | Error failure ->
                                                            return Error failure
                                                        | Ok () when not downloadLargeFiles ->
                                                            return cloneResult
                                                        | Ok () ->
                                                            let repoOptions =
                                                                createOptions normalizedTargetPath syncTimeout progress

                                                            let hydrateGitResult =
                                                                try
                                                                    match
                                                                        tokenOption
                                                                        |> Option.filter (fun token -> not (String.IsNullOrWhiteSpace token))
                                                                    with
                                                                    | Some token ->
                                                                        Ok(
                                                                            applyAuth
                                                                                createGit
                                                                                repoOptions
                                                                                host
                                                                                token
                                                                                (Some "origin")
                                                                                (Some safeRemoteUrl)
                                                                        )
                                                                    | None ->
                                                                        Ok(createGit repoOptions)
                                                                with authError ->
                                                                    Error authError

                                                            match hydrateGitResult with
                                                            | Error authError ->
                                                                return errorResult authError
                                                            | Ok hydrateGit ->
                                                                let! hydrateResult = hydrateClonedLfsContent hydrateGit

                                                                match hydrateResult with
                                                                | Ok () ->
                                                                    return cloneResult
                                                                | Error failure ->
                                                                    return Error failure
                                                }

                                            match tokenResult with
                                            | Error tokenError ->
                                                return errorResult tokenError
                                            | Ok tokenOption ->
                                                match tokenOption |> Option.filter (fun token -> not (String.IsNullOrWhiteSpace token)) with
                                                | Some token ->
                                                    let authenticatedGitResult =
                                                        try
                                                            Ok(applyAuth createGit cloneOptions host token None (Some safeRemoteUrl))
                                                        with authError ->
                                                            Error authError

                                                    match authenticatedGitResult with
                                                    | Error authError ->
                                                        return errorResult authError
                                                    | Ok authenticatedGit ->
                                                        let configuredGit = applyCloneSkipSmudge authenticatedGit

                                                        let! authenticatedCloneResult =
                                                            cloneWithGit configuredGit safeRemoteUrl normalizedTargetPath safeBranch

                                                        match authenticatedCloneResult with
                                                        | _ ->
                                                            return! hydrateIfRequested (Some token) authenticatedCloneResult
                                                | None ->
                                                    let unauthenticatedGit =
                                                        createGit cloneOptions
                                                        |> applyCloneSkipSmudge

                                                    let! unauthenticatedCloneResult =
                                                        cloneWithGit unauthenticatedGit safeRemoteUrl normalizedTargetPath safeBranch

                                                    return! hydrateIfRequested None unauthenticatedCloneResult
                            with error ->
                                return errorResult error
    }
