module Swate.Components.Template.TemplateCacheContext

open Feliz
open Swate.Components.Template.Types

type TemplateCacheContext = {
    LoadState: TemplateLoadState
    IsRefreshing: bool
    RefreshError: string option
    RefreshTemplates: unit -> unit
} with

    static member Empty = {
        LoadState = TemplateLoadState.Loading
        IsRefreshing = false
        RefreshError = None
        RefreshTemplates = fun () -> ()
    }

let TemplateCacheCtx =
    React.createContext<TemplateCacheContext> (TemplateCacheContext.Empty)

[<Hook>]
let useTemplateCacheCtx () = React.useContext TemplateCacheCtx