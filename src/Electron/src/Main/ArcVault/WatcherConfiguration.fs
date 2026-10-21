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

/// Observes the direct children of a directory with one native watcher, so the cost does not grow with
/// the number of entries. Adding, removing or renaming a child is reported through onChange, and so is
/// an event without a child name, which means the system dropped events. A content change of a child
/// is not reported.
/// The watcher ends when the directory itself is removed or moved, which Windows reports as a stream of
/// events that name the directory instead of a child, or when the system reports an error. It then
/// closes itself and calls onLost once: a native watcher never follows a directory that is created again
/// at the same path.
/// Returns the stop function. Creating the watcher of a missing directory throws.
let createDirectoryWatcherWith
    (startWatch: string -> (Filesystem.WatchEventType -> string option -> unit) -> Filesystem.FSWatcher)
    (directoryPath: string)
    (onChange: unit -> unit)
    (onLost: exn -> unit)
    =
    let mutable active = true
    let mutable watcher: Filesystem.FSWatcher option = None

    let stop () =
        if active then
            active <- false
            watcher |> Option.iter (fun watcher -> watcher.close ())

    let lose (reason: exn) =
        if active then
            stop ()
            onLost reason

    // A non-recursive watcher reports child names only, so a name with a separator is the directory itself.
    let isChildName (name: string) =
        not (name.Contains "/" || name.Contains "\\")

    let created =
        startWatch
            directoryPath
            (fun eventType name ->
                if active then
                    match eventType, name with
                    | _, Some name when not (isChildName name) ->
                        lose (exn $"The watched directory '{directoryPath}' was removed or moved.")
                    | Filesystem.WatchEventType.Change, Some _ -> ()
                    | _ -> onChange ()
            )

    watcher <- Some created
    created.on ("error", lose) |> ignore
    stop

/// Observes the direct children of a directory with a native fs.watch; see createDirectoryWatcherWith.
let createDirectoryWatcher =
    createDirectoryWatcherWith (fun path listener -> Filesystem.NativeWatch.watch (path, listener))
