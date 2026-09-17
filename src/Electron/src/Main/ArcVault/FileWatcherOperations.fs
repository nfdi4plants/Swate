/// Stateful file-watcher lifecycle and scheduling operations that remain independent of ArcVault.
/// ArcVault-specific wiring and public extensions belong in FileWatcher.
module Main.FileWatcherOperations

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Main.ArcVaultHelper
open Main.ArcVaultTypes
open Main.Bindings
open Swate.Components.Shared

type PendingSchedulerContext = {
    IsInitializing: unit -> bool
    IsBusyWriting: unit -> bool
    HasActiveImport: unit -> bool
    PendingEvents: ResizeArray<ArcVaultFileSystemEvent>
    PendingArcMergeEvents: ResizeArray<ArcVaultFileSystemEvent>
    GetTimeout: unit -> int option
    SetTimeout: int option -> unit
    ChangeQueuedBatchCount: int -> unit
    GetQueuedBatchCount: unit -> int
    ApplyPendingEvents: ArcVaultFileSystemEvent list -> ArcVaultFileSystemEvent list -> JS.Promise<unit>
    SetLoadingChanges: bool -> unit
    LogError: exn -> unit
}

let rec schedulePendingEvents (context: PendingSchedulerContext) =
    if not (context.IsInitializing()) && context.PendingEvents.Count > 0 then
        context.GetTimeout() |> Option.iter JS.clearTimeout

        if context.GetTimeout().IsNone then
            context.SetLoadingChanges true

        let timeoutId =
            JS.setTimeout
                (fun () ->
                    context.SetTimeout None

                    if context.IsBusyWriting() && context.HasActiveImport() then
                        schedulePendingEvents context
                    else
                        let pendingEvents = context.PendingEvents |> Seq.toList
                        let pendingArcMergeEvents = context.PendingArcMergeEvents |> Seq.toList
                        context.PendingEvents.Clear()
                        context.PendingArcMergeEvents.Clear()
                        context.ChangeQueuedBatchCount 1

                        promise {
                            try
                                try
                                    do! context.ApplyPendingEvents pendingEvents pendingArcMergeEvents
                                with error ->
                                    context.LogError error
                            finally
                                context.ChangeQueuedBatchCount -1

                            if
                                context.GetQueuedBatchCount() = 0
                                && context.GetTimeout().IsNone
                                && context.PendingEvents.Count = 0
                            then
                                context.SetLoadingChanges false
                        }
                        |> Promise.start
                )
                500

        context.SetTimeout(Some timeoutId)

let ensurePayloadWatcher arcPath usePolling (expandedPaths: Set<string>) getWatcher setWatcher createContext = promise {
    if getWatcher () |> Option.isNone && not expandedPaths.IsEmpty then
        let watchedPaths =
            expandedPaths
            |> Seq.map PathHelpers.normalizeCanonicalRelativePath
            |> Seq.toArray

        let ignoreFn =
            fun (path: string) (_stats: Filesystem.Stats) ->
                shouldIgnoreForPayloadWatcher arcPath expandedPaths.Contains path

        let ignored: U4<string, ResizeArray<string>, string -> bool, System.Func<string, Filesystem.Stats, bool>> =
            !^(System.Func<string, Filesystem.Stats, bool>(ignoreFn))

        let watcher =
            Chokidar.Chokidar.watch (watchedPaths, createWatcherOptions arcPath usePolling ignored (Some 0))

        watcher.on (Chokidar.Events.All, WatcherHelpers.fileEventController (createContext ()))
        |> ignore

        setWatcher (Some watcher)

        try
            do! waitForFileWatcherReady watcher
        with error ->
            setWatcher None

            try
                do! watcher.close ()
            with _ ->
                ()

            return raise error
}

let restartPayloadWatcher
    arcPath
    usePolling
    expandedPaths
    (getWatcher: unit -> Chokidar.IWatcher option)
    setWatcher
    createContext
    =
    promise {
        match getWatcher () with
        | Some watcher ->
            setWatcher None

            try
                do! watcher.close ()
            with _ ->
                ()
        | None -> ()

        do! ensurePayloadWatcher arcPath usePolling expandedPaths getWatcher setWatcher createContext
    }

let startAndWaitUntilReady (getWatcher: unit -> Chokidar.IWatcher option) startWatcher arcPath usePolling = promise {
    let shouldWaitForReady = getWatcher () |> Option.isNone
    startWatcher usePolling

    if shouldWaitForReady then
        do! waitForFileWatcherReady (getWatcher () |> Option.get)
        let! structuralEvents = reconcileArcStructureScope arcPath ""
        WatcherHelpers.attachArcStructureScopes (getWatcher ()) structuralEvents
}
