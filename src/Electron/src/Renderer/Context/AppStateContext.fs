module Renderer.Context.AppStateContext

open ARCitISA.Electron.Shared

open Feliz

let AppStateCtx = React.createContext<ArcRootPath> None

[<Hook>]
let useAppStateCtx () = React.useContext AppStateCtx
