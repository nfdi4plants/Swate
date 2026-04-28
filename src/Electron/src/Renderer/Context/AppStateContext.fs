module Renderer.Context.AppStateContext

open Swate.Electron.Shared

open Feliz

let AppStateCtx =
    React.createContext<Renderer.MainSyncedState.MainSyncedState<ArcRootPath>> (
        {
            state = None
            isLoading = true
            refresh = ignore
        }
    )

[<Hook>]
let useAppStateCtx () = React.useContext AppStateCtx
