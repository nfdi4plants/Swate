module Renderer.Context.FileStateContext

open Feliz
open Renderer.Context.FileSelectionContext
open Renderer.Context.FileStateTypes
open Renderer.Context.FileTreeContext
open Swate.Components.Shared

[<Hook>]
let useFileStateCtx () =
    let fileTreeState = useFileTreeCtx ()
    let selectionCtx = useFileSelectionCtx ()

    React.useMemo (
        (fun _ -> {
            state = {
                FileTree = fileTreeState.Entries
                Selection = selectionCtx.selection
                Status = fileTreeState.Status
            }
            setSelection = selectionCtx.setSelection
            updateSelection = selectionCtx.updateSelection
        }),
        [| box fileTreeState; box selectionCtx.selection |]
    )

[<ReactComponent>]
let FileStateCtxProvider (children: ReactElement) =
    let selection, setSelectionState = React.useStateWithUpdater ArcSelection.empty
    let fileTreeSnapshot = Renderer.MainUpdateRendererBridge.useFileTreeUpdate ()

    let fileTreeState =
        React.useMemo (
            (fun _ -> {
                Entries = fileTreeSnapshot.Value.Values |> Seq.toArray
                Status = fileTreeSnapshot.Status
            }),
            [| box fileTreeSnapshot.Value; box fileTreeSnapshot.Status |]
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

    FileTreeCtx.Provider(fileTreeState, FileSelectionCtx.Provider(selectionCtx, children))
