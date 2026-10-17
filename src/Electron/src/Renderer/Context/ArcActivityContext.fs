module Renderer.Context.ArcActivityContext

open Fable.Core
open Feliz
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

let private initialState: ArcActivityState = {
    isInitializing = false
    hasUnsavedChanges = false
}

let ArcActivityCtx = React.createContext<ArcActivityState> initialState

[<Hook>]
let useArcActivityCtx () = React.useContext ArcActivityCtx

[<ReactComponent>]
let ArcActivityCtxProvider (children: ReactElement) =
    let activity =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = initialState
            load =
                fun () -> promise {
                    match! Api.ipcArcVaultApi.getArcActivityState () with
                    | Ok state -> return state
                    | Error error -> return raise error
                }
            subscribe =
                fun setState ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IArcActivityRendererApi> {
                        arcActivityChanged = setState
                    }
            onError = fun error -> Browser.Dom.console.error ("Failed to load ARC activity state.", error.Message)
            dependencies = [||]
        }

    ArcActivityCtx.Provider(activity.state, children)
