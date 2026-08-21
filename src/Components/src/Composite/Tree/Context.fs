module Swate.Components.Composite.Tree.Context

open Feliz
open Swate.Components.Composite.Tree.Types

let TreeCtx =
    React.createContext<TreeContextValue<obj>> {
        DataSource = None
        SelectionDisabled = false
        IsNodeSelectable = fun _ -> true
        EnableLazyLoading = false
        EnableVirtualization = false
        EstimateNodeHeight = 34
        OnContextMenu = None
        RenderNode = None
        Leading = None
        Trailing = None
        StyleFn = None
        OnError = ignore
        ApiRef = None
        AriaLabel = "Tree"
        Debug = false
    }

[<Hook>]
let useTreeCtx<'T> () =
    React.useContext TreeCtx |> box |> unbox<TreeContextValue<'T>>
