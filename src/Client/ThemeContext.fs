
module Swate.Client.Theme.Context

open Feliz
open Swate.Components

let ThemeCtx = React.createContext<StateContext<Theme>> ()

[<Hook>]
let useThemeCtx () = React.useContext ThemeCtx
