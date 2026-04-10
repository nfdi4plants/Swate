module Renderer.Context.PageStateCtx

open Feliz
open Renderer.Types
open Swate.Components

let PageStateCtx =
    React.createContext<StateContext<PageState option>> ({ state = None; setState = ignore })

[<Hook>]
let usePageState () = React.useContext PageStateCtx
