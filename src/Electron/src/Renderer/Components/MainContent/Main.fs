module Renderer.Components.MainContent.Main

open Feliz
open Renderer.Types
open Swate.Electron.Shared
open Renderer.Components.MainContent.ArcFilePreviewTarget
open Renderer.Components.MainContent.DataHubBrowserTarget
open Renderer.Components.MainContent.EmptySelectionTarget
open Renderer.Components.MainContent.ErrorViewTarget
open Renderer.Components.MainContent.GitDiffTarget
open Renderer.Components.MainContent.GitMergeConflictTarget
open Renderer.Components.MainContent.GitUnsupportedTarget
open Renderer.Components.MainContent.GitLfsPointerTarget
open Renderer.Components.MainContent.LandingDraftTarget
open Renderer.Components.MainContent.NotesDraftTarget
open Renderer.Components.MainContent.NotesSearchTarget
open Renderer.Components.MainContent.TextPreviewTarget
open Renderer.Components.MainContent.UnknownPreviewTarget
open Renderer.Components.MainContent.SettingsPageTarget

module private MainHelper =

    open Fable.Core

    let loadTemplates =
        fun () ->
            promise {
                let! json =
                    Swate.Components.Api.SwateApi.SwateTemplateApi.getTemplates ()
                    |> Async.StartAsPromise

                return Ok(ARCtrl.Json.Templates.fromJsonString json)
            }
            |> Promise.catch (fun error ->
                // Handle error, e.g., log it or show a notification
                Error(sprintf "Error loading templates: %s" error.Message)
            )


module private LazyComponents =

    open Swate.Components.Primitive

    [<ReactComponent>]
    let FullPageLoadingSpinner (text: string) =
        Html.div [
            prop.className "swt:flex-1 swt:flex swt:min-w-0 swt:min-h-0 swt:grow swt:justify-center swt:items-center"
            prop.children [
                Swate.Components.Primitive.LoadingSpinner.LoadingSpinner.LoadingSpinner(
                    size = DaisyuiSize.XL,
                    color = DaisyuiColors.Primary,
                    text = text
                )
            ]
        ]

    [<ReactLazyComponent>]
    let LazySettingPage () =
        Renderer.Components.MainContent.SettingsPageTarget.SettingsPage()

    [<ReactLazyComponent>]
    let LazyMarkdownEditorTarget (content: string) =
        Renderer.Components.MainContent.MarkdownEditorTargetView.MarkdownEditorTarget(content)

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactMemoComponent>]
let Main (appRootPath: ArcRootPath, pageState: PageState option) =
    Swate.Components.Composite.Template.TemplateCacheProvider.TemplateCacheProvider(
        loadTemplates = MainHelper.loadTemplates,
        children =
            Html.div [
                prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:flex swt:justify-center swt:overflow-hidden"
                prop.children [
                    match appRootPath, pageState with
                    | _, Some PageState.DataHubBrowser -> DataHubBrowserTarget()
                    | _, Some PageState.SettingsPage ->
                        React.Suspense(
                            [ LazyComponents.LazySettingPage() ],
                            fallback = LazyComponents.FullPageLoadingSpinner("Loading settings...")
                        )
                    | None, _ ->
                        Html.div [
                            prop.className
                                "swt:flex-1 swt:min-w-0 swt:min-h-0 swt:flex swt:justify-center swt:items-center"
                            prop.children [ Renderer.Components.InitState.InitState() ]
                        ]
                    | Some _, Some(PageState.ArcFilePage arcFile) -> ArcFilePreviewTarget arcFile
                    | Some _, Some(PageState.MarkdownPage content) ->
                        React.Suspense(
                            [ LazyComponents.LazyMarkdownEditorTarget(content) ],
                            fallback = LazyComponents.FullPageLoadingSpinner("Loading markdown editor...")
                        )
                    | Some _, Some(PageState.TextPage content) -> TextPreviewTarget content
                    | Some _, Some PageState.UnknownPage -> UnknownPreviewTarget()
                    | Some _, Some(PageState.ErrorPage errMsg) -> ErrorViewTarget errMsg
                    // | Some _, Some PageState.LandingDraftPage -> LandingDraftTarget()
                    | Some _, Some PageState.NotesDraftPage -> NotesDraftTarget()
                    | Some _, Some PageState.NotesSearchPage -> NotesSearchTarget()
                    | Some _, Some(PageState.GitDiffPage diffData) -> GitDiffTarget.Main diffData
                    | Some _, Some(PageState.GitMergeConflictPage mergeData) -> GitMergeConflictTarget.Main mergeData
                    | Some _, Some(PageState.GitUnsupportedPage unsupportedPage) ->
                        GitUnsupportedTarget.Main unsupportedPage
                    | Some _, Some(PageState.GitLfsPointerPage pointerPage) -> GitLfsPointerTarget.Main pointerPage
                    | Some _, None -> EmptySelectionTarget()
                ]
            ]
    )
