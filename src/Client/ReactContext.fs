[<RequireQualifiedAccessAttribute>]
module ReactContext

open Swate.Components
open Feliz

let ThemeCtx = React.createContext<Context<Theme>> ("ThemCtx")