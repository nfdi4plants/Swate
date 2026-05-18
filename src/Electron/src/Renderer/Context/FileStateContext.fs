module Renderer.Context.FileStateContext

open System.Collections.Generic
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

type FileState = {
    FileTree: FileEntry[]
    Selection: ArcSelection
} with

    static member init() : FileState = {
        FileTree = [||]
        Selection = ArcSelection.empty
    }

type FileStateController = {
    state: FileState
    fileTreeIsLoading: bool
    refreshFileTree: unit -> unit
    setSelection: ArcSelection -> unit
    updateSelection: (ArcSelection -> ArcSelection) -> unit
}

let FileStateCtx =
    React.createContext<FileStateController> (
        {
            state = FileState.init ()
            fileTreeIsLoading = true
            refreshFileTree = ignore
            setSelection = ignore
            updateSelection = ignore
        }
    )

[<Hook>]
let useFileStateCtx () = React.useContext FileStateCtx

type FileTreeSnapshotLoader = unit -> JS.Promise<Result<Dictionary<string, FileEntry>, exn>>

let private fileTreeFromDictionary (fileTreeDict: Dictionary<string, FileEntry>) = fileTreeDict.Values |> Seq.toArray

[<ReactComponent>]
let FileStateCtxProviderWithFileTreeSnapshot (loadFileTreeSnapshot: FileTreeSnapshotLoader, children: ReactElement) =
    let selection, setSelectionState = React.useStateWithUpdater ArcSelection.empty

    let fileTree =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = [||]
            load =
                fun () -> promise {
                    match! loadFileTreeSnapshot () with
                    | Ok fileTreeDict -> return fileTreeFromDictionary fileTreeDict
                    | Error ex -> return raise ex
                }
            subscribe =
                fun setFileTree ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IFileTreeRendererApi> {
                        fileTreeUpdate = fileTreeFromDictionary >> setFileTree
                    }
            onError = fun ex -> console.error ("Failed to load file tree snapshot.", ex.Message)
            dependencies = [||]
        }

    let fileState =
        React.useMemo (
            (fun _ -> {
                FileTree = fileTree.state
                Selection = selection
            }),
            [| box fileTree.state; box selection |]
        )

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
            }),
            [| box fileState; box fileTree.isLoading |]
        )

    FileStateCtx.Provider(fileStateCtx, children)

[<ReactComponent>]
let FileStateCtxProvider (loadFileTreeSnapshot: FileTreeSnapshotLoader, children: ReactElement) =
    FileStateCtxProviderWithFileTreeSnapshot(loadFileTreeSnapshot, children)