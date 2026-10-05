module Renderer.Context.FileStateContext

open Fable.Core
open Feliz
open Swate.Components
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Renderer
open Renderer.Context.FileTreeState

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

type ActiveFileImportLoader = unit -> JS.Promise<Result<ActiveFileImportState option, exn>>

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

    let fileTree = useFileTreeState loadFileTreeSnapshot

    let fileState = {
        FileTree = fileTree.entries
        FileTreeRoot = fileTree.root
        TryFindFileTreeEntry = fileTree.tryFind
        IsFileTreeDirectory = fun path -> fileTree.tryFind path |> Option.exists _.isDirectory
        Selection = selection
    }

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

    let fileStateCtx: FileStateController = {
        state = fileState
        fileTreeIsLoading = fileTree.isLoading
        refreshFileTree = fileTree.refresh
        setSelection = fun selection -> setSelectionState (fun _ -> selection |> ArcSelection.normalize)
        updateSelection =
            fun update -> setSelectionState (fun currentSelection -> update currentSelection |> ArcSelection.normalize)
        activeFileImport = activeFileImport.state
        isCancellingFileImport = isCancellingFileImport
        importExternalFiles = importExternalFiles
        cancelFileImport = cancelFileImport
    }

    FileStateCtx.Provider(fileStateCtx, children)
