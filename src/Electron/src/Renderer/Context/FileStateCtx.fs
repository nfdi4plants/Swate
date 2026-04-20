module Renderer.Context.FileStateCtx

open Feliz
open Renderer.Context.FileStateTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

type private FileSelectionController = {
    selection: ArcSelection
    setSelection: ArcSelection -> unit
    updateSelection: (ArcSelection -> ArcSelection) -> unit
}

module private FileStateHelper =

    let FileTreeCtx = React.createContext<FileEntry[]> [||]

    let FileSelectionCtx =
        React.createContext<FileSelectionController> (
            {
                selection = ArcSelection.empty
                setSelection = ignore
                updateSelection = ignore
            }
        )

[<Hook>]
let useFileTree () =
    React.useContext FileStateHelper.FileTreeCtx

[<Hook>]
let private useFileSelection () =
    React.useContext FileStateHelper.FileSelectionCtx

[<Hook>]
let useFileState () =
    let fileTree = useFileTree ()
    let selectionCtx = useFileSelection ()

    React.useMemo (
        (fun _ -> {
            state = {
                FileTree = fileTree
                Selection = selectionCtx.selection
            }
            setSelection = selectionCtx.setSelection
            updateSelection = selectionCtx.updateSelection
        }),
        [| box fileTree; box selectionCtx.selection |]
    )

[<ReactComponent>]
let FileStateCtxProvider (children: ReactElement) =
    let selection, setSelectionState = React.useStateWithUpdater ArcSelection.empty

    let fileTreeUpdate = Renderer.MainUpdateRendererBridge.useFileTreeUpdate ()

    let fileTree =
        React.useMemo (
            (fun _ ->
                match fileTreeUpdate with
                | ValueSome fileTreeMap -> fileTreeMap.Values |> Seq.toArray
                | ValueNone -> [||]),
            [| box fileTreeUpdate |]
        )

    let setSelection =
        React.useCallback (
            (fun (nextSelection: ArcSelection) ->
                setSelectionState (fun _ -> ArcSelection.normalize nextSelection)),
            [||]
        )

    let updateSelection =
        React.useCallback (
            (fun (update: ArcSelection -> ArcSelection) ->
                setSelectionState (fun currentSelection ->
                    currentSelection
                    |> update
                    |> ArcSelection.normalize)),
            [||]
        )

    let selectionCtx =
        React.useMemo (
            (fun _ -> {
                selection = selection
                setSelection = setSelection
                updateSelection = updateSelection
            }),
            [| box selection |]
        )

    FileStateHelper.FileTreeCtx.Provider(
        fileTree,
        FileStateHelper.FileSelectionCtx.Provider(selectionCtx, children)
    )
