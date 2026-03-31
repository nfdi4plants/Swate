module Renderer.Components.MainContent.DataHubBrowserTarget

open Feliz
open Swate.Components
open Swate.Components.DataHubTypes

[<ReactComponent>]
let DataHubBrowserTarget () =
    let authCtx = Renderer.Context.AuthStateCtx.useAuthState ()

    let loaders: ExploreLoaders = {
        LoadAllRepos = loadAllRepos
        LoadMostStarredRepos = loadMostStarredRepos
        LoadUserRepos = loadUserRepos
        LoadOrganisationGroups = loadOrganisationGroups
        LoadOrganisationRepos = loadOrganisationRepos
    }

    DataHubBrowser.ExplorePanel(accounts = authCtx)