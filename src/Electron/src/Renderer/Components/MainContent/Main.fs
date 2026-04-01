module Renderer.Components.MainContent.Main

open Feliz
open Swate.Electron.Shared
open Renderer.Components.MainContent.Types
open Renderer.Types
open Renderer.Components.MainContent.ArcFilePreviewTarget
open Renderer.Components.MainContent.TextPreviewTarget
open Renderer.Components.MainContent.UnknownPreviewTarget
open Renderer.Components.MainContent.ErrorPreviewTarget
open Renderer.Components.MainContent.LandingDraftTarget
open Renderer.Components.MainContent.NotesDraftTarget
open Renderer.Components.MainContent.NotesSearchTarget
open Renderer.Components.MainContent.EmptySelectionTarget
open Renderer.Components.MainContent.DataHubBrowserTarget

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (appRootPath: ArcRootPath, pageState: PageState option) =

    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center"
        prop.children [
            match appRootPath, pageState with
            | _, Some PageState.DataHubBrowser -> DataHubBrowserTarget()
            | None, _ -> // empty state, no vault opened
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                    prop.children [ Renderer.Components.InitState.InitState() ]
                ]
            | Some _, pageState ->
                match pageState with
                | Some page ->
                    match page with
                    | PageState.ArcFilePage arcFile -> ArcFilePreviewTarget(arcFile)
                    | PageState.TextPage content -> TextPreviewTarget content
                    | PageState.UnknownPage -> UnknownPreviewTarget()
                    | PageState.ErrorPage errMsg -> ErrorPreviewTarget errMsg
                    | PageState.LandingDraftPage -> LandingDraftTarget()
                    | PageState.NotesDraftPage -> NotesDraftTarget()
                    | PageState.NotesSearchPage -> NotesSearchTarget()
                    | PageState.DataHubBrowser -> DataHubBrowserTarget()
                | None -> EmptySelectionTarget()
        ]
    ]