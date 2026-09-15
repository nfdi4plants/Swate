module Main.WatcherHelpers

open System
open System.Collections.Generic
open Fable.Electron
open Main.Bindings
open Main.ArcMerge
open Main.ArcVaultTypes
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

let eventNameEquals (expected: Chokidar.Events) (actual: string) =
    String.Equals(actual, expected.ToString(), StringComparison.OrdinalIgnoreCase)

/// Builds an ARC-root-relative watcher event from the raw chokidar payload.
let buildWatcherEvent (arcPath: string) (eventName: string) (path: string) =
    let normalizedPath = PathHelpers.normalizePath path
    let normalizedArcPath = PathHelpers.normalizePath arcPath

    let relativePath =
        match tryGetRepoRelativePath arcPath normalizedPath with
        | Some path -> PathHelpers.normalizePath path
        | None ->
            if PathHelpers.isSameOrDescendantPath normalizedPath normalizedArcPath then
                let prefix = normalizedArcPath + "/"

                if normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                    normalizedPath.Substring(prefix.Length)
                else
                    ""
            else
                normalizedPath

    let absolutePath =
        if PathHelpers.isSameOrDescendantPath normalizedPath normalizedArcPath then
            normalizedPath
        else
            $"{normalizedArcPath}/{relativePath}" |> PathHelpers.normalizePath

    {
        EventName = eventName
        RelativePath = relativePath
        AbsolutePath = absolutePath
    }

let createImportedFileWatcherEvents arcPath targetRelativePath sourceAbsolutePaths =
    let targetRelativePath =
        PathHelpers.normalizeCanonicalRelativePath targetRelativePath

    sourceAbsolutePaths
    |> Array.map (fun sourcePath ->
        let fileName = basename sourcePath

        let relativePath =
            if String.IsNullOrWhiteSpace targetRelativePath then
                fileName
            else
                $"{targetRelativePath}/{fileName}"
            |> PathHelpers.normalizePath

        buildWatcherEvent arcPath (Chokidar.Events.Add.ToString()) relativePath
    )

/// Always queues a watcher event for the file tree, while ARC merge eligibility is controlled separately.
let queueFileWatcherEvent
    isArcMergeEligible
    arcPath
    (pendingEvents: ResizeArray<ArcVaultFileSystemEvent>)
    (pendingArcMergeEvents: ResizeArray<ArcVaultFileSystemEvent>)
    eventName
    changedPath
    =
    match arcPath with
    | Some rootPath ->
        let watcherEvent = buildWatcherEvent rootPath eventName changedPath
        pendingEvents.Add watcherEvent

        if isArcMergeEligible watcherEvent then
            pendingArcMergeEvents.Add watcherEvent
    | None -> ()

/// Converts raw filesystem events into ARC merge events; unlink-dir events expand to possible canonical files.
let toArcMergeEvents (events: ArcVaultFileSystemEvent list) =
    events
    |> List.collect (fun event ->
        if eventNameEquals Chokidar.Events.Add event.EventName then
            [
                {
                    EventName = EventName.Add
                    Path = event.RelativePath
                }
            ]
        elif eventNameEquals Chokidar.Events.Change event.EventName then
            [
                {
                    EventName = EventName.Change
                    Path = event.RelativePath
                }
            ]
        elif eventNameEquals Chokidar.Events.Unlink event.EventName then
            [
                {
                    EventName = EventName.Unlink
                    Path = event.RelativePath
                }
            ]
        elif eventNameEquals Chokidar.Events.UnlinkDir event.EventName then
            event.RelativePath
            |> ArcEntityPathRules.buildFallbackUnlinkPaths
            |> List.map (fun path -> {
                EventName = EventName.Unlink
                Path = path
            })
        else
            []
    )
