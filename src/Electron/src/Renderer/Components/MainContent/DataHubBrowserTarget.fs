module Renderer.Components.MainContent.DataHubBrowserTarget

open System
open Feliz
open Swate.Components
open Swate.Components.DataHubTypes
open Swate.Components.Api.GitLabApi
open Swate.Components.Types.Actionbar
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

    let loadAllRepos (query: ExploreRepoQuery) =
        Api.ipcGitLabApi.loadAllRepos query

    let loadMostStarredRepos (query: ExploreMostStarredQuery) =
        Api.ipcGitLabApi.loadMostStarredRepos query

    let loadUserRepos (query: ExploreRepoQuery) =
        Api.ipcGitLabApi.loadUserRepos query

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
            | Error error -> Browser.Dom.console.error ($"[Swate] Could not pick download folder: {error.Message}")
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
                    match! Api.ipcArcVaultApi.openARCByPath clonedPath with
                    | Ok _ -> closeBrowser ()
                    | Error error -> Browser.Dom.console.error ($"[Swate] Could not open cloned ARC: {error.Message}")
        }
        |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:flex swt:flex-col"
        prop.children [
            GitSidebar.OperationStatusNotice(
                ?runStatus = runStatus,
                ?errorNotice = gitStateCtx.state.ErrorNotice,
                ?warningNotice = gitStateCtx.state.WarningNotice,
                busyTestId = "DataHubCloneProgressNotice",
                errorTestId = "DataHubCloneErrorNotice"
            )
            Html.div [
                prop.className "swt:px-2 swt:pt-2"
                prop.children [
                    GitSidebar.DownloadLargeFilesToggle(
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
