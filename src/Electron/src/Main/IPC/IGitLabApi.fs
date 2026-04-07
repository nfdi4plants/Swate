module Main.IPC.IGitLabApi

open System
open Fable.Core
open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Swate.Components.DataHubTypes
open Swate.Components.Api.GitLabApi
open Main.Auth

let private tryGetActiveGitLabContext () : Result<string * string, GitLabError> =
    match AuthService.tryGetActiveAccountWithToken () with
    | Some(user, token) when not (String.IsNullOrWhiteSpace user.TargetDataHub) -> Ok(user.TargetDataHub, token)
    | Some _ -> Error(GitLabError.InvalidRequest "Active account has no DataHub endpoint configured.")
    | None -> Error GitLabError.Unauthorized

let private tryGetBrowseContext () : Result<string * string, GitLabError> =
    match AuthService.tryGetPreferredDataHub () with
    | Some baseUrl when not (String.IsNullOrWhiteSpace baseUrl) ->
        let activeToken =
            AuthService.tryGetActiveAccountWithToken ()
            |> Option.map snd
            |> Option.defaultValue ""

        Ok(baseUrl, activeToken)
    | _ -> Error(GitLabError.InvalidRequest "No DataHub endpoint available. Sign in to a DataHub first.")

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
                let userId =
                    AuthService.tryGetActiveAccountWithToken ()
                    |> Option.map fst
                    |> Option.bind (fun user ->
                        let mutable parsed = 0

                        if Int32.TryParse(user.AccountId, &parsed) then
                            Some parsed
                        else
                            None
                    )

                return!
                    GitLabApi.ListUserPersonalProjects(
                        baseUrl,
                        token,
                        ?userId = userId,
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
