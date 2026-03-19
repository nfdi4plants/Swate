module Main.Git.GitService

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit
open Main.Git.GitTokenProvider
open Main.Git.GitAuthAdapter
open Main.Git.GitInternals

type GitFailureKind =
    | Unauthorized
    | Forbidden
    | Network
    | Timeout
    | Canceled
    | Unknown

type GitFailure = {
    Kind: GitFailureKind
    Message: string
}

type GitResult<'T> = Result<'T, GitFailure>

type GitFileStatusDto = {
    Path: string
    Index: string
    WorkingDir: string
    OriginalPath: string option
}

type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
    Files: GitFileStatusDto[]
}

type GitDiffSummaryDto = {
    Changed: int
    Insertions: int
    Deletions: int
}

type GitProgressCallback = GitInternals.GitProgressCallback

let private disallowedRemotePrefixes = [| "file://"; "ext::"; "fd::" |]

let private protocolOverridePattern =
    Regex("protocol\\.[^\\s=]+\\.(allow|deny)|(^|\\s)-c\\s+protocol\\.", RegexOptions.IgnoreCase)

let private remoteNamePattern = Regex("^[A-Za-z0-9._/-]+$")
let private invalidBranchCharactersPattern = Regex(@"[~^:?*\[\\\s]")

let classifyFailureKind (message: string) =
    let containsAny (terms: string[]) =
        terms
        |> Array.exists (fun term -> message.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)

    if containsAny [| "abort"; "cancelled"; "canceled"; "aborterror" |] then
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

let private createFailure kind message : GitFailure = { Kind = kind; Message = message }

let private toFailure (error: exn) : GitFailure =
    GitInternals.toFailure classifyFailureKind createFailure error

let private errorResult (error: exn) : GitResult<'T> =
    GitInternals.errorResult classifyFailureKind createFailure error

let private valueOrEmptyArray (items: 'T[]) = if isNull items then [||] else items

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

let private runSimpleGit (operation: ISimpleGit -> JS.Promise<'T>) (git: ISimpleGit) : JS.Promise<GitResult<'T>> =
    GitInternals.runSimpleGit toFailure operation git

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
                            let git = applyAuth createGit operationOptions host token
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

            let fileStatuses =
                valueOrEmptyArray status.files
                |> Array.map (fun fileStatus -> {
                    Path = fileStatus.path
                    Index = fileStatus.index
                    WorkingDir = fileStatus.working_dir
                    OriginalPath = fileStatus.``from``
                })

            return {
                Current = status.current
                Tracking = status.tracking
                Ahead = status.ahead
                Behind = status.behind
                IsClean = status.isClean ()
                Files = fileStatuses
            }
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
                                let! _ = git.pull (safeRemoteName)
                                return ()
                            | Some safeBranch ->
                                let! _ = git.pull (safeRemoteName, safeBranch)
                                return ()
                        })

                return result
    }

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
                let! result =
                    withAuthenticatedGit
                        arcPath
                        safeRemoteName
                        progressCallback
                        (fun git -> promise {
                            match safeBranchName with
                            | None ->
                                let! _ = git.push (safeRemoteName, "HEAD")
                                return ()
                            | Some safeBranch ->
                                let! _ = git.push (safeRemoteName, safeBranch)
                                return ()
                        })

                return result
    }

let stagePaths (arcPath: string) (pathSpecs: string[]) : JS.Promise<GitResult<unit>> = promise {
    match validatePathspecs pathSpecs with
    | Error validationError -> return errorResult validationError
    | Ok safePathSpecs ->
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! _ = git.add safePathSpecs
                    return ()
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

let commit (arcPath: string) (message: string) : JS.Promise<GitResult<string>> = promise {
    let normalizedMessage = message.Trim()

    if String.IsNullOrWhiteSpace normalizedMessage then
        return errorResult (exn "Commit message must not be empty.")
    else
        return!
            withLocalGit
                arcPath
                (fun git -> promise {
                    let! result = git.commit (normalizedMessage)

                    return
                        if String.IsNullOrWhiteSpace result.commit then
                            "Committed changes."
                        else
                            result.commit
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
