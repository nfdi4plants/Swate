/// Owns File Explorer expansion state and its bounded payload watcher.
[<AutoOpen>]
module Main.ArcVaultFileTree

open System.Collections.Generic
open ARCtrl
open Fable.Core
open Main
open Main.ArcVault
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

type ArcVault with

    /// Activates shallow live monitoring for an expanded directory, or removes it when collapsed.
    /// Expansion refreshes the selected subtree before publishing it.
    member this.SetFileTreeDirectoryExpanded(relativePath: string, isExpanded: bool) =
        let capturedWatcherEpoch = this.WatcherEpoch

        let queuedUpdate =
            this.FileTreeUpdateTail
            |> Promise.bind (fun () -> promise {
                match this.path with
                | None -> return raise (ArcVaultHelper.arcNotOpenError ())
                | Some _ when capturedWatcherEpoch <> this.WatcherEpoch -> ()
                | Some arcPath ->
                    if
                        isAbsolute relativePath
                        || PathHelpers.containsPathTraversalSegments relativePath
                    then
                        return raise (exn "The expanded directory must be an ARC-relative path.")

                    let normalizedRelativePath = PathHelpers.normalizeCanonicalRelativePath relativePath

                    let absolutePath =
                        ArcPathHelper.combine arcPath normalizedRelativePath
                        |> PathHelpers.normalizePath

                    match tryGetRepoRelativePathOrRoot arcPath absolutePath with
                    | None -> return raise (exn "The expanded directory must stay inside the open ARC.")
                    | Some _ ->
                        let! stats = statAsync absolutePath

                        if not (stats.isDirectory ()) then
                            return raise (exn $"Path '{relativePath}' is not a directory.")

                        if isExpanded then
                            let wasAdded = not (this.expandedDirectoryPaths.Contains normalizedRelativePath)

                            if wasAdded then
                                this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Add normalizedRelativePath

                                try
                                    do! this.RestartPayloadWatcher arcPath
                                with watcherError ->
                                    this.expandedDirectoryPaths <-
                                        this.expandedDirectoryPaths.Remove normalizedRelativePath

                                    do! this.RestartPayloadWatcher arcPath
                                    return raise watcherError

                            let! refreshedFileTree = refreshFileTreeSubtree arcPath absolutePath this.fileTree

                            if capturedWatcherEpoch = this.WatcherEpoch then
                                this.SetFileTree refreshedFileTree
                        elif this.expandedDirectoryPaths.Contains normalizedRelativePath then
                            this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Remove normalizedRelativePath

                            do! this.RestartPayloadWatcher arcPath
            })

        this.FileTreeUpdateTail <- queuedUpdate |> Promise.catch (fun _ -> ())
        queuedUpdate
