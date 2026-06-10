module Renderer.Components.MainContent.DataHubBrowserTarget

open System
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components
open Swate.Components.Page.DataHub
open Swate.Components.Page.DataHub.DataHubTypes
open Swate.Components.Api.GitLabApi
open Swate.Components.Primitive.Actionbar.Types
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Electron.Shared.GitTypes

module DataHubBrowserHelper =
    let isCancelError (error: exn) =
        String.Equals(error.Message, "Cancelled", StringComparison.OrdinalIgnoreCase)

    let toRepositoryFolderName (projectInfo: ExploreProjectDto) =
        projectInfo.path_with_namespace.Split('/')
        |> Array.tryLast
        |> Option.filter (fun segment -> not (String.IsNullOrWhiteSpace segment))
        |> Option.defaultValue projectInfo.name

    let createActionBtns (onClone: ExploreProjectDto -> unit) (projectInfo: ExploreProjectDto) : ButtonInfo[] = [|
        ButtonInfo.create (
            "swt:fluent--arrow-download-24-regular swt:size-5",
            "Download repository",
            (fun _ -> onClone projectInfo)
        )
    |]


[<ReactComponent>]
let DataHubBrowserTarget () =
    let authCtx = Renderer.Context.AuthStateContext.useAuthStateCtx ()
    let pageCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
    let errorCtx = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    let onArcError =
        createErrorModalCallback errorCtx.enqueue "Could not open ARC" appStateCtx

    let loadAllRepos (query: ExploreRepoQuery) = Api.ipcGitLabApi.loadAllRepos query

    let loadMostStarredRepos (query: ExploreMostStarredQuery) =
        Api.ipcGitLabApi.loadMostStarredRepos query

    let loadUserRepos (query: ExploreRepoQuery) = Api.ipcGitLabApi.loadUserRepos query

    let loadOrganisationGroups (query: ExploreGroupsQuery) =
        Api.ipcGitLabApi.loadOrganisationGroups query

    let loadOrganisationRepos (query: ExploreGroupProjectsQuery) =
        Api.ipcGitLabApi.loadOrganisationRepos query

    let loaders: ExploreLoaders = {
        LoadAllRepos = loadAllRepos
        LoadMostStarredRepos = loadMostStarredRepos
        LoadUserRepos = loadUserRepos
        LoadOrganisationGroups = loadOrganisationGroups
        LoadOrganisationRepos = loadOrganisationRepos
    }

    let closePage _ = pageCtx.setState None
    let closeBrowser () = pageCtx.setState None
    let isCloneBusy = gitStateCtx.state.BusyOperation.IsSome
    let runStatus = Renderer.Context.GitWorkflow.currentRunStatus gitStateCtx.state

    let cloneAndOpenRepo (projectInfo: ExploreProjectDto) =
        promise {
            let! destinationResult = Api.ipcArcVaultApi.pickDirectory ()

            match destinationResult with
            | Error error when DataHubBrowserHelper.isCancelError error -> ()
            | Error error -> onArcError $"Could not pick download folder: {error.Message}"
            | Ok destinationFolder ->
                let targetPath =
                    ARCtrl.ArcPathHelper.combine
                        destinationFolder
                        (DataHubBrowserHelper.toRepositoryFolderName projectInfo)

                let cloneRequest: GitCloneRepositoryRequest = {
                    RemoteUrl = projectInfo.http_url_to_repo
                    TargetPath = targetPath
                    Branch = None
                    DownloadLargeFiles = gitStateCtx.state.DownloadLargeFiles
                }

                match! gitStateCtx.cloneRepository cloneRequest with
                | Error _ -> ()
                | Ok clonedPath ->
                    let! wasOpened = Renderer.Components.Helper.ArcVaultHelper.openArcByPath onArcError clonedPath

                    if wasOpened then
                        closeBrowser ()
        }
        |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:flex swt:flex-col"
        prop.children [
            Swate.Components.Page.GitSidebar.OperationStatusNotice(
                ?runStatus = runStatus,
                ?errorNotice = gitStateCtx.state.ErrorNotice,
                ?warningNotice = gitStateCtx.state.WarningNotice,
                busyTestId = "DataHubCloneProgressNotice",
                errorTestId = "DataHubCloneErrorNotice"
            )
            Html.div [
                prop.className "swt:px-2 swt:pt-2"
                prop.children [
                    Swate.Components.Page.GitSidebar.DownloadLargeFilesToggle(
                        gitStateCtx.state.DownloadLargeFiles,
                        isCloneBusy,
                        gitStateCtx.saveDownloadLargeFiles,
                        testId = "DataHubDownloadLargeFilesCheckbox",
                        description = "Reuse the Git LFS download preference for repository clones from DataHub."
                    )
                ]
            ]
            DataHubBrowser.ExplorePanel(
                accounts = authCtx,
                loaders = loaders,
                projectActionBtns = DataHubBrowserHelper.createActionBtns cloneAndOpenRepo,
                classNames = "swt:grow swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:pb-4",
                onClose = closePage
            )
        ]
    ]
