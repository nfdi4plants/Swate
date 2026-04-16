module Renderer.Context.AppStateContext

open Swate.Components
open Swate.Electron.Shared

open Feliz

let AppStateCtx =
    React.createContext<StateContext<ArcRootPath>> ({ state = None; setState = ignore })

[<Hook>]
let useAppState () = React.useContext AppStateCtx