module Main.Git.GitInternals

open System
open Fable.Core
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter

type GitProgressCallback = SimpleGitProgressEvent -> unit

// `internal` stays inside the Main assembly boundary (not accessible from shared/renderer projects).
let internal unsafeOptions =
    SimpleGitUnsafeOptions(
        allowUnsafeCustomBinary = false,
        allowUnsafeProtocolOverride = false,
        allowUnsafePack = false
    )

let internal standardTimeout =
    SimpleGitTimeoutOptions(
        block = 30000,
        stdOut = true,
        stdErr = true
    )

let internal syncTimeout =
    SimpleGitTimeoutOptions(
        block = 120000,
        stdOut = false,
        stdErr = false
    )

/// Creates the standard simple-git options used by Main Git services.
/// maxConcurrentProcesses is intentionally one to serialize commands for a repository-scoped instance.
let internal createOptions
    (baseDir: string)
    (timeout: SimpleGitTimeoutOptions)
    (progressCallback: GitProgressCallback option)
    =
    let options =
        SimpleGitOptions(
            baseDir = baseDir,
            binary = U3.Case1 "git",
            maxConcurrentProcesses = 1,
            timeout = timeout,
            ``unsafe`` = unsafeOptions
        )

    progressCallback
    |> Option.iter (fun progress -> options.progress <- Some progress)

    options

/// Creates a simple-git instance with non-interactive prompt suppression applied.
let internal createGit (options: SimpleGitOptions) : ISimpleGit =
    SimpleGit.create options |> applyNonInteractiveEnv

/// Converts exceptions from simple-git/spawned Git into the service-specific failure record.
/// Messages are redacted here before they can cross IPC.
let internal toFailure
    (classifyFailureKind: string -> 'GitFailureKind)
    (createFailure: 'GitFailureKind -> string -> 'GitFailure)
    (error: exn)
    : 'GitFailure =
    let message =
        error.Message
        |> Option.ofObj
        |> Option.filter (fun m -> not (String.IsNullOrWhiteSpace m))
        |> Option.defaultValue (string error)
        |> redactToken

    createFailure (classifyFailureKind message) message

/// Convenience wrapper for returning a redacted, classified failure as Result.Error.
let internal errorResult
    (classifyFailureKind: string -> 'GitFailureKind)
    (createFailure: 'GitFailureKind -> string -> 'GitFailure)
    (error: exn)
    : Result<'T, 'GitFailure> =
    Error(toFailure classifyFailureKind createFailure error)

/// Runs a simple-git operation and maps thrown exceptions into the caller's GitResult shape.
/// Services use this boundary so validation and command failures are reported consistently.
let internal runSimpleGit
    (toFailure: exn -> 'GitFailure)
    (operation: ISimpleGit -> JS.Promise<'T>)
    (git: ISimpleGit)
    : JS.Promise<Result<'T, 'GitFailure>> =
    promise {
        try
            let! result = operation git
            return Ok result
        with error ->
            return Error(toFailure error)
    }
