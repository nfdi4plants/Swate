module Renderer.Components.MainContent.DataHubBrowserTarget

open System
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

    let private logCloneFailure (result: GitOperationResult) =
        let failureKind =
            result.FailureKind
            |> Option.map string
            |> Option.defaultValue "Unknown"

        let failureMessage = result.Message |> Option.defaultValue "Clone failed."
        Browser.Dom.console.error ($"[Swate] Clone failed ({failureKind}): {failureMessage}")

    let private cloneAndOpenRepo (projectInfo: ExploreProjectDto) (onSuccess: unit -> unit) =
        promise {
            let! destinationResult = Api.ipcArcVaultApi.pickDirectory (unbox null)

            match destinationResult with
            | Error error when isCancelError error -> ()
            | Error error -> Browser.Dom.console.error ($"[Swate] Could not pick download folder: {error.Message}")
            | Ok destinationFolder ->
                let targetPath =
                    ARCtrl.ArcPathHelper.combine destinationFolder (toRepositoryFolderName projectInfo)

                let cloneRequest: GitCloneRepositoryRequest = {
                    RemoteUrl = projectInfo.http_url_to_repo
                    TargetPath = targetPath
                    Branch = None
                }

                let! cloneResult = Api.ipcGitApi.gitCloneRepository (unbox null) cloneRequest

                match cloneResult with
                | Error error -> Browser.Dom.console.error ($"[Swate] Clone IPC failed: {error.Message}")
                | Ok result when result.Success ->
                    let clonedPath = result.Path |> Option.defaultValue targetPath
                    let! openResult = Api.ipcArcVaultApi.openARCByPath (unbox null) clonedPath

                    match openResult with
                    | Ok _ -> onSuccess ()
                    | Error error ->
                        Browser.Dom.console.error ($"[Swate] Could not open cloned ARC: {error.Message}")
                | Ok result -> logCloneFailure result
        }
        |> Promise.start

    let createActionBtns (onSuccess: unit -> unit) (projectInfo: ExploreProjectDto) : ButtonInfo[] = [|
        ButtonInfo.create (
            "swt:fluent--arrow-download-24-regular swt:size-5",
            "Download repository",
            (fun _ -> cloneAndOpenRepo projectInfo onSuccess)
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

    let closePage _ = pageCtx.setState None
    let closeBrowser () = pageCtx.setState None

    DataHubBrowser.ExplorePanel(
        accounts = authCtx,
        loaders = loaders,
        projectActionBtns = DataHubBrowserHelper.createActionBtns closeBrowser,
        classNames = "swt:grow swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:pb-4",
        onClose = closePage
    )
