module Renderer.Context.PreviewStateContext

open Feliz
open Swate.Components.Shared

type PreviewState = { PendingArcFileSave: ArcFiles option }
with
    static member init() : PreviewState = { PendingArcFileSave = None }

type PreviewStateController = {
    state: PreviewState
    setState: (PreviewState -> PreviewState) -> unit
    setPendingArcFileSave: ArcFiles option -> unit
}

let PreviewStateCtx =
    React.createContext<PreviewStateController> (
        {
            state = PreviewState.init ()
            setState = ignore
            setPendingArcFileSave = ignore
        }
    )

[<Hook>]
let usePreviewStateCtx () = React.useContext PreviewStateCtx

[<ReactComponent>]
let PreviewStateCtxProvider (children: ReactElement) =
    let previewState, setPreviewState = React.useStateWithUpdater (PreviewState.init ())

    let previewStateCtx: PreviewStateController =
        React.useMemo (
            (fun _ -> {
                state = previewState
                setState = setPreviewState
                setPendingArcFileSave =
                    fun pendingArcFileSave ->
                        setPreviewState (fun currentState -> {
                            currentState with
                                PendingArcFileSave = pendingArcFileSave
                        })
            }),
            [| box previewState |]
        )

    PreviewStateCtx.Provider(previewStateCtx, children)
