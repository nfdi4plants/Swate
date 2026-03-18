namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Fetch

open DataHubTypes
open Swate.Components.Api.GitLabApi

type private ExploreTab =
    | YourRepos
    | MostStarred
    | YourOrganisations

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

        Html.li [
            prop.testId ("GitLabRepoRow-" + string project.id)
            prop.className [
                "swt:list-row swt:items-start swt:gap-2"
                if isLocallyCloned then
                    "swt:bg-success/10 swt:border swt:border-success/30 swt:rounded"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:avatar"
                    prop.children [
                        Html.div [
                            prop.className "swt:w-10 swt:h-10 swt:rounded"
                            prop.children [
                                Html.img [
                                    prop.src (
                                        project.avatar_url |> Option.defaultValue "https://picsum.photos/40/40?fallback"
                                    )
                                    prop.alt project.name
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
                                Html.span [
                                    prop.className "swt:badge swt:badge-ghost swt:badge-xs"
                                    prop.text "author: n/a"
                                ]
                                Html.span [
                                    prop.className "swt:badge swt:badge-outline swt:badge-xs"
                                    prop.text (
                                        if project.path_with_namespace.Contains("private") then
                                            "private"
                                        else
                                            "public"
                                    )
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
                            prop.className "swt:flex swt:items-center swt:gap-1 swt:pt-1"
                            prop.children [
                                Html.span [
                                    prop.className "swt:badge swt:badge-outline swt:badge-xs"
                                    prop.text ("stars " + string project.star_count)
                                ]
                                Html.span [
                                    prop.className "swt:badge swt:badge-ghost swt:badge-xs"
                                    prop.text "tag: arc"
                                ]
                                Html.span [
                                    prop.className "swt:badge swt:badge-ghost swt:badge-xs"
                                    prop.text "tag: swate"
                                ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.button [
                            prop.testId ("GitLabRepoCloneButton-" + string project.id)
                            prop.className "swt:btn swt:btn-xs swt:btn-outline"
                            prop.text "Clone"
                            prop.onClick (fun _ -> onClone project)
                        ]
                        match isLocallyCloned, onOpen with
                        | true, Some openFn ->
                            Html.button [
                                prop.testId ("GitLabRepoOpenButton-" + string project.id)
                                prop.className "swt:btn swt:btn-xs swt:btn-success"
                                prop.text "Open"
                                prop.onClick (fun _ -> openFn project)
                            ]
                        | _ -> Html.none
                        match extraButtons with
                        | Some render -> Html.div [ prop.children (render project) ]
                        | None -> Html.none
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ExplorePanel
        (
            tab: ExploreTab,
            setTab: ExploreTab -> unit,
            searchTerm: string,
            setSearchTerm: string -> unit,
            groups: GroupDto array,
            selectedGroupId: int option,
            setSelectedGroupId: int option -> unit,
            repos: ExploreProjectDto array,
            isLoading: bool,
            error: string option,
            pagination: PaginationMetadata option,
            onPageChange: int -> unit,
            onRefresh: unit -> unit,
            isLocallyCloned: ExploreProjectDto -> bool,
            onClone: ExploreProjectDto -> unit,
            ?onOpen: (ExploreProjectDto -> unit),
            ?extraButtons: (ExploreProjectDto -> ReactElement list)
        ) =
        Html.div [
            prop.testId "GitLabExplorePanel"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-2"
            prop.children [
                DataHubComponents.DataHubComponents.SectionHeading("GitLab Explore")
                Html.div [
                    prop.testId "GitLabExploreTabs"
                    prop.role.tabList
                    prop.className "swt:tabs swt:tabs-box swt:tabs-sm"
                    prop.children [
                        let tabs = [
                            ExploreTab.YourRepos, "Your Repos", "YourRepos"
                            ExploreTab.MostStarred, "Most Starred", "MostStarred"
                            ExploreTab.YourOrganisations, "Your Organisations", "YourOrganisations"
                        ]

                        for mode, label, tid in tabs do
                            Html.a [
                                prop.testId ("GitLabExploreTab-" + tid)
                                prop.className [
                                    "swt:tab"
                                    if tab = mode then
                                        "swt:tab-active"
                                ]
                                prop.onClick (fun _ -> setTab mode)
                                prop.text label
                            ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col sm:swt:flex-row swt:gap-2"
                    prop.children [
                        Html.input [
                            prop.testId "GitLabExploreSearchInput"
                            prop.className "swt:input swt:input-sm swt:w-full"
                            prop.placeholder "Search repositories"
                            prop.value searchTerm
                            prop.onChange setSearchTerm
                            prop.onKeyDown (fun e ->
                                if e.key = "Enter" then
                                    onRefresh ()
                            )
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
                                prop.children [
                                    Html.option [ prop.value ""; prop.text "Select organisation" ]
                                    for g in groups do
                                        Html.option [ prop.value (string g.id); prop.text g.name ]
                                ]
                            ]
                    ]
                ]
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
                if (not isLoading) && repos.Length = 0 then
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
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let tab, setTab = React.useState ExploreTab.YourRepos
        let searchTerm, setSearchTerm = React.useState ""
        let debouncedSearchTerm = React.useDebounce (searchTerm, 300)
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

        let load () =
            promise {
                setIsLoading true
                setError None
                do! Promise.sleep 250

                let source =
                    match tab with
                    | ExploreTab.YourRepos -> MockData.DataHub.yourRepos
                    | ExploreTab.MostStarred -> MockData.DataHub.mostStarred
                    | ExploreTab.YourOrganisations ->
                        match selectedGroupId with
                        | Some gid -> MockData.DataHub.orgRepos |> Map.tryFind gid |> Option.defaultValue [||]
                        | None -> [||]

                let filtered =
                    source
                    |> Array.filter (fun p ->
                        System.String.IsNullOrWhiteSpace debouncedSearchTerm
                        || containsCi p.name debouncedSearchTerm
                        || containsCi p.path_with_namespace debouncedSearchTerm
                        || containsCi (p.description |> Option.defaultValue "") debouncedSearchTerm
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
                box debouncedSearchTerm
                box selectedGroupId
            |]
        )

        DataHubBrowser.ExplorePanel(
            tab,
            (fun next ->
                setTab next
                setPage 1
            ),
            searchTerm,
            (fun term ->
                setSearchTerm term
                setPage 1
            ),
            MockData.DataHub.groups,
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
            load,
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
    static member GitLabEntry() =
        let baseUrl, setBaseUrl = React.useState "https://git.nfdi4plants.org"
        let pat, setPat = React.useState "UfCieq6HDu32MFUDkfOMem86MQp1OmY0CA.01.0y00qka22"
        let tab, setTab = React.useState ExploreTab.YourRepos
        let searchTerm, setSearchTerm = React.useState ""
        let debouncedSearchTerm = React.useDebounce (searchTerm, 500)
        let groups, setGroups = React.useState [||]
        let selectedGroupId, setSelectedGroupId = React.useState (None: int option)
        let repos, setRepos = React.useState [||]
        let page, setPage = React.useState 1
        let isLoading, setIsLoading = React.useState false
        let error, setError = React.useState (None: string option)
        let pagination, setPagination = React.useState (None: PaginationMetadata option)

        let isConnected =
            not (System.String.IsNullOrWhiteSpace baseUrl)
            && not (System.String.IsNullOrWhiteSpace pat)

        let tryGetHeader (response: obj) (name: string) : string option =
            let headers = response?headers

            if isNullOrUndefined headers then
                None
            else
                headers?get (name) |> Option.ofObj

        let tryParseInt (value: string option) =
            match value with
            | Some raw when not (System.String.IsNullOrWhiteSpace raw) ->
                let mutable parsed = 0

                if System.Int32.TryParse(raw, &parsed) then
                    Some parsed
                else
                    None
            | _ -> None

        let toPagination (response: obj) : PaginationMetadata = {
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

        let requestOptions = [
            RequestProperties.Method HttpMethod.GET
            requestHeaders [
                HttpRequestHeaders.Custom("PRIVATE-TOKEN", pat)
                HttpRequestHeaders.Accept "application/json"
            ]
        ]

        let getCurrentUserId () = promise {
            let! response = fetchUnsafe ($"{baseUrl.TrimEnd('/')}/api/v4/user") requestOptions

            if not response.Ok then
                return Error $"HTTP {response.Status} on /user"
            else
                let! user = response.json<{| id: int |}> ()
                return Ok user.id
        }

        let fetchGroups () = promise {
            let url = $"{baseUrl.TrimEnd('/')}/api/v4/groups?page=1&per_page=100"
            let! response = fetchUnsafe url requestOptions

            if not response.Ok then
                return Error $"HTTP {response.Status} on /groups"
            else
                let! payload = response.json<GroupDto array> ()
                let pagination = toPagination (box response)

                return
                    Ok {
                        Items = payload
                        Pagination = pagination
                    }
        }

        let fetchProjects (url: string) = promise {
            let! response = fetchUnsafe url requestOptions

            if not response.Ok then
                return Error $"HTTP {response.Status} on projects endpoint"
            else
                let! payload = response.json<ExploreProjectDto array> ()
                let pagination = toPagination (box response)

                return
                    Ok {
                        Items = payload
                        Pagination = pagination
                    }
        }

        let loadGroupsIfNeeded () = promise {
            if groups.Length = 0 && isConnected then
                let! groupsResult = fetchGroups ()

                match groupsResult with
                | Ok g ->
                    setGroups g.Items

                    if selectedGroupId.IsNone && g.Items.Length > 0 then
                        setSelectedGroupId (Some g.Items[0].id)
                | Error err -> setError (Some(string err))
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

                    if tab = ExploreTab.YourOrganisations then
                        do! loadGroupsIfNeeded ()

                    let! result =
                        match tab with
                        | ExploreTab.YourRepos -> promise {
                            let! userIdResult = getCurrentUserId ()

                            match userIdResult with
                            | Error e -> return Error e
                            | Ok userId ->
                                let url =
                                    $"{baseUrl.TrimEnd('/')}/api/v4/users/{userId}/projects?page={page}&per_page=20&search={JS.encodeURIComponent debouncedSearchTerm}"

                                return! fetchProjects url
                          }
                        | ExploreTab.MostStarred ->
                            let url =
                                $"{baseUrl.TrimEnd('/')}/api/v4/projects?page={page}&per_page=20&order_by=star_count&sort=desc&search={JS.encodeURIComponent debouncedSearchTerm}"

                            fetchProjects url
                        | ExploreTab.YourOrganisations ->
                            match selectedGroupId with
                            | Some gid ->
                                let url =
                                    $"{baseUrl.TrimEnd('/')}/api/v4/groups/{gid}/projects?page={page}&per_page=20&include_subgroups=true&with_shared=true&search={JS.encodeURIComponent debouncedSearchTerm}"

                                fetchProjects url
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

                    match result with
                    | Ok okResult ->
                        setRepos okResult.Items
                        setPagination (Some okResult.Pagination)
                    | Error err ->
                        setError (Some err)
                        setRepos [||]

                    setIsLoading false
            }
            |> Promise.start

        React.useEffect (
            (fun () -> load ()),
            [|
                box tab
                box page
                box debouncedSearchTerm
                box selectedGroupId
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
                                load ()
                            )
                        ]
                        Html.button [
                            prop.text "Test"
                            prop.onClick (fun _ ->
                                Browser.Dom.console.log "Testing GitLab API connection with current credentials..."

                                promise {
                                    let! user = GitLabApi.ListProjects(baseUrl, pat)
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
                    searchTerm,
                    (fun term ->
                        setSearchTerm term
                        setPage 1
                    ),
                    groups,
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
                    load,
                    (fun _ -> false),
                    (fun p -> Browser.Dom.console.log ($"clone {p.path_with_namespace}"))
                )
            ]
        ]