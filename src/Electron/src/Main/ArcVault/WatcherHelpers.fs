/// Pure and reusable file-watcher event, path, and ARC-structure reconciliation helpers.
/// Stateful watcher lifecycle and scheduling belong in FileWatcherOperations.
module Main.WatcherHelpers

open System
open System.Collections.Generic
open Fable.Core
open Fable.Electron
open Main.Bindings
open Main.ArcMerge
open Main.ARCtrlExtensions
open Main.ArcVaultTypes
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

type FileWatcherControllerContext = {
    ArcPath: unit -> string option
    Watcher: unit -> Chokidar.IWatcher option
    ImportedPaths: HashSet<string>
    IsActiveImport: unit -> bool
    IsArcMergeEligible: unit -> bool
    PendingEvents: ResizeArray<ArcVaultFileSystemEvent>
    PendingArcMergeEvents: ResizeArray<ArcVaultFileSystemEvent>
    SchedulePendingEvents: unit -> unit
    LogEvent: string -> string -> unit
    LogReconciliationError: string -> exn -> unit
}

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

/// Retains only the final event for each case-sensitive normalized path while preserving final-event order.
let coalesceEventsByPath (events: ArcVaultFileSystemEvent list) =
    events
    |> List.rev
    |> List.distinctBy (fun event -> PathHelpers.normalizePath event.AbsolutePath)
    |> List.rev

/// True when an event can change the in-memory ARC model. Payload events still update the FileTree.
let isArcMergeRelevant (event: ArcVaultFileSystemEvent) =
    if
        eventNameEquals Chokidar.Events.Add event.EventName
        || eventNameEquals Chokidar.Events.Change event.EventName
        || eventNameEquals Chokidar.Events.Unlink event.EventName
    then
        isArcModelReadContractPath event.RelativePath
    elif eventNameEquals Chokidar.Events.UnlinkDir event.EventName then
        event.RelativePath
        |> ArcEntityPathRules.buildFallbackUnlinkPaths
        |> List.isEmpty
        |> not
    else
        false

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

        if isArcMergeRelevant watcherEvent && isArcMergeEligible watcherEvent then
            pendingArcMergeEvents.Add watcherEvent
    | None -> ()

let queueArcVaultFileWatcherEvent (context: FileWatcherControllerContext) eventName path =
    queueFileWatcherEvent
        (fun event ->
            let importedPathKey =
                PathHelpers.normalizePath (event.AbsolutePath.ToLowerInvariant())

            let isImportedPath = context.ImportedPaths.Remove importedPathKey

            not isImportedPath && (context.IsActiveImport() || context.IsArcMergeEligible())
        )
        (context.ArcPath())
        context.PendingEvents
        context.PendingArcMergeEvents
        eventName
        path

let attachArcStructureScopes (watcher: Chokidar.IWatcher option) (events: (string * string) array) =
    match watcher with
    | None -> ()
    | Some watcher ->
        events
        |> Array.choose (fun (eventName, relativePath) ->
            if
                eventNameEquals Chokidar.Events.AddDir eventName
                && ArcVaultHelper.isArcStructureWatchScopePath relativePath
            then
                Some(PathHelpers.normalizeCanonicalRelativePath relativePath)
            else
                None
        )
        |> function
            | [||] -> ()
            | scopes -> watcher.add scopes |> ignore

let reconcileArcStructureScope (context: FileWatcherControllerContext) arcPath relativeScopePath = promise {
    let! events = ArcVaultHelper.reconcileArcStructureScope arcPath relativeScopePath
    attachArcStructureScopes (context.Watcher()) events

    for eventName, relativePath in events do
        queueArcVaultFileWatcherEvent context eventName relativePath

    context.SchedulePendingEvents()
}

let fileEventController (context: FileWatcherControllerContext) =
    fun (eventName: string) (path: string) ->
        context.LogEvent eventName path

        if eventNameEquals Chokidar.Events.AddDir eventName then
            match context.ArcPath() with
            | Some arcPath ->
                match ArcVaultHelper.tryGetWatcherRelativePath arcPath path with
                | Some relativePath when ArcVaultHelper.isArcStructureWatchScopePath relativePath ->
                    promise {
                        try
                            context.Watcher()
                            |> Option.iter (fun watcher ->
                                watcher.add (PathHelpers.normalizeCanonicalRelativePath relativePath) |> ignore
                            )

                            do! reconcileArcStructureScope context arcPath relativePath
                        with reconciliationError ->
                            context.LogReconciliationError relativePath reconciliationError
                    }
                    |> Promise.start
                | _ -> ()
            | None -> ()

        queueArcVaultFileWatcherEvent context eventName path
        context.SchedulePendingEvents()

/// Converts raw filesystem events into ARC merge events; unlink-dir events expand to possible canonical files.
let toArcMergeEvents (events: ArcVaultFileSystemEvent list) =
    events
    |> List.filter isArcMergeRelevant
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
