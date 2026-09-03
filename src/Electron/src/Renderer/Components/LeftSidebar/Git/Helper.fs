module Renderer.Components.LeftSidebar.Git.Helper

open Renderer.Components.LeftSidebar.Git.Types
open Renderer.Context.ArcOpeningContext

let createOpenArcAction (arcOpening: ArcOpeningController) : EmptyStateAction = {
    Label = "Open ARC"
    IconClassName = "swt:fluent--folder-open-24-regular"
    Disabled = arcOpening.isOpeningArc
    OnClick = arcOpening.openArc
}
