namespace Swate.Components

open Fable.Core
open Feliz
open Swate.Components.Types.Actionbar

open DataHubTypes
open Swate.Components.Api.GitLabApi
open Swate.Components.DataHubTypes

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
            buttonSize = DaisyUISize.SM,
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
                            Html.option [ prop.value ""; prop.text "Select organisation" ]
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
            user: CurrentUserDto option,
            loadRepos: ExploreLoadRequest -> JS.Promise<Result<ExploreLoadResult, string>>,
            reloadTrigger: int,
            ?onOpen: (ExploreProjectDto -> unit),
            ?projectActionBtns: (ExploreProjectDto -> ButtonInfo[])
        ) =

        let tab, setTab = React.useState ExploreTab.All
        let searchTerm, setSearchTerm = React.useState ""
        /// Is used to be set when the user submits the search to trigger the useEffect that loads the repos. This is needed to avoid loading the repos on every keystroke when the user types in the search field.
        let submittedSearchTerm, setSubmittedSearchTerm = React.useState ""
        let sortField, setSortField = React.useState ExploreSortField.LastUpdated
        let sortDirection, setSortDirection = React.useState SortDirection.Desc
        let selectedGroupId, setSelectedGroupId = React.useState (None: int option)
        let groups, setGroups = React.useState [||]
        let groupsLoaded, setGroupsLoaded = React.useState false
        let groupsLoadError, setGroupsLoadError = React.useState (None: string option)
        let repos, setRepos = React.useState [||]
        let page, setPage = React.useState 1
        let pageSize = 20
        let isLoading, setIsLoading = React.useState false
        let error, setError = React.useState (None: string option)
        let pagination, setPagination = React.useState (None: PaginationMetadata option)

        let isAuthenticated = user.IsSome

        let onSearchSubmit () =
            setSubmittedSearchTerm searchTerm
            setPage 1

        React.useEffect (
            (fun () ->
                promise {
                    setIsLoading true
                    setError None

                    let request = {
                        Target = tab
                        SearchTerm = submittedSearchTerm
                        Page = page
                        PerPage = pageSize
                        SortField = sortField
                        SortDirection = sortDirection
                        SelectedGroupId = selectedGroupId
                        IsAuthenticated = isAuthenticated
                        User = user
                    }

                    let! result = loadRepos request

                    match result with
                    | Ok loaded ->
                        setRepos loaded.Repos
                        setPagination loaded.Pagination
                        setGroups loaded.Groups
                        setGroupsLoaded loaded.GroupsLoaded
                        setGroupsLoadError loaded.GroupsLoadError

                        if selectedGroupId.IsNone && loaded.Groups.Length > 0 then
                            setSelectedGroupId (Some loaded.Groups[0].id)
                    | Error err ->
                        setError (Some err)
                        setRepos [||]
                        setPagination None

                    setIsLoading false
                }
                |> Promise.start
            ),
            [|
                box tab
                box page
                box submittedSearchTerm
                box selectedGroupId
                box sortField
                box sortDirection
                box isAuthenticated
                box reloadTrigger
            |]
        )

        Html.div [
            prop.testId "GitLabExplorePanel"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-2"
            prop.children [
                DataHubComponents.DataHubComponents.SectionHeading("GitLab Explore")
                // tabs
                DataHubBrowser.Tabs(
                    tab,
                    (fun next ->
                        setTab next
                        setPage 1
                    ),
                    isAuthenticated
                )
                // filter
                DataHubBrowser.Filter(
                    tab,
                    isAuthenticated,
                    searchTerm,
                    (fun term ->
                        setSearchTerm term
                        setPage 1
                    ),
                    onSearchSubmit,
                    sortField,
                    (fun next ->
                        setSortField next
                        setPage 1
                    ),
                    sortDirection,
                    (fun next ->
                        setSortDirection next
                        setPage 1
                    ),
                    groups,
                    selectedGroupId,
                    (fun gid ->
                        setSelectedGroupId gid
                        setPage 1
                    ),
                    groupsLoadError
                )
                if isLoading then
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
                match error with
                | Some err ->
                    Html.div [
                        prop.testId "GitLabExploreError"
                        prop.className "swt:alert swt:alert-error swt:text-xs"
                        prop.children [ Html.span err ]
                    ]
                | None -> Html.none
                let showOrgNoGroups =
                    tab = ExploreTab.YourOrganisations
                    && isAuthenticated
                    && groupsLoaded
                    && groups.Length = 0
                    && groupsLoadError.IsNone

                let showOrgLoadError = tab = ExploreTab.YourOrganisations && groupsLoadError.IsSome

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
                elif (not isLoading) && repos.Length = 0 then
                    Html.p [
                        prop.testId "GitLabExploreEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text "No repositories found for current filter and search."
                    ]
                elif repos.Length > 0 then
                    Html.ul [
                        prop.testId "GitLabExploreRepoList"
                        prop.className "swt:list swt:bg-base-100 swt:rounded-box swt:shadow-md"
                        prop.children [
                            for repo in repos do
                                DataHubBrowser.RepoListRow(repo, ?extraButtons = projectActionBtns)
                        ]
                    ]

                // Pagination
                DataHubBrowser.Pagination(pagination, setPage)
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let isAuthenticated = false
        let reloadTrigger = 0

        let isLocallyCloned (p: ExploreProjectDto) = p.id % 2 = 0

        let paginate (items: ExploreProjectDto array) (page: int) (pageSize: int) =
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

        let loadRepos (request: ExploreLoadRequest) = promise {
            do! Promise.sleep 250

            let source =
                match request.Target with
                | ExploreTab.All ->
                    if request.IsAuthenticated then
                        allRepos
                    else
                        allRepos |> Array.filter isPublicRepo
                | ExploreTab.YourRepos -> MockData.DataHub.yourRepos
                | ExploreTab.MostStarred -> MockData.DataHub.mostStarred
                | ExploreTab.YourOrganisations ->
                    match request.SelectedGroupId with
                    | Some gid -> MockData.DataHub.orgRepos |> Map.tryFind gid |> Option.defaultValue [||]
                    | None -> [||]

            let filtered =
                source
                |> Array.filter (fun p ->
                    System.String.IsNullOrWhiteSpace request.SearchTerm
                    || containsCi p.name request.SearchTerm
                    || containsCi p.path_with_namespace request.SearchTerm
                    || containsCi (p.description |> Option.defaultValue "") request.SearchTerm
                )
                |> (fun items ->
                    if request.Target = ExploreTab.MostStarred then
                        items |> Array.sortByDescending (fun p -> p.star_count)
                    else
                        sortRepos request.SortField request.SortDirection items
                )

            let pageItems, pageMeta = paginate filtered request.Page request.PerPage

            return
                Ok {
                    Repos = pageItems
                    Pagination = Some pageMeta
                    Groups = MockData.DataHub.groups
                    GroupsLoaded = true
                    GroupsLoadError = None
                }
        }

        DataHubBrowser.ExplorePanel(
            None,
            loadRepos,
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

    [<ReactComponent>]
    static member GitLabEntry() =
        let currentUser, setCurrentUser = React.useState None
        let baseUrl, setBaseUrl = React.useState "https://git.nfdi4plants.org"
        let pat, setPat = React.useState "UfCieq6HDu32MFUDkfOMem86MQp1OmY0CA.01.0y00qka22"
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
                | Ok user -> setCurrentUser (Some user)
                | Error err ->
                    Browser.Dom.console.error (
                        "Failed to get current user: "
                        + (
                            match err with
                            | GitLabError.DecodeError ex -> ex.Message
                            | _ -> ""
                        )
                    )

                    setCurrentUser None
            }

        let gitLabErrorToString =
            function
            | GitLabError.NetworkError ex -> $"Network error: {ex.Message}"
            | GitLabError.Unauthorized -> "Unauthorized (check your Personal Access Token)."
            | GitLabError.Forbidden -> "Forbidden (missing permissions for this resource)."
            | GitLabError.NotFound -> "GitLab resource not found."
            | GitLabError.HttpError code -> $"GitLab request failed with HTTP {code}."
            | GitLabError.DecodeError ex -> $"Failed to decode GitLab response: {ex.Message}"
            | GitLabError.InvalidRequest message -> message

        let loadRepos (request: ExploreLoadRequest) = promise {
            if not isConnected then
                return Ok ExploreLoadResult.empty
            else
                let visibility = if request.IsAuthenticated then None else Some "public"

                let requestPat = if request.IsAuthenticated then pat else ""

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

                let! groupsResult =
                    if request.Target = ExploreTab.YourOrganisations && request.IsAuthenticated then
                        GitLabApi.ListGroupsForCurrentUser(baseUrl, pat, page = 1, perPage = 100)
                    else
                        promise {
                            return
                                Ok {
                                    Items = [||]
                                    Pagination = {
                                        Link = None
                                        NextPage = None
                                        Page = Some 1
                                        PerPage = Some 100
                                        PrevPage = None
                                        Total = Some 0
                                        TotalPages = Some 1
                                        NextCursor = None
                                        PrevCursor = None
                                    }
                                }
                        }

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
                    | ExploreTab.All ->
                        GitLabApi.ListProjects(
                            baseUrl,
                            requestPat,
                            page = request.Page,
                            perPage = request.PerPage,
                            search = request.SearchTerm,
                            orderBy = orderBy,
                            sort = sort,
                            ?visibility = visibility
                        )
                    | ExploreTab.YourRepos ->
                        if request.IsAuthenticated then
                            GitLabApi.ListUserPersonalProjects(
                                baseUrl,
                                pat,
                                page = request.Page,
                                perPage = request.PerPage,
                                search = request.SearchTerm,
                                orderBy = orderBy,
                                sort = sort
                            )
                        else
                            promise { return Ok(emptyResponse request.Page request.PerPage) }
                    | ExploreTab.MostStarred ->
                        GitLabApi.ListExploreMostStarred(
                            baseUrl,
                            requestPat,
                            page = request.Page,
                            perPage = request.PerPage,
                            search = request.SearchTerm,
                            ?visibility = visibility
                        )
                    | ExploreTab.YourOrganisations ->
                        if request.IsAuthenticated then
                            match selectedGroupId with
                            | Some gid ->
                                GitLabApi.ListGroupProjects(
                                    baseUrl,
                                    pat,
                                    gid,
                                    page = request.Page,
                                    perPage = request.PerPage,
                                    includeSubgroups = true,
                                    withShared = true,
                                    search = request.SearchTerm,
                                    orderBy = orderBy,
                                    sort = sort
                                )
                            | None -> promise { return Ok(emptyResponse request.Page request.PerPage) }
                        else
                            promise { return Ok(emptyResponse request.Page request.PerPage) }

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
                | Error err -> return Error(gitLabErrorToString err)
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
                            "swt:iconify swt:fluent--arrow-download-24-filled",
                            "Download",
                            fun _ -> Browser.Dom.window.alert ("Download " + project.name)
                        )
                        ButtonInfo.create (
                            "swt:iconify swt:fluent--star-24-regular",
                            "Star",
                            fun _ -> Browser.Dom.window.alert ("Star " + project.name)
                        )
                    |]

                DataHubBrowser.ExplorePanel(currentUser, loadRepos, reloadTrigger, projectActionBtns = btnInfo)
            ]
        ]