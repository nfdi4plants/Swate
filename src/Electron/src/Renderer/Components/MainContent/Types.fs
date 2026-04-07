module Renderer.Components.MainContent.Types


open Swate.Components.Shared
open Swate.Electron.Shared

type ArcTargetProps = {
    AppState: ArcRootPath
    PageState: PageState option
}
