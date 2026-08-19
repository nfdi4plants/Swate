module Renderer.Components.MainContent.Types


open Swate.Components.Shared
open Swate.Electron.Shared
open Renderer

type ArcTargetProps = {
    AppState: ArcRootPath
    PageState: PageState option
}
