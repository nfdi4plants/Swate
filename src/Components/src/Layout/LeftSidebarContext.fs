module Swate.Components.LayoutContexts.LeftSidebarContext

open Feliz
open Swate.Components.Types

type LeftSidebarCtxState = StateContext<bool>

module LeftSidebarCtxState =

    let Empty: LeftSidebarCtxState = { state = false; setState = ignore }

let LeftSidebarCtx =
    React.createContext<LeftSidebarCtxState> (LeftSidebarCtxState.Empty)

[<Hook>]
let useLeftSidebarCtx () = React.useContext LeftSidebarCtx
