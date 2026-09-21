module Swate.Components.Composite.Tree.Context

open Feliz
open Swate.Components.Composite.Tree.Types

let TreeCtx =
    React.createContext<TreeContextValue> {
        SelectionDisabled = false
        IsNodeSelectable = fun _ -> true
        RenderNode = None
        Leading = None
        Trailing = None
        StyleFn = None
        Debug = false
    }

[<Hook>]
let useTreeCtx () = React.useContext TreeCtx
