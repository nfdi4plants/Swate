module Renderer.context.AppStateCtx

open Swate.Components
open Swate.Electron.Shared

open Feliz

let AppStateCtx =
    React.createContext<StateContext<AppState>> (
        {
            state = AppState.Init
            setState = ignore
        }
    )