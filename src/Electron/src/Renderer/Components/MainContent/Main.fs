module Renderer.Components.MainContent.Main

open Feliz
open Renderer.Types
open Swate.Electron.Shared
open Renderer.Components.MainContent.ArcFilePreviewTarget
open Renderer.Components.MainContent.ArcObjectExplorerTarget
open Renderer.Components.MainContent.DataHubBrowserTarget
open Renderer.Components.MainContent.EmptySelectionTarget
open Renderer.Components.MainContent.ErrorViewTarget
open Renderer.Components.MainContent.GitDiffTarget
open Renderer.Components.MainContent.GitMergeConflictTarget
open Renderer.Components.MainContent.GitUnsupportedTarget
open Renderer.Components.MainContent.LandingDraftTarget
open Renderer.Components.MainContent.NotesDraftTarget
open Renderer.Components.MainContent.NotesSearchTarget
open Renderer.Components.MainContent.TextPreviewTarget
open Renderer.Components.MainContent.UnknownPreviewTarget

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (appRootPath: ArcRootPath, pageState: PageState option, leftSidebarTarget: LeftSidebarPage) =
    Html.div [
        prop.className "swt:w-full swt:h-full swt:flex swt:justify-center swt:overflow-hidden"
        prop.children [
            match appRootPath, pageState with
            | _, Some PageState.DataHubBrowser -> DataHubBrowserTarget()
            | None, _ ->
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                    prop.children [ Renderer.Components.InitState.InitState() ]
                ]
            | Some _, _ when leftSidebarTarget = LeftSidebarPage.ArcObjectExplorer -> ArcObjectExplorerTarget.Main()
            | Some _, Some(PageState.ArcFilePage arcFile) -> ArcFilePreviewTarget arcFile
            | Some _, Some(PageState.TextPage content) -> TextPreviewTarget content
            | Some _, Some PageState.UnknownPage -> UnknownPreviewTarget()
            | Some _, Some(PageState.ErrorPage errMsg) -> ErrorViewTarget errMsg
            | Some _, Some PageState.LandingDraftPage -> LandingDraftTarget()
            | Some _, Some PageState.NotesDraftPage -> NotesDraftTarget()
            | Some _, Some PageState.NotesSearchPage -> NotesSearchTarget()
            | Some _, Some(PageState.GitDiffPage diffData) -> GitDiffTarget.Main diffData
            | Some _, Some(PageState.GitMergeConflictPage mergeData) -> GitMergeConflictTarget.Main mergeData
            | Some _, Some(PageState.GitUnsupportedPage unsupportedPage) -> GitUnsupportedTarget.Main unsupportedPage
            | Some _, None -> EmptySelectionTarget()
        ]
    ]