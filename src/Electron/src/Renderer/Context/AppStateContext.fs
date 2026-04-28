module Renderer.Context.AppStateContext

open Swate.Electron.Shared

open Feliz

let AppStateCtx =
    React.createContext<ArcRootPath> None

[<Hook>]
let useAppStateCtx () = React.useContext AppStateCtx
