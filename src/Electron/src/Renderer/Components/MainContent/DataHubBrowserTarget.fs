module Renderer.Components.MainContent.DataHubBrowserTarget

open System
open System.IO
open Feliz
open Swate.Components
open Swate.Components.DataHubTypes
open Swate.Components.Api.GitLabApi
open Swate.Components.Types.Actionbar
open Swate.Electron.Shared.GitTypes

module DataHubBrowserHelper =
    let private isCancelError (error: exn) =
        String.Equals(error.Message, "Cancelled", StringComparison.OrdinalIgnoreCase)

    let private toRepositoryFolderName (projectInfo: ExploreProjectDto) =
        projectInfo.path_with_namespace.Split('/')
        |> Array.tryLast
        |> Option.filter (fun segment -> not (String.IsNullOrWhiteSpace segment))
        |> Option.defaultValue projectInfo.name

    let private cloneAndOpenRepo (projectInfo: ExploreProjectDto) =
        promise {
            let! destinationResult = Api.ipcArcVaultApi.pickDirectory (unbox null)

            match destinationResult with
            | Error error when isCancelError error -> ()
            | Error error -> Browser.Dom.console.error ($"[Swate] Could not pick download folder: {error.Message}")
            | Ok destinationFolder ->
                let targetPath =
                    ARCtrl.ArcPathHelper.combine destinationFolder (toRepositoryFolderName projectInfo)

                Browser.Dom.window.alert targetPath

        // ----
        // TODO: Wire up actual git call for cloning, waiting for git branch from @caro
        // ----

        // let cloneRequest: GitCloneRepositoryRequest = {
        //     // Let main-process git services own remote validation/auth handling.
        //     RemoteUrl = projectInfo.web_url
        //     TargetPath = targetPath
        //     Branch = None
        // }

        // let! cloneResult = Api.ipcGitApi.gitCloneRepository (unbox null) cloneRequest

        // match cloneResult with
        // | Error error -> Browser.Dom.console.error ($"[Swate] Clone IPC failed: {error.Message}")
        // | Ok result when result.Success ->
        //     match result.Path with
        //     | Some clonedPath ->
        //         let! openResult = Api.ipcArcVaultApi.openARCByPath (unbox null) clonedPath

        //         match openResult with
        //         | Ok _ -> ()
        //         | Error error ->
        //             Browser.Dom.console.error ($"[Swate] Could not open cloned ARC: {error.Message}")
        //     | None -> Browser.Dom.console.error ("[Swate] Clone finished but no path was returned.")
        // | Ok result ->
        //     let failureMessage = result.Message |> Option.defaultValue ""

        //     Browser.Dom.console.error ($"[Swate] Clone failed: {result.FailureKind} {failureMessage}")
        }
        |> Promise.start

    let createActionBtns (projectInfo: ExploreProjectDto) : ButtonInfo[] = [|
        ButtonInfo.create (
            "swt:fluent--arrow-download-24-regular swt:size-5",
            "Download repository",
            (fun _ -> cloneAndOpenRepo projectInfo)
        )
    |]


[<ReactComponent>]
let DataHubBrowserTarget () =
    let authCtx = Renderer.Context.AuthStateCtx.useAuthState ()
    let pageCtx = Renderer.Context.PageStateCtx.usePageState ()

    let loadAllRepos (query: ExploreRepoQuery) =
        Api.ipcGitLabApi.loadAllRepos (unbox null) query

    let loadMostStarredRepos (query: ExploreMostStarredQuery) =
        Api.ipcGitLabApi.loadMostStarredRepos (unbox null) query

    let loadUserRepos (query: ExploreRepoQuery) =
        Api.ipcGitLabApi.loadUserRepos (unbox null) query

    let loadOrganisationGroups (query: ExploreGroupsQuery) =
        Api.ipcGitLabApi.loadOrganisationGroups (unbox null) query

    let loadOrganisationRepos (query: ExploreGroupProjectsQuery) =
        Api.ipcGitLabApi.loadOrganisationRepos (unbox null) query

    let loaders: ExploreLoaders = {
        LoadAllRepos = loadAllRepos
        LoadMostStarredRepos = loadMostStarredRepos
        LoadUserRepos = loadUserRepos
        LoadOrganisationGroups = loadOrganisationGroups
        LoadOrganisationRepos = loadOrganisationRepos
    }

    let closePage = fun _ -> pageCtx.setState None

    DataHubBrowser.ExplorePanel(
        accounts = authCtx,
        loaders = loaders,
        projectActionBtns = DataHubBrowserHelper.createActionBtns,
        classNames = "swt:grow swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:pb-4",
        onClose = closePage
    )