module Renderer.Context.FileStateCtx

open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Components.Shared
open Fable.Electron.Remoting.Renderer

open Feliz

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

    let (fileState, setFileState) = React.useStateWithUpdater (FileState.init ())

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        IMainUpdateRendererApi.empty with
            fileTreeUpdate =
                fun fileTreeDict ->
                    let fileTree = fileTreeDict.Values |> Seq.toArray
                    setFileState (fun fs -> { fs with FileTree = fileTree })
    }


    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    let fileStateCtx: FileStateController =
        React.useMemo (
            (fun _ -> {
                state = fileState
                setState = setFileState
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
            [| box fileState |]
        )

    FileStateCtx.Provider(fileStateCtx, children)
