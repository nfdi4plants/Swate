module ARCitISA.Components.Composite.ThemeSelector.Context

open Feliz
open ARCitISA.Components
open ARCitISA.Components.Composite.ThemeSelector.Types

let ThemeCtx = React.createContext<StateContext<Theme>> ()

[<Hook>]
let useThemeCtx () = React.useContext ThemeCtx
