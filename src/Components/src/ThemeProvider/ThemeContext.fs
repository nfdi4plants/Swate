
module Swate.Components.ThemeProvider.Context

open Feliz
open Swate.Components

let ThemeCtx = React.createContext<StateContext<Theme>> ()

[<Hook>]
let useThemeCtx () = React.useContext ThemeCtx
