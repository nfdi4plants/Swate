module Renderer.Context.FileStateCtx

open Feliz
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

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
