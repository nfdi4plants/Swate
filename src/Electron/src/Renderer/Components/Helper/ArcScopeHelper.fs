module Renderer.Components.Helper.ArcScopeHelper

open System
open Feliz
open Swate.Components.Shared

[<Hook>]
let useCurrentArcScopeId () =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    appStateCtx
    |> Option.map PathHelpers.normalizePath
    |> Option.bind (fun path ->
        if String.IsNullOrWhiteSpace path then
            None
        else
            Some path)
