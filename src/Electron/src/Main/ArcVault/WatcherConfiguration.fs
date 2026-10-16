module Main.WatcherConfiguration

open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

let shouldUsePollingByDefault (platform: string) =
    System.String.Equals(platform, "win32", System.StringComparison.OrdinalIgnoreCase)

let private currentNodePlatform () : string =
    emitJsExpr () "process.platform" |> unbox<string>

let isFileWatcherPathIgnored (path: string) =
    let normalizedPath = PathHelpers.normalizeSeparators path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    let temporaryLfsBackupPattern = """\.vcs-lfs-backup-[0-9a-fA-F]{32}$"""

    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
    || isGitMetadataPath normalizedPath
    || isLegacyDataMapPath normalizedPath
    || isTemporaryImportPath normalizedPath
    || System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, temporaryLfsBackupPattern)

let isPermanentFileWatcherPathIgnored (arcPath: string) (path: string) =
    if isFileWatcherPathIgnored path then
        true
    else
        match tryGetRepoRelativePathOrRoot arcPath path with
        | None -> true
        | Some relativePath ->
            let segments =
                (PathHelpers.normalizeCanonicalRelativePath relativePath)
                    .Split([| '/' |], System.StringSplitOptions.RemoveEmptyEntries)

            // The permanent watcher observes ARC metadata only. The root and payload folders shown in
            // File Explorer are observed by the loaded-directory watchers.
            match segments with
            | [||] -> false
            | [| rootName |] -> not (ArcEntityPathRules.isStructuralRootName rootName)
            | [| zoneName; _ |] -> not (ArcEntityPathRules.isAddZoneSegment zoneName)
            | _ -> ArcEntityPathRules.tryParseCanonicalArcFileTarget relativePath |> Option.isNone

let private captureWatcherReadiness (onError: exn -> unit) (watcher: Chokidar.IWatcher) =
    JS.Constructors.Promise.Create(fun resolve _ ->
        let mutable isSettled = false

        watcher.on (
            Chokidar.Events.Ready,
            fun (_: string) ->
                if not isSettled then
                    isSettled <- true
                    resolve (Ok())
        )
        |> ignore

        watcher.on (
            Chokidar.Events.Error,
            fun (error: exn) ->
                onError error

                if not isSettled then
                    isSettled <- true
                    resolve (Error error)
        )
        |> ignore
    )

/// Creates the permanent watcher and captures readiness before returning it.
let createFileWatcherWithReady (path: string) (usePolling: bool option) (onError: exn -> unit) =

    // Native Windows file events can keep handles that block app-initiated folder renames.
    let usePolling =
        defaultArg usePolling (shouldUsePollingByDefault (currentNodePlatform ()))

    let watcherOptions =
        if usePolling then
            Chokidar.WatchOptions(
                cwd = path,
                awaitWriteFinish = true,
                ignored = !^(isPermanentFileWatcherPathIgnored path),
                ignoreInitial = true,
                usePolling = true,
                interval = 200,
                binaryInterval = 400
            )
        else
            Chokidar.WatchOptions(
                cwd = path,
                awaitWriteFinish = true,
                ignored = !^(isPermanentFileWatcherPathIgnored path),
                ignoreInitial = true
            )

    let watcher = Chokidar.Chokidar.watch (path, watcherOptions)

    watcher, captureWatcherReadiness onError watcher

/// Polling interval of the File Explorer directory observation.
let directoryPollIntervalMilliseconds = 500

/// Observes a directory through its modification time, which changes whenever a direct child is
/// added, removed or renamed. Only stat calls are made, so no handle stays open on the directory and
/// the cost does not grow with the number of entries, unlike a native watcher, which keeps one handle
/// per entry.
/// A directory that disappears is reported as one change, and its absence becomes the new baseline.
/// onChange returns whether it handled the change; an unhandled change keeps the previous baseline, so
/// it is reported again by the next poll instead of being lost.
/// Returns the stop function and a readiness promise that settles once the baseline is taken.
let createDirectoryModificationPoller
    (directoryPath: string)
    (intervalMilliseconds: int)
    (onChange: unit -> bool)
    (onError: exn -> unit)
    =
    let mutable stopped = false
    let mutable timeout: int option = None
    // None until the baseline is taken; Some None while the directory does not exist.
    let mutable baseline: float option option = None

    let observe () = promise {
        try
            let! stats = Filesystem.statAsync directoryPath
            return Ok(Some stats.mtimeMs)
        with error ->
            match Filesystem.tryGetNodeErrorCode error with
            | Some "ENOENT"
            | Some "ENOTDIR" -> return Ok None
            | _ -> return Error error
    }

    let poll () = promise {
        match! observe () with
        | Ok observed ->
            match baseline with
            | Some previous when previous <> observed ->
                if not stopped && onChange () then
                    baseline <- Some observed
            | Some _ -> ()
            | None -> baseline <- Some observed

            return Ok()
        | Error error ->
            if not stopped then
                onError error

            return Error error
    }

    let rec schedule () =
        if not stopped then
            timeout <-
                Some(
                    JS.setTimeout
                        (fun () -> poll () |> Promise.map (fun _ -> schedule ()) |> Promise.start)
                        intervalMilliseconds
                )

    let ready = promise {
        let! baseline = poll ()

        match baseline with
        | Ok() -> schedule ()
        | Error _ -> ()

        return baseline
    }

    let stop () =
        stopped <- true
        timeout |> Option.iter JS.clearTimeout
        timeout <- None
        JS.Constructors.Promise.resolve ()

    stop, ready
