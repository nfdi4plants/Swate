/// Owns ArcVault file-tree queries and expansion state, including the payload-watcher updates
/// required when expanded directories change.
[<AutoOpen>]
module Main.ArcVaultFileTree

open System.Collections.Generic
open ARCtrl
open Main
open Main.ArcVault
open Main.ArcVaultHelper
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes

type ArcVault with

    member this.GetRendererFileTreeSnapshot() = promise {
        match this.path with
        | None -> return Dictionary<string, FileEntry>()
        | Some arcPath ->
            if this.fileTree.Count = 0 then
                let! fileTree = getFileTree arcPath
                this.fileTree <- fileTree

            return toRendererFileTree arcPath this.fileTree.Values
    }

    /// Activates live monitoring for an expanded directory, or stops it when collapsed.
    /// Expansion also replaces the potentially stale subtree before it is displayed.
    member this.SetFileTreeDirectoryExpanded(relativePath: string, isExpanded: bool) =
        this.fileTreeWorkQueue.EnqueueFileTreeWork(fun () -> promise {
            match this.path with
            | None -> return raise (arcNotOpenError ())
            | Some arcPath ->
                let relativePath = PathHelpers.normalizeCanonicalRelativePath relativePath

                let absolutePath =
                    ArcPathHelper.combine arcPath relativePath |> PathHelpers.normalizePath

                if isExpanded then
                    if not (this.expandedDirectoryPaths.Contains relativePath) then
                        this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Add relativePath

                        try
                            do! this.RestartPayloadWatcher arcPath
                        with watcherError ->
                            this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Remove relativePath
                            do! this.RestartPayloadWatcher arcPath
                            return raise watcherError

                    let! refreshedFileTree = refreshFileTreeSubtree arcPath absolutePath this.fileTree
                    this.SetFileTree refreshedFileTree
                elif this.expandedDirectoryPaths.Contains relativePath then
                    this.expandedDirectoryPaths <- this.expandedDirectoryPaths.Remove relativePath
                    do! this.RestartPayloadWatcher arcPath
        })
