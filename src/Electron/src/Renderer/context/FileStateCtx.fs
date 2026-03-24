module Renderer.Context.FileStateCtx

open Swate.Components
open Swate.Components.Types
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.FileIOTypes
open Fable.Electron.Remoting.Renderer

open Feliz

type FileState = {
    FileTree: FileEntry[]
    SelectedTreeItemPath: string option
} with

    static member init() : FileState = {
        FileTree = [||]
        SelectedTreeItemPath = None
    }


type FileStateCtx = {
    state: FileState
    setState: FileState -> unit
    setFileTree: FileEntry[] -> unit
    setSelectedTreeItemPath: string option -> unit
}

let FileStateCtx =
    React.createContext<FileStateCtx> (
        {
            state = FileState.init ()
            setState = ignore
            setFileTree = ignore
            setSelectedTreeItemPath = ignore
        }
    )

[<Hook>]
let useFileState () = React.useContext FileStateCtx

[<ReactComponent>]
let FileStateCtxProvider (children: ReactElement) =

    let (fileState, setFileState) = React.useState FileState.init

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        IMainUpdateRendererApi.empty with
            fileTreeUpdate =
                fun fileTreeDict ->
                    let fileTree = fileTreeDict.Values |> Seq.toArray
                    setFileState { fileState with FileTree = fileTree }
    }


    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    let fileStateCtx: FileStateCtx =
        React.useMemo (
            (fun _ -> {
                state = fileState
                setState = fun fs -> setFileState fs
                setFileTree = fun fileTree -> setFileState { fileState with FileTree = fileTree }
                setSelectedTreeItemPath =
                    fun selectedTreeItemPath ->
                        setFileState {
                            fileState with
                                SelectedTreeItemPath = selectedTreeItemPath
                        }
            }),
            [| box fileState |]
        )

    FileStateCtx.Provider(fileStateCtx, children)