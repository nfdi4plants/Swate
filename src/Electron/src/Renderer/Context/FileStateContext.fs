module Renderer.Context.FileStateContext

open System.Collections.Generic
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Renderer

type FileState = {
    FileTree: FileEntry[]
    FileTreeRevision: int
    FileTreeRoot: FileTreeNode option
    FileTreeDirectoryPaths: HashSet<string>
    TryFindFileTreeEntry: string -> FileEntry option
    Selection: ArcSelection
} with

    static member init() : FileState = {
        FileTree = [||]
        FileTreeRevision = 0
        FileTreeRoot = None
        FileTreeDirectoryPaths = HashSet()
        TryFindFileTreeEntry = fun _ -> None
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

type RendererFileTreeDeltaApplication = {
    processedPathCount: int
    removedEntryCount: int
    upsertedEntryCount: int
    rebuiltDisplayTree: bool
}

/// Renderer FileTree storage with O(1) path lookup and in-place delta application.
/// The stable entry buffer avoids copying unrelated entries; Revision drives React updates.
type RendererFileTreeState = private {
    entries: ResizeArray<FileEntry>
    indicesByPath: Dictionary<string, int>
    directoryPaths: HashSet<string>
    displayRoot: FileTreeNode option
    displayNodesByPath: Dictionary<string, FileTreeNode>
    revision: int
}

module RendererFileTreeState =

    let private buildDisplayTree (entries: FileEntry[]) =
        let nodesByPath = Dictionary<string, FileTreeNode>()

        let rec indexNode (node: FileTreeNode) =
            nodesByPath.[PathHelpers.normalizePath node.path] <- node
            node.children.Values |> Seq.iter indexNode

        let root =
            match entries with
            | [||] -> None
            | entries -> entries |> toFileTreeNode |> collapseSingleChildSameName |> Some

        root |> Option.iter indexNode
        root, nodesByPath

    let rec private removeDisplaySubtree (nodesByPath: Dictionary<string, FileTreeNode>) (node: FileTreeNode) =
        node.children.Values
        |> Seq.toArray
        |> Array.iter (removeDisplaySubtree nodesByPath)

        nodesByPath.Remove(PathHelpers.normalizePath node.path) |> ignore

    let private parentPath path =
        PathHelpers.tryGetParentPath path
        |> Option.defaultValue ""
        |> PathHelpers.normalizePath

    let private wouldCollapse (node: FileTreeNode) =
        if node.isDirectory && node.children.Count = 1 then
            let onlyChild = node.children.Values |> Seq.exactlyOne

            onlyChild.isDirectory
            && System.String.Equals(node.name, onlyChild.name, System.StringComparison.OrdinalIgnoreCase)
        else
            false

    let empty () = {
        entries = ResizeArray()
        indicesByPath = Dictionary()
        directoryPaths = HashSet()
        displayRoot = None
        displayNodesByPath = Dictionary()
        revision = 0
    }

    let ofSnapshot (fileTree: Dictionary<string, FileEntry>) =
        let entries = ResizeArray<FileEntry>(fileTree.Count)
        let indicesByPath = Dictionary<string, int>(fileTree.Count)
        let directoryPaths = HashSet<string>()

        fileTree.Values
        |> Seq.iter (fun entry ->
            let index = entries.Count
            entries.Add entry
            indicesByPath.[PathHelpers.normalizePath entry.path] <- index

            if entry.isDirectory then
                directoryPaths.Add(PathHelpers.normalizePath entry.path) |> ignore
        )

        let displayRoot, displayNodesByPath =
            buildDisplayTree (GlobalBindings.resizeArrayAsArray entries)

        {
            entries = entries
            indicesByPath = indicesByPath
            directoryPaths = directoryPaths
            displayRoot = displayRoot
            displayNodesByPath = displayNodesByPath
            revision = 0
        }

    let count state = state.entries.Count
    let revision state = state.revision
    let displayRoot state = state.displayRoot
    let directoryPaths state = state.directoryPaths

    let tryFind path state =
        match state.indicesByPath.TryGetValue(PathHelpers.normalizePath path) with
        | true, index -> Some state.entries.[index]
        | false, _ -> None

    let tryFindDisplayNode path state =
        match state.displayNodesByPath.TryGetValue(PathHelpers.normalizePath path) with
        | true, node -> Some node
        | false, _ -> None

    let applyDelta (delta: FileTreeDelta) state =
        let mutable removedEntryCount = 0
        let affectedDisplayParentPaths = HashSet<string>()
        let mutable requiresDisplayTreeRebuild = false

        delta.removedPaths
        |> Array.iter (fun path ->
            let normalizedPath = PathHelpers.normalizePath path

            match state.indicesByPath.TryGetValue normalizedPath with
            | false, _ -> ()
            | true, removedIndex ->
                let lastIndex = state.entries.Count - 1
                let removedEntry = state.entries.[removedIndex]

                if removedEntry.isDirectory then
                    state.directoryPaths.Remove normalizedPath |> ignore

                if removedIndex <> lastIndex then
                    let lastEntry = state.entries.[lastIndex]
                    state.entries.[removedIndex] <- lastEntry
                    state.indicesByPath.[PathHelpers.normalizePath lastEntry.path] <- removedIndex

                state.entries.RemoveAt lastIndex
                state.indicesByPath.Remove normalizedPath |> ignore
                removedEntryCount <- removedEntryCount + 1
        )

        delta.removedPaths
        |> Array.iter (fun path ->
            if not requiresDisplayTreeRebuild then
                let normalizedPath = PathHelpers.normalizePath path

                match state.displayNodesByPath.TryGetValue normalizedPath with
                | false, _ -> ()
                | true, node ->
                    let normalizedParentPath = parentPath normalizedPath

                    match state.displayNodesByPath.TryGetValue normalizedParentPath with
                    | false, _ -> requiresDisplayTreeRebuild <- true
                    | true, parent ->
                        parent.children.Remove node.name |> ignore
                        removeDisplaySubtree state.displayNodesByPath node
                        affectedDisplayParentPaths.Add normalizedParentPath |> ignore
        )

        delta.upsertedEntries
        |> Array.iter (fun entry ->
            let normalizedPath = PathHelpers.normalizePath entry.path

            match state.indicesByPath.TryGetValue normalizedPath with
            | true, index ->
                let previousEntry = state.entries.[index]
                state.entries.[index] <- entry

                if previousEntry.isDirectory && not entry.isDirectory then
                    state.directoryPaths.Remove normalizedPath |> ignore
                elif entry.isDirectory then
                    state.directoryPaths.Add normalizedPath |> ignore
            | false, _ ->
                state.indicesByPath.[normalizedPath] <- state.entries.Count
                state.entries.Add entry

                if entry.isDirectory then
                    state.directoryPaths.Add normalizedPath |> ignore
        )

        delta.upsertedEntries
        |> Array.iter (fun entry ->
            if not requiresDisplayTreeRebuild then
                let normalizedPath = PathHelpers.normalizePath entry.path
                let normalizedParentPath = parentPath normalizedPath

                match state.displayNodesByPath.TryGetValue normalizedParentPath with
                | false, _ -> requiresDisplayTreeRebuild <- true
                | true, parent ->
                    match state.displayNodesByPath.TryGetValue normalizedPath with
                    | true, existing ->
                        let updated = {
                            existing with
                                name = entry.name
                                isDirectory = entry.isDirectory
                                largeObject = entry.largeObject
                                children =
                                    if entry.isDirectory then
                                        existing.children
                                    else
                                        Dictionary()
                        }

                        parent.children.Remove existing.name |> ignore
                        parent.children.[updated.name] <- updated
                        state.displayNodesByPath.[normalizedPath] <- updated
                    | false, _ ->
                        let added =
                            FileTreeNode.create (
                                entry.name,
                                entry.isDirectory,
                                entry.path,
                                Dictionary(),
                                entry.largeObject
                            )

                        parent.children.[added.name] <- added
                        state.displayNodesByPath.[normalizedPath] <- added

                    affectedDisplayParentPaths.Add normalizedParentPath |> ignore
        )

        if
            not requiresDisplayTreeRebuild
            && affectedDisplayParentPaths
               |> Seq.exists (fun path ->
                   match state.displayNodesByPath.TryGetValue path with
                   | true, node -> wouldCollapse node
                   | false, _ -> true
               )
        then
            requiresDisplayTreeRebuild <- true

        let displayRoot, displayNodesByPath =
            if requiresDisplayTreeRebuild then
                buildDisplayTree (GlobalBindings.resizeArrayAsArray state.entries)
            else
                state.displayRoot, state.displayNodesByPath

        let processedPathCount = delta.removedPaths.Length + delta.upsertedEntries.Length

        let nextState =
            if processedPathCount = 0 then
                state
            else
                {
                    state with
                        displayRoot = displayRoot
                        displayNodesByPath = displayNodesByPath
                        revision = state.revision + 1
                }

        nextState,
        {
            processedPathCount = processedPathCount
            removedEntryCount = removedEntryCount
            upsertedEntryCount = delta.upsertedEntries.Length
            rebuiltDisplayTree = requiresDisplayTreeRebuild
        }

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
    let latestFileTreeStateRef = React.useRef initialFileTreeState

    let fileTree =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = initialFileTreeState
            load =
                fun () -> promise {
                    match! loadFileTreeSnapshot () with
                    | Ok fileTreeDict -> return RendererFileTreeState.ofSnapshot fileTreeDict
                    | Error ex -> return raise ex
                }
            subscribe =
                fun setFileTreeState ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IFileTreeRendererApi> {
                        fileTreeUpdate =
                            fun snapshot ->
                                let nextState = RendererFileTreeState.ofSnapshot snapshot
                                latestFileTreeStateRef.current <- nextState
                                setFileTreeState nextState
                        fileTreeDelta =
                            fun delta ->
                                let nextState, _ =
                                    RendererFileTreeState.applyDelta delta latestFileTreeStateRef.current

                                latestFileTreeStateRef.current <- nextState
                                setFileTreeState nextState
                    }
            onError = fun ex -> console.error ("Failed to load file tree snapshot.", ex.Message)
            dependencies = [||]
        }

    latestFileTreeStateRef.current <- fileTree.state

    let fileTreeEntries = GlobalBindings.resizeArrayAsArray fileTree.state.entries
    let fileTreeRevision = RendererFileTreeState.revision fileTree.state
    let fileTreeRoot = RendererFileTreeState.displayRoot fileTree.state
    let fileTreeDirectoryPaths = RendererFileTreeState.directoryPaths fileTree.state

    let fileState =
        React.useMemo (
            (fun _ -> {
                FileTree = fileTreeEntries
                FileTreeRevision = fileTreeRevision
                FileTreeRoot = fileTreeRoot
                FileTreeDirectoryPaths = fileTreeDirectoryPaths
                TryFindFileTreeEntry = fun path -> RendererFileTreeState.tryFind path fileTree.state
                Selection = selection
            }),
            [| box fileTree.state; box selection |]
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
                fileTreeIsLoading = fileTree.isLoading
                refreshFileTree = fileTree.refresh
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
                box fileTree.isLoading
                box activeFileImport.state
                box isCancellingFileImport
            |]
        )

    FileStateCtx.Provider(fileStateCtx, children)
