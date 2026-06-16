module Main.Git.GitInternals

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Swate.Electron.Shared.GitTypes
open Main.Bindings.SimpleGit
open Main.Git.GitAuthAdapter

type GitProgressCallback = GitProgressDto -> unit

let private gitLfsProgressPattern =
    Regex(
        @"^\s*(?<stage>[^:\r\n]+):\s*(?<percent>\d+(?:\.\d+)?)%\s+\((?<processed>\d+(?:\.\d+)?)/(?<total>\d+(?:\.\d+)?)\)",
        RegexOptions.IgnoreCase
    )

let internal createProgressDto methodName stage progress processed total : GitProgressDto = {
    Method = methodName
    Stage = stage
    Progress = progress
    Processed = processed
    Total = total
}

let private parseFloat (value: string) =
    let parsed: float = emitJsExpr value "Number.parseFloat($0)"

    if Double.IsNaN parsed then None else Some parsed

let internal progressFromSimpleGit (progressEvent: SimpleGitProgressEvent) =
    createProgressDto
        (Some progressEvent.method)
        (Some progressEvent.stage)
        (Some progressEvent.progress)
        (Some progressEvent.processed)
        (Some progressEvent.total)

let internal reportPhase (progressCallback: GitProgressCallback option) methodName stage =
    progressCallback
    |> Option.iter (fun report -> createProgressDto (Some methodName) (Some stage) None None None |> report)

let internal tryParseGitLfsProgressMessage (message: string) =
    let matchResult =
        message
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> gitLfsProgressPattern.Match

    if not matchResult.Success then
        None
    else
        match
            parseFloat matchResult.Groups["percent"].Value,
            parseFloat matchResult.Groups["processed"].Value,
            parseFloat matchResult.Groups["total"].Value
        with
        | Some progress, Some processed, Some total ->
            Some(
                createProgressDto
                    (Some "lfs")
                    (Some(matchResult.Groups["stage"].Value.Trim()))
                    (Some progress)
                    (Some processed)
                    (Some total)
            )
        | _ -> None

let internal reportGitLfsProgressText (progressCallback: GitProgressCallback option) (text: string) =
    progressCallback
    |> Option.iter (fun report ->
        text
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> fun value -> value.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.choose tryParseGitLfsProgressMessage
        |> Array.iter report
    )

let internal withGitLfsOutputProgress (progressCallback: GitProgressCallback option) (git: ISimpleGit) =
    match progressCallback with
    | None -> git
    | Some _ ->
        git.outputHandler (fun (_command: string) (stdout: obj) (stderr: obj) (_args: string[]) ->
            let handleChunk chunk =
                reportGitLfsProgressText progressCallback (string chunk)

            stdout?on ("data", handleChunk) |> ignore
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
