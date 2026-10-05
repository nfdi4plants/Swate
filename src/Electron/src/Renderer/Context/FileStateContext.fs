module Renderer.Context.FileStateContext

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

type FileState = {
    FileTree: seq<FileEntry>
    FileTreeRoot: FileTreeNode option
    TryFindFileTreeEntry: string -> FileEntry option
    IsFileTreeDirectory: string -> bool
    Selection: ArcSelection
} with

    static member init() : FileState = {
        FileTree = [||]
        FileTreeRoot = None
        TryFindFileTreeEntry = fun _ -> None
        IsFileTreeDirectory = fun _ -> false
        Selection = ArcSelection.empty
    }

type FileStateController = {
    state: FileState
    fileTreeIsLoading: bool
    refreshFileTree: unit -> unit
    setSelection: ArcSelection -> unit
    updateSelection: (ArcSelection -> ArcSelection) -> unit
    activeFileImport: ActiveFileImportState option
    isCancellingFileImport: bool
    importExternalFiles: string -> JS.Promise<Result<unit, exn>>
    cancelFileImport: unit -> JS.Promise<Result<unit, exn>>
}

let FileStateCtx =
    React.createContext<FileStateController> (
        {
            state = FileState.init ()
            fileTreeIsLoading = true
            refreshFileTree = ignore
            setSelection = ignore
            updateSelection = ignore
            activeFileImport = None
            isCancellingFileImport = false
            importExternalFiles = fun _ -> JS.Constructors.Promise.resolve (Ok())
            cancelFileImport = fun () -> JS.Constructors.Promise.resolve (Ok())
        }
    )

[<Hook>]
let useFileStateCtx () = React.useContext FileStateCtx

type FileTreeSnapshotLoader = unit -> JS.Promise<Result<Dictionary<string, FileEntry>, exn>>
type ActiveFileImportLoader = unit -> JS.Promise<Result<ActiveFileImportState option, exn>>

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

type FileImportApi = {
    loadActiveImport: ActiveFileImportLoader
    pickAbsolutePaths: unit -> JS.Promise<Result<string option, exn>>
    runImport: ImportExternalFilesRequest -> JS.Promise<Result<ImportExternalFilesResult, exn>>
    cancelImport: string -> JS.Promise<Result<unit, exn>>
}

[<ReactComponent>]
let FileStateCtxProviderWithSnapshots
    (loadFileTreeSnapshot: FileTreeSnapshotLoader, fileImportApi: FileImportApi, children: ReactElement)
    =
    let selection, setSelectionState = React.useStateWithUpdater ArcSelection.empty
    let isFilePickerOpenRef = React.useRef false
    let isCancellingFileImport, setIsCancellingFileImport = React.useState false
    let initialFileTreeState = React.useMemo (RendererFileTreeState.empty, [||])
    let fileTreeState, setFileTreeState = React.useState initialFileTreeState
    let fileTreeIsLoading, setFileTreeIsLoading = React.useState true
    let latestFileTreeStateRef = React.useRef initialFileTreeState
    let snapshotRequestRef = React.useRef 0
    let directoryUpdateSequenceRef = React.useRef 0L
    let snapshotInstalledRef = React.useRef false

    let bufferedDirectoryUpdatesRef =
        React.useRef (Dictionary<string, int64 * FileTreeDirectoryUpdate>())

    let installFileTreeState nextState =
        latestFileTreeStateRef.current <- nextState
        setFileTreeState nextState

    let applyBufferedDirectoryUpdates state =
        let mutable nextState = state

        let updates =
            bufferedDirectoryUpdatesRef.current.Values
            |> Seq.sortBy fst
            |> Seq.map snd
            |> Seq.toArray

        bufferedDirectoryUpdatesRef.current.Clear()

        updates
        |> Array.iter (fun update -> nextState <- RendererFileTreeState.applyDirectoryUpdate update nextState)

        nextState

    let loadFileTreeSnapshotWithBufferedUpdates () =
        snapshotRequestRef.current <- snapshotRequestRef.current + 1
        let request = snapshotRequestRef.current
        let hadInstalledSnapshot = snapshotInstalledRef.current
        snapshotInstalledRef.current <- false
        setFileTreeIsLoading true

        promise {
            match! loadFileTreeSnapshot () with
            | Error ex when request = snapshotRequestRef.current ->
                if hadInstalledSnapshot then
                    snapshotInstalledRef.current <- true

                    latestFileTreeStateRef.current
                    |> applyBufferedDirectoryUpdates
                    |> installFileTreeState

                setFileTreeIsLoading false
                console.error ("Failed to load file tree snapshot.", ex.Message)
            | Error _ -> ()
            | Ok snapshot when request = snapshotRequestRef.current ->
                snapshotInstalledRef.current <- true

                snapshot
                |> RendererFileTreeState.ofSnapshot
                |> applyBufferedDirectoryUpdates
                |> installFileTreeState

                setFileTreeIsLoading false
            | Ok _ -> ()
        }
        |> Promise.start

    React.useEffect (
        (fun () ->
            let dispose =
                Renderer.IpcReceiver.subscribeProxyReceiver<IFileTreeRendererApi> {
                    fileTreeUpdate =
                        fun snapshot ->
                            snapshotRequestRef.current <- snapshotRequestRef.current + 1
                            bufferedDirectoryUpdatesRef.current.Clear()
                            snapshotInstalledRef.current <- true
                            installFileTreeState (RendererFileTreeState.ofSnapshot snapshot)
                            setFileTreeIsLoading false
                    fileTreeDirectoryUpdate =
                        fun update ->
                            if snapshotInstalledRef.current then
                                let nextState =
                                    RendererFileTreeState.applyDirectoryUpdate update latestFileTreeStateRef.current

                                installFileTreeState nextState
                            else
                                directoryUpdateSequenceRef.current <- directoryUpdateSequenceRef.current + 1L

                                bufferedDirectoryUpdatesRef.current.[PathHelpers.normalizePath update.directoryPath] <-
                                    directoryUpdateSequenceRef.current, update
                }

            loadFileTreeSnapshotWithBufferedUpdates ()

            fun () ->
                snapshotRequestRef.current <- snapshotRequestRef.current + 1
                dispose ()
        ),
        [||]
    )

    let fileTreeEntries =
        React.useMemo ((fun () -> seq { yield! fileTreeState.entriesByPath.Values }), [| box fileTreeState |])

    let fileTreeRoot = fileTreeState.displayRoot

    let fileState =
        React.useMemo (
            (fun _ -> {
                FileTree = fileTreeEntries
                FileTreeRoot = fileTreeRoot
                TryFindFileTreeEntry = fun path -> RendererFileTreeState.tryFind path fileTreeState
                IsFileTreeDirectory =
                    fun path -> RendererFileTreeState.tryFind path fileTreeState |> Option.exists _.isDirectory
                Selection = selection
            }),
            [| box fileTreeState; box selection |]
        )

    let activeFileImport =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = None
            load =
                fun () -> promise {
                    match! fileImportApi.loadActiveImport () with
                    | Ok state -> return state
                    | Error ex -> return raise ex
                }
            subscribe =
                fun setActiveImport ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IFileImportRendererApi> {
                        fileImportStateUpdate =
                            fun state ->
                                if
                                    state.IsNone
                                    || state |> Option.exists (fun value -> value.phase = FileImportPhase.Finalizing)
                                then
                                    setIsCancellingFileImport false

                                setActiveImport state
                    }
            onError = fun ex -> console.error ("Failed to load active file import state.", ex.Message)
            dependencies = [||]
        }

    let importExternalFiles targetRelativePath = promise {
        if activeFileImport.state.IsSome || isFilePickerOpenRef.current then
            return Ok()
        else
            isFilePickerOpenRef.current <- true

            try
                match! fileImportApi.pickAbsolutePaths () with
                | Error ex -> return Error ex
                | Ok None -> return Ok()
                | Ok(Some authorizationId) ->
                    let requestId = System.Guid.NewGuid().ToString()

                    match!
                        fileImportApi.runImport {
                            requestId = requestId
                            targetRelativePath = targetRelativePath
                            authorizationId = authorizationId
                        }
                    with
                    | Error ex -> return Error ex
                    | Ok ImportExternalFilesResult.Completed
                    | Ok ImportExternalFilesResult.Cancelled -> return Ok()
            finally
                isFilePickerOpenRef.current <- false
    }

    let cancelFileImport () = promise {
        match activeFileImport.state with
        | None
        | Some { phase = FileImportPhase.Finalizing } -> return Ok()
        | Some activeImport ->
            setIsCancellingFileImport true

            match! fileImportApi.cancelImport activeImport.requestId with
            | Ok() -> return Ok()
            | Error ex ->
                setIsCancellingFileImport false
                return Error ex
    }

    let fileStateCtx: FileStateController =
        React.useMemo (
            (fun _ -> {
                state = fileState
                fileTreeIsLoading = fileTreeIsLoading
                refreshFileTree = loadFileTreeSnapshotWithBufferedUpdates
                setSelection = fun selection -> setSelectionState (fun _ -> selection |> ArcSelection.normalize)
                updateSelection =
                    fun update ->
                        setSelectionState (fun currentSelection -> update currentSelection |> ArcSelection.normalize)
                activeFileImport = activeFileImport.state
                isCancellingFileImport = isCancellingFileImport
                importExternalFiles = importExternalFiles
                cancelFileImport = cancelFileImport
            }),
            [|
                box fileState
                box fileTreeIsLoading
                box activeFileImport.state
                box isCancellingFileImport
            |]
        )

    FileStateCtx.Provider(fileStateCtx, children)
