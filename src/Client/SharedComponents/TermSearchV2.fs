namespace Components

open Fable.Core
open Feliz
open Feliz.DaisyUI

type private Modals =
| AdvancedSearch
| Details

type Term = {
    Name: string option
    Id: string option
    IsObsolete: bool option
    Href: string option
    Description: string option
    Data: obj option
} with
    static member init(?name, ?id, ?obsolete: bool, ?href, ?description, ?data) = {
        Name = name
        Id = id
        IsObsolete = obsolete
        Href = href
        Description = description
        Data = data
    }

type SearchCall = string -> JS.Promise<ResizeArray<Term>>

type ParentSearchCall = string -> string -> JS.Promise<ResizeArray<Term>>

type AllChildrenSearchCall = string -> JS.Promise<ResizeArray<Term>>

type TermSearchResult = {
    Term: Term
    IsDirectedSearchResult: bool
} with
    static member addSearchResults (prevResults: ResizeArray<TermSearchResult>) (newResults: ResizeArray<TermSearchResult>) =
        for newResult in newResults do
            // check if new result is already in the list by id
            let index = prevResults.FindIndex(fun x -> x.Term.Id = newResult.Term.Id)
            // if it exists and the newResult is result of directedSearch, we update the item
            // Directed search normally takes longer to complete but is additional information
            // so we update non-directed search results with the directed search results
            if index >= 0 && newResult.IsDirectedSearchResult then
                prevResults.[index] <- newResult
            // if it exists but the new result is not a directed search result, we do nothing
            // maybe update the item with new information in the future
            elif index >= 0 then
                ()
            else
                // if it does not exist, we add it to the results
                prevResults.Add(newResult)
        ResizeArray(prevResults)


type SearchState =
| Idle
| SearchDone of ResizeArray<TermSearchResult>
with
    static member init() =
        SearchState.Idle

    member this.Results =
        match this with
        | SearchDone results -> results
        | _ -> ResizeArray()

module private API =

    module Mocks =

        // default search, should always be runnable
        let callSearch = fun (query: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return ResizeArray [|
                    Term.init("Term 1", "1", false, "/term/1", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.")
                    Term.init("Term 2", "2", false, "/term/2", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.")
                    Term.init("Term 3", "3", false)
                    Term.init("Term 4 Is a Very special term with a extremely long name", "4", false, "/term/4")
                    Term.init("Term 5", "5", true, "/term/5")
                |]
            }

        // search with parent, is run in parallel to default search,
        // better results, but slower and requires parent
        let callParentSearch = fun (parent: string) (query: string) ->
            promise {
                do! Promise.sleep 2000
                //Init mock data for about 10 items
                return ResizeArray [|
                    Term.init("Term 1", "1", false, "/term/1")
                |]
            }

        // search all children of parent without actual query. Quite fast
        // Only triggered onDoubleClick into empty input
        let callAllChildSearch = fun (parent: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return ResizeArray [|
                    // for i in 0 .. 100 do
                    //     Term.init(sprintf "Child %d" i, i.ToString(), i % 5 = 0, sprintf "/term/%d" i)
                |]
            }

[<Mangle(false); Erase>]
type TermSearchV2 =

    [<ReactComponent>]
    static member private TermItem(term: TermSearchResult, onTermSelect: Term option -> unit) =
        let collapsed, setCollapsed = React.useState(false)
        let isObsolete = term.Term.IsObsolete.IsSome && term.Term.IsObsolete.Value
        let isDirectedSearch = term.IsDirectedSearchResult
        let activeClasses = "group-[.collapse-open]:bg-secondary group-[.collapse-open]:text-secondary-content"
        Html.div [
            prop.className [
                "group collapse rounded-none"
                if collapsed then "collapse-open"
            ]
            prop.children [
                Html.div [
                    prop.onClick (fun e ->
                        e.stopPropagation()
                        onTermSelect (Some term.Term)
                    )
                    prop.className [
                        "collapse-title p-2 min-h-fit"
                        activeClasses
                    ]
                    prop.children [
                        Html.div [
                            prop.className "items-center grid grid-cols-[auto,1fr,auto,auto] gap-2"
                            prop.children [
                                Html.i [
                                    if isObsolete then
                                        prop.title "Obsolete"
                                    elif isDirectedSearch then
                                        prop.title "Directed Search"
                                    prop.className [
                                        "w-5"
                                        if isObsolete then
                                            "fa-solid fa-link-slash text-error";
                                        elif isDirectedSearch then
                                            "fa-solid fa-diagram-project text-primary";
                                    ]
                                ]
                                Html.span [
                                    let name = Option.defaultValue "<no-name>" term.Term.Name
                                    prop.title name
                                    prop.className [
                                        "truncate font-bold"
                                        if isObsolete then "line-through"
                                    ]
                                    prop.text name
                                ]
                                Html.a [
                                    let id = Option.defaultValue "<no-id>" term.Term.Id
                                    if term.Term.Href.IsSome then
                                        prop.onClick (fun e -> e.stopPropagation())
                                        prop.target.blank
                                        prop.href term.Term.Href.Value
                                        prop.className "link link-primary"
                                    prop.text id
                                ]
                                Components.CollapseButton(collapsed, setCollapsed, classes="btn-sm rounded")
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className [
                        "collapse-content prose-sm"
                        activeClasses
                    ]
                    prop.children [
                        Html.p [
                            prop.className "text-sm"
                            prop.children [
                                Html.text (Option.defaultValue "<no-description>" term.Term.Description)
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member NoResultsElement() =
        Html.div [
            prop.className "gap-y-2"
            prop.children [
                Html.div "No terms found matching your input."
                // if advancedTermSearchActiveSetter.IsSome then
                //     Html.div [
                //         prop.classes ["term-select-item"]
                //         prop.children [
                //             Html.span "Can't find the term you are looking for? "
                //             Html.a [
                //                 prop.className "link link-primary"
                //                 prop.onClick(fun e -> e.preventDefault(); e.stopPropagation(); advancedTermSearchActiveSetter.Value true)
                //                 prop.text "Try Advanced Search!"
                //             ]
                //         ]
                //     ]
                Html.div [
                    Html.span "Can't find what you need? Get in "
                    Html.a [prop.href Shared.URLs.Helpdesk.UrlOntologyTopic; prop.target.blank; prop.text "contact"; prop.className "link link-primary"]
                    Html.span " with us!"
                ]
            ]
        ]


    static member private TermDropdown(onTermSelect, state: SearchState, loading: Set<string>) =
        Html.div [
            prop.style [style.scrollbarGutter.stable]
            prop.className [
                "min-w-[400px]"
                "absolute top-[100%] left-0 right-0 z-50"
                "bg-base-200 rounded shadow-lg border-2 border-primary py-2 pl-4 max-h-[400px] overflow-y-auto divide-y divide-dashed divide-base-100"
                match state with | SearchState.Idle -> "hidden" | _ -> ""
            ]

            // "flex flex-col w-full absolute z-10 top-[100%] left-0 right-0 bg-white shadow-lg rounded-md divide-y-2 max-h-[400px] overflow-y-auto"
            prop.children [
                match state with
                // when search is not idle and all loading is done, but no results are found
                | SearchState.SearchDone searchResults when searchResults.Count = 0 && loading.IsEmpty ->
                    TermSearchV2.NoResultsElement()
                | SearchState.SearchDone searchResults ->
                    for res in searchResults do
                        TermSearchV2.TermItem(res, onTermSelect)
                | _ -> Html.none
            ]
        ]

    static member IndicatorItem(indicatorPosition, tooltip, tooltipPosition, icon: string, onclick) =
        Html.span [
            prop.className [
                "indicator-item text-sm"
                indicatorPosition
            ]
            prop.children [
                Html.span [
                    prop.className ["tooltip"; tooltipPosition]
                    prop.custom("data-tip", tooltip)
                    prop.children [
                        Html.button [
                            prop.onClick onclick
                            prop.className "btn btn-xs btn-ghost px-2"
                            prop.children [
                                Html.i [
                                    prop.className icon
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ExportDefaultAttribute; NamedParams>]
    static member TermSearch(
        onTermSelect: Term option -> unit,
        ?term: Term,
        ?parentId: string,
        ?termSearchQueries: ResizeArray<string * SearchCall>,
        ?parentSearchQueries: ResizeArray<string * ParentSearchCall>,
        ?allChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>,
        ?advancedSearch: bool,
        ?showDetails: bool,
        ?debug: bool
    ) =

        let showDetails = defaultArg showDetails false
        let advancedSearch = defaultArg advancedSearch false
        let debug = defaultArg debug false

        let (searchResults: SearchState), setSearchResults = React.useStateWithUpdater(SearchState.init())
        let loading, setLoading = React.useStateWithUpdater(Set.empty)
        let inputRef = React.useInputRef()
        let containerRef = React.useRef(None)

        let (modal: Modals option), setModal = React.useState None

        let onTermSelect = fun (term: Term option) ->
            if inputRef.current.IsSome then
                let v = Option.bind (fun t -> t.Name) term |> Option.defaultValue ""
                inputRef.current.Value.value <- v
            setSearchResults (fun _ -> SearchState.init())
            onTermSelect term

        let startLoadingBy = fun (key: string) ->
            setLoading(fun l ->
                let key = "L_" + key
                l.Add key)

        let stopLoadingBy = fun (key: string) ->
            setLoading(fun l ->
                let key = "L_" + key
                l.Remove key)

        let createTermSearch = fun (id: string) (search: SearchCall) ->
            let id = "T_" + id
            fun (query: string) ->
                promise {
                    startLoadingBy id
                    let! termSearchResults = search query
                    let termSearchResults = termSearchResults.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = false})
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults.Results termSearchResults |> SearchState.SearchDone)
                    stopLoadingBy id
                }

        let createParentChildTermSearch = fun (id: string) (search: ParentSearchCall) ->
            let id = "PC_" + id
            fun (parentId: string) (query: string) ->
                promise {
                    startLoadingBy id
                    let! termSearchResults = search parentId query
                    let termSearchResults = termSearchResults.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = true})
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults.Results termSearchResults |> SearchState.SearchDone)
                    stopLoadingBy id
                }

        let createAllChildTermSearch = fun (id: string) (search: AllChildrenSearchCall) ->
            let id = "AC_" + id
            fun (parentId: string) ->
                promise {
                    startLoadingBy id
                    let! termSearchResults = search parentId
                    let termSearchResults = termSearchResults.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = true})
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults.Results termSearchResults |> SearchState.SearchDone)
                    stopLoadingBy id
                }

        /// Collect all given search functions into one combined search
        let termSearchFunc = fun (query: string) ->
            [
                createTermSearch "DEFAULT_SIMPLE" API.Mocks.callSearch query
                if termSearchQueries.IsSome then
                    for id, termSearch in termSearchQueries.Value do
                        createTermSearch id termSearch query
            ]
            |> Promise.all
            |> Promise.start

        let parentSearch = fun (query: string) ->
            [
                if parentId.IsSome then
                    createParentChildTermSearch "DEFAULT_PARENTCHILD" API.Mocks.callParentSearch parentId.Value query
                    if parentSearchQueries.IsSome then
                        for id, parentSearch in parentSearchQueries.Value do
                            createParentChildTermSearch id parentSearch parentId.Value query
                    // setLoading(false)
            ]
            |> Promise.all
            |> Promise.start

        let allChildSearch = fun () ->
            [
                if parentId.IsSome then
                    createAllChildTermSearch "DEFAULT_ALLCHILD" API.Mocks.callAllChildSearch parentId.Value
                    if allChildrenSearchQueries.IsSome then
                        for id, allChildSearch in allChildrenSearchQueries.Value do
                            createAllChildTermSearch id allChildSearch parentId.Value
            ]
            |> Promise.all
            |> Promise.start

        let cancelSearch, search =
            let id = "DEFAULT_DEBOUNCE_SIMPLE"
            let startDebounceLoading = fun () -> startLoadingBy id
            let stopDebounceLoading = fun () -> stopLoadingBy id
            React.useDebouncedCallbackWithCancel(termSearchFunc, 500, stopDebounceLoading, startDebounceLoading, stopDebounceLoading)
        let cancelParentSearch, parentSearch =
            let id = "DEFAULT_DEBOUNCE_PARENT"
            let startDebounceLoading = fun () -> startLoadingBy id
            let stopDebounceLoading = fun () -> stopLoadingBy id
            React.useDebouncedCallbackWithCancel(parentSearch, 500, stopDebounceLoading, startDebounceLoading, stopDebounceLoading)
        let cancelAllChildSearch, allChildSearch = React.useDebouncedCallbackWithCancel(allChildSearch, 0)
        let cancel() =
            cancelSearch()
            cancelParentSearch()
            cancelAllChildSearch()
            setLoading(fun _ -> Set.empty)
            setSearchResults (fun _ -> SearchState.init())
        let startSearch = fun (query: string) ->
            onTermSelect (Some <| Term.init(query))
            search query
            parentSearch query

        React.useListener.onClickAway(containerRef, fun _ -> setSearchResults(fun _ -> SearchState.init()); cancel())
        Html.div [
            prop.className "form-control"
            prop.children [
                Html.div [
                    prop.className "indicator"
                    prop.children [
                        match term with
                        | Some { Name = Some name; Id = Some id } -> // full term indicator
                            if System.String.IsNullOrWhiteSpace id |> not then
                                TermSearchV2.IndicatorItem(
                                    "",
                                    sprintf "%s - %s" name id,
                                    "tooltip-left",
                                    "fa-solid fa-square-check text-primary",
                                    fun _ -> setModal (if modal.IsSome && modal.Value = Details then None else Some Modals.Details)
                                )
                        | _ when showDetails ->
                            TermSearchV2.IndicatorItem(
                                "",
                                "Details",
                                "tooltip-left",
                                "fa-solid fa-circle-info text-primary",
                                fun _ -> setModal (if modal.IsSome && modal.Value = Details then None else Some Modals.Details)
                            )
                        | _ ->
                            Html.none

                        if advancedSearch then
                            TermSearchV2.IndicatorItem(
                                "indicator-bottom",
                                "Advanced Search",
                                "tooltip-left",
                                "fa-solid fa-magnifying-glass-plus text-primary",
                                fun _ -> setModal (if modal.IsSome && modal.Value = AdvancedSearch then None else Some AdvancedSearch)
                            )

                        Html.div [ // main search component
                            if debug then
                                prop.custom("data-debug-loading", Fable.Core.JS.JSON.stringify loading)
                                prop.custom("data-debug-searchresults", Fable.Core.JS.JSON.stringify searchResults)
                            prop.ref containerRef
                            prop.className "input input-bordered flex flex-row items-center relative"
                            prop.children [
                                Html.input [
                                    prop.ref(inputRef)
                                    prop.defaultValue (term |> Option.bind _.Name |> Option.defaultValue "")
                                    prop.placeholder "..."
                                    prop.onChange (fun (e: string) ->
                                        if System.String.IsNullOrEmpty e then
                                            onTermSelect None
                                            cancel()
                                        else
                                            startSearch e
                                    )
                                    prop.onDoubleClick(fun _ ->
                                        // if we have parent id and the input is empty, we search all children of the parent
                                        if parentId.IsSome && System.String.IsNullOrEmpty inputRef.current.Value.value then
                                            allChildSearch()
                                        // if we have input we start search
                                        elif System.String.IsNullOrEmpty inputRef.current.Value.value |> not then
                                            startSearch inputRef.current.Value.value
                                    )
                                    prop.onKeyDown (key.escape, fun _ ->
                                        log "Escape"
                                        cancel()
                                    )
                                ]
                                Daisy.loading [
                                    prop.className [
                                        "text-primary loading-sm"
                                        if loading.IsEmpty then "invisible";
                                    ]
                                ]
                                TermSearchV2.TermDropdown(onTermSelect, searchResults, loading)
                            ]
                        ]
                    ]
                ]
                // if (parentId.IsSome && displayParent) then
                //     // Optional elements
                //     Html.div [
                //         prop.className "label not-prose"
                //         prop.children [
                //             // if advancedSearchDispatch.IsSome then
                //             //     Components.AdvancedSearch.Main(advancedSearchActive, setAdvancedSearchActive, (fun t ->
                //             //         setAdvancedSearchActive false
                //             //         Some t |> selectTerm),
                //             //         advancedSearchDispatch.Value
                //             //     )
                //             //     Html.span [
                //             //         prop.className "label-text-alt link-primary cursor-pointer"
                //             //         prop.onClick(fun e -> e.preventDefault(); e.stopPropagation(); setAdvancedSearchActive true)
                //             //         prop.text "Use advanced search"
                //             //     ]
                //             ]
                //     ]
            ]
        ]