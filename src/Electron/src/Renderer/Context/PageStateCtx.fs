module Renderer.Context.PageStateCtx

open Swate.Components
open Swate.Components.Shared

open Feliz

let PageStateCtx =
    React.createContext<StateContext<PageState option>> ({ state = None; setState = ignore })

[<Hook>]
let usePageState () = React.useContext PageStateCtx