namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

type private Modals =
| AdvancedSearch
| Details

[<AllowNullLiteral>]
[<Global>]
type Term
    [<ParamObjectAttribute; Emit("$0")>]
    (?name, ?id, ?description, ?source: string, ?href, ?isObsolete: bool, ?data) =
    member val name: string option = jsNative with get, set
    member val id: string option = jsNative with get, set
    member val description: string option = jsNative with get, set
    member val source: string option = jsNative with get, set
    member val href: string option = jsNative with get, set
    member val isObsolete: bool option = jsNative with get, set
    member val data: obj option = jsNative with get, set


type AdvancedSearchController = {|
    startSearch: unit -> unit
    cancel: unit -> unit
|}

type AdvancedSearch<'A> = {|
    input: 'A
    search: 'A -> JS.Promise<ResizeArray<Term>>
    form: AdvancedSearchController -> ReactElement
|}

[<AutoOpen>]
module TypeDefs =

    emitJsStatement ( ) """/**
 * Represents a term object with optional metadata.
 * @typedef {Object} Term
 * @property {string} [name] - The name of the term.
 * @property {string} [id] - The unique identifier for the term.
 * @property {string} [description] - A description of the term.
 * @property {string} [source] - The source from which the term originates.
 * @property {string} [href] - A URL linking to more information about the term.
 * @property {boolean} [isObsolete] - Whether the term is obsolete.
 * @property {Object} [data] - Additional metadata associated with the term.
 */"""

    emitJsStatement ( ) """/**
 * A search function that resolves a list of terms.
 * @typedef {function(string): Promise<Term[]>} SearchCall
 */"""

    emitJsStatement ( ) """/**
 * A parent search function that resolves a list of terms based on a parent ID and query.
 * @typedef {function(string, string): Promise<Term[]>} ParentSearchCall
 */"""

    emitJsStatement ( ) """/**
 * A function that fetches all child terms of a parent.
 * @typedef {function(string): Promise<Term[]>} AllChildrenSearchCall
 */"""


    ///
    /// A search function that resolves a list of terms.
    /// @typedef {function(string): Promise<Term[]>} SearchCall
    ///
    type SearchCall = string -> JS.Promise<ResizeArray<Term>>

    //
    // A parent search function that resolves a list of terms based on a parent ID and query.
    // @typedef {function(string, string): Promise<Term[]>} ParentSearchCall
    //
    type ParentSearchCall = string -> string -> JS.Promise<ResizeArray<Term>>

    ///
    /// A function that fetches all child terms of a parent.
    /// @typedef {function(string): Promise<Term[]>} AllChildrenSearchCall
    ///
    type AllChildrenSearchCall = string -> JS.Promise<ResizeArray<Term>>

type TermSearchResult = {
    Term: Term
    IsDirectedSearchResult: bool
} with
    static member addSearchResults (prevResults: ResizeArray<TermSearchResult>) (newResults: ResizeArray<TermSearchResult>) =
        for newResult in newResults do
            // check if new result is already in the list by id
            let index = prevResults.FindIndex(fun x -> x.Term.id = newResult.Term.id)
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
                    Term("Term 1", "1", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.", "MS", isObsolete = false, href= "www.test.de'/1")
                    Term("Term 2", "2", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.", "MS", isObsolete = false, href= "www.test.de'/2")
                    Term("Term 3", "3", isObsolete = false)
                    Term("Term 4 Is a Very special term with a extremely long name", "4", isObsolete = false, href= "www.test.de'/3")
                    Term("Term 5", "5", isObsolete = false, href= "www.test.de'/4")
                |]
            }

        // search with parent, is run in parallel to default search,
        // better results, but slower and requires parent
        let callParentSearch = fun (parent: string) (query: string) ->
            promise {
                do! Promise.sleep 2000
                //Init mock data for about 10 items
                return ResizeArray [|
                    Term("Term 1", "1", href= "/term/1", isObsolete = false)
                |]
            }

        // search all children of parent without actual query. Quite fast
        // Only triggered onDoubleClick into empty input
        let callAllChildSearch = fun (parent: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return ResizeArray [|
                    for i in 0 .. 100 do
                        Term(sprintf "Child %d" i, i.ToString(), href = (sprintf "/term/%d" i), isObsolete = (i % 5 = 0))
                |]
            }

    // let callSearch = fun (query: string) ->
    //     Api.ontology.searchTerm (Shared.DTOs.TermQuery.TermQueryDto.create query)
    //     |> Async.StartAsPromise
    //     //|> Promise.map(fun results ->
    //     //    results.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = false})
    //     //)

    // let callParentSearch = fun (parent: string) (query: string) ->
    //     Api.ontology.searchTerm (Shared.DTOs.TermQuery.TermQueryDto.create(query, parentTermId = parent))
    //     |> Async.StartAsPromise

    // let callAllChildSearch = fun (parent: string) ->
    //     Api.ontology.findAllChildTerms (Shared.DTOs.ParentTermQuery.ParentTermQueryDto.create(parent))
    //     |> Async.StartAsPromise

[<Mangle(false); Erase>]
type TermSearchV2 =

    [<ReactComponent>]
    static member private TermItem(term: TermSearchResult, onTermSelect: Term option -> unit, ?key: string) =
        let collapsed, setCollapsed = React.useState(false)
        let isObsolete = term.Term.isObsolete.IsSome && term.Term.isObsolete.Value
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
                        "collapse-title p-2 min-h-fit cursor-pointer"
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
                                    let name = Option.defaultValue "<no-name>" term.Term.name
                                    prop.title name
                                    prop.className [
                                        "truncate font-bold"
                                        if isObsolete then "line-through"
                                    ]
                                    prop.text name
                                ]
                                Html.a [
                                    let id = Option.defaultValue "<no-id>" term.Term.id
                                    if term.Term.href.IsSome then
                                        prop.onClick (fun e -> e.stopPropagation())
                                        prop.target.blank
                                        prop.href term.Term.href.Value
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
                                Html.text (Option.defaultValue "<no-description>" term.Term.description)
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member private NoResultsElement() =
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
                    Html.a [prop.href @"https://github.com/nfdi4plants/nfdi4plants_ontology/issues/new/choose"; prop.target.blank; prop.text "contact"; prop.className "link link-primary"]
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
                if state = SearchState.Idle then "hidden"
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

    static member private IndicatorItem(indicatorPosition: string, tooltip, tooltipPosition, icon: string, onclick, ?isActive: bool, ?props: IReactProperty list) =
        let isActive = defaultArg isActive true
        Html.span [
            prop.className [
                "indicator-item text-sm transition-[opacity] opacity-0"
                indicatorPosition
                if isActive then "!opacity-100"
            ]
            prop.children [
                Html.button [
                    prop.custom("data-tip", tooltip)
                    prop.onClick onclick
                    for prop in defaultArg props [] do
                        prop
                    prop.className [
                        "btn btn-xs btn-ghost px-2"
                        "tooltip"; tooltipPosition
                    ]
                    prop.children [
                        Html.i [
                            prop.className icon
                        ]
                    ]
                ]
            ]
        ]

    static member private BaseModal(
        title: string,
        content: ReactElement,
        rmv: _ -> unit,
        ?debug: string
    ) =
        Html.div [
            if debug.IsSome then
                prop.custom("data-testid", debug.Value)
            prop.className "fixed top-0 left-0 right-0 bottom-0 z-50 bg-base-300 bg-opacity-50 flex items-center justify-center p-2 sm:p-10"
            prop.onMouseDown(fun _ -> rmv())
            prop.children [
                Html.div [ // centered box
                    prop.onMouseDown(fun e -> e.stopPropagation())
                    prop.onClick(fun e -> e.stopPropagation())
                    prop.className "bg-base-100 rounded shadow-lg p-2 sm:p-4 flex flex-col gap-2 min-w-80 grow sm:max-w-md md:max-w-2xl max-h-[100%] overflow-hidden"
                    prop.children [
                        Html.div [ // header
                            prop.className "flex justify-between items-center gap-4"
                            prop.children [
                                Html.h1 [
                                    prop.className "text-3xl font-bold"
                                    prop.text title
                                ]
                                Components.DeleteButton(props=[prop.onClick (fun _ -> rmv())])
                            ]
                        ]
                        content
                    ]
                ]
            ]
        ]

    static member private DetailsModal(rvm, term: Term) =
        let label (str: string) = Html.div [
            prop.className "font-bold"
            prop.text str
        ]
        let content = Html.div [
            prop.className "grid grid-cols-1 md:grid-cols-[auto,1fr] gap-4 lg:gap-x-8"
            prop.children [
                label "Name"
                Html.div (Option.defaultValue "<no-name>" term.name)
                label "Id"
                Html.div (Option.defaultValue "<no-id>" term.id)
                label "Description"
                Html.div (Option.defaultValue "<no-description>" term.description)
                label "Source"
                Html.div (Option.defaultValue "<no-source>" term.source)
                if term.isObsolete.IsSome && term.isObsolete.Value then
                    Html.div [
                        prop.className "text-error"
                        prop.text "obsolete"
                    ]
                if term.href.IsSome then
                    Html.a [
                        prop.className "link link-primary"
                        prop.href term.href.Value
                        prop.target.blank
                        prop.text "Link"
                    ]
            ]
        ]
        TermSearchV2.BaseModal("Details", content, rvm)


    [<ReactComponent>]
    static member AdvancedSearchModal(rvm, advancedSearch: AdvancedSearch<'A>, onTermSelect, ?debug: bool) =
        let searchResults, setSearchResults = React.useState(SearchState.init)
        /// tempPagination is used to store the value of the input field, which can differ from the actual current pagination value
        let (tempPagination: int option), setTempPagination = React.useState(None)
        let pagination, setPagination = React.useState(0)
        let BinSize = 20
        let BinCount = React.useMemo((fun () -> searchResults.Results.Count / BinSize), [|box searchResults|])
        let controller = {|
          startSearch = fun () ->
            advancedSearch.search advancedSearch.input
            |> Promise.map(fun results ->
                let results = results.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = false})
                setSearchResults (SearchState.SearchDone results)
            )
            |> Promise.start
          cancel = rvm
        |}
        // Ensure that clicking on "Next"/"Previous" button will update the pagination input field
        React.useEffect(
            (fun () -> setTempPagination (pagination + 1 |> Some)),
            [|box pagination|]
        )
        let searchFormComponent() = React.fragment [
            advancedSearch.form controller
            Html.button [
                prop.className "btn btn-primary"
                prop.onClick(fun _ ->
                    advancedSearch.search advancedSearch.input
                    |> Promise.map(fun results ->
                        let results = results.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = false})
                        setSearchResults (SearchState.SearchDone results)
                    )
                    |> Promise.start
                )
                prop.text "Submit"
            ]
        ]
        let resultsComponent (results: ResizeArray<TermSearchResult>) = React.fragment [
            Html.div [
                Html.textf "Results: %i" results.Count
            ]
            Html.div [
                prop.className "max-h-[50%] overflow-y-auto"
                prop.children [
                    for res in results.GetRange(pagination * BinSize, BinSize) do
                        TermSearchV2.TermItem(res, onTermSelect, JS.JSON.stringify res)
                ]
            ]
            if BinCount > 1 then
                Html.div [
                    prop.className "join"
                    prop.children [
                        Html.input [
                            prop.className "input input-bordered join-item grow"
                            prop.type'.number
                            prop.min 1
                            prop.valueOrDefault (tempPagination |> Option.defaultValue pagination)
                            prop.max BinCount
                            prop.onChange (fun (e: int) -> System.Math.Min(System.Math.Max(e, 1), BinCount) |> Some |> setTempPagination)
                        ]
                        Html.button [
                            prop.className "btn btn-primary join-item"
                            let disabled = tempPagination.IsNone || (tempPagination.Value-1) = pagination
                            prop.disabled disabled
                            prop.onClick(fun _ ->
                              tempPagination |> Option.iter ((fun x -> x-1) >> setPagination)
                            )
                            prop.text "Go"
                        ]
                        Html.button [
                            let disabled = pagination = 0
                            prop.className "btn join-item"
                            prop.disabled disabled
                            prop.onClick(fun _ -> setPagination (pagination - 1))
                            prop.text "Previous"
                        ]
                        Html.button [
                            let disabled = pagination = BinCount-1
                            prop.disabled disabled
                            prop.className "btn join-item"
                            prop.onClick(fun _ -> setPagination (pagination + 1))
                            prop.text "Next"
                        ]
                    ]
                ]
            Html.button [
                prop.className "btn btn-primary"
                prop.onClick(fun _ -> setSearchResults SearchState.Idle)
                prop.text "Back"
            ]
        ]
        let content = Html.div [
            prop.className "flex flex-col gap-2 overflow-hidden p-2"
            prop.children [
                match searchResults with
                | SearchState.Idle ->
                    searchFormComponent()
                | SearchState.SearchDone results ->
                    resultsComponent results
            ]
        ]
        TermSearchV2.BaseModal("Advanced Search", content, rvm, ?debug = (Option.map (fun _ -> "advanced-search-modal") debug))

    ///
    /// Executes a term search with various search parameters and callbacks.
    ///
    /// @param {Object} params - The parameters for the term search.
    /// @param {function(Term|undefined): void} params.onTermSelect - A callback triggered when a term is selected.
    /// @param {Term|undefined} params.term - A plain JavaScript object representing the currently selected term, or `null` if no term is selected.
    /// @param {string} [params.parentId] - The ID of the parent term, if applicable.
    /// @param {Array<[string, SearchCall]>} [params.termSearchQueries] -
    ///   A list of term search queries. Each item is a tuple containing a description and a search function.
    /// @param {Array<[string, ParentSearchCall]>} [params.parentSearchQueries] -
    ///   A list of parent term search queries. Each item is a tuple containing a description and a search function.
    /// @param {Array<[string, AllChildrenSearchCall]>} [params.allChildrenSearchQueries] -
    ///   A list of child term search queries. Each item is a tuple containing a description and a search function.
    /// @param {AdvancedSearch<any>} [params.advancedSearch] - Configuration for performing custom advanced searches.
    /// @param {boolean} [params.showDetails=false] - Indicates whether detailed information about terms should be displayed.
    /// @param {boolean} [params.debug=false] - Indicates whether to enable debug mode.
    ///
    [<ExportDefaultAttribute; NamedParams>]
    static member TermSearch(
        onTermSelect: Term option -> unit,
        term: Term option,
        ?parentId: string,
        ?termSearchQueries: ResizeArray<string * SearchCall>,
        ?parentSearchQueries: ResizeArray<string * ParentSearchCall>,
        ?allChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>,
        ?advancedSearch: AdvancedSearch<'A>,
        ?showDetails: bool,
        ?debug: bool
    ) =

        let showDetails = defaultArg showDetails false
        let debug = defaultArg debug false

        let (searchResults: SearchState), setSearchResults = React.useStateWithUpdater(SearchState.init())
        // Set of string ids for each action started. As long as one id is still contained, shows loading spinner
        let (loading: Set<string>), setLoading = React.useStateWithUpdater(Set.empty)
        let inputRef = React.useInputRef()
        let containerRef = React.useRef(None)
        // Used to show indicator buttons only when focused
        let focused, setFocused = React.useState(false)

        let cancelled = React.useRef(false)

        let (modal: Modals option), setModal = React.useState None

        let onTermSelect = fun (term: Term option) ->
            if inputRef.current.IsSome then
                let v = Option.bind (fun (t: Term) -> t.name) term |> Option.defaultValue ""
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
                    if not cancelled.current then
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
                    if not cancelled.current then
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
                    if not cancelled.current then
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
            setSearchResults (fun _ -> SearchState.init())
            cancelled.current <- true
            setLoading(fun _ -> Set.empty) // without this cancel will await finishing the queries before stopping loading spinner
            cancelSearch()
            cancelParentSearch()
            cancelAllChildSearch()

        let startSearch = fun (query: string) ->
            cancelled.current <- false
            setSearchResults (fun _ -> SearchState.init())
            search query
            parentSearch query

        let startAllChildSearch = fun () ->
            cancelled.current <- false
            setSearchResults (fun _ -> SearchState.init())
            allChildSearch()

        React.useListener.onClickAway(containerRef,
            (fun _ ->
                setFocused false
                setSearchResults(fun _ -> SearchState.init())
                cancel()
            )
        )

        Html.div [
            if debug then
                prop.custom("data-debug-loading", Fable.Core.JS.JSON.stringify loading)
                prop.custom("data-debug-searchresults", Fable.Core.JS.JSON.stringify searchResults)
            prop.className "form-control"
            prop.ref containerRef
            prop.children [
                match modal with
                | Some Details when term.IsSome ->
                    TermSearchV2.DetailsModal((fun () -> setModal None), term.Value)
                | Some AdvancedSearch when advancedSearch.IsSome ->
                    let onTermSelect = fun (term: Term option) ->
                        onTermSelect term
                        setModal None
                    TermSearchV2.AdvancedSearchModal((fun () -> setModal None), advancedSearch.Value, onTermSelect, debug)
                | _ -> Html.none
                Html.div [
                    prop.className "indicator"
                    prop.children [
                        match term with
                        | Some term when term.name.IsSome && term.id.IsSome -> // full term indicator, show always
                            if System.String.IsNullOrWhiteSpace term.id.Value |> not then
                                TermSearchV2.IndicatorItem(
                                    "",
                                    sprintf "%s - %s" term.name.Value term.id.Value,
                                    "tooltip-left",
                                    "fa-solid fa-square-check text-primary",
                                    fun _ -> setModal (if modal.IsSome && modal.Value = Details then None else Some Modals.Details)
                                )
                        | _ when showDetails && term.IsSome -> // show only when focused
                            TermSearchV2.IndicatorItem(
                                "",
                                "Details",
                                "tooltip-left",
                                "fa-solid fa-circle-info text-info",
                                (fun _ -> setModal (if modal.IsSome && modal.Value = Details then None else Some Modals.Details)),
                                focused
                            )
                        | _ ->
                            Html.none

                        if advancedSearch.IsSome then
                            TermSearchV2.IndicatorItem(
                                "indicator-bottom",
                                "Advanced Search",
                                "tooltip-left",
                                "fa-solid fa-magnifying-glass-plus text-primary",
                                (fun _ -> setModal (if modal.IsSome && modal.Value = AdvancedSearch then None else Some AdvancedSearch)),
                                focused,
                                [prop.custom("data-testid", "advanced-search-indicator")]
                            )

                        Html.div [ // main search component
                            prop.className "input input-bordered flex flex-row items-center relative"
                            prop.children [
                                Html.input [
                                    if debug then
                                        prop.custom("data-testid", "term-search-input")
                                    prop.ref(inputRef)
                                    prop.defaultValue (term |> Option.bind _.name |> Option.defaultValue "")
                                    prop.placeholder "..."
                                    prop.onChange (fun (e: string) ->
                                        if System.String.IsNullOrEmpty e then
                                            onTermSelect None
                                            cancel()
                                        else
                                            onTermSelect (Some <| Term(e))
                                            startSearch e
                                    )
                                    prop.onDoubleClick(fun _ ->
                                        // if we have parent id and the input is empty, we search all children of the parent
                                        if parentId.IsSome && System.String.IsNullOrEmpty inputRef.current.Value.value then
                                            startAllChildSearch()
                                        // if we have input we start search
                                        elif System.String.IsNullOrEmpty inputRef.current.Value.value |> not then
                                            startSearch inputRef.current.Value.value
                                    )
                                    prop.onKeyDown (key.escape, fun _ ->
                                        cancel()
                                    )
                                    prop.onFocus(fun _ ->
                                        setFocused true
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
            ]
        ]