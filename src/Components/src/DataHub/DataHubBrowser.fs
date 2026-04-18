namespace Swate.Components

open Fable.Core
open Elmish
open Feliz
open Feliz.UseElmish
open Swate.Components.Authentication.Types
open Swate.Components.Types.Actionbar
open DataHubTypes
open Swate.Components.Api.GitLabApi

module private DatahubBrowserModel =

    type Msg =
        | SetTab of ExploreTab
        | SetDraftSearchTerm of string
        | SubmitSearch
        | SetSortField of ExploreSortField
        | SetSortDirection of SortDirection
        | SetSelectedGroupId of int option
        | SetPage of int
        | LoadReposRequest of ExploreLoadRequest
        | LoadReposResponse of System.Guid * Result<ExploreLoadResult, GitLabError>

    type State = {
        /// This field is used to store the id of the latest fetch request for repositories. When a response is received, its id is compared to this field to determine if the response is still relevant (i.e., if it corresponds to the most recent request). This helps prevent race conditions.
        LatestReposFetchID: System.Guid option
        Tab: ExploreTab
        DraftSearchTerm: string
        SubmittedSearchTerm: string
        SortField: ExploreSortField
        SortDirection: SortDirection
        SelectedGroupId: int option
        Groups: GroupDto array
        GroupsLoaded: bool
        GroupsLoadError: string option
        Repos: ExploreProjectDto array
        Page: int
        PerPage: int
        IsLoading: bool
        Error: GitLabError option
        Pagination: PaginationMetadata option
        IsAuthenticated: bool
    }

    let private buildRequest (state: State) : ExploreLoadRequest = {
        Target = state.Tab
        SearchTerm = state.SubmittedSearchTerm
        Page = state.Page
        PerPage = state.PerPage
        SortField = state.SortField
        SortDirection = state.SortDirection
        SelectedGroupId = state.SelectedGroupId
        IsAuthenticated = state.IsAuthenticated
    }

    let init (user: Authentication.Types.AuthUserDto option) =
        let state = {
            LatestReposFetchID = None
            Tab = ExploreTab.All
            DraftSearchTerm = ""
            SubmittedSearchTerm = ""
            SortField = ExploreSortField.LastUpdated
            SortDirection = SortDirection.Desc
            SelectedGroupId = None
            Groups = [||]
            GroupsLoaded = false
            GroupsLoadError = None
            Repos = [||]
            Page = 1
            PerPage = 20
            IsLoading = false
            Error = None
            Pagination = None
            IsAuthenticated = user.IsSome
        }

        state, Cmd.ofMsg (LoadReposRequest(buildRequest state))

    /// Wrapper for calling LoadReposRequest after updating the state.
    let private reloadWith (state: State) =
        state, Cmd.ofMsg (LoadReposRequest(buildRequest state))

    let update (loadRepos: ExploreLoadRequest -> JS.Promise<Result<ExploreLoadResult, GitLabError>>) msg state =
        match msg with
        | SetTab tab ->
            if tab = state.Tab then
                state, Cmd.none
            else
                reloadWith { state with Tab = tab; Page = 1 }
        | SetDraftSearchTerm term ->
            {
                state with
                    DraftSearchTerm = term
                    Page = 1
            },
            Cmd.none
        | SubmitSearch ->
            let nextState = {
                state with
                    SubmittedSearchTerm = state.DraftSearchTerm
                    Page = 1
            }

            reloadWith nextState
        | SetSortField sortField ->
            if sortField = state.SortField then
                state, Cmd.none
            else
                reloadWith {
                    state with
                        SortField = sortField
                        Page = 1
                }
        | SetSortDirection sortDirection ->
            if sortDirection = state.SortDirection then
                state, Cmd.none
            else
                reloadWith {
                    state with
                        SortDirection = sortDirection
                        Page = 1
                }
        | SetSelectedGroupId selectedGroupId ->
            if selectedGroupId = state.SelectedGroupId then
                state, Cmd.none
            else
                reloadWith {
                    state with
                        SelectedGroupId = selectedGroupId
                        Page = 1
                }
        | SetPage page ->
            if page = state.Page then
                state, Cmd.none
            else
                reloadWith { state with Page = page }
        | LoadReposRequest request ->
            let requestId = System.Guid.NewGuid()

            let nextState = {
                state with
                    LatestReposFetchID = Some requestId
                    IsLoading = true
                    Error = None
            }

            let cmd =
                Cmd.OfPromise.either
                    loadRepos
                    request
                    (function
                    | Ok result -> LoadReposResponse(requestId, Ok result)
                    | Error err -> LoadReposResponse(requestId, Error err))
                    (fun err -> LoadReposResponse(requestId, Error(GitLabError.Unknown err)))

            nextState, cmd

        | LoadReposResponse(guid, result) ->
            match result with
            | Ok _ when state.LatestReposFetchID <> Some guid ->
                // This response is outdated, ignore it
                state, Cmd.none
            | Ok response ->
                let updatedState = {
                    state with
                        LatestReposFetchID = None
                        Repos = response.Repos
                        Pagination = response.Pagination
                        Groups = response.Groups
                        GroupsLoaded = response.GroupsLoaded
                        GroupsLoadError = response.GroupsLoadError
                        IsLoading = false
                        Error = None
                }

                if
                    state.Tab = ExploreTab.YourOrganisations
                    && state.SelectedGroupId.IsNone
                    && response.Groups.Length > 0
                then
                    let nextState = {
                        updatedState with
                            SelectedGroupId = Some response.Groups[0].id
                    }

                    reloadWith nextState
                else
                    updatedState, Cmd.none
            | Error err ->
                {
                    state with
                        LatestReposFetchID =
                            if state.LatestReposFetchID = Some guid then
                                None
                            else
                                state.LatestReposFetchID
                        Error = Some err
                        Repos = [||]
                        Pagination = None
                        IsLoading = false
                },
                Cmd.none

module private DataHubBrowserHelper =

    let timeAgoUpdated (dt: System.DateTime option) =
        match dt with
        | Some dateTime ->
            let span = System.DateTime.UtcNow - dateTime.ToUniversalTime()

            if span.TotalSeconds < 60.0 then
                sprintf "Updated %d seconds ago" (int span.TotalSeconds)
            elif span.TotalMinutes < 60.0 then
                sprintf "Updated %d minutes ago" (int span.TotalMinutes)
            elif span.TotalHours < 24.0 then
                sprintf "Updated %d hours ago" (int span.TotalHours)
            elif span.TotalDays < 30.0 then
                sprintf "Updated %d days ago" (int span.TotalDays)
            elif span.TotalDays < 365.0 then
                sprintf "Updated %d months ago" (int (span.TotalDays / 30.0))
            else
                sprintf "Updated %d years ago" (int (span.TotalDays / 365.0))
            |> Some
        | None -> None

[<Erase; Mangle(false)>]
type DataHubBrowser =


    [<ReactComponent>]
    static member private ActionbarButtons(buttonInfos: ButtonInfo[]) =
        Actionbar.Main(
            buttonInfos,
            1,
            barClassName =
                "swt:w-fit swt:h-fit swt:flex swt:flex-col swt:bg-base-300 swt:rounded-lg swt:shadow-sm swt:join swt:join-vertical",
            tooltipPosition = DaisyuiTooltipPosition.Left,
            buttonSize = DaisyuiSize.SM,
            buttonClassName = "swt:btn swt:btn-primary swt:btn-square swt:join-item"
        )

    [<ReactComponent>]
    static member private RepoListRow
        (
            project: ExploreProjectDto,
            // onClone: ExploreProjectDto -> unit,
            // ?onOpen: (ExploreProjectDto -> unit),
            ?extraButtons: ExploreProjectDto -> ButtonInfo[]
        ) =

        let visibility = project.visibility |> Option.defaultValue "public"

        let visibilityLabel, visibilityIcon =
            match visibility with
            | "private" -> "Private repository", "swt:fluent--lock-closed-24-regular swt:size-4"
            | "internal" -> "Internal repository", "swt:fluent--shield-24-regular swt:size-4"
            | _ -> "Public repository", "swt:fluent--globe-24-regular swt:size-4"

        let avatarInitial =
            if System.String.IsNullOrWhiteSpace project.name then
                "?"
            else
                let c = System.Math.Min(3, project.name.Length)
                project.name.Substring(0, c).ToUpperInvariant()

        let lastActivityString =
            project.last_activity_at |> DataHubBrowserHelper.timeAgoUpdated

        // let actionButtons = [|
        //     ButtonInfo.create (
        //         "swt:fluent--arrow-download-24-regular swt:size-5",
        //         "Clone repository",
        //         (fun () -> onClone project)
        //     )
        //     // match isLocallyCloned, onOpen with
        //     // | true, Some openFn ->
        //     //     ButtonInfo.create (
        //     //         "swt:fluent--open-24-regular swt:size-5",
        //     //         "Open local repository",
        //     //         (fun () -> openFn project)
        //     //     )
        //     | _ -> ()
        // |]

        Html.li [
            prop.testId ("GitLabRepoRow-" + string project.id)
            prop.className [ "swt:list-row" ]
            prop.children [
                // Avatar
                Html.div [
                    prop.className "swt:avatar swt:self-start swt:min-w-16"
                    prop.children [
                        Html.div [
                            prop.className "swt:w-16 swt:h-16 swt:rounded"
                            prop.children [
                                match project.avatar_url with
                                | Some avatarUrl when not (System.String.IsNullOrWhiteSpace avatarUrl) ->
                                    Html.img [ prop.src avatarUrl; prop.alt project.name ]
                                | _ ->
                                    Html.div [
                                        prop.className
                                            "swt:w-full swt:h-full swt:rounded swt:bg-base-300 swt:flex swt:items-center swt:justify-center swt:text-xl swt:font-semibold"
                                        prop.text avatarInitial
                                    ]
                            ]
                        ]
                    ]
                ]
                // Center ref
                Html.div [
                    prop.className "swt:flex swt:items-center swt:grow swt:min-w-0 swt:gap-2"
                    prop.children [
                        Html.a [
                            prop.href project.web_url
                            prop.target.blank
                            prop.rel "noopener noreferrer"
                            prop.className "swt:link swt:link-hover swt:text-base-content/70 swt:block swt:truncate"
                            prop.children [
                                Html.span [
                                    prop.className "swt:max-md:hidden"
                                    prop.text (project.``namespace``.name + " / ")
                                ]
                                Html.span [
                                    prop.className "swt:text-base-content swt:font-semibold"
                                    prop.text project.name
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:tooltip swt:tooltip-top swt:size-4"
                            prop.ariaLabel visibilityLabel
                            prop.children [
                                Html.div [
                                    prop.className "swt:tooltip-content"
                                    prop.text visibilityLabel
                                ]
                                Html.i [ prop.className [ "swt:iconify"; visibilityIcon ] ]
                            ]
                        ]
                    ]
                ]
                if project.description.IsSome then
                    Html.div [
                        prop.className "swt:list-col-wrap swt:gap-1 swt:flex swt:flex-col"
                        prop.children [
                            Html.div [
                                prop.className
                                    "swt:text-xs swt:text-base-content/70 swt:max-sm:hidden swt:max-md:line-clamp-3"
                                prop.text project.description.Value
                            ]
                            Html.div [
                                prop.className "swt:flex swt:gap-1 swt:flex-wrap swt:flex-row"
                                prop.children [
                                    for tag in project.tag_list |> Array.truncate 3 do
                                        Html.span [
                                            prop.className "swt:badge swt:badge-neutral swt:badge-xs"
                                            prop.text tag
                                        ]
                                ]
                            ]
                        ]
                    ]
                Html.div [
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:justify-end swt:gap-1"
                            prop.children [
                                Html.span [ prop.className "swt:iconify swt:fluent--star-12-regular" ]
                                Html.span [
                                    prop.className "swt:text-xs swt:text-base-content/70"
                                    prop.text (string project.star_count)
                                ]
                            ]
                        ]
                        match lastActivityString with
                        | Some _ ->
                            Html.div [
                                prop.className "swt:text-xs swt:text-base-content/60 swt:text-right"
                                prop.text (lastActivityString.Value)
                            ]
                        | None -> Html.none
                    ]
                ]
                match extraButtons with
                | Some fn ->
                    let buttons = fn project
                    DataHubBrowser.ActionbarButtons(buttons)
                | None -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member private Filter
        (
            tab: ExploreTab,
            isAuthenticated: bool,
            searchTerm: string,
            setSearchTerm: string -> unit,
            onSearchSubmit: unit -> unit,
            sortField: ExploreSortField,
            setSortField: ExploreSortField -> unit,
            sortDirection: SortDirection,
            setSortDirection: SortDirection -> unit,
            groups: GroupDto array,
            selectedGroupId: int option,
            setSelectedGroupId: int option -> unit,
            groupsLoadError: string option
        ) =

        let sortLabel =
            match sortDirection with
            | SortDirection.Asc -> "Sort ascending"
            | SortDirection.Desc -> "Sort descending"

        Html.div [
            prop.className
                "swt:flex swt:flex-col swt:sm:flex-row sm:swt:flex-row swt:gap-2 swt:px-2 swt:py-6 swt:bg-base-300 swt:border-y swt:border-base-content/50"
            prop.children [
                Html.div [
                    prop.className "swt:join swt:grow"
                    prop.children [
                        Html.input [
                            prop.testId "GitLabExploreSearchInput"
                            prop.className
                                "swt:input swt:input-sm swt:input-bordered swt:border-base-content swt:w-full swt:join-item"
                            prop.placeholder "Search repositories"
                            prop.value searchTerm
                            prop.onChange setSearchTerm
                            prop.onKeyDown (key.enter, (fun _ -> onSearchSubmit ()))
                        ]
                        Html.button [
                            prop.testId "GitLabExploreSearchButton"
                            prop.className "swt:btn swt:btn-sm swt:btn-outline swt:join-item"
                            prop.text "Search"
                            prop.onClick (fun _ -> onSearchSubmit ())
                        ]
                    ]
                ]
                if tab <> ExploreTab.MostStarred then
                    Html.div [
                        prop.className "swt:join"
                        prop.children [
                            Html.select [
                                prop.testId "GitLabExploreSortFieldSelect"
                                prop.className
                                    "swt:select swt:select-sm swt:btn swt:join-item swt:max-sm:w-full swt:*:min-w-max"
                                prop.value (
                                    match sortField with
                                    | ExploreSortField.LastUpdated -> "last-updated"
                                    | ExploreSortField.DateCreated -> "date-created"
                                    | ExploreSortField.Name -> "name"
                                    | ExploreSortField.Stars -> "stars"
                                )
                                prop.onChange (fun (value: string) ->
                                    let next =
                                        match value with
                                        | "last-updated" -> ExploreSortField.LastUpdated
                                        | "date-created" -> ExploreSortField.DateCreated
                                        | "name" -> ExploreSortField.Name
                                        | "stars" -> ExploreSortField.Stars
                                        | _ -> ExploreSortField.LastUpdated

                                    setSortField next
                                )
                                prop.children [
                                    Html.option [ prop.value "last-updated"; prop.text "Last Updated" ]
                                    Html.option [ prop.value "date-created"; prop.text "Date Created" ]
                                    Html.option [ prop.value "name"; prop.text "Name" ]
                                    Html.option [ prop.value "stars"; prop.text "Stars" ]
                                ]
                            ]
                            Html.button [
                                prop.testId "GitLabExploreSortDirectionButton"
                                prop.className
                                    "swt:btn swt:btn-sm swt:btn-outline swt:join-item swt:bg-base-100 swt:border-base-content/30!"
                                prop.ariaLabel sortLabel
                                prop.title sortLabel
                                prop.onClick (fun _ ->
                                    let nextDirection =
                                        match sortDirection with
                                        | SortDirection.Asc -> SortDirection.Desc
                                        | SortDirection.Desc -> SortDirection.Asc

                                    setSortDirection nextDirection
                                )
                                prop.children [
                                    Html.i [
                                        prop.className [
                                            "swt:iconify"
                                            match sortDirection with
                                            | SortDirection.Asc ->
                                                "swt:fluent--arrow-sort-up-lines-16-regular swt:size-4"
                                            | SortDirection.Desc ->
                                                "swt:fluent--arrow-sort-down-lines-16-regular swt:size-4"
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                if tab = ExploreTab.YourOrganisations then
                    Html.select [
                        prop.testId "GitLabExploreOrganisationSelect"
                        prop.className "swt:select swt:select-sm swt:min-w-56"
                        prop.value (selectedGroupId |> Option.map string |> Option.defaultValue "")
                        prop.onChange (fun (value: string) ->
                            if System.String.IsNullOrWhiteSpace value then
                                setSelectedGroupId None
                            else
                                setSelectedGroupId (Some(int value))
                        )
                        prop.disabled ((not isAuthenticated) || groups.Length = 0 || groupsLoadError.IsSome)
                        prop.children [
                            Html.option [
                                prop.value ""
                                prop.text "Select organisation"
                                prop.disabled true
                            ]
                            for g in groups do
                                Html.option [ prop.value (string g.id); prop.text g.name ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private Tabs(tab, setTab, isAuthenticated) =
        Html.div [
            prop.testId "GitLabExploreTabs"
            prop.role.tabList
            prop.className "swt:tabs swt:tabs-box swt:tabs-sm"
            prop.children [
                let tabs = [
                    ExploreTab.All, "All", "All"
                    ExploreTab.YourRepos, "Your Repos", "YourRepos"
                    ExploreTab.MostStarred, "Most Starred", "MostStarred"
                    ExploreTab.YourOrganisations, "Your Organisations", "YourOrganisations"
                ]

                for mode, label, tid in tabs do
                    let isRestricted =
                        (not isAuthenticated)
                        && (mode = ExploreTab.YourRepos || mode = ExploreTab.YourOrganisations)

                    Html.div [
                        prop.className (
                            if isRestricted then
                                "swt:tooltip swt:tooltip-bottom"
                            else
                                ""
                        )
                        prop.children [
                            if isRestricted then
                                Html.div [
                                    prop.className "swt:tooltip-content"
                                    prop.text "Log in to access this tab"
                                ]
                            Html.a [
                                prop.testId ("GitLabExploreTab-" + tid)
                                prop.tabIndex 0
                                prop.custom ("aria-disabled", if isRestricted then "true" else "false")
                                prop.className [
                                    "swt:tab"
                                    if tab = mode then
                                        "swt:tab-active"
                                    if isRestricted then
                                        "swt:opacity-50 swt:cursor-not-allowed"
                                ]
                                prop.onClick (fun _ ->
                                    if not isRestricted then
                                        setTab mode
                                )
                                prop.text label
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponentAttribute>]
    static member private Pagination(pagination, onPageChange) =
        match pagination with
        | Some pageMeta ->
            Html.div [
                prop.testId "GitLabExplorePagination"
                prop.className "swt:flex swt:items-center swt:justify-between swt:pt-1"
                prop.children [
                    Html.button [
                        prop.testId "GitLabExplorePrevPageButton"
                        prop.className "swt:btn swt:btn-xs"
                        prop.text "Prev"
                        prop.disabled pageMeta.PrevPage.IsNone
                        prop.onClick (fun _ -> pageMeta.PrevPage |> Option.iter onPageChange)
                    ]
                    Html.span [
                        prop.testId "GitLabExplorePageIndicator"
                        prop.className "swt:text-xs swt:text-base-content/70"
                        prop.textf "Page %s" (pageMeta.Page |> Option.map string |> Option.defaultValue "1")
                    ]
                    Html.button [
                        prop.testId "GitLabExploreNextPageButton"
                        prop.className "swt:btn swt:btn-xs"
                        prop.text "Next"
                        prop.disabled pageMeta.NextPage.IsNone
                        prop.onClick (fun _ -> pageMeta.NextPage |> Option.iter onPageChange)
                    ]
                ]
            ]
        | None -> Html.none

    [<ReactComponent>]
    static member ExplorePanel
        (
            accounts: Swate.Components.Authentication.Types.AuthStateDto,
            loaders: ExploreLoaders,
            ?reloadTrigger: int,
            ?onRender: (ExploreProjectDto -> unit),
            ?projectActionBtns: (ExploreProjectDto -> ButtonInfo[]),
            ?classNames: string,
            ?onClose: Browser.Types.MouseEvent -> unit
        ) =

        let user = accounts.UsableActiveUser()

        let emptyPagination (page: int) (perPage: int) : PaginationMetadata = {
            Link = None
            NextPage = None
            Page = Some page
            PerPage = Some perPage
            PrevPage = None
            Total = Some 0
            TotalPages = Some 1
            NextCursor = None
            PrevCursor = None
        }

        let emptyProjectsResponse (page: int) (perPage: int) : PagedResponse<ExploreProjectDto> = {
            Items = [||]
            Pagination = emptyPagination page perPage
        }

        let emptyGroupsResponse (page: int) (perPage: int) : PagedResponse<GroupDto> = {
            Items = [||]
            Pagination = emptyPagination page perPage
        }

        let loadRepos (request: ExploreLoadRequest) = promise {
            let visibility = if request.IsAuthenticated then None else Some "public"

            let orderBy =
                if request.Target = ExploreTab.MostStarred then
                    ProjectSortField.StarCount
                else
                    ExploreSortField.toProjectSortField request.SortField

            let sort =
                if request.Target = ExploreTab.MostStarred then
                    SortDirection.Desc
                else
                    request.SortDirection

            let repoQuery = {
                SearchTerm = request.SearchTerm
                Page = request.Page
                PerPage = request.PerPage
                OrderBy = orderBy
                Sort = sort
                Visibility = visibility
            }

            let! groupsResult =
                if request.Target = ExploreTab.YourOrganisations && request.IsAuthenticated then
                    loaders.LoadOrganisationGroups { Page = 1; PerPage = 100 }
                else
                    promise { return Ok(emptyGroupsResponse 1 100) }

            let groups, groupsLoaded, groupsLoadError =
                match groupsResult with
                | Ok g -> g.Items, (request.Target <> ExploreTab.YourOrganisations) || request.IsAuthenticated, None
                | Error _ -> [||], true, Some "Failed to load groups"

            let selectedGroupId =
                match request.SelectedGroupId with
                | Some gid -> Some gid
                | None when groups.Length > 0 -> Some groups[0].id
                | None -> None

            let! result =
                match request.Target with
                | ExploreTab.All -> loaders.LoadAllRepos repoQuery
                | ExploreTab.YourRepos ->
                    if request.IsAuthenticated then
                        loaders.LoadUserRepos repoQuery
                    else
                        promise { return Ok(emptyProjectsResponse request.Page request.PerPage) }
                | ExploreTab.MostStarred ->
                    loaders.LoadMostStarredRepos {
                        SearchTerm = request.SearchTerm
                        Page = request.Page
                        PerPage = request.PerPage
                        Visibility = visibility
                    }
                | ExploreTab.YourOrganisations ->
                    if request.IsAuthenticated then
                        match selectedGroupId with
                        | Some gid ->
                            loaders.LoadOrganisationRepos {
                                GroupId = gid
                                SearchTerm = request.SearchTerm
                                Page = request.Page
                                PerPage = request.PerPage
                                OrderBy = orderBy
                                Sort = sort
                                IncludeSubgroups = true
                                WithShared = true
                            }
                        | None -> promise { return Ok(emptyProjectsResponse request.Page request.PerPage) }
                    else
                        promise { return Ok(emptyProjectsResponse request.Page request.PerPage) }

            match result with
            | Ok okResult ->
                return
                    Ok {
                        Repos = okResult.Items
                        Pagination = Some okResult.Pagination
                        Groups = groups
                        GroupsLoaded = groupsLoaded
                        GroupsLoadError = groupsLoadError
                    }
            | Error err -> return Error(err)
        }

        let model, dispatch =
            React.useElmish (
                (fun () -> DatahubBrowserModel.init user),
                DatahubBrowserModel.update loadRepos,
                [| box user; box reloadTrigger |]
            )

        React.useEffect (fun () ->
            match onRender with
            | Some fn -> model.Repos |> Array.iter fn
            | None -> ()
        )

        let onSearchSubmit () =
            dispatch DatahubBrowserModel.SubmitSearch

        Html.div [
            prop.testId "GitLabExplorePanel"
            prop.className [
                classNames
                |> Option.defaultValue "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:grow swt:overflow-hidden"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children [
                        DataHubComponents.DataHubComponents.SectionHeading("GitLab Explore")
                        match onClose with
                        | Some closeFn ->
                            Html.div [
                                prop.className "swt:ml-auto"
                                prop.children [
                                    Components.DeleteButton(props = [ prop.onClick closeFn ])
                                ]
                            ]
                        | None -> Html.none
                    ]
                ]
                // tabs
                DataHubBrowser.Tabs(
                    model.Tab,
                    (fun next -> dispatch (DatahubBrowserModel.SetTab next)),
                    model.IsAuthenticated
                )
                // filter
                DataHubBrowser.Filter(
                    model.Tab,
                    model.IsAuthenticated,
                    model.DraftSearchTerm,
                    (fun term -> dispatch (DatahubBrowserModel.SetDraftSearchTerm term)),
                    onSearchSubmit,
                    model.SortField,
                    (fun next -> dispatch (DatahubBrowserModel.SetSortField next)),
                    model.SortDirection,
                    (fun next -> dispatch (DatahubBrowserModel.SetSortDirection next)),
                    model.Groups,
                    model.SelectedGroupId,
                    (fun gid -> dispatch (DatahubBrowserModel.SetSelectedGroupId gid)),
                    model.GroupsLoadError
                )
                if model.IsLoading then
                    Html.div [
                        prop.testId "GitLabExploreLoading"
                        prop.className "swt:flex swt:items-center swt:gap-2"
                        prop.children [
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                            ]
                            Html.span "Loading repositories..."
                        ]
                    ]
                else
                    Html.none
                match model.Error with
                | Some err ->
                    Html.div [
                        prop.testId "GitLabExploreError"
                        prop.className "swt:alert swt:alert-error swt:text-xs"
                        prop.children [ Html.span err.GitLabErrorToString ]
                    ]
                | None -> Html.none
                let showOrgNoGroups =
                    model.Tab = ExploreTab.YourOrganisations
                    && model.IsAuthenticated
                    && model.GroupsLoaded
                    && model.Groups.Length = 0
                    && model.GroupsLoadError.IsNone

                let showOrgLoadError =
                    model.Tab = ExploreTab.YourOrganisations && model.GroupsLoadError.IsSome

                if showOrgLoadError then
                    Html.p [
                        prop.testId "GitLabExploreGroupsError"
                        prop.className "swt:text-sm swt:text-error"
                        prop.text "Failed to load groups"
                    ]
                elif showOrgNoGroups then
                    Html.p [
                        prop.testId "GitLabExploreGroupsEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text "No groups found"
                    ]
                elif (not model.IsLoading) && model.Repos.Length = 0 then
                    Html.p [
                        prop.testId "GitLabExploreEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text "No repositories found for current filter and search."
                    ]
                elif model.Repos.Length > 0 then
                    Html.ul [
                        prop.testId "GitLabExploreRepoList"
                        prop.className
                            "swt:list swt:bg-base-100 swt:rounded-box swt:shadow-md swt:overflow-x-auto swt:grow-2"
                        prop.children [
                            for repo in model.Repos do
                                DataHubBrowser.RepoListRow(repo, ?extraButtons = projectActionBtns)
                        ]
                    ]

                // Pagination
                DataHubBrowser.Pagination(model.Pagination, (fun page -> dispatch (DatahubBrowserModel.SetPage page)))
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let accounts, setAccounts =
            React.useState (Authentication.Types.AuthStateDto.Empty: Authentication.Types.AuthStateDto)

        let mockUser: AccountSummary = {
            User = {
                AccountId = "1"
                Name = "storybook-user"
                Email = "Storybook User"
                AvatarUrl = ""
                TargetDataHub = ""
            }
            DateAdded = "2026-01-01T00:00:00.0000000Z"
            TokenInvalid = false
        }

        let login () =
            setAccounts (
                {
                    Authentication.Types.AuthStateDto.Empty with
                        ActiveAccount = Some mockUser
                        StoredAccounts = [| mockUser |]
                }
            )

        let logout () =
            setAccounts Authentication.Types.AuthStateDto.Empty

        let reloadTrigger = 0

        let callCount, setCallCount =
            React.useState (
                {|
                    all = 0
                    mostStarred = 0
                    userRepos = 0
                    organisationGroups = 0
                    organisationRepos = 0
                |}
            )

        let isLocallyCloned (p: ExploreProjectDto) = p.id % 2 = 0

        let paginate (items: 'T array) (page: int) (pageSize: int) =
            let totalPages =
                let f = float items.Length / float pageSize
                let ceilV = System.Math.Ceiling f |> int
                max 1 ceilV

            let p = max 1 (min page totalPages)
            let startIndex = (p - 1) * pageSize
            let pageItems = items |> Array.skip startIndex |> Array.truncate pageSize

            let pageMeta: PaginationMetadata = {
                Link = None
                NextPage = if p < totalPages then Some(p + 1) else None
                Page = Some p
                PerPage = Some pageSize
                PrevPage = if p > 1 then Some(p - 1) else None
                Total = Some items.Length
                TotalPages = Some totalPages
                NextCursor = None
                PrevCursor = None
            }

            pageItems, pageMeta

        let containsCi (text: string) (query: string) =
            text.ToLowerInvariant().Contains(query.ToLowerInvariant())

        let sortRepos (sortField: ExploreSortField) (sortDirection: SortDirection) (items: ExploreProjectDto array) =
            let sorted =
                match sortField with
                | ExploreSortField.LastUpdated -> items |> Array.sortBy (fun p -> p.last_activity_at)
                | ExploreSortField.DateCreated -> items |> Array.sortBy (fun p -> p.created_at)
                | ExploreSortField.Name -> items |> Array.sortBy (fun p -> p.name.ToLowerInvariant())
                | ExploreSortField.Stars -> items |> Array.sortBy (fun p -> p.star_count)

            match sortDirection with
            | SortDirection.Asc -> sorted
            | SortDirection.Desc -> sorted |> Array.rev

        let allRepos =
            [|
                yield! MockData.DataHub.yourRepos
                yield! MockData.DataHub.mostStarred
                for kvp in MockData.DataHub.orgRepos do
                    yield! kvp.Value
            |]
            |> Array.distinctBy (fun p -> p.id)

        let isPublicRepo (project: ExploreProjectDto) =
            not (project.path_with_namespace.Contains("/private/"))

        let sortReposByProjectSort
            (sortField: ProjectSortField)
            (sortDirection: SortDirection)
            (items: ExploreProjectDto array)
            =
            let sorted =
                match sortField with
                | ProjectSortField.Name -> items |> Array.sortBy (fun p -> p.name.ToLowerInvariant())
                | ProjectSortField.CreatedAt -> items |> Array.sortBy (fun p -> p.created_at)
                | ProjectSortField.UpdatedAt -> items |> Array.sortBy (fun p -> p.last_activity_at)
                | ProjectSortField.StarCount -> items |> Array.sortBy (fun p -> p.star_count)
                | ProjectSortField.LastActivityAt -> items |> Array.sortBy (fun p -> p.last_activity_at)

            match sortDirection with
            | SortDirection.Asc -> sorted
            | SortDirection.Desc -> sorted |> Array.rev

        let filterRepos (searchTerm: string) (items: ExploreProjectDto array) =
            items
            |> Array.filter (fun p ->
                System.String.IsNullOrWhiteSpace searchTerm
                || containsCi p.name searchTerm
                || containsCi p.path_with_namespace searchTerm
                || containsCi (p.description |> Option.defaultValue "") searchTerm
            )

        /// This is used to test race conditions by including specific tokens in the search term that trigger different response delays
        let getMockDelayAndSearchTerm (searchTerm: string) =
            let slowToken = "__race_slow__"
            let fastToken = "__race_fast__"

            let delayMs =
                if searchTerm.Contains(slowToken) then 900
                elif searchTerm.Contains(fastToken) then 60
                else 250

            let cleanedSearchTerm =
                searchTerm.Replace(slowToken, "").Replace(fastToken, "").Trim()

            delayMs, cleanedSearchTerm

        let loadAllRepos (query: ExploreRepoQuery) = promise {
            let delayMs, cleanedSearchTerm = getMockDelayAndSearchTerm query.SearchTerm

            setCallCount {|
                callCount with
                    all = callCount.all + 1
            |}

            do! Promise.sleep delayMs

            let source =
                match query.Visibility with
                | Some "public" -> allRepos |> Array.filter isPublicRepo
                | _ -> allRepos

            let filtered =
                source
                |> filterRepos cleanedSearchTerm
                |> sortReposByProjectSort query.OrderBy query.Sort

            let pageItems, pageMeta = paginate filtered query.Page query.PerPage

            return
                Ok {
                    Items = pageItems
                    Pagination = pageMeta
                }
        }

        let loadMostStarredRepos (query: ExploreMostStarredQuery) = promise {
            let delayMs, cleanedSearchTerm = getMockDelayAndSearchTerm query.SearchTerm

            setCallCount {|
                callCount with
                    mostStarred = callCount.mostStarred + 1
            |}

            do! Promise.sleep delayMs

            let source =
                match query.Visibility with
                | Some "public" -> MockData.DataHub.mostStarred |> Array.filter isPublicRepo
                | _ -> MockData.DataHub.mostStarred

            let filtered =
                source
                |> filterRepos cleanedSearchTerm
                |> Array.sortByDescending (fun p -> p.star_count)

            let pageItems, pageMeta = paginate filtered query.Page query.PerPage

            return
                Ok {
                    Items = pageItems
                    Pagination = pageMeta
                }
        }

        let loadUserRepos (query: ExploreRepoQuery) = promise {
            let delayMs, cleanedSearchTerm = getMockDelayAndSearchTerm query.SearchTerm

            setCallCount {|
                callCount with
                    userRepos = callCount.userRepos + 1
            |}

            do! Promise.sleep delayMs

            let filtered =
                MockData.DataHub.yourRepos
                |> filterRepos cleanedSearchTerm
                |> sortReposByProjectSort query.OrderBy query.Sort

            let pageItems, pageMeta = paginate filtered query.Page query.PerPage

            return
                Ok {
                    Items = pageItems
                    Pagination = pageMeta
                }
        }

        let loadOrganisationGroups (query: ExploreGroupsQuery) = promise {
            setCallCount {|
                callCount with
                    organisationGroups = callCount.organisationGroups + 1
            |}

            do! Promise.sleep 250

            let pageItems, pageMeta = paginate MockData.DataHub.groups query.Page query.PerPage

            return
                Ok {
                    Items = pageItems
                    Pagination = pageMeta
                }
        }

        let loadOrganisationRepos (query: ExploreGroupProjectsQuery) = promise {
            let delayMs, cleanedSearchTerm = getMockDelayAndSearchTerm query.SearchTerm

            setCallCount {|
                callCount with
                    organisationRepos = callCount.organisationRepos + 1
            |}

            do! Promise.sleep delayMs

            let source =
                MockData.DataHub.orgRepos
                |> Map.tryFind query.GroupId
                |> Option.defaultValue [||]

            let filtered =
                source
                |> filterRepos cleanedSearchTerm
                |> sortReposByProjectSort query.OrderBy query.Sort

            let pageItems, pageMeta = paginate filtered query.Page query.PerPage

            return
                Ok {
                    Items = pageItems
                    Pagination = pageMeta
                }
        }

        let loaders: ExploreLoaders = {
            LoadAllRepos = loadAllRepos
            LoadMostStarredRepos = loadMostStarredRepos
            LoadUserRepos = loadUserRepos
            LoadOrganisationGroups = loadOrganisationGroups
            LoadOrganisationRepos = loadOrganisationRepos
        }

        let currentUser = accounts.ActiveUser()

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.div [
                    prop.testId "GitLabExploreMockAuthControls"
                    prop.className "swt:flex swt:items-center swt:gap-2 swt:flex-wrap swt:text-xs"
                    prop.children [
                        Html.span [
                            prop.testId "GitLabExploreMockAuthState"
                            prop.textf "signed-in:%s" (if currentUser.IsSome then "true" else "false")
                        ]
                        Html.button [
                            prop.testId "GitLabExploreMockLoginButton"
                            prop.className "swt:btn swt:btn-xs"
                            prop.text "Login"
                            prop.disabled currentUser.IsSome
                            prop.onClick (fun _ -> login ())
                        ]
                        Html.button [
                            prop.testId "GitLabExploreMockLogoutButton"
                            prop.className "swt:btn swt:btn-xs swt:btn-outline"
                            prop.text "Logout"
                            prop.disabled currentUser.IsNone
                            prop.onClick (fun _ -> logout ())
                        ]
                    ]
                ]
                Html.div [
                    prop.testId "GitLabExploreMockCallCounter"
                    prop.className "swt:flex swt:gap-2 swt:flex-wrap swt:text-xs swt:text-base-content/70"
                    prop.children [
                        Html.span [
                            prop.testId "GitLabExploreMockCountAll"
                            prop.textf "all:%i" callCount.all
                        ]
                        Html.span [
                            prop.testId "GitLabExploreMockCountMostStarred"
                            prop.textf "most-starred:%i" callCount.mostStarred
                        ]
                        Html.span [
                            prop.testId "GitLabExploreMockCountUserRepos"
                            prop.textf "user-repos:%i" callCount.userRepos
                        ]
                        Html.span [
                            prop.testId "GitLabExploreMockCountOrgGroups"
                            prop.textf "org-groups:%i" callCount.organisationGroups
                        ]
                        Html.span [
                            prop.testId "GitLabExploreMockCountOrgRepos"
                            prop.textf "org-repos:%i" callCount.organisationRepos
                        ]
                    ]
                ]
                DataHubBrowser.ExplorePanel(
                    accounts,
                    loaders,
                    reloadTrigger,
                    projectActionBtns =
                        (fun p -> [|
                            ButtonInfo.create (
                                "swt:fluent--arrow-download-24-regular swt:size-5",
                                "Clone repository",
                                (fun _ -> Browser.Dom.window.alert ("Clone " + p.web_url))
                            )
                        |])
                )
            ]
        ]

    [<ReactComponent>]
    static member GitLabEntry() =
        let accounts, setAccounts = React.useState (AuthStateDto.Empty)
        let currentUser = accounts.ActiveUser()
        let baseUrl, setBaseUrl = React.useState "https://git.nfdi4plants.org"
        let pat, setPat = React.useState ""
        let reloadTrigger, setReloadTrigger = React.useState 0

        let isConnected = not (System.String.IsNullOrWhiteSpace baseUrl)

        let emptyResponse (page: int) (perPage: int) : PagedResponse<ExploreProjectDto> = {
            Items = [||]
            Pagination = {
                Link = None
                NextPage = None
                Page = Some page
                PerPage = Some perPage
                PrevPage = None
                Total = Some 0
                TotalPages = Some 1
                NextCursor = None
                PrevCursor = None
            }
        }

        let login =
            fun (pat: string) -> promise {
                let! userResponse = GitLabApi.GetCurrentUser(baseUrl, pat)

                match userResponse with
                | Ok user ->
                    let userDTO: AccountSummary = {
                        User = {
                            AccountId = string user.id
                            Name = user.name
                            AvatarUrl = user.avatar_url |> Option.defaultValue ""
                            Email = ""
                            TargetDataHub = baseUrl
                        }
                        DateAdded = "2026-01-01T00:00:00.0000000Z"
                        TokenInvalid = false
                    }

                    setAccounts {
                        ActiveAccount = Some userDTO
                        StoredAccounts = [| userDTO |]
                    }
                | Error err ->
                    Browser.Dom.console.error (
                        "Failed to get current user: "
                        + (
                            match err with
                            | GitLabError.DecodeError ex -> ex.Message
                            | _ -> ""
                        )
                    )

                    setAccounts AuthStateDto.Empty
            }

        let emptyGroupsResponse (page: int) (perPage: int) : PagedResponse<GroupDto> = {
            Items = [||]
            Pagination = {
                Link = None
                NextPage = None
                Page = Some page
                PerPage = Some perPage
                PrevPage = None
                Total = Some 0
                TotalPages = Some 1
                NextCursor = None
                PrevCursor = None
            }
        }

        let loadAllRepos (query: ExploreRepoQuery) =
            if isConnected then
                let requestPat = if query.Visibility.IsSome then "" else pat

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
            else
                promise { return Ok(emptyResponse query.Page query.PerPage) }

        let loadMostStarredRepos (query: ExploreMostStarredQuery) =
            if isConnected then
                let requestPat = if query.Visibility.IsSome then "" else pat

                GitLabApi.ListExploreMostStarred(
                    baseUrl,
                    requestPat,
                    page = query.Page,
                    perPage = query.PerPage,
                    search = query.SearchTerm,
                    ?visibility = query.Visibility
                )
            else
                promise { return Ok(emptyResponse query.Page query.PerPage) }

        let loadUserRepos (query: ExploreRepoQuery) =
            if isConnected then
                GitLabApi.ListUserPersonalProjects(
                    baseUrl,
                    pat,
                    ?userId =
                        (currentUser
                         |> Option.bind (fun u ->
                             System.Int32.TryParse(u.AccountId)
                             |> function
                                 | true, id -> Some id
                                 | _ -> None
                         )),
                    page = query.Page,
                    perPage = query.PerPage,
                    search = query.SearchTerm,
                    orderBy = query.OrderBy,
                    sort = query.Sort
                )
            else
                promise { return Ok(emptyResponse query.Page query.PerPage) }

        let loadOrganisationGroups (query: ExploreGroupsQuery) =
            if isConnected then
                GitLabApi.ListGroupsForCurrentUser(baseUrl, pat, page = query.Page, perPage = query.PerPage)
            else
                promise { return Ok(emptyGroupsResponse query.Page query.PerPage) }

        let loadOrganisationRepos (query: ExploreGroupProjectsQuery) =
            if isConnected then
                GitLabApi.ListGroupProjects(
                    baseUrl,
                    pat,
                    query.GroupId,
                    page = query.Page,
                    perPage = query.PerPage,
                    includeSubgroups = query.IncludeSubgroups,
                    withShared = query.WithShared,
                    search = query.SearchTerm,
                    orderBy = query.OrderBy,
                    sort = query.Sort
                )
            else
                promise { return Ok(emptyResponse query.Page query.PerPage) }

        let loaders: ExploreLoaders = {
            LoadAllRepos = loadAllRepos
            LoadMostStarredRepos = loadMostStarredRepos
            LoadUserRepos = loadUserRepos
            LoadOrganisationGroups = loadOrganisationGroups
            LoadOrganisationRepos = loadOrganisationRepos
        }

        Html.div [
            prop.testId "GitLabEntryPanel"
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col sm:swt:flex-row swt:gap-2"
                    prop.children [
                        Html.input [
                            prop.testId "GitLabEntryBaseUrlInput"
                            prop.className "swt:input swt:input-sm swt:w-full"
                            prop.placeholder "GitLab Base URL"
                            prop.value baseUrl
                            prop.onChange setBaseUrl
                        ]
                        Html.div [
                            prop.className "swt:join swt:w-full"
                            prop.children [
                                Html.input [
                                    prop.type'.password
                                    prop.testId "GitLabEntryPatInput"
                                    prop.className "swt:input swt:input-sm swt:w-full swt:join-item"
                                    prop.placeholder "Personal Access Token"
                                    prop.value pat
                                    prop.onChange setPat
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:join-item"
                                    prop.text "Login"
                                    prop.disabled (not isConnected || System.String.IsNullOrWhiteSpace pat)
                                    prop.onClick (fun _ -> login pat |> Promise.start)
                                ]
                            ]
                        ]
                        Html.button [
                            prop.testId "GitLabEntryLoadButton"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "Reload Repos"
                            prop.disabled (not isConnected)
                            prop.onClick (fun _ -> setReloadTrigger (reloadTrigger + 1))
                        ]
                        Html.button [
                            prop.text "Test"
                            prop.onClick (fun _ ->
                                Browser.Dom.console.log "Testing GitLab API connection with current credentials..."

                                promise {
                                    let! user = GitLabApi.ListExploreMostStarred(baseUrl, pat)
                                    Browser.Dom.console.log (user)
                                }
                                |> Promise.start
                            )
                        ]
                    ]
                ]

                let btnInfo =
                    fun (project: ExploreProjectDto) -> [|
                        ButtonInfo.create (
                            "swt:fluent--arrow-download-24-filled",
                            "Download",
                            fun _ -> Browser.Dom.window.alert ("Download " + project.name)
                        )
                        ButtonInfo.create (
                            "swt:fluent--star-24-regular",
                            "Star",
                            fun _ -> Browser.Dom.window.alert ("Star " + project.name)
                        )
                    |]

                DataHubBrowser.ExplorePanel(accounts, loaders, reloadTrigger, projectActionBtns = btnInfo)
            ]
        ]