namespace Components

open Fable.Core
open Feliz
open Feliz.DaisyUI


type Term = {
    Name: string
    Id: string
    IsObsolete: bool option
    Href: string option
} with
    static member init(?name, ?id, ?obsolete: bool, ?href) = {
        Name = defaultArg name ""
        Id = defaultArg id ""
        IsObsolete = obsolete
        Href = href
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

module private API =

    module Mocks =

        // default search, should always be runnable
        let callSearch = fun (query: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return ResizeArray [|
                    Term.init("Term 1", "1", false, "/term/1")
                    Term.init("Term 2", "2", false, "/term/2")
                    Term.init("Term 3", "3", false, "/term/3")
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
                    for i in 0 .. 100 do
                        Term.init(sprintf "Child %d" i, i.ToString(), i % 5 = 0, sprintf "/term/%d" i)
                |]
            }

[<Mangle(false); Erase>]
type TermSearchV2 =

    [<ReactComponent>]
    static member private TermItem(term: TermSearchResult, onTermSelect: Term option -> unit) =
        let collapsed, setCollapsed = React.useState(false)
        let isObsolete = term.Term.IsObsolete.IsSome && term.Term.IsObsolete.Value
        let isDirectedSearch = term.IsDirectedSearchResult
        Html.div [
            prop.className [
                "group collapse rounded-none"
                if collapsed then "collapse-open"
            ]
            prop.children [
                Html.div [
                    prop.className "collapse-title p-2 min-h-fit group-[.collapse-open]:bg-primary"
                    prop.children [
                        Html.div [
                            prop.className "items-center grid grid-cols-[auto,1fr,1fr,auto] gap-2"
                            prop.children [
                                Html.i [
                                    prop.className [
                                        "size-4"
                                        if isObsolete then
                                            "text-error"
                                            "fas fa-exclamation-triangle"
                                        elif isDirectedSearch then
                                            "text-primary"
                                            "fas fa-tag"
                                    ]
                                ]
                                Html.span [
                                    prop.children [
                                        Html.text term.Term.Name
                                    ]
                                ]
                                Html.span [
                                    prop.children [
                                        Html.text term.Term.Id
                                    ]
                                ]
                                Components.CollapseButton(collapsed, setCollapsed, classes="btn-sm rounded group-[.collapse-open]:btn-glass")
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "collapse-content"
                    prop.children [
                        Html.div "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum. Nullam nec purus nec libero fermentum fermentum."
                    ]
                ]
            ]

        ]

    static member private TermDropdown(onTermSelect, searchResults: ResizeArray<TermSearchResult>, setSearchResults) =
        Html.div [
            prop.className [
                "min-w-[400px]"
                "absolute top-[100%] left-0 right-0 z-50"
                "bg-base-200 rounded shadow-lg border-2 border-primary py-2 pl-4 max-h-[400px] overflow-y-auto"
                if searchResults.Count = 0 then "hidden"
            ]

            // "flex flex-col w-full absolute z-10 top-[100%] left-0 right-0 bg-white shadow-lg rounded-md divide-y-2 max-h-[400px] overflow-y-auto"
            prop.children [
                for res in searchResults do
                    TermSearchV2.TermItem(res, onTermSelect)
            ]
        ]

    [<ExportDefaultAttribute; NamedParams>]
    static member TermSearch(onTermSelect: Term option -> unit, ?term: Term, ?parentId: string, ?termSearchQueries: ResizeArray<string * SearchCall>, ?parentSearchQueries: ResizeArray<string * ParentSearchCall>, ?allChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>) =
        let (searchResults: ResizeArray<TermSearchResult>), setSearchResults = React.useStateWithUpdater(ResizeArray())
        let loading, setLoading = React.useStateWithUpdater(Set.empty)
        let inputRef = React.useInputRef()
        let containerRef = React.useRef(None)

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
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults termSearchResults)
                    stopLoadingBy id
                }

        let createParentChildTermSearch = fun (id: string) (search: ParentSearchCall) ->
            let id = "PC_" + id
            fun (parentId: string) (query: string) ->
                promise {
                    startLoadingBy id
                    let! termSearchResults = search parentId query
                    let termSearchResults = termSearchResults.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = true})
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults termSearchResults)
                    stopLoadingBy id
                }

        let createAllChildTermSearch = fun (id: string) (search: AllChildrenSearchCall) ->
            let id = "AC_" + id
            fun (parentId: string) ->
                promise {
                    startLoadingBy id
                    let! termSearchResults = search parentId
                    let termSearchResults = termSearchResults.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = true})
                    setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults termSearchResults)
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
        let startSearch = fun (query: string) ->
            onTermSelect (Some <| Term.init(query))
            setSearchResults (fun _ -> ResizeArray())
            search query
            parentSearch query

        React.useListener.onClickAway(containerRef, fun _ -> setSearchResults(fun _ -> ResizeArray()); cancel())

        Html.label [
            prop.custom("data-debug-loading", Fable.Core.JS.JSON.stringify loading)
            prop.ref containerRef
            prop.className "input input-bordered flex flex-row items-center relative"
            prop.children [
                Html.input [
                    prop.ref(inputRef)
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
                TermSearchV2.TermDropdown(onTermSelect, searchResults, setSearchResults)
            ]
        ]