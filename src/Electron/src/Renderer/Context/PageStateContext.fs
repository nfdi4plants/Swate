module Renderer.Context.PageStateContext

open Feliz
open Renderer.Types
open Swate.Components

let PageStateCtx =
    React.createContext<StateContext<PageState option>> ({ state = None; setState = ignore })

[<Hook>]
let usePageStateCtx () = React.useContext PageStateCtx
