module Swate.Components.Composite.ThemeSelector.Context

open Feliz
open Swate.Components
open Swate.Components.Composite.ThemeSelector

let ThemeCtx = React.createContext<StateContext<Theme>> ()

[<Hook>]
let useThemeCtx () = React.useContext ThemeCtx