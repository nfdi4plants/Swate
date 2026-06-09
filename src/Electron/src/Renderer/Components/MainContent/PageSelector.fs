module Renderer.Components.MainContent.PageSelector

open Feliz
open Renderer.Types
open Swate.Electron.Shared
open Renderer.Components.MainContent.ArcFilePreviewTarget
open Renderer.Components.MainContent.DataHubBrowserTarget
open Renderer.Components.MainContent.EmptySelectionTarget
open Renderer.Components.MainContent.ErrorViewTarget
open Renderer.Components.MainContent.NotesDraftTarget
open Renderer.Components.MainContent.NotesSearchTarget
open Renderer.Components.MainContent.TextPreviewTarget
open Renderer.Components.MainContent.UnknownPreviewTarget

let Main (appRootPath: ArcRootPath) (pageState: PageState option) =
    match appRootPath, pageState with
    | _, Some PageState.DataHubBrowser -> DataHubBrowserTarget()
    | _, Some PageState.SettingsPage ->
        Html.div [
            prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
            prop.testId "main-content-settings-page"
            prop.children [
                Swate.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage()
            ]
        ]
    | None, _ ->
        Html.div [
            prop.className
                "swt:flex-1 swt:min-w-0 swt:min-h-0 swt:flex swt:justify-center swt:items-center"
            prop.children [ Renderer.Components.InitState.InitState() ]
        ]
    | Some _, Some(PageState.ArcFilePage arcFile) -> ArcFilePreviewTarget arcFile
    | Some _, Some(PageState.TextPage content) -> TextPreviewTarget content
    | Some _, Some PageState.UnknownPage -> UnknownPreviewTarget()
    | Some _, Some(PageState.ErrorPage errMsg) -> ErrorViewTarget errMsg
    | Some _, Some PageState.NotesDraftPage -> NotesDraftTarget()
    | Some _, Some PageState.NotesSearchPage -> NotesSearchTarget()
    | Some _, Some(PageState.GitDiffPage diffData) -> GitDiffTarget.Main diffData
    | Some _, Some(PageState.GitMergeConflictPage mergeData) -> GitMergeConflictTarget.Main mergeData
    | Some _, Some(PageState.GitUnsupportedPage unsupportedPage) ->
        GitUnsupportedTarget.Main unsupportedPage
    | Some _, None -> EmptySelectionTarget()
