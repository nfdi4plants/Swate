module Swate.Components.Composite.Tree.Context

open Feliz
open Swate.Components.Composite.Tree.Types

let TreeCtx =
    React.createContext<TreeContextValue<obj>> {
        DataSource = None
        SelectionMode = TreeSelectionMode.Single
        SelectionDisabled = false
        IsNodeSelectable = fun _ -> true
        RenderNode = None
        Leading = None
        Trailing = None
        StyleFn = None
        ContextMenuItems = None
        OnError = ignore
        Debug = false
    }

[<Hook>]
let useTreeCtx<'T> () =
    React.useContext TreeCtx |> box |> unbox<TreeContextValue<'T>>
