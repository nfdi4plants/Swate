module Main.IPC.IGitLabApi

open System
open Fable.Core
open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Swate.Components.DataHubTypes
open Swate.Components.Api.GitLabApi
open Swate.Components.Authentication.Helper
open Main.Auth

let private defaultPublicDataHubBaseUrl = Default_DataHub_Url

let private tryGetActiveGitLabContext () : Result<string * string, GitLabError> =
    match AuthService.tryGetActiveAccountWithToken () with
    | Some(user, token) when not (String.IsNullOrWhiteSpace user.TargetDataHub) -> Ok(user.TargetDataHub, token)
    | Some _ -> Error(GitLabError.InvalidRequest "Active account has no DataHub endpoint configured.")
    | None -> Error GitLabError.Unauthorized

let private tryGetBrowseContext () : Result<string * string, GitLabError> =
    let baseUrl =
        AuthService.tryGetPreferredDataHub ()
        |> Option.filter (fun current -> not (String.IsNullOrWhiteSpace current))
        |> Option.defaultValue defaultPublicDataHubBaseUrl

    let activeToken =
        AuthService.tryGetActiveAccountWithToken ()
        |> Option.map snd
        |> Option.defaultValue ""

    Ok(baseUrl, activeToken)

let api: IGitLabApi = {
    loadAllRepos =
        fun (_event: IpcMainEvent) (query: ExploreRepoQuery) -> promise {
            match tryGetBrowseContext () with
            | Error err -> return Error err
            | Ok(baseUrl, activeToken) ->
                let requestPat = if query.Visibility.IsSome then "" else activeToken

                return!
                    GitLabApi.ListProjects(
                        baseUrl,
                        requestPat,
                        page = query.Page,
                        perPage = query.PerPage,
                        search = query.SearchTerm,
                        orderBy = query.OrderBy,
                        sort = query.Sort,
                        ?visibility = query.Visibility
                    )
        }
    loadMostStarredRepos =
        fun (_event: IpcMainEvent) (query: ExploreMostStarredQuery) -> promise {
            match tryGetBrowseContext () with
            | Error err -> return Error err
            | Ok(baseUrl, activeToken) ->
                let requestPat = if query.Visibility.IsSome then "" else activeToken

                return!
                    GitLabApi.ListExploreMostStarred(
                        baseUrl,
                        requestPat,
                        page = query.Page,
                        perPage = query.PerPage,
                        search = query.SearchTerm,
                        ?visibility = query.Visibility
                    )
        }
    loadUserRepos =
        fun (_event: IpcMainEvent) (query: ExploreRepoQuery) -> promise {
            match tryGetActiveGitLabContext () with
            | Error err -> return Error err
            | Ok(baseUrl, token) ->
                return!
                    GitLabApi.ListUserPersonalProjects(
                        baseUrl,
                        token,
                        page = query.Page,
                        perPage = query.PerPage,
                        search = query.SearchTerm,
                        orderBy = query.OrderBy,
                        sort = query.Sort
                    )
        }
    loadOrganisationGroups =
        fun (_event: IpcMainEvent) (query: ExploreGroupsQuery) -> promise {
            match tryGetActiveGitLabContext () with
            | Error err -> return Error err
            | Ok(baseUrl, token) ->
                return! GitLabApi.ListGroupsForCurrentUser(baseUrl, token, page = query.Page, perPage = query.PerPage)
        }
    loadOrganisationRepos =
        fun (_event: IpcMainEvent) (query: ExploreGroupProjectsQuery) -> promise {
            match tryGetActiveGitLabContext () with
            | Error err -> return Error err
            | Ok(baseUrl, token) ->
                return!
                    GitLabApi.ListGroupProjects(
                        baseUrl,
                        token,
                        query.GroupId,
                        page = query.Page,
                        perPage = query.PerPage,
                        includeSubgroups = query.IncludeSubgroups,
                        withShared = query.WithShared,
                        search = query.SearchTerm,
                        orderBy = query.OrderBy,
                        sort = query.Sort
                    )
        }
}
