module Renderer.Components.MainContent.DataHubBrowserTarget

open Feliz
open Swate.Components
open Swate.Components.DataHubTypes

[<ReactComponent>]
let DataHubBrowserTarget () =
    let authCtx = Renderer.Context.AuthStateCtx.useAuthState ()

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

    DataHubBrowser.ExplorePanel(accounts = authCtx, loaders = loaders)