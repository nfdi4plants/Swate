module Swate.Components.Composite.Tree.Context

open Feliz
open Swate.Components.Composite.Tree.Types

let TreeCtx = React.createContext<obj option> None

[<Hook>]
let useTreeCtx<'T> () =
    React.useContext TreeCtx |> Option.map unbox<TreeContextValue<'T>>
