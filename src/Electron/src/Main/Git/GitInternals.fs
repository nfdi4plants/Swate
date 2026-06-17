module Main.Git.GitInternals

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Electron.Shared.GitTypes
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter

type GitProgressCallback = GitProgressDto -> unit

let internal createProgressDto methodName stage progress processed total output : GitProgressDto = {
    Method = methodName
    Stage = stage
    Progress = progress
    Processed = processed
    Total = total
    Output = output
}

let internal progressFromSimpleGit (progressEvent: SimpleGitProgressEvent) =
    createProgressDto
        (Some progressEvent.method)
        (Some progressEvent.stage)
        (Some progressEvent.progress)
        (Some progressEvent.processed)
        (Some progressEvent.total)
        None

let internal reportPhase (progressCallback: GitProgressCallback option) methodName stage =
    progressCallback
    |> Option.iter (fun report -> createProgressDto (Some methodName) (Some stage) None None None None |> report)

let internal reportOutputText (progressCallback: GitProgressCallback option) (text: string) =
    progressCallback
    |> Option.iter (fun report ->
        let output = text |> Option.ofObj |> Option.defaultValue String.Empty |> redactToken

        if not (String.IsNullOrEmpty output) then
            createProgressDto None None None None None (Some output) |> report
    )

[<Emit("$0 != null && typeof $0.on === 'function'")>]
let private hasStreamListener (_stream: obj) : bool = jsNative

[<Emit("$0.outputHandler(function(command, stdout, stderr, args) { $1(command, stdout, stderr, args); })")>]
let private attachOutputHandler (_git: ISimpleGit) (_handler: string -> obj -> obj -> string[] -> unit) : ISimpleGit =
    jsNative

let internal withGitOutputProgress (progressCallback: GitProgressCallback option) (git: ISimpleGit) =
    match progressCallback with
    | None -> git
    | Some _ ->
        attachOutputHandler
            git
            (fun (_command: string) (stdout: obj) (stderr: obj) (_args: string[]) ->
                let handleChunk chunk =
                    reportOutputText progressCallback (string chunk)

                if hasStreamListener stdout then
                    stdout?on ("data", handleChunk) |> ignore

                if hasStreamListener stderr then
                    stderr?on ("data", handleChunk) |> ignore
            )

// `internal` stays inside the Main assembly boundary (not accessible from shared/renderer projects).
let internal unsafeOptions =
    SimpleGitUnsafeOptions(
        allowUnsafeCustomBinary = false,
        allowUnsafeProtocolOverride = false,
        allowUnsafePack = false
    )

let internal standardTimeout =
    SimpleGitTimeoutOptions(block = 30000, stdOut = true, stdErr = true)

let internal syncTimeout =
    SimpleGitTimeoutOptions(block = 120000, stdOut = true, stdErr = true)

/// Creates the standard simple-git options used by Main Git services.
/// maxConcurrentProcesses is intentionally one to serialize commands for a repository-scoped instance.
let internal createOptions
    (baseDir: string)
    (timeout: SimpleGitTimeoutOptions)
    (progressCallback: GitProgressCallback option)
    =
    let progressHandler =
        progressCallback
        |> Option.map (fun progress -> progressFromSimpleGit >> progress)

    match progressHandler with
    | Some handler ->
        SimpleGitOptions(
            baseDir = baseDir,
            binary = U3.Case1 "git",
            maxConcurrentProcesses = 1,
            timeout = timeout,
            ``unsafe`` = unsafeOptions,
            progress = handler
        )
    | None ->
        SimpleGitOptions(
            baseDir = baseDir,
            binary = U3.Case1 "git",
            maxConcurrentProcesses = 1,
            timeout = timeout,
            ``unsafe`` = unsafeOptions
        )

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
