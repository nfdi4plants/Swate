module Renderer.Components.MainContent.Types


open ARCitISA.Components.Shared
open ARCitISA.Electron.Shared
open Renderer

type ArcTargetProps = {
    AppState: ArcRootPath
    PageState: PageState option
}
