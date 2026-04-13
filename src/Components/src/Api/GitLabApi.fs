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
    | Unknown of exn

    member this.GitLabErrorToString =
        match this with
        | GitLabError.NetworkError ex -> $"Network error: {ex.Message}"
        | GitLabError.Unauthorized -> "Unauthorized (check your Personal Access Token)."
        | GitLabError.Forbidden -> "Forbidden (missing permissions for this resource)."
        | GitLabError.NotFound -> "GitLab resource not found."
        | GitLabError.HttpError code -> $"GitLab request failed with HTTP {code}."
        | GitLabError.DecodeError ex -> $"Failed to decode GitLab response: {ex.Message}"
        | GitLabError.InvalidRequest message -> message
        | GitLabError.Unknown ex -> $"An unknown error occurred: {ex.Message}"

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
    http_url_to_repo: string
    ssh_url_to_repo: string option
    avatar_url: string option
    visibility: string option
    star_count: int
    created_at: System.DateTime option
    last_activity_at: System.DateTime option
    tag_list: string array
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
    http_url_to_repo: string
    ssh_url_to_repo: string option
    avatar_url: string option
    visibility: string option
    star_count: int
    created_at: string
    last_activity_at: string
    topics: string array option
    tag_list: string array option
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
        http_url_to_repo = project.http_url_to_repo
        ssh_url_to_repo = project.ssh_url_to_repo
        avatar_url = project.avatar_url
        visibility = project.visibility
        star_count = project.star_count
        created_at =
            match System.DateTime.TryParse project.created_at with
            | (true, dt) -> Some dt
            | _ -> None
        last_activity_at =
            match System.DateTime.TryParse project.last_activity_at with
            | (true, dt) -> Some dt
            | _ -> None
        tag_list = project.topics |> Option.orElse project.tag_list |> Option.defaultValue [||]
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

    let sendGet<'T> (url: string) (pat: string option) : JS.Promise<Result<PagedResponse<'T>, GitLabError>> =

        promise {
            let requestOptions = makeGetRequestOptions pat true

            try
                let! response = fetchUnsafe url requestOptions

                if not response.Ok then
                    return Error(mapHttpError response.Status)
                else
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

    let sendJson<'TResponse>
        (url: string)
        (pat: string)
        (method': HttpMethod)
        (body: obj)
        : JS.Promise<Result<'TResponse, GitLabError>> =
        promise {
            if String.IsNullOrWhiteSpace pat then
                return Error(GitLabError.InvalidRequest "Personal Access Token is required.")
            else
                let requestOptions = [
                    RequestProperties.Method method'
                    requestHeaders [
                        HttpRequestHeaders.Custom("PRIVATE-TOKEN", pat)
                        HttpRequestHeaders.Accept "application/json"
                        HttpRequestHeaders.ContentType "application/json"
                    ]
                    RequestProperties.Body(unbox (Fable.Core.JS.JSON.stringify body))
                ]

                try
                    let! response = fetchUnsafe url requestOptions

                    if not response.Ok then
                        return Error(mapHttpError response.Status)
                    else
                        try
                            let! payload = response.json<'TResponse> ()
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

    static member CreateProject(baseUrl: string, pat: string, projectName: string) : JS.Promise<Result<ExploreProjectDto, GitLabError>> =
        promise {
            let normalizedName = projectName.Trim()

            if String.IsNullOrWhiteSpace normalizedName then
                return Error(GitLabError.InvalidRequest "Repository name is required.")
            else
                let! response =
                    Internals.sendJson<GitLabProjectResponse>
                        $"{baseUrl.TrimEnd('/')}/api/v4/projects"
                        pat
                        HttpMethod.POST
                        (createObj [
                            "name" ==> normalizedName
                            "initialize_with_readme" ==> false
                        ])

                return response |> Result.map Internals.toExploreProjectDto
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
                |> Result.map (fun x ->
                    let itemsDTOs = x.Items |> Array.map Internals.toExploreProjectDto

                    {
                        Items = itemsDTOs
                        Pagination = x.Pagination
                    }
                )
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
