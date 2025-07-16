namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

[<RequireQualifiedAccess>]
type private Modals =
    | AdvancedSearch
    | Details

module private APIExtentions =

    let private optionOfString (str: string) =
        Option.whereNot System.String.IsNullOrWhiteSpace str

    type Swate.Components.Shared.Database.Term with
        member this.ToComponentTerm() =
            Term(
                ?name = optionOfString this.Name,
                ?id = optionOfString this.Accession,
                ?description = optionOfString this.Description,
                ?source = optionOfString this.FK_Ontology,
                isObsolete = this.IsObsolete
            )

open APIExtentions
open Browser.Types

type private KeyboardNavigationController = {
    SelectedTermSearchResult: int option
} with

    static member init() = { SelectedTermSearchResult = None }

type private TermSearchResult = {
    Term: Term
    IsDirectedSearchResult: bool
} with

    static member addSearchResults
        (prevResults: ResizeArray<TermSearchResult>)
        (newResults: ResizeArray<TermSearchResult>)
        =
        for newResult in newResults do
            // check if new result is already in the list by id
            let index = prevResults.FindIndex(fun x -> x.Term.id = newResult.Term.id)
            // if it exists and the newResult is result of directedSearch, we update the item, priority on the new
            // Directed search normally takes longer to complete but is additional information
            // so we update non-directed search results with the directed search results
            if index >= 0 then
                match prevResults.[index], newResult with
                | {
                      IsDirectedSearchResult = false
                      Term = t1
                  },
                  {
                      IsDirectedSearchResult = true
                      Term = t2
                  } ->
                    prevResults.[index] <- {
                        IsDirectedSearchResult = true
                        Term = Term.joinLeft t2 t1
                    }
                | {
                      IsDirectedSearchResult = true
                      Term = t1
                  },
                  {
                      IsDirectedSearchResult = false
                      Term = t2
                  } ->
                    prevResults.[index] <- {
                        IsDirectedSearchResult = true
                        Term = Term.joinLeft t1 t2
                    }
                | {
                      IsDirectedSearchResult = false
                      Term = t1
                  },
                  {
                      IsDirectedSearchResult = false
                      Term = t2
                  } ->
                    prevResults.[index] <- {
                        IsDirectedSearchResult = false
                        Term = Term.joinLeft t1 t2
                    }
                | {
                      IsDirectedSearchResult = true
                      Term = t1
                  },
                  {
                      IsDirectedSearchResult = true
                      Term = t2
                  } ->
                    prevResults.[index] <- {
                        IsDirectedSearchResult = true
                        Term = Term.joinLeft t1 t2
                    }
            else
                prevResults.Add(newResult)

        ResizeArray(prevResults)


type private SearchState =
    | Idle
    | SearchDone of ResizeArray<TermSearchResult>

    static member init() = SearchState.Idle

    member this.Results =
        match this with
        | SearchDone results -> results
        | _ -> ResizeArray()

module private TermSearchHelper =

    let (|SearchOngoing|SearchIsDoneEmpty|SearchIsDone|) (state: SearchState) =
        match state with
        | SearchState.Idle -> SearchOngoing
        | SearchState.SearchDone res when res.Count = 0 -> SearchIsDoneEmpty
        | SearchState.SearchDone res -> SearchIsDone res

open TermSearchHelper

module private API =

    module Mocks =

        // default search, should always be runnable
        let callSearch =
            fun (query: string) -> promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return
                    ResizeArray [|
                        Term(
                            "Term 1",
                            "1",
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.",
                            "MS",
                            isObsolete = false,
                            href = "www.test.de'/1"
                        )
                        Term(
                            "Term 2",
                            "2",
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.",
                            "MS",
                            isObsolete = false,
                            href = "www.test.de'/2"
                        )
                        Term("Term 3", "3", isObsolete = false)
                        Term(
                            "Term 4 Is a Very special term with a extremely long name",
                            "4",
                            isObsolete = false,
                            href = "www.test.de'/3"
                        )
                        Term("Term 5", "5", isObsolete = false, href = "www.test.de'/4")
                    |]
            }

        // search with parent, is run in parallel to default search,
        // better results, but slower and requires parent
        let callParentSearch =
            fun (parent: string) (query: string) -> promise {
                do! Promise.sleep 2000
                //Init mock data for about 10 items
                return ResizeArray [| Term("Term 1", "1", href = "/term/1", isObsolete = false) |]
            }

        // search all children of parent without actual query. Quite fast
        // Only triggered onDoubleClick into empty input
        let callAllChildSearch =
            fun (parent: string) -> promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return
                    ResizeArray [|
                        for i in 0..100 do
                            Term(
                                sprintf "Child %d" i,
                                i.ToString(),
                                href = (sprintf "/term/%d" i),
                                isObsolete = (i % 5 = 0)
                            )
                    |]
            }

    let callSearch =
        fun (query: string) ->
            Api.SwateApi.searchTerm (Swate.Components.Shared.DTOs.TermQuery.create (query, 10))
            |> Async.StartAsPromise
            |> Promise.map (fun results -> results |> Array.map (fun t0 -> t0.ToComponentTerm()) |> ResizeArray)

    let callParentSearch =
        fun (parent: string, query: string) ->
            Api.SwateApi.searchTerm (Swate.Components.Shared.DTOs.TermQuery.create (query, 10, parentTermId = parent))
            |> Async.StartAsPromise
            |> Promise.map (fun results -> results |> Array.map (fun t0 -> t0.ToComponentTerm()) |> ResizeArray)

    let callAllChildSearch =
        fun (parent: string) ->
            Api.SwateApi.searchChildTerms (Swate.Components.Shared.DTOs.ParentTermQuery.create (parent, 300))
            |> Async.StartAsPromise
            |> Promise.map (fun results -> results.results |> Array.map (fun t0 -> t0.ToComponentTerm()) |> ResizeArray)

    let callAdvancedSearch =
        fun dto ->
            Api.SwateApi.searchTermAdvanced dto
            |> Async.StartAsPromise
            |> Promise.map (fun results -> results |> Array.map (fun t0 -> t0.ToComponentTerm()) |> ResizeArray)

[<Mangle(false); Erase>]
type TermSearch =

    [<ReactComponent>]
    static member private TermItem
        (term: TermSearchResult, onTermSelect: Term option -> unit, ?isActive: bool, ?key: string)
        =
        let isObsolete = term.Term.isObsolete.IsSome && term.Term.isObsolete.Value
        let isDirectedSearch = term.IsDirectedSearchResult

        let activeClasses = "swt:bg-base-300 swt:text-base-content"
        let ref = React.useElementRef ()

        React.useEffect (
            (fun _ ->
                if isActive.IsSome && isActive.Value then
                    ref.current.Value.scrollIntoView (
                        jsOptions<ScrollIntoViewOptions> (fun o ->
                            // o.behavior <- Browser.Types.ScrollBehavior.Smooth
                            o.block <- Browser.Types.ScrollAlignment.Nearest // Only scroll if needed
                        )
                    )
            ),
            [| box isActive |]
        )

        Html.li [
            prop.ref ref
            prop.className [
                "swt:list-row swt:items-center swt:cursor-pointer swt:min-w-0 swt:max-w-full swt:w-full /
                swt:bg-base-100 swt:text-base-content /
                swt:hover:bg-base-300 swt:hover:text-base-content swt:transition-colors /
                swt:rounded-none"
                if isActive.IsSome && isActive.Value then
                    activeClasses
            ]
            prop.onClick (fun e ->
                e.stopPropagation ()
                onTermSelect (Some term.Term)
            )
            prop.children [
                Html.i [
                    if isObsolete then
                        prop.title "Obsolete"
                    elif isDirectedSearch then
                        prop.title "Directed Search"
                    prop.className [
                        "swt:w-5"
                        if isObsolete then
                            "fa-solid fa-link-slash swt:text-error"
                        elif isDirectedSearch then
                            "fa-solid fa-diagram-project swt:text-primary"
                    ]
                ]
                Html.span [
                    let name = Option.defaultValue "<no-name>" term.Term.name
                    prop.title name

                    prop.className [
                        "swt:truncate swt:font-bold"
                        if isObsolete then
                            "swt:line-through"
                    ]

                    prop.text name
                ]
                Html.div [
                    prop.className "swt:list-col-wrap swt:text-xs swt:text-wrap"
                    prop.children [
                        Html.p (Option.defaultValue "<no-description>" term.Term.description)
                        if term.Term.data.IsSome then
                            Html.pre [
                                Html.code (Fable.Core.JS.JSON.stringify (term.Term.data.Value, space = '\t'))
                            ]
                    ]
                // prop.children [
                //     Html.p [
                //     ]
                //     if term.Term.data.IsSome then
                // Html.pre [
                //     Html.code (Fable.Core.JS.JSON.stringify (term.Term.data.Value, space = '\t'))
                // ]
                // ]
                ]
                Html.a [
                    let id = Option.defaultValue "<no-id>" term.Term.id

                    if term.Term.href.IsSome then
                        prop.onClick (fun e -> e.stopPropagation ())
                        prop.target.blank
                        prop.href term.Term.href.Value
                        prop.className "swt:link swt:link-primary"

                    prop.text id
                ]
            // Html.div [
            //     prop.className [ "swt:collapse-content swt:prose-sm"; "swt:col-span-4"; activeClasses ]
            // ]
            ]
        ]

    static member private NoResultsElement(advancedSearchToggle: (unit -> unit) option) =
        Html.div [
            prop.className "swt:gap-y-2 swt:py-2 swt:px-4"
            prop.children [
                Html.div "No terms found matching your input."
                if advancedSearchToggle.IsSome then
                    Html.div [
                        prop.children [
                            Html.span "Can't find the term you are looking for? "
                            Html.a [
                                prop.className "swt:link swt:link-primary"
                                prop.onClick (fun e ->
                                    e.preventDefault ()
                                    e.stopPropagation ()
                                    advancedSearchToggle.Value()
                                )
                                prop.text "Try Advanced Search!"
                            ]
                        ]
                    ]
                Html.div [
                    Html.span "Can't find what you need? Get in "
                    Html.a [
                        prop.href @"https://github.com/nfdi4plants/nfdi4plants_ontology/issues/new/choose"
                        prop.target.blank
                        prop.text "contact"
                        prop.className "swt:link swt:link-primary"
                    ]
                    Html.span " with us!"
                ]
            ]
        ]

    [<ReactComponent>]

    static member private TermDropdown
        (
            termDropdownRef: IRefValue<option<HTMLElement>>,
            onTermSelect,
            state: SearchState,
            loading: Set<string>,
            advancedSearchToggle: (unit -> unit) option,
            keyboardNavState: KeyboardNavigationController,
            ?debug: bool
        ) =

        let searchIsDone =
            match state with
            | SearchIsDone _ -> true
            | _ -> false

        let debug = defaultArg debug false

        Html.ul [
            prop.ref termDropdownRef
            if debug then
                prop.testId "term_dropdown"
            prop.style [ style.scrollbarGutter.stable ]
            prop.className [
                if not searchIsDone then
                    "swt:hidden"
                else
                    """swt:min-w-[400px] not-prose swt:absolute swt:top-[100%] swt:left-0 swt:right-0 swt:z-50 swt:bg-base-200
                    swt:rounded-sm swt:shadow-lg swt:border-2 swt:border-primary swt:max-h-[400px] swt:overflow-y-auto swt:list swt:[&_.swt\:list-row]:!p-1"""
            ]

            prop.children [
                match state with
                // when search is not idle and all loading is done, but no results are found
                | SearchIsDoneEmpty -> TermSearch.NoResultsElement(advancedSearchToggle)
                | SearchIsDone searchResults ->
                    for i in 0 .. searchResults.Count - 1 do
                        let res = searchResults.[i]

                        let isActive =
                            keyboardNavState.SelectedTermSearchResult |> Option.map (fun x -> x = i)

                        TermSearch.TermItem(res, onTermSelect, ?isActive = isActive)
                | SearchOngoing -> Html.none
            ]
        ]

    static member private IndicatorItem
        (
            indicatorPosition: string,
            tooltip: string,
            tooltipPosition: string,
            icon: string,
            onclick,
            ?btnClasses: string,
            ?isActive: bool,
            ?props: IReactProperty list
        ) =
        let isActive = defaultArg isActive true

        Html.label [
            if props.IsSome then
                yield! props.Value
            prop.className [
                "swt:indicator-item swt:text-sm swt:transition-[swt:/opacity] swt:opacity-0 swt:z-10"
                indicatorPosition
                if isActive then
                    "swt:!opacity-100"
            ]
            prop.children [
                Html.button [
                    prop.custom ("data-tip", tooltip)
                    prop.onClick onclick
                    prop.className [
                        "swt:btn swt:btn-square swt:btn-xs swt:px-2 swt:!rounded-[var(--radius-field)]"
                        "swt:tooltip"
                        tooltipPosition
                        if btnClasses.IsSome then
                            btnClasses.Value
                    ]
                    prop.children [ Html.i [ prop.className icon ] ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private DetailsModal(rvm, term: Term option, config: (string * string) list) =
        let showConfig, setShowConfig = React.useState (false)

        let label (str: string) =
            Html.div [ prop.className "swt:font-bold"; prop.text str ]

        let termContent =
            match term with
            | Some term ->
                Html.div [
                    prop.className "swt:grid swt:grid-cols-1 swt:md:grid-cols-[auto,1fr] swt:gap-4 swt:lg:gap-x-8"
                    prop.children [
                        label "Name"
                        Html.div (Option.defaultValue "<no-name>" term.name)
                        label "Id"
                        Html.div (Option.defaultValue "<no-id>" term.id)
                        label "Description"
                        Html.div (Option.defaultValue "<no-description>" term.description)
                        label "Source"
                        Html.div (Option.defaultValue "<no-source>" term.source)
                        if term.data.IsSome then
                            label "Data"

                            Html.pre [
                                prop.className "swt:text-xs"
                                prop.children [
                                    Html.code (Fable.Core.JS.JSON.stringify (term.data.Value, space = '\t'))
                                ]
                            ]
                        if term.isObsolete.IsSome && term.isObsolete.Value then
                            Html.div [ prop.className "swt:text-error"; prop.text "obsolete" ]
                        if term.href.IsSome then
                            Html.a [
                                prop.className "swt:link swt:link-primary"
                                prop.href term.href.Value
                                prop.target.blank
                                prop.text "Source Link"
                            ]
                    ]
                ]
            | _ -> Html.div [ prop.text "No term selected." ]

        let componentConfig =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-4"
                prop.children [
                    for (key, value) in config ->
                        Html.div [
                            prop.className "swt:flex swt:flex-row swt:items-start swt:gap-4"
                            prop.children [
                                Html.label [ prop.className "swt:w-80 swt:font-bold"; prop.text key ]
                                Html.div value
                            ]
                        ]
                ]
            ]

        let content =
            React.fragment [
                match showConfig with
                | false ->
                    termContent

                    Html.div [
                        prop.className "swt:w-full swt:flex swt:justify-end"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-primary swt:btn-xs"
                                prop.onClick (fun _ -> setShowConfig (not showConfig))
                                prop.children [ Html.i [ prop.className "fa-solid fa-cog" ] ]
                            ]
                        ]
                    ]
                | true ->

                    componentConfig

                    Html.button [
                        prop.className "swt:btn swt:btn-xs swt:btn-neutral"
                        prop.onClick (fun _ -> setShowConfig (not showConfig))
                        prop.children [ Html.i [ prop.className "fa-solid fa-arrow-left" ]; Html.span "back" ]
                    ]
            ]

        BaseModal.BaseModal(rvm, header = Html.div "Details", content = content)

    static member private AdvancedSearchDefault
        (advancedSearchState: Swate.Components.Shared.DTOs.AdvancedSearchQuery, setAdvancedSearchState)
        =
        fun (cc: AdvancedSearchController) ->
            React.fragment [
                Html.div [
                    prop.className "swt:prose"
                    prop.children [
                        Html.p "Use the following fields to search for terms."
                        Html.p [
                            Html.text "Name and Description fields follow Apache Lucene query syntax. "
                            Html.a [
                                prop.href @"https://lucene.apache.org/core/2_9_4/queryparsersyntax.html"
                                prop.target.blank
                                prop.text "Learn more!"
                                prop.className "swt:text-xs"
                            ]
                        ]

                    ]
                ]
                Html.label [
                    prop.className "swt:w-full"
                    prop.children [
                        Html.div [
                            prop.className "swt:label"
                            prop.children [ Html.span [ prop.className "swt:label-text"; prop.text "Term Name" ] ]
                        ]
                        Html.input [
                            prop.testid "advanced-search-term-name-input"
                            prop.className "swt:input swt:w-full"
                            prop.type'.text
                            prop.autoFocus true //Due to react strict mode we render double -> could lead to losing focus
                            prop.value advancedSearchState.TermName
                            prop.onChange (fun e ->
                                setAdvancedSearchState {
                                    advancedSearchState with
                                        TermName = e
                                }
                            )
                            prop.onKeyDown (key.enter, fun _ -> cc.startSearch ())
                        ]
                    ]
                ]
                Html.label [
                    prop.className "swt:form-control swt:w-full"
                    prop.children [
                        Html.div [
                            prop.className "label"
                            prop.children [
                                Html.span [ prop.className "swt:label-text"; prop.text "Term Description" ]
                            ]
                        ]
                        Html.input [
                            prop.testid "advanced-search-term-description-input"
                            prop.className "swt:input swt:w-full"
                            prop.type'.text
                            prop.value advancedSearchState.TermDefinition
                            prop.onChange (fun e ->
                                setAdvancedSearchState {
                                    advancedSearchState with
                                        TermDefinition = e
                                }
                            )
                            prop.onKeyDown (key.enter, fun _ -> cc.startSearch ())
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:form-control swt:max-w-xs"
                    prop.children [
                        Html.label [
                            prop.className "swt:label swt:cursor-pointer"
                            prop.children [
                                Html.span [ prop.className "swt:label-text"; prop.text "Keep Obsolete" ]
                                Html.input [
                                    prop.className "swt:checkbox"
                                    prop.type'.checkbox
                                    prop.isChecked advancedSearchState.KeepObsolete
                                    prop.onChange (fun e ->
                                        setAdvancedSearchState {
                                            advancedSearchState with
                                                KeepObsolete = e
                                        }
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member private AdvancedSearchModal
        (rmv, advancedSearch0: U2<AdvancedSearch, bool>, onTermSelect, ?debug: bool)
        =
        let searchResults, setSearchResults = React.useState (SearchState.init)
        /// tempPagination is used to store the value of the input field, which can differ from the actual current pagination value
        let (tempPagination: int option), setTempPagination = React.useState (None)
        let pagination, setPagination = React.useState (0)
        // Only used if advancedSearch is set to default
        let advancedSearchState, setAdvancedSearchState =
            React.useState (Swate.Components.Shared.DTOs.AdvancedSearchQuery.init)

        let advancedSearch =
            match advancedSearch0 with
            | U2.Case1 advancedSearch -> advancedSearch
            | U2.Case2 _ ->
                AdvancedSearch(
                    (fun () -> API.callAdvancedSearch advancedSearchState),
                    TermSearch.AdvancedSearchDefault(advancedSearchState, setAdvancedSearchState)
                )

        let BinSize = 20

        let BinCount =
            React.useMemo ((fun () -> searchResults.Results.Count / BinSize), [| box searchResults |])

        let controller =
            AdvancedSearchController(
                startSearch =
                    (fun () ->
                        advancedSearch.search ()
                        |> Promise.map (fun results ->
                            let results =
                                results.ConvertAll(fun t0 -> {
                                    Term = t0
                                    IsDirectedSearchResult = false
                                })

                            setSearchResults (SearchState.SearchDone results)
                        )
                        |> Promise.start
                    ),
                cancel = rmv
            )
        // Ensure that clicking on "Next"/"Previous" button will update the pagination input field
        React.useEffect ((fun () -> setTempPagination (pagination + 1 |> Some)), [| box pagination |])

        let searchFormComponent () =
            React.fragment [
                advancedSearch.form controller
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.onClick (fun _ -> controller.startSearch ())
                    prop.text "Submit"
                ]
            ]

        let resultsComponent (results: ResizeArray<TermSearchResult>) =
            React.fragment [
                Html.div [ Html.textf "Results: %i" results.Count ]
                Html.ul [
                    prop.className "swt:max-h-[50%] swt:overflow-y-auto swt:list"
                    prop.children [
                        for res in results.GetRange(pagination * BinSize, BinSize) do
                            TermSearch.TermItem(res, onTermSelect, key = JS.JSON.stringify res)
                    ]
                ]
                if BinCount > 1 then
                    Html.div [
                        prop.className "swt:join"
                        prop.children [
                            Html.input [
                                prop.className "swt:input swt:join-item swt:grow"
                                prop.type'.number
                                prop.min 1
                                prop.valueOrDefault (tempPagination |> Option.defaultValue pagination)
                                prop.max BinCount
                                prop.onChange (fun (e: int) ->
                                    System.Math.Min(System.Math.Max(e, 1), BinCount) |> Some |> setTempPagination
                                )
                            ]
                            Html.div [
                                prop.className
                                    "swt:input swt:join-item swt:shrink swt:flex swt:justify-center /
                                    swt:items-center swt:cursor-not-allowed swt:border-l-0 swt:select-none"
                                prop.type'.text
                                prop.text ($"/{BinCount}")
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-primary swt:join-item"
                                let disabled = tempPagination.IsNone || (tempPagination.Value - 1) = pagination
                                prop.disabled disabled

                                prop.onClick (fun _ ->
                                    tempPagination |> Option.iter ((fun x -> x - 1) >> setPagination)
                                )

                                prop.text "Go"
                            ]
                            Html.button [
                                let disabled = pagination = 0
                                prop.className "swt:btn swt:join-item"
                                prop.disabled disabled
                                prop.onClick (fun _ -> setPagination (pagination - 1))
                                prop.text "Previous"
                            ]
                            Html.button [
                                let disabled = pagination = BinCount - 1
                                prop.disabled disabled
                                prop.className "swt:btn swt:join-item"
                                prop.onClick (fun _ -> setPagination (pagination + 1))
                                prop.text "Next"
                            ]
                        ]
                    ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.onClick (fun _ -> setSearchResults SearchState.Idle)
                    prop.text "Back"
                ]
            ]

        let content =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-hidden swt:p-2"
                prop.children [
                    match searchResults with
                    | SearchState.Idle -> searchFormComponent ()
                    | SearchState.SearchDone results -> resultsComponent results
                ]
            ]

        BaseModal.BaseModal(
            !!rmv,
            header = Html.div "Advanced Search",
            content = content,
            ?debug = (Option.map (fun _ -> "advanced-search-modal") debug)
        )

    ///
    /// Customizable react component for term search. Utilizing SwateDB search by default.
    //
    // #if SWATE_ENVIRONMENT
    // [<ReactComponent>]
    // #else
    // [<ExportDefaultAttribute; NamedParams>]
    // #endif
    [<ReactComponent(true)>]
    static member TermSearch
        (
            onTermSelect: Term option -> unit,
            term: Term option,
            ?parentId: string,
            ?termSearchQueries: ResizeArray<string * SearchCall>,
            ?parentSearchQueries: ResizeArray<string * ParentSearchCall>,
            ?allChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>,
            ?advancedSearch: U2<AdvancedSearch, bool>,
            ?onFocus: unit -> unit,
            ?onBlur: unit -> unit,
            ?onKeyDown: Browser.Types.KeyboardEvent -> unit,
            ?showDetails: bool,
            ?debug: bool,
            ?disableDefaultSearch: bool,
            ?disableDefaultParentSearch: bool,
            ?disableDefaultAllChildrenSearch: bool,
            ?portalTermDropdown: PortalTermDropdown,
            ?portalModals: HTMLElement,
            ?fullwidth: bool,
            ?autoFocus: bool,
            ?classNames: TermSearchStyle,
            ?props: IReactProperty list
        ) =

        let showDetails = defaultArg showDetails false
        let debug = defaultArg debug false
        let fullwidth = defaultArg fullwidth false
        let autoFocus = defaultArg autoFocus false

        let (keyboardNavState: KeyboardNavigationController), setKeyboardNavState =
            React.useState (KeyboardNavigationController.init)

        let (searchResults: SearchState), setSearchResults =
            React.useStateWithUpdater (SearchState.init ())

        /// Set of string ids for each action started. As long as one id is still contained, shows loading spinner
        let (loading: Set<string>), setLoading = React.useStateWithUpdater (Set.empty)
        let inputRef = React.useInputRef ()
        let containerRef: IRefValue<option<HTMLElement>> = React.useElementRef ()
        let termDropdownRef: IRefValue<option<HTMLElement>> = React.useElementRef ()
        let modalContainerRef: IRefValue<option<HTMLElement>> = React.useElementRef ()
        /// Used to show indicator buttons only when focused
        let focused, setFocused = React.useState (false)
        let cancelled = React.useRef (false)

        let (modal: Modals option), setModal = React.useState None

        let inputText = term |> Option.bind _.name |> Option.defaultValue ""

        React.useLayoutEffect (
            (fun () ->
                if inputRef.current.IsSome then
                    inputRef.current.Value.value <- inputText

                ()
            ),
            [| box term |]
        )

        /// Close term search result window when opening a modal
        let setModal =
            fun (modal: Modals option) ->
                if modal.IsSome then
                    setSearchResults (fun _ -> SearchState.init ())

                setModal modal

        let onTermSelect =
            fun (term: Term option) ->
                if inputRef.current.IsSome then
                    let v = Option.bind (fun (t: Term) -> t.name) term |> Option.defaultValue ""
                    inputRef.current.Value.value <- v

                setSearchResults (fun _ -> SearchState.init ())
                onTermSelect term

        let startLoadingBy =
            fun (key: string) ->
                setLoading (fun l ->
                    let key = "L_" + key
                    l.Add key
                )

        let stopLoadingBy =
            fun (key: string) ->
                setLoading (fun l ->
                    let key = "L_" + key
                    l.Remove key
                )

        let createTermSearch =
            fun (id: string) (search: SearchCall) ->
                let id = "T_" + id

                fun (query: string) -> promise {
                    startLoadingBy id
                    let! termSearchResults = search query

                    let termSearchResults =
                        termSearchResults.ConvertAll(fun t0 -> {
                            Term = t0
                            IsDirectedSearchResult = false
                        })

                    if not cancelled.current then
                        setSearchResults (fun prevResults ->
                            TermSearchResult.addSearchResults prevResults.Results termSearchResults
                            |> SearchState.SearchDone
                        )

                    stopLoadingBy id
                }

        let createParentChildTermSearch =
            fun (id: string) (search: ParentSearchCall) ->
                let id = "PC_" + id

                fun (parentId: string, query: string) -> promise {
                    startLoadingBy id
                    let! termSearchResults = search (parentId, query)

                    let termSearchResults =
                        termSearchResults.ConvertAll(fun t0 -> {
                            Term = t0
                            IsDirectedSearchResult = true
                        })

                    if not cancelled.current then
                        setSearchResults (fun prevResults ->
                            TermSearchResult.addSearchResults prevResults.Results termSearchResults
                            |> SearchState.SearchDone
                        )

                    stopLoadingBy id
                }

        let createAllChildTermSearch =
            fun (id: string) (search: AllChildrenSearchCall) ->
                let id = "AC_" + id

                fun (parentId: string) -> promise {
                    startLoadingBy id
                    let! termSearchResults = search parentId

                    let termSearchResults =
                        termSearchResults.ConvertAll(fun t0 -> {
                            Term = t0
                            IsDirectedSearchResult = true
                        })

                    if not cancelled.current then
                        setSearchResults (fun prevResults ->
                            TermSearchResult.addSearchResults prevResults.Results termSearchResults
                            |> SearchState.SearchDone
                        )

                    stopLoadingBy id
                }

        /// Collect all given search functions into one combined search
        let termSearchFunc =
            fun (query: string) ->
                [
                    if disableDefaultSearch.IsSome && disableDefaultSearch.Value then
                        ()
                    else
                        createTermSearch "DEFAULT_SIMPLE" API.callSearch query
                    if termSearchQueries.IsSome then
                        for id, termSearch in termSearchQueries.Value do
                            createTermSearch id termSearch query
                ]
                |> Promise.all
                |> Promise.start

        let parentSearch =
            fun (query: string) ->
                [
                    if parentId.IsSome then
                        if disableDefaultParentSearch.IsSome && disableDefaultParentSearch.Value then
                            ()
                        else
                            createParentChildTermSearch
                                "DEFAULT_PARENTCHILD"
                                API.callParentSearch
                                (parentId.Value, query)

                        if parentSearchQueries.IsSome then
                            for id, parentSearch in parentSearchQueries.Value do
                                createParentChildTermSearch id parentSearch (parentId.Value, query)
                // setLoading(false)
                ]
                |> Promise.all
                |> Promise.start

        let allChildSearch =
            fun () ->
                [
                    if parentId.IsSome then
                        if disableDefaultAllChildrenSearch.IsSome && disableDefaultAllChildrenSearch.Value then
                            ()
                        else
                            createAllChildTermSearch "DEFAULT_ALLCHILD" API.callAllChildSearch parentId.Value

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

            React.useDebouncedCallbackWithCancel (
                termSearchFunc,
                500,
                stopDebounceLoading,
                startDebounceLoading,
                stopDebounceLoading
            )

        let cancelParentSearch, parentSearch =
            let id = "DEFAULT_DEBOUNCE_PARENT"
            let startDebounceLoading = fun () -> startLoadingBy id
            let stopDebounceLoading = fun () -> stopLoadingBy id

            React.useDebouncedCallbackWithCancel (
                parentSearch,
                500,
                stopDebounceLoading,
                startDebounceLoading,
                stopDebounceLoading
            )

        let cancelAllChildSearch, allChildSearch =
            React.useDebouncedCallbackWithCancel (allChildSearch, 0)

        let cancel () =
            setSearchResults (fun _ -> SearchState.init ())
            cancelled.current <- true
            setLoading (fun _ -> Set.empty) // without this cancel will await finishing the queries before stopping loading spinner
            cancelSearch ()
            cancelParentSearch ()
            cancelAllChildSearch ()
            setKeyboardNavState (KeyboardNavigationController.init ())

        let startSearch =
            fun (query: string) ->
                cancelled.current <- false
                setSearchResults (fun _ -> SearchState.init ())
                search query
                parentSearch query

        let startAllChildSearch =
            fun () ->
                cancelled.current <- false
                setSearchResults (fun _ -> SearchState.init ())
                allChildSearch ()

        React.useEffectOnce (fun () ->
            if autoFocus && inputRef.current.IsSome then

                let id = Fable.Core.JS.setTimeout (fun () -> inputRef.current.Value.focus ()) 0

                React.createDisposable (fun () -> Fable.Core.JS.clearTimeout id)
            else
                React.createDisposable (fun () -> ())
        )

        // Handles click outside events to close dropdown
        React.useListener.onMouseDown (
            (fun e ->
                if focused then
                    // these are the refs, which must be checked. It boils down to: base ref (containerRef)
                    // + one ref for everything, which can be portalled outside
                    let refs = [|
                        if containerRef.current.IsSome then
                            containerRef.current.Value
                        if modalContainerRef.current.IsSome then
                            modalContainerRef.current.Value
                        if termDropdownRef.current.IsSome then
                            termDropdownRef.current.Value
                    |]

                    let refsContain =
                        refs |> Array.forall (fun el -> not (el.contains (unbox e.target)))

                    match refsContain with
                    | true ->
                        setFocused false
                        setSearchResults (fun _ -> SearchState.init ())

                        if onBlur.IsSome then
                            onBlur.Value()

                        cancel ()
                    | _ -> ()
            )
        )

        // keyboard navigation
        let keyboardNav =
            (fun (e: Browser.Types.KeyboardEvent) -> promise {
                if focused then // only run when focused
                    match searchResults, e.code with
                    | _, kbdEventCode.escape -> cancel ()
                    | SearchState.SearchDone res, kbdEventCode.arrowUp when res.Count > 0 -> // up
                        setKeyboardNavState (
                            match keyboardNavState.SelectedTermSearchResult with
                            | Some 0 -> None
                            | Some i -> Some(System.Math.Max(i - 1, 0))
                            | _ -> None
                            |> fun x -> {
                                keyboardNavState with
                                    SelectedTermSearchResult = x
                            }
                        )
                    | SearchState.SearchDone res, kbdEventCode.arrowDown when res.Count > 0 -> // down
                        setKeyboardNavState (
                            match keyboardNavState.SelectedTermSearchResult with
                            | Some i -> Some(System.Math.Min(i + 1, searchResults.Results.Count - 1))
                            | _ -> Some(0)
                            |> fun x -> {
                                keyboardNavState with
                                    SelectedTermSearchResult = x
                            }
                        )
                    | SearchState.Idle, kbdEventCode.arrowDown when
                        inputRef.current.IsSome
                        && System.String.IsNullOrWhiteSpace inputRef.current.Value.value |> not
                        -> // down
                        startSearch inputRef.current.Value.value
                    | SearchState.SearchDone res, kbdEventCode.enter when
                        keyboardNavState.SelectedTermSearchResult.IsSome
                        -> // enter
                        onTermSelect (Some res.[keyboardNavState.SelectedTermSearchResult.Value].Term)
                        cancel ()
                    | _ -> ()
            })

        /// Could move this outside, but there are so many variable to pass to...
        let modalContainer =
            let configDetails =
                [
                    "Parent Id", parentId
                    "Disable Default Search", Option.map string disableDefaultSearch
                    "Disable Default Parent Search", Option.map string disableDefaultParentSearch
                    "Disable Default All Children Search", Option.map string disableDefaultAllChildrenSearch
                    "Custom Term Search Queries", Option.map (Seq.map fst >> String.concat "; ") termSearchQueries
                    "Custom Parent Search Queries", Option.map (Seq.map fst >> String.concat "; ") parentSearchQueries
                    "Custom All Children Search Queries",
                    Option.map (Seq.map fst >> String.concat "; ") allChildrenSearchQueries
                    "Advanced Search",
                    Option.map
                        (function
                        | U2.Case1 _ -> "Custom"
                        | U2.Case2 _ -> "Default")
                        advancedSearch
                ]
                |> List.fold
                    (fun acc (key, value) ->
                        match value with
                        | Some value -> (key, value) :: acc
                        | _ -> acc
                    )
                    []
                |> List.rev

            Html.div [
                prop.className
                    "swt:z-[9999] swt:left-0 swt:top-0 swt:fixed swt:w-screen swt:h-screen swt:pointer-events-none"
                prop.ref modalContainerRef
                prop.children [
                    match modal with
                    | Some Modals.Details -> TermSearch.DetailsModal((fun _ -> setModal None), term, configDetails)
                    | Some Modals.AdvancedSearch when advancedSearch.IsSome ->
                        let onTermSelect =
                            fun (term: Term option) ->
                                onTermSelect term
                                setModal None

                        TermSearch.AdvancedSearchModal(
                            (fun _ -> setModal None),
                            advancedSearch.Value,
                            onTermSelect,
                            debug
                        )
                    | _ -> Html.none
                ]
            ]

        Html.div [
            if debug then
                prop.testId "term-search-container"
                prop.custom ("data-debug-loading", Fable.Core.JS.JSON.stringify loading)
                prop.custom ("data-debug-searchresults", Fable.Core.JS.JSON.stringify searchResults)
            prop.className [
                "not-prose"
                if fullwidth then
                    "swt:w-full"
            ]
            if props.IsSome then
                for prop in props.Value do
                    prop
            prop.ref containerRef
            prop.children [
                if modal.IsSome then
                    if portalModals.IsSome then
                        ReactDOM.createPortal (modalContainer, portalModals.Value)
                    else
                        modalContainer
                Html.div [
                    prop.className "swt:indicator swt:w-full swt:h-full"
                    prop.children [
                        match term with
                        | Some term when term.name.IsSome && term.id.IsSome -> // full term indicator, show always
                            if System.String.IsNullOrWhiteSpace term.id.Value |> not then
                                TermSearch.IndicatorItem(
                                    "",
                                    sprintf "%s - %s" term.name.Value term.id.Value,
                                    "swt:tooltip-left",
                                    "fa-solid fa-square-check",
                                    (fun _ ->
                                        setModal (
                                            if modal.IsSome && modal.Value = Modals.Details then
                                                None
                                            else
                                                Some Modals.Details
                                        )
                                    ),
                                    btnClasses = "swt:btn-primary"
                                )
                        | _ when showDetails -> // show only when focused
                            TermSearch.IndicatorItem(
                                "",
                                "Details",
                                "swt:tooltip-left",
                                "fa-solid fa-circle-info",
                                (fun _ ->
                                    setModal (
                                        if modal.IsSome && modal.Value = Modals.Details then
                                            None
                                        else
                                            Some Modals.Details
                                    )
                                ),
                                btnClasses = "swt:btn-info",
                                isActive = focused
                            )
                        | _ -> Html.none

                        if advancedSearch.IsSome then
                            TermSearch.IndicatorItem(
                                "swt:indicator-bottom",
                                "Advanced Search",
                                "swt:tooltip-left",
                                "fa-solid fa-magnifying-glass-plus",
                                (fun _ ->
                                    setModal (
                                        if modal.IsSome && modal.Value = Modals.AdvancedSearch then
                                            None
                                        else
                                            Some Modals.AdvancedSearch
                                    )
                                ),
                                btnClasses = "swt:btn-primary",
                                isActive = focused,
                                props = [ prop.testid "advanced-search-indicator" ]
                            )

                        Html.label [ // main search component
                            prop.className [
                                "swt:input swt:flex swt:flex-row swt:items-center swt:relative swt:w-full
                                swt:focus:!outline-0 swt:focus-within:!outline-0"
                                if classNames.IsSome && classNames.Value.inputLabel.IsSome then
                                    TermSearchStyle.resolveStyle classNames.Value.inputLabel.Value
                            ]
                            prop.children [
                                Html.i [
                                    prop.className [
                                        "fa-solid fa-search swt:text-primary swt:pr-2 swt:transition-all swt:w-6 swt:overflow-x-hidden swt:opacity-100"
                                        if
                                            focused
                                            || inputRef.current.IsSome
                                               && System.String.IsNullOrEmpty inputRef.current.Value.value |> not
                                        then
                                            "swt:!w-0 swt:!opacity-0"
                                    ]
                                ]
                                Html.input [
                                    prop.className "swt:grow swt:shrink swt:min-w-[50px] swt:w-full"
                                    if debug then
                                        prop.testid "term-search-input"
                                        prop.custom ("data-debug-focus", $"{autoFocus}-{focused}")
                                    prop.ref (inputRef)
                                    prop.defaultValue inputText
                                    prop.placeholder "..."
                                    prop.autoFocus autoFocus
                                    prop.onChange (fun (e: string) ->
                                        if System.String.IsNullOrEmpty e then
                                            onTermSelect None
                                            cancel ()
                                        else
                                            onTermSelect (Some <| Term(e))
                                            startSearch e
                                    )
                                    prop.onDoubleClick (fun _ ->
                                        // if we have parent id and the input is empty, we search all children of the parent
                                        if
                                            parentId.IsSome && System.String.IsNullOrEmpty inputRef.current.Value.value
                                        then
                                            startAllChildSearch ()
                                        // if we have input we start search
                                        elif System.String.IsNullOrEmpty inputRef.current.Value.value |> not then
                                            startSearch inputRef.current.Value.value
                                    )
                                    prop.onKeyDown (fun e ->
                                        e.stopPropagation ()

                                        promise {
                                            do! keyboardNav e

                                            if onKeyDown.IsSome then
                                                onKeyDown.Value e
                                        }
                                        |> Promise.start
                                    )
                                    prop.onFocus (fun _ ->
                                        if onFocus.IsSome then
                                            onFocus.Value()

                                        setFocused true
                                    )
                                ]
                                //Daisy.loading [
                                Html.div [
                                    prop.className [
                                        "swt:loading swt:text-primary swt:loading-sm"
                                        if loading.IsEmpty then
                                            "swt:invisible"
                                    ]
                                ]

                                let advancedSearchToggle =
                                    advancedSearch
                                    |> Option.map (fun _ ->
                                        fun _ ->
                                            setModal (
                                                if modal.IsSome && modal.Value = Modals.AdvancedSearch then
                                                    None
                                                else
                                                    Some Modals.AdvancedSearch
                                            )
                                    )

                                match portalTermDropdown with
                                | Some portalTermSelectArea when containerRef.current.IsSome ->
                                    ReactDOM.createPortal (
                                        (portalTermSelectArea.renderer
                                            (containerRef.current.Value.getBoundingClientRect ())
                                            (TermSearch.TermDropdown(
                                                termDropdownRef,
                                                onTermSelect,
                                                searchResults,
                                                loading,
                                                advancedSearchToggle,
                                                keyboardNavState,
                                                debug = debug
                                            ))),
                                        portalTermSelectArea.portal
                                    )
                                | _ ->
                                    TermSearch.TermDropdown(
                                        termDropdownRef,
                                        onTermSelect,
                                        searchResults,
                                        loading,
                                        advancedSearchToggle,
                                        keyboardNavState,
                                        debug = debug
                                    )
                            ]
                        ]
                    ]
                ]
            ]
        ]