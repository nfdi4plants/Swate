module Renderer.Context.FileStateCtx

open Feliz
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes

type FileState = {
    FileTree: FileEntry[]
    Selection: ArcSelection
}
with
    static member init() : FileState = {
        FileTree = [||]
        Selection = ArcSelection.empty
    }

type FileStateController = {
    state: FileState
    setState: (FileState -> FileState) -> unit
    setFileTree: FileEntry[] -> unit
    setSelection: ArcSelection -> unit
}

let FileStateCtx =
    React.createContext<FileStateController> (
        {
            state = FileState.init ()
            setState = ignore
            setFileTree = ignore
            setSelection = ignore
        }
    )

[<Hook>]
let useFileState () = React.useContext FileStateCtx

[<ReactComponent>]
let FileStateCtxProvider (children: ReactElement) =
    let fileState, setFileState = React.useStateWithUpdater (FileState.init ())

    let fileTreeUpdate = Renderer.MainUpdateRendererBridge.useFileTreeUpdate ()

    let currentFileTree =
        React.useMemo (
            (fun _ ->
                match fileTreeUpdate with
                | ValueSome fileTreeMap -> fileTreeMap.Values |> Seq.toArray
                | ValueNone -> fileState.FileTree),
            [| box fileTreeUpdate; box fileState.FileTree |]
        )

    let fileStateCtx: FileStateController =
        React.useMemo (
            (fun _ -> {
                state = {
                    FileTree = currentFileTree
                    Selection = fileState.Selection
                }
                setState = setFileState
                // Retained for the existing context shape. After the first IPC
                // file-tree snapshot, useFileTreeUpdate is authoritative and this
                // only updates the pre-broadcast fallback state.
                setFileTree =
                    fun fileTree ->
                        setFileState (fun fs -> { fs with FileTree = fileTree })
                setSelection =
                    fun selection ->
                        setFileState (fun fs -> {
                            fs with
                                Selection = ArcSelection.normalize selection
                        })
            }),
            [| box currentFileTree; box fileState.Selection |]
        )

    FileStateCtx.Provider(fileStateCtx, children)
