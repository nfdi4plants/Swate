/// Owns File Explorer expansion state and its bounded payload watcher.
[<AutoOpen>]
module Main.ArcVaultFileTree

open System
open System.Collections.Generic
open ARCtrl
open Fable.Core
open Main
open Main.ArcVault
open Main.ArcVaultHelper
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

type ArcVault with

    /// Reconciles the complete set of directories that are actively expanded and visible in the
    /// renderer. Added scopes are watched and scanned shallowly; removed scopes do not need to exist.
    member this.SetActiveFileTreeDirectories(relativePaths: string[], ?refreshPaths: string[]) =
        let capturedWatcherEpoch = this.WatcherEpoch

        let queuedUpdate =
            this.FileTreeUpdateTail
            |> Promise.bind (fun () -> promise {
                match this.path with
                | None -> return raise (ArcVaultHelper.arcNotOpenError ())
                | Some _ when capturedWatcherEpoch <> this.WatcherEpoch -> ()
                | Some arcPath ->
                    let normalizeRelativePath relativePath =
                        if
                            isAbsolute relativePath
                            || PathHelpers.containsPathTraversalSegments relativePath
                        then
                            raise (exn "Expanded directories must be ARC-relative paths.")

                        let normalizedRelativePath = PathHelpers.normalizeCanonicalRelativePath relativePath

                        let absolutePath =
                            ArcPathHelper.combine arcPath normalizedRelativePath
                            |> PathHelpers.normalizePath

                        match tryGetRepoRelativePathOrRoot arcPath absolutePath with
                        | None -> raise (exn "Expanded directories must stay inside the open ARC.")
                        | Some _ -> normalizedRelativePath

                    let normalizedPaths =
                        relativePaths |> Array.map normalizeRelativePath |> Set.ofArray

                    let requestedRefreshPaths =
                        defaultArg refreshPaths [||] |> Array.map normalizeRelativePath |> Set.ofArray

                    let requestedAdded = Set.difference normalizedPaths this.expandedDirectoryPaths
                    let validAdded = ResizeArray<string>()

                    for relativePath in requestedAdded do
                        let absolutePath =
                            ArcPathHelper.combine arcPath relativePath |> PathHelpers.normalizePath

                        try
                            let! stats = statAsync absolutePath

                            if stats.isDirectory () then
                                validAdded.Add relativePath
                            else
                                swatelogfn
                                    this.window.id
                                    "Ignoring non-directory File Explorer scope '%s'."
                                    relativePath
                        with validationError ->
                            swatelogfn
                                this.window.id
                                "Ignoring unavailable File Explorer scope '%s': %s"
                                relativePath
                                validationError.Message

                    let nextActivePaths =
                        Set.difference normalizedPaths requestedAdded
                        |> Set.union (validAdded |> Set.ofSeq)

                    let newlyExpandedScopes = Set.difference nextActivePaths this.expandedDirectoryPaths

                    let scopesToScan =
                        Set.union newlyExpandedScopes (Set.intersect requestedRefreshPaths nextActivePaths)

                    this.expandedDirectoryPaths <- nextActivePaths

                    do! this.ReconcilePayloadWatcherScopes arcPath

                    let shallowScans = ResizeArray<string * FileEntry[]>()

                    for relativePath in scopesToScan do
                        let absolutePath =
                            ArcPathHelper.combine arcPath relativePath |> PathHelpers.normalizePath

                        try
                            let! scannedEntries = scanImmediateFileEntries absolutePath
                            shallowScans.Add(absolutePath, scannedEntries)
                        with scanError ->
                            // The path can disappear after validation. It must not remain an active scope.
                            this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Remove relativePath
                            do! this.ReconcilePayloadWatcherScopes arcPath

                            swatelogfn
                                this.window.id
                                "Unable to reconcile expanded directory '%s': %s"
                                relativePath
                                scanError.Message

                    let! preparedBatches =
                        shallowScans
                        |> Seq.map snd
                        |> Seq.toArray
                        |> fun batches -> prepareImmediateFileEntryBatches arcPath batches this.fileTree

                    if capturedWatcherEpoch = this.WatcherEpoch && this.path = Some arcPath then
                        // Read/copy the current tree only after every async disk/metadata operation.
                        let mutable nextTree = this.fileTree
                        let mutable changed = false

                        for index in 0 .. shallowScans.Count - 1 do
                            let absolutePath, _ = shallowScans.[index]
                            let entries = preparedBatches.[index]

                            let reconciledTree, directoryChanged =
                                reconcileImmediateFileEntries absolutePath entries nextTree

                            if directoryChanged then
                                nextTree <- reconciledTree
                                changed <- true

                        if changed && capturedWatcherEpoch = this.WatcherEpoch then
                            this.SetFileTree nextTree
            })

        this.FileTreeUpdateTail <- queuedUpdate |> Promise.catch (fun _ -> ())
        queuedUpdate

    /// Temporarily releases native payload watcher handles that overlap an app-initiated filesystem
    /// mutation. Existing scopes are re-added through the authoritative reconciliation path, which
    /// also shallowly refreshes their direct children after the mutation.
    member this.WithPayloadScopesSuspended<'T>
        (relativePaths: string[], operation: unit -> JS.Promise<'T>)
        : JS.Promise<'T> =
        promise {
            let normalizedMutationPaths =
                relativePaths
                |> Array.map PathHelpers.normalizeCanonicalRelativePath
                |> Set.ofArray

            let suspendedScopes =
                this.expandedDirectoryPaths
                |> Set.filter (fun scope ->
                    normalizedMutationPaths
                    |> Set.exists (fun mutationPath ->
                        Main.FileTreeCreator.isSameOrDescendantLogicalPath scope mutationPath
                        || Main.FileTreeCreator.isSameOrDescendantLogicalPath mutationPath scope
                    )
                )

            if not suspendedScopes.IsEmpty then
                for scope in suspendedScopes do
                    match this.payloadWatcherScopeSuspensions.TryGetValue scope with
                    | true, count -> this.payloadWatcherScopeSuspensions.[scope] <- count + 1
                    | false, _ -> this.payloadWatcherScopeSuspensions.[scope] <- 1

                match this.path with
                | Some arcPath -> do! this.ReconcilePayloadWatcherScopes arcPath
                | None -> ()

            let! outcome = promise {
                try
                    let! result = operation ()
                    return Ok result
                with error ->
                    return Error error
            }

            let releasedScopes = ResizeArray<string>()

            for scope in suspendedScopes do
                match this.payloadWatcherScopeSuspensions.TryGetValue scope with
                | true, count when count > 1 -> this.payloadWatcherScopeSuspensions.[scope] <- count - 1
                | true, _ ->
                    this.payloadWatcherScopeSuspensions.Remove scope |> ignore
                    releasedScopes.Add scope
                | false, _ -> ()

            match this.path with
            | Some arcPath when not suspendedScopes.IsEmpty ->
                let restorableScopes =
                    releasedScopes
                    |> Seq.filter (fun scope ->
                        if not (this.expandedDirectoryPaths.Contains scope) then
                            false
                        else
                            let absolutePath = ArcPathHelper.combine arcPath scope |> PathHelpers.normalizePath

                            try
                                existsSync absolutePath && (statSync absolutePath).isDirectory ()
                            with _ ->
                                false
                    )
                    |> Set.ofSeq

                let unavailableReleasedScopes =
                    let releasedDesiredScopes =
                        releasedScopes |> Seq.filter this.expandedDirectoryPaths.Contains |> Set.ofSeq

                    Set.difference releasedDesiredScopes restorableScopes

                this.expandedDirectoryPaths <- Set.difference this.expandedDirectoryPaths unavailableReleasedScopes

                try
                    do!
                        this.SetActiveFileTreeDirectories(
                            this.expandedDirectoryPaths |> Set.toArray,
                            refreshPaths = (restorableScopes |> Set.toArray)
                        )
                with reconciliationError ->
                    swatelogfn
                        this.window.id
                        "Unable to restore payload watcher scopes after filesystem mutation: %s"
                        reconciliationError.Message
            | _ -> ()

            match outcome with
            | Ok result -> return result
            | Error error -> return raise error
        }
