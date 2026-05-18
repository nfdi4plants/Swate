module Swate.Components.Composite.Layout.LeftSidebarContext

open Feliz
open Swate.Components

type LeftSidebarCtxState = StateContext<bool>

module private Helper =

    let Empty: LeftSidebarCtxState = { state = false; setState = ignore }

open Helper

let LeftSidebarCtx =
    React.createContext<LeftSidebarCtxState>(Empty)

[<Hook>]
let useLeftSidebarCtx () = React.useContext LeftSidebarCtx
