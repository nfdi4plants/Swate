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
    let temporaryImportPattern = """(^|/)\.swate-import-[0-9a-fA-F]{32}(/|$)"""
    let temporaryLfsBackupPattern = """\.vcs-lfs-backup-[0-9a-fA-F]{32}$"""

    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
    || isGitMetadataPath normalizedPath
    || isLegacyDataMapPath normalizedPath
    || System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, temporaryImportPattern)
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

            match segments with
            | [||] -> false
            | [| rootName |] ->
                let structuralRootNames = [
                    ARCtrl.ArcPathHelper.InvestigationFileName
                    ARCtrl.ArcPathHelper.StudiesFolderName
                    ARCtrl.ArcPathHelper.AssaysFolderName
                    ARCtrl.ArcPathHelper.WorkflowsFolderName
                    ARCtrl.ArcPathHelper.RunsFolderName
                    ".gitattributes"
                    "README.md"
                ]

                PathHelpers.pathMatchesAny structuralRootNames rootName |> not
            | [| zoneName; _ |] ->
                let zoneNames = [
                    ARCtrl.ArcPathHelper.StudiesFolderName
                    ARCtrl.ArcPathHelper.AssaysFolderName
                    ARCtrl.ArcPathHelper.WorkflowsFolderName
                    ARCtrl.ArcPathHelper.RunsFolderName
                ]

                PathHelpers.pathMatchesAny zoneNames zoneName |> not
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

/// Creates a native, shallow watcher for loaded FileTree directories and captures readiness.
/// Chokidar emits `ready` once after its initial scan, not after later `add` calls.
let createLoadedDirectoryWatcherWithReady (arcPath: string) (paths: string[]) (onError: exn -> unit) =
    let watcher =
        Chokidar.Chokidar.watch (
            paths,
            Chokidar.WatchOptions(
                cwd = arcPath,
                awaitWriteFinish = true,
                ignored = !^isFileWatcherPathIgnored,
                ignoreInitial = true,
                usePolling = false,
                depth = 0
            )
        )

    watcher, captureWatcherReadiness onError watcher
