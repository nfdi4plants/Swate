module Renderer.Components.MainContent.Types

open ARCtrl
open Swate.Electron.Shared
open Renderer

type ArcTargetProps = {
    AppState: ArcRootPath
    PageState: PageState option
}