module Swate.Components.Api.GitLabApi

open System
open Fable.Core
open Fable.Core.JsInterop
open Fetch
open Swate.Components.Api.Helper

// Maintainer notes:
// - This module intentionally uses the same Fable.Fetch request style as Authentication.getUserAPIRequest.
// - PAT auth uses PRIVATE-TOKEN by default.
// - "Trending" is derived from REST data because GitLab REST has no dedicated trending endpoint.

[<RequireQualifiedAccess>]
type ProjectSortField =
    | Name
    | CreatedAt
    | UpdatedAt
    | StarCount
    | LastActivityAt

[<RequireQualifiedAccess>]
type SortDirection =
    | Asc
    | Desc

[<RequireQualifiedAccess>]
type PaginationMode =
    | Offset
    | Keyset

[<RequireQualifiedAccess>]
type ExploreScope =
    | MostStarred
    | Trending
    | MyRepos
    | GroupRepos

[<RequireQualifiedAccess>]
type TrendingStrategy =
    | ByLastActivity
    | ByStarsAndRecency

[<RequireQualifiedAccess>]
type UiProjectSort =
    | Name
    | DateCreated
    | DateUpdated

[<RequireQualifiedAccess>]
type GitLabError =
    | NetworkError of exn
    | Unauthorized
    | Forbidden
    | NotFound
    | HttpError of int
    | DecodeError of exn
    | InvalidRequest of string

type ExploreNamespaceDto = {
    id: int
    name: string
    kind: string
    full_path: string
}

type ExploreProjectDto = {
    id: int
    name: string
    path_with_namespace: string
    name_with_namespace: string
    description: string option
    web_url: string
    avatar_url: string option
    visibility: string option
    star_count: int
    created_at: string
    updated_at: string
    last_activity_at: string
    tag_list: string array
    license_name: string option
    ``namespace``: ExploreNamespaceDto
}

type CurrentUserDto = {
    id: int
    username: string
    name: string
    avatar_url: string option
}

type GroupDto = {
    id: int
    name: string
    full_path: string
    web_url: string
    avatar_url: string option
}

type PaginationMetadata = {
    Link: string option
    NextPage: int option
    Page: int option
    PerPage: int option
    PrevPage: int option
    Total: int option
    TotalPages: int option
    NextCursor: string option
    PrevCursor: string option
}

type PagedResponse<'T> = {
    Items: 'T array
    Pagination: PaginationMetadata
}

type GroupProjectsDto = {
    Group: GroupDto
    Projects: PagedResponse<ExploreProjectDto>
}

type ExploreBootstrapDto = {
    MostStarred: PagedResponse<ExploreProjectDto>
    Trending: PagedResponse<ExploreProjectDto>
    CurrentUser: CurrentUserDto
    MyRepos: PagedResponse<ExploreProjectDto>
    Groups: PagedResponse<GroupDto>
    GroupRepos: GroupProjectsDto array
}

type private GitLabNamespaceResponse = {
    id: int
    name: string
    kind: string
    full_path: string
}

type private GitLabProjectResponse = {
    id: int
    name: string
    path_with_namespace: string
    name_with_namespace: string
    description: string option
    web_url: string
    avatar_url: string option
    visibility: string option
    star_count: int
    created_at: string
    updated_at: string
    last_activity_at: string
    topics: string array option
    tag_list: string array option
    license: {| name: string option |} option
    ``namespace``: GitLabNamespaceResponse
}

type private GitLabCurrentUserResponse = {
    id: int
    username: string
    name: string
    avatar_url: string option
}

type private GitLabGroupResponse = {
    id: int
    name: string
    full_path: string
    web_url: string
    avatar_url: string option
}

module private Internals =

    let private mapSortField =
        function
        | ProjectSortField.Name -> "name"
        | ProjectSortField.CreatedAt -> "created_at"
        | ProjectSortField.UpdatedAt -> "updated_at"
        | ProjectSortField.StarCount -> "star_count"
        | ProjectSortField.LastActivityAt -> "last_activity_at"

    let private mapSortDirection =
        function
        | SortDirection.Asc -> "asc"
        | SortDirection.Desc -> "desc"

    let private mapPaginationMode =
        function
        | PaginationMode.Offset -> "offset"
        | PaginationMode.Keyset -> "keyset"

    let mapUiSortField =
        function
        | UiProjectSort.Name -> ProjectSortField.Name
        | UiProjectSort.DateCreated -> ProjectSortField.CreatedAt
        | UiProjectSort.DateUpdated -> ProjectSortField.UpdatedAt

    let mapHttpError status =
        match status with
        | 401 -> GitLabError.Unauthorized
        | 403 -> GitLabError.Forbidden
        | 404 -> GitLabError.NotFound
        | code -> GitLabError.HttpError code

    let makeGetRequestOptions (pat: string option) (acceptJson: bool) =
        let headers = [
            match pat with
            | Some token when not (String.IsNullOrWhiteSpace token) -> HttpRequestHeaders.Custom("PRIVATE-TOKEN", token)
            | _ -> ()
            if acceptJson then
                HttpRequestHeaders.Accept "application/json"
        ]

        [
            RequestProperties.Method HttpMethod.GET
            requestHeaders headers
        ]

    let tryGetHeader (response: Fetch.Types.Response) (name: string) : string option =
        response.Headers.get name |> Option.ofObj

    let tryParseInt (value: string option) : int option =
        match value with
        | Some raw when not (String.IsNullOrWhiteSpace raw) ->
            let mutable parsed = 0

            if Int32.TryParse(raw, &parsed) then Some parsed else None
        | _ -> None

    let readPagination (response: Fetch.Types.Response) : PaginationMetadata = {
        Link = tryGetHeader response "link"
        NextPage = tryParseInt (tryGetHeader response "x-next-page")
        Page = tryParseInt (tryGetHeader response "x-page")
        PerPage = tryParseInt (tryGetHeader response "x-per-page")
        PrevPage = tryParseInt (tryGetHeader response "x-prev-page")
        Total = tryParseInt (tryGetHeader response "x-total")
        TotalPages = tryParseInt (tryGetHeader response "x-total-pages")
        NextCursor = tryGetHeader response "x-next-cursor"
        PrevCursor = tryGetHeader response "x-prev-cursor"
    }

    let addString (name: string) (value: string option) (query: (string * obj) list) =
        match value with
        | Some x -> (name, box x) :: query
        | None -> query

    let addInt (name: string) (value: int option) (query: (string * obj) list) =
        match value with
        | Some x -> (name, box x) :: query
        | None -> query

    let addBool (name: string) (value: bool option) (query: (string * obj) list) =
        match value with
        | Some x -> (name, box x) :: query
        | None -> query

    let addSortField (value: ProjectSortField option) (query: (string * obj) list) =
        value |> Option.map mapSortField |> addString "order_by" <| query

    let addSortDirection (value: SortDirection option) (query: (string * obj) list) =
        value |> Option.map mapSortDirection |> addString "sort" <| query

    let addPaginationMode (value: PaginationMode option) (query: (string * obj) list) =
        value |> Option.map mapPaginationMode |> addString "pagination" <| query

    let toExploreNamespaceDto (ns: GitLabNamespaceResponse) : ExploreNamespaceDto = {
        id = ns.id
        name = ns.name
        kind = ns.kind
        full_path = ns.full_path
    }

    let toExploreProjectDto (project: GitLabProjectResponse) : ExploreProjectDto = {
        id = project.id
        name = project.name
        path_with_namespace = project.path_with_namespace
        description = project.description
        web_url = project.web_url
        avatar_url = project.avatar_url
        visibility = project.visibility
        star_count = project.star_count
        created_at = project.created_at
        updated_at = project.updated_at
        last_activity_at = project.last_activity_at
        tag_list = project.topics |> Option.orElse project.tag_list |> Option.defaultValue [||]
        license_name = project.license |> Option.bind (fun license -> license.name)
        ``namespace`` = toExploreNamespaceDto project.``namespace``
        name_with_namespace = project.name_with_namespace
    }

    let toGroupDto (group: GitLabGroupResponse) : GroupDto = {
        id = group.id
        name = group.name
        full_path = group.full_path
        web_url = group.web_url
        avatar_url = group.avatar_url
    }

    let toCurrentUserDto (user: GitLabCurrentUserResponse) : CurrentUserDto = {
        id = user.id
        username = user.username
        name = user.name
        avatar_url = user.avatar_url
    }

    [<Emit("Date.parse($0)")>]
    let private parseDateMilliseconds (_value: string) : float = jsNative

    [<Emit("Date.now()")>]
    let private nowMilliseconds () : float = jsNative

    let private trendingScore (project: ExploreProjectDto) =
        let updated = parseDateMilliseconds project.updated_at

        if Double.IsNaN(updated) then
            float project.star_count
        else
            let ageInDays = max 0.0 ((nowMilliseconds () - updated) / 86400000.0)
            float project.star_count + (30.0 / (1.0 + ageInDays))

    let rankTrending (projects: ExploreProjectDto array) =
        projects |> Array.sortByDescending trendingScore

    let sendGet<'T> (url: string) (pat: string option) : JS.Promise<Result<PagedResponse<'T>, GitLabError>> =

        promise {
            let requestOptions = makeGetRequestOptions pat true

            try
                let! response = fetchUnsafe url requestOptions

                if not response.Ok then
                    return Error(mapHttpError response.Status)
                else
                    Browser.Dom.console.log (response)
                    let pagination = readPagination response

                    try
                        let! payload = response.json<'T array> ()

                        return
                            Ok {
                                Items = payload
                                Pagination = pagination
                            }
                    with decodeError ->
                        return Error(GitLabError.DecodeError decodeError)
            with networkError ->
                return Error(GitLabError.NetworkError networkError)
        }

    let sendGetSingle<'T> (url: string) (pat: string) : JS.Promise<Result<'T, GitLabError>> = promise {
        if String.IsNullOrWhiteSpace pat then
            return Error(GitLabError.InvalidRequest "Personal Access Token is required.")
        else
            let requestOptions = makeGetRequestOptions (Some pat) true

            try
                let! response = fetchUnsafe url requestOptions

                if not response.Ok then
                    return Error(mapHttpError response.Status)
                else
                    try
                        let! payload = response.json<'T> ()
                        return Ok payload
                    with decodeError ->
                        return Error(GitLabError.DecodeError decodeError)
            with networkError ->
                return Error(GitLabError.NetworkError networkError)
    }

[<AttachMembers>]
type GitLabApi =

    /// GET /api/v4/user
    static member GetCurrentUser(baseUrl: string, pat: string) : JS.Promise<Result<CurrentUserDto, GitLabError>> = promise {
        let url = $"{baseUrl.TrimEnd('/')}/api/v4/user"
        let! response = Internals.sendGetSingle<GitLabCurrentUserResponse> url pat
        return response |> Result.map Internals.toCurrentUserDto
    }

    /// GET /api/v4/projects
    static member ListProjects
        (
            baseUrl: string,
            pat: string,
            ?page: int,
            ?perPage: int,
            ?pagination: PaginationMode,
            ?orderBy: ProjectSortField,
            ?sort: SortDirection,
            ?membership: bool,
            ?owned: bool,
            ?starred: bool,
            ?search: string,
            ?visibility: string,
            ?minAccessLevel: int,
            ?topic: string
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        promise {
            let baseEndpoint = $"{baseUrl.TrimEnd('/')}/api/v4/projects"

            let queryParams =
                []
                |> Internals.addInt "page" page
                |> Internals.addInt "per_page" perPage
                |> Internals.addPaginationMode pagination
                |> Internals.addSortField orderBy
                |> Internals.addSortDirection sort
                |> Internals.addBool "membership" membership
                |> Internals.addBool "owned" owned
                |> Internals.addBool "starred" starred
                |> Internals.addString "search" search
                |> Internals.addString "visibility" visibility
                |> Internals.addInt "min_access_level" minAccessLevel
                |> Internals.addString "topic" topic

            let effectiveQueryParams =
                match pagination with
                | Some PaginationMode.Keyset ->
                    // For projects, keyset pagination requires order_by=id.
                    queryParams
                    |> List.filter (fun (key, _) -> key <> "order_by")
                    |> fun q -> ("order_by", box "id") :: q
                    |> fun q ->
                        if q |> List.exists (fun (key, _) -> key = "sort") then
                            q
                        else
                            ("sort", box "desc") :: q
                | _ -> queryParams

            let url = appendQueryParams baseEndpoint effectiveQueryParams
            let patHeader = if String.IsNullOrWhiteSpace pat then None else Some pat
            let! response = Internals.sendGet<GitLabProjectResponse> url patHeader

            return
                response
                |> Result.map (fun x -> {
                    Items = x.Items |> Array.map Internals.toExploreProjectDto
                    Pagination = x.Pagination
                })
        }

    /// GET /api/v4/users/:user_id/projects
    static member ListUserPersonalProjects
        (
            baseUrl: string,
            pat: string,
            // Without a userId, this endpoint fetches user by PAT and uses their ID, which is the common case for "My Repos". To improve performance, callers that already have the user ID can provide it to skip the extra user fetch.
            ?userId: int,
            ?page: int,
            ?perPage: int,
            ?pagination: PaginationMode,
            ?orderBy: ProjectSortField,
            ?sort: SortDirection,
            ?membership: bool,
            ?owned: bool,
            ?starred: bool,
            ?search: string,
            ?visibility: string,
            ?minAccessLevel: int
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        promise {
            let! currentUserId =
                match userId with
                | Some id -> Promise.lift (Ok id)
                | None -> promise {
                    let! user = GitLabApi.GetCurrentUser(baseUrl, pat)

                    return
                        match user with
                        | Ok u -> Ok u.id
                        | Error err -> Error err
                  }

            match currentUserId with
            | Error err -> return Error err
            | Ok userId ->
                let baseEndpoint = $"{baseUrl.TrimEnd('/')}/api/v4/users/{userId}/projects"

                let queryParams =
                    []
                    |> Internals.addInt "page" page
                    |> Internals.addInt "per_page" perPage
                    |> Internals.addPaginationMode pagination
                    |> Internals.addSortField orderBy
                    |> Internals.addSortDirection sort
                    |> Internals.addBool "membership" membership
                    |> Internals.addBool "owned" owned
                    |> Internals.addBool "starred" starred
                    |> Internals.addString "search" search
                    |> Internals.addString "visibility" visibility
                    |> Internals.addInt "min_access_level" minAccessLevel

                let url = appendQueryParams baseEndpoint queryParams
                let! response = Internals.sendGet<GitLabProjectResponse> url (Some pat)
                Browser.Dom.console.log response

                return
                    response
                    |> Result.map (fun x -> {
                        Items = x.Items |> Array.map Internals.toExploreProjectDto
                        Pagination = x.Pagination
                    })
        }

    /// GET /api/v4/groups
    static member ListGroupsForCurrentUser
        (
            baseUrl: string,
            pat: string,
            ?minAccessLevel: int,
            ?owned: bool,
            ?allAvailable: bool,
            ?search: string,
            ?page: int,
            ?perPage: int
        ) : JS.Promise<Result<PagedResponse<GroupDto>, GitLabError>> =
        promise {
            let baseEndpoint = $"{baseUrl.TrimEnd('/')}/api/v4/groups"

            let queryParams =
                []
                |> Internals.addInt "min_access_level" minAccessLevel
                |> Internals.addBool "owned" owned
                |> Internals.addBool "all_available" allAvailable
                |> Internals.addString "search" search
                |> Internals.addInt "page" page
                |> Internals.addInt "per_page" perPage

            let url = appendQueryParams baseEndpoint queryParams
            let! response = Internals.sendGet<GitLabGroupResponse> url (Some pat)

            return
                response
                |> Result.map (fun x -> {
                    Items = x.Items |> Array.map Internals.toGroupDto
                    Pagination = x.Pagination
                })
        }

    /// GET /api/v4/groups/:id/projects
    static member ListGroupProjects
        (
            baseUrl: string,
            pat: string,
            groupId: int,
            ?includeSubgroups: bool,
            ?withShared: bool,
            ?orderBy: ProjectSortField,
            ?sort: SortDirection,
            ?page: int,
            ?perPage: int,
            ?search: string
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        promise {
            let baseEndpoint = $"{baseUrl.TrimEnd('/')}/api/v4/groups/{groupId}/projects"

            let queryParams =
                []
                |> Internals.addBool "include_subgroups" includeSubgroups
                |> Internals.addBool "with_shared" withShared
                |> Internals.addSortField orderBy
                |> Internals.addSortDirection sort
                |> Internals.addInt "page" page
                |> Internals.addInt "per_page" perPage
                |> Internals.addString "search" search

            let url = appendQueryParams baseEndpoint queryParams
            let! response = Internals.sendGet<GitLabProjectResponse> url (Some pat)

            return
                response
                |> Result.map (fun x -> {
                    Items = x.Items |> Array.map Internals.toExploreProjectDto
                    Pagination = x.Pagination
                })
        }

    /// Wrapper over ListProjects for "Most starred".
    static member ListExploreMostStarred
        (
            baseUrl: string,
            pat: string,
            ?page: int,
            ?perPage: int,
            ?pagination: PaginationMode,
            ?visibility: string,
            ?topic: string,
            ?search: string
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        GitLabApi.ListProjects(
            baseUrl,
            pat,
            ?page = page,
            ?perPage = perPage,
            ?pagination = pagination,
            orderBy = ProjectSortField.StarCount,
            sort = SortDirection.Desc,
            ?visibility = visibility,
            ?topic = topic,
            ?search = search
        )

    /// Derived trending list because REST has no dedicated trending endpoint.
    static member ListExploreTrending
        (
            baseUrl: string,
            pat: string,
            ?strategy: TrendingStrategy,
            ?page: int,
            ?perPage: int,
            ?visibility: string,
            ?search: string
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        promise {
            let strategy = defaultArg strategy TrendingStrategy.ByLastActivity

            match strategy with
            | TrendingStrategy.ByLastActivity ->
                return!
                    GitLabApi.ListProjects(
                        baseUrl,
                        pat,
                        ?page = page,
                        ?perPage = perPage,
                        orderBy = ProjectSortField.LastActivityAt,
                        sort = SortDirection.Desc,
                        ?visibility = visibility,
                        ?search = search
                    )
            | TrendingStrategy.ByStarsAndRecency ->
                let! result =
                    GitLabApi.ListProjects(
                        baseUrl,
                        pat,
                        ?page = page,
                        ?perPage = perPage,
                        orderBy = ProjectSortField.UpdatedAt,
                        sort = SortDirection.Desc,
                        ?visibility = visibility,
                        ?search = search
                    )

                return
                    result
                    |> Result.map (fun x -> {
                        Items = Internals.rankTrending x.Items
                        Pagination = x.Pagination
                    })
        }

    /// UI compatibility helper for sort mapping.
    static member ListProjectsForUiSort
        (
            baseUrl: string,
            pat: string,
            uiSort: UiProjectSort,
            ?direction: SortDirection,
            ?page: int,
            ?perPage: int,
            ?search: string,
            ?visibility: string
        ) : JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>> =
        GitLabApi.ListProjects(
            baseUrl,
            pat,
            ?page = page,
            ?perPage = perPage,
            orderBy = Internals.mapUiSortField uiSort,
            sort = defaultArg direction SortDirection.Desc,
            ?search = search,
            ?visibility = visibility
        )

    /// Explore bootstrap call: most starred, trending, user repos, groups, and per-group repos.
    static member GetExploreBootstrap
        (
            baseUrl: string,
            pat: string,
            ?page: int,
            ?perPage: int,
            ?trendingStrategy: TrendingStrategy,
            ?maxGroups: int,
            ?groupProjectsPerPage: int
        ) : JS.Promise<Result<ExploreBootstrapDto, GitLabError>> =
        promise {
            let page = defaultArg page 1
            let perPage = defaultArg perPage 20
            let maxGroups = defaultArg maxGroups 5
            let groupProjectsPerPage = defaultArg groupProjectsPerPage 20

            let! mostStarredResult = GitLabApi.ListExploreMostStarred(baseUrl, pat, page = page, perPage = perPage)

            match mostStarredResult with
            | Error err -> return Error err
            | Ok mostStarred ->
                let! trendingResult =
                    GitLabApi.ListExploreTrending(
                        baseUrl,
                        pat,
                        strategy = defaultArg trendingStrategy TrendingStrategy.ByLastActivity,
                        page = page,
                        perPage = perPage
                    )

                match trendingResult with
                | Error err -> return Error err
                | Ok trending ->
                    let! userResult = GitLabApi.GetCurrentUser(baseUrl, pat)

                    match userResult with
                    | Error err -> return Error err
                    | Ok currentUser ->
                        let! myReposResult =
                            GitLabApi.ListUserPersonalProjects(
                                baseUrl,
                                pat,
                                page = page,
                                perPage = perPage,
                                sort = SortDirection.Desc,
                                orderBy = ProjectSortField.UpdatedAt,
                                owned = true
                            )

                        match myReposResult with
                        | Error err -> return Error err
                        | Ok myRepos ->
                            let! groupsResult =
                                GitLabApi.ListGroupsForCurrentUser(baseUrl, pat, page = page, perPage = perPage)

                            match groupsResult with
                            | Error err -> return Error err
                            | Ok groups ->
                                let selectedGroups = groups.Items |> Array.truncate maxGroups

                                let rec fetchGroupRepos
                                    (remaining: GroupDto list)
                                    (acc: GroupProjectsDto list)
                                    : JS.Promise<Result<GroupProjectsDto list, GitLabError>> =
                                    promise {
                                        match remaining with
                                        | [] -> return Ok(List.rev acc)
                                        | group :: rest ->
                                            let! projectsResult =
                                                GitLabApi.ListGroupProjects(
                                                    baseUrl,
                                                    pat,
                                                    group.id,
                                                    page = 1,
                                                    perPage = groupProjectsPerPage,
                                                    orderBy = ProjectSortField.UpdatedAt,
                                                    sort = SortDirection.Desc,
                                                    includeSubgroups = true,
                                                    withShared = true
                                                )

                                            match projectsResult with
                                            | Error err -> return Error err
                                            | Ok projects ->
                                                return!
                                                    fetchGroupRepos rest ({ Group = group; Projects = projects } :: acc)
                                    }

                                let! groupReposResult = fetchGroupRepos (selectedGroups |> Array.toList) []

                                match groupReposResult with
                                | Error err -> return Error err
                                | Ok groupRepos ->
                                    return
                                        Ok {
                                            MostStarred = mostStarred
                                            Trending = trending
                                            CurrentUser = currentUser
                                            MyRepos = myRepos
                                            Groups = groups
                                            GroupRepos = groupRepos |> List.toArray
                                        }
        }