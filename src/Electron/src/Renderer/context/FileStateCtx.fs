module Renderer.Context.FileStateCtx

open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.FileIOTypes

open Feliz

type FileState = {
    FileTree: FileEntry[]
    SelectedTreeItemPath: string option
} with

    static member init() : FileState = {
        FileTree = [||]
        SelectedTreeItemPath = None
    }


type FileStateController = {
    state: FileState
    setState: (FileState -> FileState) -> unit
    setFileTree: FileEntry[] -> unit
    setSelectedTreeItemPath: string option -> unit
}

let FileStateCtx =
    React.createContext<FileStateController> (
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

    let (fileState, setFileState) = React.useStateWithUpdater (FileState.init ())

    React.useEffectOnce (fun () ->
        Renderer.MainUpdateRendererBridge.subscribeFileTreeUpdate (fun fileTreeDict ->
            let fileTree = fileTreeDict.Values |> Seq.toArray
            setFileState (fun fs -> { fs with FileTree = fileTree })
        ))

    let fileStateCtx: FileStateController =
        React.useMemo (
            (fun _ -> {
                state = fileState
                setState = setFileState
                setFileTree =
                    fun fileTree ->
                        setFileState (fun fs -> { fs with FileTree = fileTree })
                setSelectedTreeItemPath =
                    fun selectedTreeItemPath ->
                        setFileState (fun fs -> {
                            fs with
                                SelectedTreeItemPath = selectedTreeItemPath
                        })
            }),
            [| box fileState |]
        )

    FileStateCtx.Provider(fileStateCtx, children)
