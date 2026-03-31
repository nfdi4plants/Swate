[<AutoOpen>]
module Main.ArcFileTreeService

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Chokidar
open Swate.Electron.Shared.FileIOTypes
open ARCtrl

type private ArcFileTreeState = {
    mutable FileTree: Dictionary<string, FileEntry>
    mutable ReloadTimeout: int option
}

module private ArcFileTreeServiceHelper =

    let normalizeTrackedPath = Swate.Electron.Shared.FileIOHelper.normalizeSeparators

    let createFileWatcher (path: string) =

        let ignoreFn =
            fun (path: string) ->
                let normalizedPath = normalizeTrackedPath path

                let segments =
                    normalizedPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)

                let tempXlsxPattern = """\.~\$.*\.xlsx$"""

                if System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern) then
                    true
                elif segments |> Array.exists (fun segment -> segment = ".git") then
                    true
                else
                    false

        Chokidar.watch (
            path,
            WatchOptions(cwd = path, awaitWriteFinish = true, ignored = !^ignoreFn, ignoreInitial = true)
        )

type ArcFileTreeService() =

    member val private StateByWindow = Dictionary<int, ArcFileTreeState>() with get

    member private this.EnsureState(windowId: int) =
        match this.StateByWindow.TryGetValue(windowId) with
        | true, state -> state
        | false, _ ->
            let state = {
                FileTree = Dictionary<string, FileEntry>()
                ReloadTimeout = None
            }

            this.StateByWindow.Add(windowId, state)
            state

    member this.TryGet(windowId: int) =
        match this.StateByWindow.TryGetValue(windowId) with
        | true, state -> Some state.FileTree
        | false, _ -> None

    member this.Set(windowId: int, fileTree: Dictionary<string, FileEntry>) =
        let state = this.EnsureState(windowId)
        state.FileTree <- fileTree
        fileTree

    member this.Refresh(windowId: int, arcPath: string) = promise {
        let! fileEntries = getFileEntries arcPath
        let fileTree = createFileEntryTree fileEntries
        return this.Set(windowId, fileTree)
    }

    member this.StartWatching
        (
            windowId: int,
            arcPath: string,
            isBusyWriting: unit -> bool,
            onLoadingChanges: bool -> unit,
            onReloadArc: unit -> JS.Promise<unit>,
            onFileTreeChanged: Dictionary<string, FileEntry> -> unit
        ) =
        let state = this.EnsureState(windowId)

        let pathUpdater (path: string) =
            ArcPathHelper.combine arcPath path |> ArcFileTreeServiceHelper.normalizeTrackedPath

        let scheduleReload (eventName: string) (path: string) =
            match state.ReloadTimeout with
            | Some timeoutId -> Fable.Core.JS.clearTimeout timeoutId
            | None ->
                if isBusyWriting () then
                    onLoadingChanges true

            if isBusyWriting () then
                onLoadingChanges false
            else
                let timeoutId =
                    Fable.Core.JS.setTimeout
                        (fun () ->
                            state.ReloadTimeout <- None

                            promise {
                                do! onReloadArc ()
                                onLoadingChanges false

                                match eventName.ToLowerInvariant() with
                                | name when name = Events.Add.ToString().ToLowerInvariant() ->
                                    let newPath = pathUpdater path
                                    let! addedFile = getFileEntry newPath
                                    let newFileTree = state.FileTree
                                    newFileTree.Add(addedFile.path, addedFile)
                                    state.FileTree <- newFileTree
                                    onFileTreeChanged newFileTree
                                | name when name = Events.AddDir.ToString().ToLowerInvariant() ->
                                    let newPath = pathUpdater path
                                    let! addedFile = getFileEntry newPath
                                    let newFileTree = state.FileTree
                                    newFileTree.Add(addedFile.path, addedFile)
                                    state.FileTree <- newFileTree
                                    onFileTreeChanged newFileTree
                                | name when name = Events.Unlink.ToString().ToLowerInvariant() ->
                                    let newPath = pathUpdater path

                                    if state.FileTree.ContainsKey(newPath) then
                                        let newFileTree = state.FileTree
                                        newFileTree.Remove(newPath) |> ignore
                                        state.FileTree <- newFileTree
                                        onFileTreeChanged newFileTree
                                | name when name = Events.UnlinkDir.ToString().ToLowerInvariant() ->
                                    let newPath = pathUpdater path

                                    if state.FileTree.ContainsKey(newPath) then
                                        let newFileTree = state.FileTree

                                        let affectedPaths =
                                            state.FileTree.Keys
                                            |> Array.ofSeq
                                            |> Array.filter (fun existingPath -> existingPath.Contains(newPath))

                                        newFileTree.Remove(newPath) |> ignore

                                        affectedPaths
                                        |> Array.iter (fun affectedPath -> newFileTree.Remove(affectedPath) |> ignore)

                                        state.FileTree <- newFileTree
                                        onFileTreeChanged newFileTree
                                | _ ->
                                    let! fileTree = this.Refresh(windowId, arcPath)
                                    onFileTreeChanged fileTree
                            }
                            |> Promise.start
                        )
                        500

                state.ReloadTimeout <- Some timeoutId

        let watcher = ArcFileTreeServiceHelper.createFileWatcher arcPath
        watcher.on (Events.All, scheduleReload) |> ignore

    member this.Dispose(windowId: int) =
        match this.StateByWindow.TryGetValue(windowId) with
        | false, _ -> ()
        | true, _ ->
            this.StateByWindow.Remove(windowId) |> ignore

let ARC_FILE_TREES = ArcFileTreeService()
