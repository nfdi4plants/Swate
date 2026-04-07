module Renderer.Components.MainContent.ArcTarget

open Feliz
open Swate.Electron.Shared
open Renderer.Components.MainContent.Types
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Renderer.Types
open Renderer.Components.MainContent.ArcFilePreviewTarget
open Renderer.Components.MainContent.TextPreviewTarget
open Renderer.Components.MainContent.UnknownPreviewTarget
open Renderer.Components.MainContent.ErrorPreviewTarget
open Renderer.Components.MainContent.LandingDraftTarget
open Renderer.Components.MainContent.NotesDraftTarget
open Renderer.Components.MainContent.NotesSearchTarget
open Renderer.Components.MainContent.EmptySelectionTarget

[<ReactComponent>]
let ArcTarget () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    // let arcFileCts = Renderer.Context.

    Html.div [
        prop.id "arc-page-target"
        prop.className "swt:size-full swt:flex swt:justify-center"
        prop.children [
            match pageStateCtx.state with
            | Some page ->
                match page with
                | PageState.ArcFilePage arcFile -> ArcFilePreviewTarget(arcFile)
                | PageState.TextPage content -> TextPreviewTarget content
                | PageState.UnknownPage -> UnknownPreviewTarget()
                | PageState.ErrorPage errMsg -> ErrorPreviewTarget errMsg
                | PageState.LandingDraftPage -> LandingDraftTarget()
                | PageState.NotesDraftPage -> NotesDraftTarget()
                | PageState.NotesSearchPage -> NotesSearchTarget()
            | None -> EmptySelectionTarget()

        ]
    ]