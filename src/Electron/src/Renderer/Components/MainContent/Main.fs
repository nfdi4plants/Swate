module Renderer.Components.MainContent.Main

open Feliz
open Swate.Electron.Shared
open Renderer.Components.MainContent.Types
open Renderer.Types
open Renderer.Components.MainContent.DataHubBrowserTarget
open Renderer.Components.MainContent.InitTarget
open Renderer.Components.MainContent.ArcTarget

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (appRootPath: ArcRootPath, pageState: PageState option) =
    match pageState with
    | Some PageState.DataHubBrowser -> DataHubBrowserTarget()
    | _ ->
        match appRootPath with
        | None -> InitTarget()
        | Some _ -> ArcTarget()
