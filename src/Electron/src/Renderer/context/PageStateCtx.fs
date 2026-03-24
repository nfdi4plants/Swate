module Renderer.Context.PageStateCtx

open Swate.Components
open Renderer.Types
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Feliz
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

let PageStateCtx =
    React.createContext<StateContext<PageState option>> ({ state = None; setState = ignore })

[<Hook>]
let usePageState () = React.useContext PageStateCtx