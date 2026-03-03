module Main.Git.GitProvisioningService

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals
open Main.Git.GitTokenProvider

let private fsDynamic: obj = importAll "fs"
let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

type CloneTargetState =
    | Missing
    | ExistingEmpty
    | ExistingNonEmpty

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

let shouldRetryWithoutAuth (failureKind: GitService.GitFailureKind) =
    match failureKind with
    | GitService.GitFailureKind.Unauthorized
    | GitService.GitFailureKind.Forbidden -> true
    | GitService.GitFailureKind.Network
    | GitService.GitFailureKind.Timeout
    | GitService.GitFailureKind.Canceled
    | GitService.GitFailureKind.Unknown -> false

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

let private clearDirectoryContentsAsync (directoryPath: string) : JS.Promise<unit> =
    promise {
        let! entries = fsPromisesDynamic?readdir(directoryPath) |> unbox<JS.Promise<string[]>>
        let safeEntries = if isNull entries then [||] else entries

        for entry in safeEntries do
            let entryPath = pathDynamic?join(directoryPath, entry) |> unbox<string>
            do! removeRecursiveAsync entryPath
    }

let private cleanupCloneTargetForRetry
    (initialTargetState: CloneTargetState)
    (targetPath: string)
    : JS.Promise<Result<unit, exn>> =
    promise {
        try
            match initialTargetState with
            | CloneTargetState.Missing ->
                do! removeRecursiveAsync targetPath
            | CloneTargetState.ExistingEmpty ->
                do! clearDirectoryContentsAsync targetPath
            | CloneTargetState.ExistingNonEmpty ->
                ()

            return Ok ()
        with error ->
            return Error(exn $"Failed to clean clone target before retry: {error.Message}")
    }

let private getCloneTargetState (targetPath: string) : JS.Promise<CloneTargetState> =
    promise {
        let targetExists = fsDynamic?existsSync(targetPath) |> unbox<bool>

        if targetExists then
            let! entries = fsPromisesDynamic?readdir(targetPath) |> unbox<JS.Promise<obj[]>>
            let entryCount = if isNull entries then 0 else entries.Length
            return classifyCloneTargetState true entryCount
        else
            return CloneTargetState.Missing
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
                let targetExists = fsDynamic?existsSync(normalizedTargetPath) |> unbox<bool>

                let! preInitResult =
                    if targetExists then
                        promise {
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
                        }
                    else
                        promise {
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
                            do! mkdirRecursiveAsync targetParent

                            let! cloneTargetState = getCloneTargetState normalizedTargetPath

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
                                            | Error failure when shouldRetryWithoutAuth failure.Kind ->
                                                let! cleanupResult =
                                                    cleanupCloneTargetForRetry cloneTargetState normalizedTargetPath

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
