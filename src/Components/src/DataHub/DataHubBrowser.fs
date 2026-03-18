namespace Swate.Components

open Fable.Core
open Feliz
open Swate.Components.Types.Actionbar

open DataHubTypes
open Swate.Components.Api.GitLabApi

type private ExploreTab =
    | All
    | YourRepos
    | MostStarred
    | YourOrganisations

type private ExploreSortField =
    | LastUpdated
    | DateCreated
    | Name
    | Stars

module private ExploreSortField =

    let toProjectSortField =
        function
        | ExploreSortField.LastUpdated -> ProjectSortField.UpdatedAt
        | ExploreSortField.DateCreated -> ProjectSortField.CreatedAt
        | ExploreSortField.Name -> ProjectSortField.Name
        | ExploreSortField.Stars -> ProjectSortField.StarCount

[<Erase; Mangle(false)>]
type DataHubBrowser =

    [<ReactComponent>]
    static member ARCDetails(project: ARCProject, onSelectProject: ARCProject -> unit, ?isSelected: bool) =
        Html.li [
            prop.className "swt:list-row"
            prop.key (string project.Id)
            prop.children [
                Html.div [
                    Html.a [
                        prop.testId ("ARCBrowserItem-" + string project.Id)
                        prop.className [
                            "swt:link swt:font-medium"
                            if defaultArg isSelected false then
                                "swt:text-primary"
                        ]
                        prop.onClick (fun _ -> onSelectProject project)
                        prop.text project.Name
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member DataHubBrowser
        (
            browserMode: ARCBrowserMode,
            onBrowserModeChange: ARCBrowserMode -> unit,
            browserProjects: ARCProject[],
            isLoadingBrowser: bool,
            onSelectProject: ARCProject -> unit,
            selectedProject: ARCProject option
        ) =
        Html.div [
            prop.testId "ARCBrowserPanel"
            prop.className "swt:flex swt:flex-col swt:gap-1"
            prop.children [
                DataHubComponents.DataHubComponents.SectionHeading("ARC Browser")
                Html.div [
                    prop.testId "ARCBrowserTabs"
                    prop.role.tabList
                    prop.className "swt:tabs swt:tabs-box swt:tabs-xs swt:w-full"
                    prop.children [
                        let modes = [
                            ARCBrowserMode.YourARCs, "Your ARCs"
                            ARCBrowserMode.Latest, "Latest"
                            ARCBrowserMode.Featured, "Featured"
                        ]

                        for mode, label in modes do
                            Html.div [
                                prop.role.tab
                                prop.testId ("ARCBrowserTab-" + label.Replace(" ", ""))
                                prop.key label
                                prop.className [
                                    "swt:tab"
                                    if browserMode = mode then
                                        "swt:tab-active"
                                ]
                                prop.onClick (fun _ -> onBrowserModeChange mode)
                                prop.text label
                            ]
                    ]
                ]
                if isLoadingBrowser then
                    Html.div [
                        prop.testId "ARCBrowserLoading"
                        prop.className "swt:flex swt:items-center swt:gap-2 swt:py-2"
                        prop.children [
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                            ]
                            Html.span [
                                prop.className "swt:text-sm swt:text-base-content/70"
                                prop.text "Loading ARCs..."
                            ]
                        ]
                    ]
                elif browserProjects.Length = 0 then
                    Html.p [
                        prop.testId "ARCBrowserEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60 swt:py-2"
                        prop.text "No ARCs available."
                    ]
                else
                    Html.ul [
                        prop.className "swt:list swt:bg-base-100 swt:rounded-box swt:shadow-md"
                        prop.children [
                            for project in browserProjects do
                                let isSelected =
                                    selectedProject
                                    |> Option.map (fun p -> p.Id = project.Id)
                                    |> Option.defaultValue false

                                DataHubBrowser.ARCDetails(project, onSelectProject, isSelected)
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private RepoListRow
        (
            project: ExploreProjectDto,
            isLocallyCloned: bool,
            onClone: ExploreProjectDto -> unit,
            ?onOpen: (ExploreProjectDto -> unit),
            ?extraButtons: (ExploreProjectDto -> ReactElement list)
        ) =
        let owner =
            project.path_with_namespace.Split('/')
            |> Array.tryHead
            |> Option.defaultValue project.``namespace``.name

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
                project.name.Substring(0, 1).ToUpperInvariant()

        let actionButtons = [|
            ButtonInfo.create (
                "swt:fluent--arrow-download-24-regular swt:size-5",
                "Clone repository",
                (fun () -> onClone project)
            )
            match isLocallyCloned, onOpen with
            | true, Some openFn ->
                ButtonInfo.create (
                    "swt:fluent--open-24-regular swt:size-5",
                    "Open local repository",
                    (fun () -> openFn project)
                )
            | _ -> ()
        |]

        Html.li [
            prop.testId ("GitLabRepoRow-" + string project.id)
            prop.className [
                "swt:list-row swt:flex-col sm:swt:flex-row swt:items-start swt:gap-3"
                if isLocallyCloned then
                    "swt:bg-success/10 swt:border swt:border-success/30 swt:rounded"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:avatar swt:self-start"
                    prop.children [
                        Html.div [
                            prop.className "swt:w-10 swt:h-10 swt:rounded"
                            prop.children [
                                match project.avatar_url with
                                | Some avatarUrl when not (System.String.IsNullOrWhiteSpace avatarUrl) ->
                                    Html.img [ prop.src avatarUrl; prop.alt project.name ]
                                | _ ->
                                    Html.div [
                                        prop.className
                                            "swt:w-full swt:h-full swt:rounded swt:bg-base-300 swt:flex swt:items-center swt:justify-center swt:text-xs swt:font-semibold"
                                        prop.text avatarInitial
                                    ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex-1 swt:min-w-0"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:gap-2"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:text-base-content/60"
                                    prop.text owner
                                ]
                                Html.div [
                                    prop.className "swt:tooltip swt:tooltip-top"
                                    prop.ariaLabel visibilityLabel
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:tooltip-content"
                                            prop.text visibilityLabel
                                        ]
                                        Html.i [ prop.className [ "swt:iconify"; visibilityIcon ] ]
                                    ]
                                ]
                                if isLocallyCloned then
                                    Html.span [
                                        prop.className "swt:badge swt:badge-success swt:badge-xs"
                                        prop.text "cloned"
                                    ]
                            ]
                        ]
                        Html.a [
                            prop.href project.web_url
                            prop.target "_blank"
                            prop.rel "noopener noreferrer"
                            prop.className "swt:link swt:link-primary swt:font-semibold swt:block swt:truncate"
                            prop.text project.name
                        ]
                        Html.p [
                            prop.className "swt:text-xs swt:text-base-content/70"
                            prop.text (project.description |> Option.defaultValue "No description")
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-1 swt:pt-1"
                            prop.children [
                                Html.span [
                                    prop.className "swt:badge swt:badge-outline swt:badge-xs"
                                    prop.text ("stars " + string project.star_count)
                                ]
                                Html.span [
                                    prop.className "swt:text-xs swt:text-base-content/60"
                                    prop.text ("updated " + project.updated_at)
                                ]
                                match project.license_name with
                                | Some licenseName when not (System.String.IsNullOrWhiteSpace licenseName) ->
                                    Html.span [
                                        prop.className "swt:badge swt:badge-ghost swt:badge-xs"
                                        prop.text ("license " + licenseName)
                                    ]
                                | _ -> Html.none
                                for tag in project.tag_list |> Array.truncate 3 do
                                    Html.span [
                                        prop.className "swt:badge swt:badge-ghost swt:badge-xs"
                                        prop.text ("tag " + tag)
                                    ]
                            ]
                        ]
                        match extraButtons with
                        | Some render ->
                            Html.div [
                                prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:pt-1"
                                prop.children (render project)
                            ]
                        | None -> Html.none
                    ]
                ]
                Html.div [
                    prop.className "swt:self-end sm:swt:self-center"
                    prop.children [ Actionbar.Main(actionButtons, 2) ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private Filter(onSearchSubmit, tab, groups: GroupDto array, isAuthenticated: bool) =
        let searchTerm, setSearchTerm = React.useState ""
        let sortField, setSortField = React.useState ExploreSortField.LastUpdated
        let sortDirection, setSortDirection = React.useState SortDirection.Desc
        let selectedGroupId, setSelectedGroupId = React.useState (None: int option)

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
                                prop.className "swt:select swt:select-sm swt:btn swt:join-item swt:max-sm:w-full"
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
                                prop.ariaLabel (
                                    match sortDirection with
                                    | SortDirection.Asc -> "Sort ascending"
                                    | SortDirection.Desc -> "Sort descending"
                                )
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
                                            | SortDirection.Asc -> "swt:fluent--arrow-sort-up-24-regular swt:size-4"
                                            | SortDirection.Desc -> "swt:fluent--arrow-sort-down-24-regular swt:size-4"
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                // TODO: reenable
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
            tab: ExploreTab,
            setTab: ExploreTab -> unit,
            isAuthenticated: bool,
            searchTerm: string,
            setSearchTerm: string -> unit,
            onSearchSubmit: unit -> unit,
            sortField: ExploreSortField,
            setSortField: ExploreSortField -> unit,
            sortDirection: SortDirection,
            setSortDirection: SortDirection -> unit,
            groups: GroupDto array,
            groupsLoaded: bool,
            groupsLoadError: string option,
            selectedGroupId: int option,
            setSelectedGroupId: int option -> unit,
            repos: ExploreProjectDto array,
            isLoading: bool,
            error: string option,
            pagination: PaginationMetadata option,
            onPageChange: int -> unit,
            isLocallyCloned: ExploreProjectDto -> bool,
            onClone: ExploreProjectDto -> unit,
            ?onOpen: (ExploreProjectDto -> unit),
            ?extraButtons: (ExploreProjectDto -> ReactElement list)
        ) =

        let tab, setTab = React.useState ExploreTab.All

        Html.div [
            prop.testId "GitLabExplorePanel"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-2"
            prop.children [
                DataHubComponents.DataHubComponents.SectionHeading("GitLab Explore")
                // tabs
                DataHubBrowser.Tabs(tab, setTab, isAuthenticated)
                // filter
                DataHubBrowser.Filter(onSearchSubmit, tab, groups, isAuthenticated)
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
                                DataHubBrowser.RepoListRow(
                                    repo,
                                    isLocallyCloned repo,
                                    onClone,
                                    ?onOpen = onOpen,
                                    ?extraButtons = extraButtons
                                )
                        ]
                    ]

                // Pagination
                DataHubBrowser.Pagination(pagination, onPageChange)
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let tab, setTab = React.useState ExploreTab.All
        let isAuthenticated = false
        let searchTerm, setSearchTerm = React.useState ""
        let submittedSearchTerm, setSubmittedSearchTerm = React.useState ""
        let sortField, setSortField = React.useState ExploreSortField.LastUpdated
        let sortDirection, setSortDirection = React.useState SortDirection.Desc
        let selectedGroupId, setSelectedGroupId = React.useState (Some 100)
        let repos, setRepos = React.useState [||]
        let page, setPage = React.useState 1
        let pageSize = 2
        let isLoading, setIsLoading = React.useState false
        let error, setError = React.useState (None: string option)
        let pagination, setPagination = React.useState (None: PaginationMetadata option)

        let isLocallyCloned (p: ExploreProjectDto) = p.id % 2 = 0

        let paginate (items: ExploreProjectDto array) (page: int) =
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

        let sortRepos (items: ExploreProjectDto array) =
            let sorted =
                match sortField with
                | ExploreSortField.LastUpdated -> items |> Array.sortBy (fun p -> p.updated_at)
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

        let load () =
            promise {
                setIsLoading true
                setError None
                do! Promise.sleep 250

                let source =
                    match tab with
                    | ExploreTab.All ->
                        if isAuthenticated then
                            allRepos
                        else
                            allRepos |> Array.filter isPublicRepo
                    | ExploreTab.YourRepos -> MockData.DataHub.yourRepos
                    | ExploreTab.MostStarred -> MockData.DataHub.mostStarred
                    | ExploreTab.YourOrganisations ->
                        match selectedGroupId with
                        | Some gid -> MockData.DataHub.orgRepos |> Map.tryFind gid |> Option.defaultValue [||]
                        | None -> [||]

                let filtered =
                    source
                    |> Array.filter (fun p ->
                        System.String.IsNullOrWhiteSpace submittedSearchTerm
                        || containsCi p.name submittedSearchTerm
                        || containsCi p.path_with_namespace submittedSearchTerm
                        || containsCi (p.description |> Option.defaultValue "") submittedSearchTerm
                    )
                    |> (fun items ->
                        if tab = ExploreTab.MostStarred then
                            items |> Array.sortByDescending (fun p -> p.star_count)
                        else
                            sortRepos items
                    )

                let pageItems, pageMeta = paginate filtered page
                setRepos pageItems
                setPagination (Some pageMeta)
                setIsLoading false
            }
            |> Promise.start

        React.useEffect (
            (fun () -> load ()),
            [|
                box tab
                box page
                box submittedSearchTerm
                box selectedGroupId
                box sortField
                box sortDirection
            |]
        )

        DataHubBrowser.ExplorePanel(
            tab,
            (fun next ->
                setTab next
                setPage 1
            ),
            isAuthenticated,
            searchTerm,
            (fun term ->
                setSearchTerm term
                setPage 1
            ),
            (fun () ->
                setSubmittedSearchTerm searchTerm
                setPage 1
            ),
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
            MockData.DataHub.groups,
            true,
            None,
            selectedGroupId,
            (fun gid ->
                setSelectedGroupId gid
                setPage 1
            ),
            repos,
            isLoading,
            error,
            pagination,
            setPage,
            isLocallyCloned,
            (fun p -> Browser.Dom.console.log ($"clone {p.path_with_namespace}")),
            onOpen = (fun p -> Browser.Dom.console.log ($"open {p.path_with_namespace}")),
            extraButtons =
                (fun p ->
                    if p.star_count > 100 then
                        [
                            Html.button [
                                prop.testId ("GitLabRepoStarredBadgeButton-" + string p.id)
                                prop.className "swt:btn swt:btn-xs swt:btn-warning"
                                prop.text "Hot"
                            ]
                        ]
                    else
                        []
                )
        )

    [<ReactComponent>]
    static member GitLabEntry(?user: CurrentUserDto) =
        let user, setUser = React.useState (user)
        let baseUrl, setBaseUrl = React.useState "https://git.nfdi4plants.org"
        let pat, setPat = React.useState "UfCieq6HDu32MFUDkfOMem86MQp1OmY0CA.01.0y00qka22"
        let tab, setTab = React.useState ExploreTab.All
        let submittedSearchTerm, setSubmittedSearchTerm = React.useState ""
        // let searchTerm, setSearchTerm = React.useState ""
        // let sortField, setSortField = React.useState ExploreSortField.LastUpdated
        // let sortDirection, setSortDirection = React.useState SortDirection.Desc
        let groups, setGroups = React.useState [||]
        // let selectedGroupId, setSelectedGroupId = React.useState (None: int option)
        let repos, setRepos = React.useState [||]
        let page, setPage = React.useState 1
        let isLoading, setIsLoading = React.useState false
        let error, setError = React.useState (None: string option)
        let pagination, setPagination = React.useState (None: PaginationMetadata option)
        let groupsLoaded, setGroupsLoaded = React.useState false
        let groupsLoadError, setGroupsLoadError = React.useState (None: string option)

        let isAuthenticated = true
        let isConnected = not (System.String.IsNullOrWhiteSpace baseUrl)

        let gitLabErrorToString =
            function
            | GitLabError.NetworkError ex -> $"Network error: {ex.Message}"
            | GitLabError.Unauthorized -> "Unauthorized (check your Personal Access Token)."
            | GitLabError.Forbidden -> "Forbidden (missing permissions for this resource)."
            | GitLabError.NotFound -> "GitLab resource not found."
            | GitLabError.HttpError code -> $"GitLab request failed with HTTP {code}."
            | GitLabError.DecodeError ex -> $"Failed to decode GitLab response: {ex.Message}"
            | GitLabError.InvalidRequest message -> message

        let loadGroupsIfNeeded () = promise {
            if groups.Length = 0 && isConnected && isAuthenticated then
                setGroupsLoadError None
                let! groupsResult = GitLabApi.ListGroupsForCurrentUser(baseUrl, pat, page = 1, perPage = 100)

                match groupsResult with
                | Ok g ->
                    setGroups g.Items
                    setGroupsLoaded true
                    setGroupsLoadError None

                    if selectedGroupId.IsNone && g.Items.Length > 0 then
                        setSelectedGroupId (Some g.Items[0].id)
                | Error _ ->
                    setGroupsLoaded true
                    setGroupsLoadError (Some "Failed to load groups")
        }

        let load () =
            promise {
                if not isConnected then
                    setRepos [||]
                    setPagination None
                    return ()
                else
                    setIsLoading true
                    setError None

                    let visibility = if isAuthenticated then None else Some "public"

                    let requestPat = if isAuthenticated then pat else ""

                    let orderBy =
                        if tab = ExploreTab.MostStarred then
                            ProjectSortField.StarCount
                        else
                            ExploreSortField.toProjectSortField sortField

                    let sort =
                        if tab = ExploreTab.MostStarred then
                            SortDirection.Desc
                        else
                            sortDirection

                    if tab = ExploreTab.YourOrganisations then
                        do! loadGroupsIfNeeded ()

                    let! result =
                        match tab with
                        | ExploreTab.All ->
                            GitLabApi.ListProjects(
                                baseUrl,
                                requestPat,
                                page = page,
                                perPage = 20,
                                search = submittedSearchTerm,
                                orderBy = orderBy,
                                sort = sort,
                                ?visibility = visibility
                            )
                        | ExploreTab.YourRepos ->
                            if isAuthenticated then
                                GitLabApi.ListUserPersonalProjects(
                                    baseUrl,
                                    pat,
                                    page = page,
                                    perPage = 20,
                                    search = submittedSearchTerm,
                                    orderBy = orderBy,
                                    sort = sort
                                )
                            else
                                promise {
                                    return
                                        Ok {
                                            Items = [||]
                                            Pagination = {
                                                Link = None
                                                NextPage = None
                                                Page = Some 1
                                                PerPage = Some 20
                                                PrevPage = None
                                                Total = Some 0
                                                TotalPages = Some 1
                                                NextCursor = None
                                                PrevCursor = None
                                            }
                                        }
                                }
                        | ExploreTab.MostStarred ->
                            GitLabApi.ListExploreMostStarred(
                                baseUrl,
                                requestPat,
                                page = page,
                                perPage = 20,
                                search = submittedSearchTerm,
                                ?visibility = visibility
                            )
                        | ExploreTab.YourOrganisations ->
                            if isAuthenticated then
                                match selectedGroupId with
                                | Some gid ->
                                    GitLabApi.ListGroupProjects(
                                        baseUrl,
                                        pat,
                                        gid,
                                        page = page,
                                        perPage = 20,
                                        includeSubgroups = true,
                                        withShared = true,
                                        search = submittedSearchTerm,
                                        orderBy = orderBy,
                                        sort = sort
                                    )
                                | None -> promise {
                                    return
                                        Ok {
                                            Items = [||]
                                            Pagination = {
                                                Link = None
                                                NextPage = None
                                                Page = Some 1
                                                PerPage = Some 20
                                                PrevPage = None
                                                Total = Some 0
                                                TotalPages = Some 1
                                                NextCursor = None
                                                PrevCursor = None
                                            }
                                        }
                                  }
                            else
                                promise {
                                    return
                                        Ok {
                                            Items = [||]
                                            Pagination = {
                                                Link = None
                                                NextPage = None
                                                Page = Some 1
                                                PerPage = Some 20
                                                PrevPage = None
                                                Total = Some 0
                                                TotalPages = Some 1
                                                NextCursor = None
                                                PrevCursor = None
                                            }
                                        }
                                }

                    match result with
                    | Ok okResult ->
                        setRepos okResult.Items
                        setPagination (Some okResult.Pagination)
                    | Error err ->
                        setError (Some(gitLabErrorToString err))
                        setRepos [||]

                    setIsLoading false
            }
            |> Promise.start

        React.useEffect (
            (fun () -> load ()),
            [|
                box tab
                box page
                box submittedSearchTerm
                box selectedGroupId
                box sortField
                box sortDirection
                box isAuthenticated
            |]
        )

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
                        Html.input [
                            prop.type'.password
                            prop.testId "GitLabEntryPatInput"
                            prop.className "swt:input swt:input-sm swt:w-full"
                            prop.placeholder "Personal Access Token"
                            prop.value pat
                            prop.onChange setPat
                        ]
                        Html.button [
                            prop.testId "GitLabEntryLoadButton"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "Load"
                            prop.disabled (not isConnected)
                            prop.onClick (fun _ ->
                                setPage 1
                                setSubmittedSearchTerm searchTerm
                                load ()
                            )
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
                DataHubBrowser.ExplorePanel(
                    tab,
                    (fun next ->
                        setTab next
                        setPage 1
                    ),
                    isAuthenticated,
                    searchTerm,
                    (fun term ->
                        setSearchTerm term
                        setPage 1
                    ),
                    (fun () ->
                        setSubmittedSearchTerm searchTerm
                        setPage 1
                    ),
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
                    groupsLoaded,
                    groupsLoadError,
                    selectedGroupId,
                    (fun gid ->
                        setSelectedGroupId gid
                        setPage 1
                    ),
                    repos,
                    isLoading,
                    error,
                    pagination,
                    setPage,
                    (fun _ -> false),
                    (fun p -> Browser.Dom.console.log ($"clone {p.path_with_namespace}"))
                )
            ]
        ]