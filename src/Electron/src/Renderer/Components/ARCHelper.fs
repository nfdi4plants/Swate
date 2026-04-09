module Renderer.Components.ARCHelper

open System
open Feliz
open Swate.Electron.Shared.FileIOHelper

[<Hook>]
let useCurrentArcScopeId () =
    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()

    appStateCtx.state
    |> Option.map normalizePath
    |> Option.bind (fun path ->
        if String.IsNullOrWhiteSpace path then
            None
        else
            Some path
    )
