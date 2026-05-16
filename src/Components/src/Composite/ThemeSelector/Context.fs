module Swate.Components.Theme.Context

open Feliz
open Swate.Components
open Swate.Components.Primitives

let ThemeCtx = React.createContext<StateContext<Theme>> ()

[<Hook>]
let useThemeCtx () = React.useContext ThemeCtx