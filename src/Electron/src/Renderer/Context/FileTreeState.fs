module Renderer.Context.FileTreeState

open System.Collections.Generic
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Shared.PathChildrenIndex
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Renderer

type FileTreeSnapshotLoader = unit -> JS.Promise<Result<Dictionary<string, FileEntry>, exn>>

/// The path-keyed dictionary is the renderer's canonical FileTree entry state.
/// The direct-child index and display tree are derived navigation caches updated only for one directory.
type RendererFileTreeState = private {
    entriesByPath: Dictionary<string, FileEntry>
    directChildren: PathChildrenIndex
    displayRoot: FileTreeNode option
}

module RendererFileTreeState =

    let private buildDisplayTree (entries: seq<FileEntry>) =
        let entries = entries |> Seq.toArray

        match entries with
        | [||] -> None
        | entries -> entries |> toFileTreeNode |> collapseSingleChildSameName |> Some

    let rec private tryFindDisplayNodeCore (normalizedPath: string) (node: FileTreeNode) =
        let nodePath = PathHelpers.normalizePath node.path

        if nodePath = normalizedPath then
            Some node
        else
            node.children.Values
            |> Seq.tryFind (fun child ->
                PathHelpers.isSameOrDescendantPath normalizedPath (PathHelpers.normalizePath child.path)
            )
            |> Option.bind (tryFindDisplayNodeCore normalizedPath)

    let private updateDisplayDirectory
        (directoryPath: string)
        (children: FileEntry[])
        (displayRoot: FileTreeNode option)
        =
        displayRoot
        |> Option.bind (tryFindDisplayNodeCore directoryPath)
        |> Option.iter (fun directoryNode ->
            let existingChildren = Dictionary<string, FileTreeNode>()

            directoryNode.children.Values
            |> Seq.iter (fun child -> existingChildren.[PathHelpers.normalizePath child.path] <- child)

            let nextChildren = Dictionary<string, FileTreeNode>()

            children
            |> Array.iter (fun entry ->
                let normalizedPath = PathHelpers.normalizePath entry.path

                let nextNode =
                    match existingChildren.TryGetValue normalizedPath with
                    | true, existing when existing.isDirectory && entry.isDirectory -> {
                        existing with
                            name = entry.name
                            path = normalizedPath
                            largeObject = entry.largeObject
                      }
                    | _ ->
                        FileTreeNode.create (
                            entry.name,
                            entry.isDirectory,
                            normalizedPath,
                            Dictionary(),
                            entry.largeObject
                        )

                nextChildren.[nextNode.name] <- nextNode
            )

            directoryNode.children.Clear()

            nextChildren
            |> Seq.iter (fun pair -> directoryNode.children.[pair.Key] <- pair.Value)
        )

    let empty () = {
        entriesByPath = Dictionary()
        directChildren = PathChildrenIndex()
        displayRoot = None
    }

    let ofSnapshot (fileTree: Dictionary<string, FileEntry>) =
        let entriesByPath = Dictionary<string, FileEntry>(fileTree.Count)

        fileTree.Values
        |> Seq.iter (fun entry ->
            let normalizedPath = PathHelpers.normalizePath entry.path

            entriesByPath.[normalizedPath] <-
                if normalizedPath = entry.path then
                    entry
                else
                    { entry with path = normalizedPath }
        )

        let directChildren = PathChildrenIndex()
        directChildren.Rebuild entriesByPath.Keys

        {
            entriesByPath = entriesByPath
            directChildren = directChildren
            displayRoot = buildDisplayTree entriesByPath.Values
        }

    let tryFind path state =
        match state.entriesByPath.TryGetValue(PathHelpers.normalizePath path) with
        | true, entry -> Some entry
        | false, _ -> None

    let applyDirectoryUpdate (update: FileTreeDirectoryUpdate) state =
        let directoryPath = PathHelpers.normalizePath update.directoryPath
        let childrenByPath = Dictionary<string, FileEntry>()

        update.children
        |> Array.iter (fun child ->
            let childPath = PathHelpers.normalizePath child.path

            if
                PathHelpers.tryGetParentPath childPath
                |> Option.map PathHelpers.normalizePath
                |> Option.defaultValue ""
                |> (=) directoryPath
            then
                childrenByPath.[childPath] <- { child with path = childPath }
        )

        let oldDirectChildPaths = state.directChildren.GetDirectChildPaths directoryPath

        let removalRoots =
            oldDirectChildPaths
            |> Array.choose (fun oldPath ->
                match state.entriesByPath.TryGetValue oldPath, childrenByPath.TryGetValue oldPath with
                | (true, oldEntry), (true, nextEntry) when oldEntry.isDirectory = nextEntry.isDirectory -> None
                | _ -> Some oldPath
            )

        let removedPaths = state.directChildren.CollectSubtreePaths removalRoots

        let hasEntryChanges =
            oldDirectChildPaths.Length <> childrenByPath.Count
            || removedPaths.Length > 0
            || childrenByPath
               |> Seq.exists (fun pair ->
                   match state.entriesByPath.TryGetValue pair.Key with
                   | true, existing -> existing <> pair.Value
                   | false, _ -> true
               )

        if hasEntryChanges then
            removedPaths
            |> Array.iter (fun path ->
                state.entriesByPath.Remove path |> ignore
                state.directChildren.Remove path
            )

            childrenByPath
            |> Seq.iter (fun pair ->
                state.entriesByPath.[pair.Key] <- pair.Value
                state.directChildren.Add pair.Key
            )

            updateDisplayDirectory directoryPath (childrenByPath.Values |> Seq.toArray) state.displayRoot

        if hasEntryChanges then
            {
                state with
                    displayRoot = state.displayRoot
            }
        else
            state

type SyncedFileTreeState = {
    entries: seq<FileEntry>
    root: FileTreeNode option
    tryFind: string -> FileEntry option
    isLoading: bool
    refresh: unit -> unit
}

[<Hook>]
let useFileTreeState (loadSnapshot: FileTreeSnapshotLoader) =
    let state, setState = React.useStateWithUpdater (RendererFileTreeState.empty ())
    let isLoading, setIsLoading = React.useState true
    let requestRef = React.useRef 0
    let snapshotPendingRef = React.useRef true
    let bufferedUpdatesRef = React.useRef (ResizeArray<FileTreeDirectoryUpdate>())

    let takeBufferedUpdates () =
        let updates = bufferedUpdatesRef.current.ToArray()
        bufferedUpdatesRef.current.Clear()
        snapshotPendingRef.current <- false
        updates

    let applyUpdates updates state =
        updates
        |> Array.fold (fun current update -> RendererFileTreeState.applyDirectoryUpdate update current) state

    let refresh () =
        requestRef.current <- requestRef.current + 1
        let request = requestRef.current
        snapshotPendingRef.current <- true
        setIsLoading true

        promise {
            match! loadSnapshot () with
            | Error ex when request = requestRef.current ->
                let updates = takeBufferedUpdates ()
                setState (applyUpdates updates)
                setIsLoading false
                console.error ("Failed to load file tree snapshot.", ex.Message)
            | Error _ -> ()
            | Ok snapshot when request = requestRef.current ->
                let updates = takeBufferedUpdates ()

                snapshot
                |> RendererFileTreeState.ofSnapshot
                |> applyUpdates updates
                |> fun state -> setState (fun _ -> state)

                setIsLoading false
            | Ok _ -> ()
        }
        |> Promise.start

    let installSnapshot snapshot =
        requestRef.current <- requestRef.current + 1
        snapshotPendingRef.current <- false
        bufferedUpdatesRef.current.Clear()
        let state = RendererFileTreeState.ofSnapshot snapshot
        setState (fun _ -> state)
        setIsLoading false

    let applyOrBufferUpdate update =
        if snapshotPendingRef.current then
            let directoryPath = PathHelpers.normalizePath update.directoryPath

            bufferedUpdatesRef.current
            |> Seq.tryFindIndex (fun buffered -> PathHelpers.normalizePath buffered.directoryPath = directoryPath)
            |> Option.iter bufferedUpdatesRef.current.RemoveAt

            bufferedUpdatesRef.current.Add update
        else
            setState (RendererFileTreeState.applyDirectoryUpdate update)

    React.useEffect (
        (fun () ->
            let dispose =
                Renderer.IpcReceiver.subscribeProxyReceiver<IFileTreeRendererApi> {
                    fileTreeUpdate = installSnapshot
                    fileTreeDirectoryUpdate = applyOrBufferUpdate
                }

            refresh ()

            fun () ->
                requestRef.current <- requestRef.current + 1
                dispose ()
        ),
        [| box loadSnapshot |]
    )

    {
        entries = seq { yield! state.entriesByPath.Values }
        root = state.displayRoot
        tryFind = fun path -> RendererFileTreeState.tryFind path state
        isLoading = isLoading
        refresh = refresh
    }
