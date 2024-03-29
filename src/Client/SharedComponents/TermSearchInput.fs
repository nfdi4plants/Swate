﻿namespace Components

open Feliz
open Feliz.Bulma
open Browser.Types
open ARCtrl.ISA
open Shared
open Fable.Core.JsInterop

module TermSearchAux =

    let [<Literal>] SelectAreaID = "TermSearch_SelectArea" 

    [<RequireQualifiedAccess>]
    type SearchIs =
    | Idle
    | Running
    | Done

    type SearchState = {
        SearchIs: SearchIs
        Results: TermTypes.Term []
    } with
        static member init() = {
            SearchIs = SearchIs.Idle
            Results = [||]
        }

    let searchByName(query: string, setResults: TermTypes.Term [] -> unit) =
        async {
            let! terms = Api.ontology.searchTerms {|limit = 5; ontologies = []; query=query|}
            setResults terms
        }

    let searchByParent(query: string, parentTAN: string, setResults: TermTypes.Term [] -> unit) =
        async {
            let! terms = Api.ontology.searchTermsByParent {|limit = 50; parentTAN = parentTAN; query = query|}
            setResults terms
        }

    let searchAllByParent(parentTAN: string, setResults: TermTypes.Term [] -> unit) =
        async {
            let! terms = Api.api.getAllTermsByParentTerm <| TermTypes.TermMinimal.create "" parentTAN
            setResults terms
        }

    let allByParentSearch (
        parent: OntologyAnnotation, 
        setSearchTreeState: SearchState -> unit,
        setLoading: bool -> unit,
        stopSearch: unit -> unit,
        debounceStorage: System.Collections.Generic.Dictionary<string,int>,
        debounceTimer: int
    ) = 
        let queryDB() =
            [
                async { 
                    ClickOutsideHandler.AddListener(SelectAreaID, fun e -> stopSearch())
                }
                searchAllByParent(parent.TermAccessionShort,fun terms -> setSearchTreeState {Results = terms; SearchIs = SearchIs.Done})
            ]
            |> Async.Parallel
            |> Async.Ignore
            |> Async.StartImmediate
        setSearchTreeState <| {Results = [||]; SearchIs = SearchIs.Running}
        debouncel debounceStorage "TermSearch" debounceTimer setLoading queryDB ()

    let mainSearch (
        queryString: string, 
        parent: OntologyAnnotation option,
        setSearchNameState: SearchState -> unit,
        setSearchTreeState: SearchState -> unit,
        setLoading: bool -> unit,
        stopSearch: unit -> unit,
        debounceStorage: System.Collections.Generic.Dictionary<string,int>,
        debounceTimer: int
    ) =
        let queryDB() =
            [
                async { 
                    ClickOutsideHandler.AddListener(SelectAreaID, fun e -> stopSearch())
                }
                searchByName(queryString, fun terms -> setSearchNameState {Results = terms; SearchIs = SearchIs.Done })
                if parent.IsSome then searchByParent(queryString, parent.Value.TermAccessionShort, fun terms -> setSearchTreeState {Results = terms; SearchIs = SearchIs.Done })
            ]
            |> Async.Parallel
            |> Async.Ignore
            |> Async.StartImmediate
        setSearchNameState <| SearchState.init()
        debouncel debounceStorage "TermSearch" debounceTimer setLoading queryDB ()

    module Components =

        let termSeachNoResults = [
            Html.div [
                prop.key $"TermSelectItem_NoResults"
                prop.classes ["term-select-item"]
                prop.children [
                    Html.div "No terms found matching your input."
                ]
            ]
            Html.div [
                prop.key $"TermSelectItem_Suggestion"
                prop.classes ["term-select-item"]
                prop.children [
                    Html.div "Can't find the term you are looking for? Try Advanced Search!"
                ]
            ]
            Html.div [
                prop.key $"TermSelectItem_Contact"
                prop.classes ["term-select-item"]
                prop.children [
                    Html.div [
                        Html.span "Still can't find what you need? Get in " 
                        Html.a [prop.href Shared.URLs.Helpdesk.UrlOntologyTopic; prop.target.blank; prop.text "contact"]
                        Html.span " with us!"
                    ]
                ]
            ]
        ]

        let searchIcon =
            Bulma.icon [
                Bulma.icon.isLeft
                Bulma.icon.isSmall
                prop.children [
                    Html.i [prop.className "fa-solid fa-magnifying-glass"]
                ]
            ]

        let verifiedIcon =
            Bulma.icon [
                Bulma.icon.isRight
                Bulma.icon.isSmall
                prop.children [
                    Html.i [prop.className "fa-solid fa-check"]
                ]
            ]

        let termSelectItemMain (term: TermTypes.Term, show, setShow, setTerm, isDirectedSearchResult: bool) = 
            Html.div [
                prop.classes ["is-flex"; "is-flex-direction-row"; "term-select-item-main"]
                prop.onClick setTerm
                prop.style [style.position.relative]
                prop.children [
                    Html.i [
                        prop.style [style.width (length.px 20)]
                        if isDirectedSearchResult then 
                            prop.classes ["fa-solid fa-share-nodes"; "is-flex"; "is-align-items-center"]
                            prop.title "Related Term"
                    ]
                    Html.div [
                        prop.classes ["has-text-weight-bold"]
                        prop.style [style.flexGrow 1; style.textOverflow.ellipsis; style.whitespace.nowrap; style.overflow.hidden]
                        prop.text term.Name
                    ]
                    Html.div [
                        prop.classes ["is-flex"; "is-align-items-center"; "is-justify-content-end"; ]
                        prop.style [style.flexGrow 1]
                        prop.children [
                            Html.a [
                                prop.href (Shared.URLs.termAccessionUrlOfAccessionStr term.Accession)
                                prop.target.blank
                                prop.onClick(fun e -> e.stopPropagation())
                                prop.text term.Accession
                            ]
                        ]
                    ]
                    Html.button [
                        prop.onClick(fun e -> 
                            e.stopPropagation()
                            setShow (not show)
                        )
                        prop.classes ["term-select-item-toggle-button"]
                        prop.children [ Html.i [prop.classes ["fa-solid"; if show then "fa-angle-up" else "fa-angle-down"]] ]
                    ]
                ]
            ]

        let termSelectItemMore (term: TermTypes.Term, show) =
            Bulma.field.div [
                prop.classes [ 
                    if not show then "is-hidden";
                    "term-select-item-more"    
                ]
                prop.children [
                    Bulma.table [
                        Bulma.table.isFullWidth
                        //prop.className "p-0"
                        prop.children [
                            Html.tbody [
                                Html.tr [
                                    Html.td [ prop.className "has-text-weight-bold pl-0"; prop.text "Name:"]
                                    Html.td [ prop.text (if term.Name = "" then "<no name>" else term.Name)]
                                ]
                                Html.tr [
                                    Html.td [ prop.className "has-text-weight-bold pl-0"; prop.text "Description:"]
                                    Html.td [ prop.text (if term.Description = "" then "<no description>" else term.Description)]
                                ]
                                Html.tr [
                                    Html.td [ prop.className "has-text-weight-bold pl-0"; prop.text "Source:"]
                                    Html.td [ prop.text (term.FK_Ontology)]
                                ]
                                if term.IsObsolete then
                                    Html.tr [
                                        Html.td [ prop.classes ["pl-0"; "has-text-danger"]; prop.text "Obsolete"]
                                        Html.td [ ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
            
open TermSearchAux
open Fable.Core.JsInterop

type TermSearch =

    [<ReactComponent>]
    static member TermSelectItem (term: TermTypes.Term, setTerm, ?isDirectedSearchResult: bool) =
        let isDirectedSearchResult = defaultArg isDirectedSearchResult false
        let show, setShow = React.useState(false)
        Html.div [
            prop.key $"TermSelectItem_{term.Accession}"
            prop.classes ["term-select-item"]
            prop.children [
                Components.termSelectItemMain(term, show, setShow, setTerm, isDirectedSearchResult)
                Components.termSelectItemMore(term, show)
            ]
        ]

    static member TermSelectArea (id: string, searchNameState: SearchState, searchTreeState: SearchState, setTerm: TermTypes.Term option -> unit, show: bool, width: Styles.ICssUnit, alignRight) =
        let searchesAreComplete = searchNameState.SearchIs = SearchIs.Done && searchTreeState.SearchIs = SearchIs.Done
        let foundInBoth (term:TermTypes.Term) =
            (searchTreeState.Results |> Array.contains term)
            && (searchNameState.Results |> Array.contains term)
        let matchSearchState (ss: SearchState) (isDirectedSearch: bool) =
            match ss with
            | {SearchIs = SearchIs.Done; Results = [||]} when not isDirectedSearch ->  
                Components.termSeachNoResults
            | {SearchIs = SearchIs.Done; Results = results} -> [
                    for term in results do
                        let setTerm = fun (e: MouseEvent) -> setTerm (Some term)
                        // Term is found in both: Do not show in real directed search, update first search hit instead
                        if searchesAreComplete && foundInBoth term then 
                            if isDirectedSearch then
                                Html.none
                            else
                                TermSearch.TermSelectItem (term, setTerm, true)
                        else
                            TermSearch.TermSelectItem (term, setTerm, isDirectedSearch)
                ]
            | {SearchIs = SearchIs.Running; Results = _ } -> [
                    Html.div  "loading.."
                ]
            | _ -> [
                    Html.none
                ]
        Html.div [
            prop.id id
            prop.classes ["term-select-area"; if not show then "is-hidden";]
            prop.style [style.width width; if alignRight then style.right 0]
            prop.children [
                yield! matchSearchState searchNameState false
                yield! matchSearchState searchTreeState true
            ]
        ]

    [<ReactComponent>]
    static member Input (
        setter: OntologyAnnotation option -> unit, dispatch, 
        ?input: OntologyAnnotation, ?parent': OntologyAnnotation, 
        ?showAdvancedSearch: bool,
        ?fullwidth: bool, ?size: IReactProperty, ?isExpanded: bool, ?dropdownWidth: Styles.ICssUnit, ?alignRight: bool, ?displayParent: bool) 
        =
        let displayParent = defaultArg displayParent true
        let alignRight = defaultArg alignRight false
        let dropdownWidth = defaultArg dropdownWidth (length.perc 100)
        let isExpanded = defaultArg isExpanded false
        let showAdvancedSearch = defaultArg showAdvancedSearch false
        let advancedSearchActive, setAdvancedSearchActive = React.useState(false)
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let searchNameState, setSearchNameState = React.useState(SearchState.init)
        let searchTreeState, setSearchTreeState = React.useState(SearchState.init)
        let isSearching, setIsSearching = React.useState(false)
        let debounceStorage, setdebounceStorage = React.useState(newDebounceStorage)
        let parent, setParent = React.useState(parent')
        let stopSearch() = 
            debounceStorage.Clear()
            setLoading false
            setIsSearching false
            setSearchTreeState {searchTreeState with SearchIs = SearchIs.Idle}
            setSearchNameState {searchNameState with SearchIs = SearchIs.Idle}
        let selectTerm (t:TermTypes.Term option) =
            let oaOpt = t |> Option.map OntologyAnnotation.fromTerm 
            setState oaOpt
            setter oaOpt
            setIsSearching false
        let startSearch(queryString: string option) =
            let oaOpt = queryString |> Option.map (fun s -> OntologyAnnotation.fromString(s) )
            setter oaOpt
            setState oaOpt
            setSearchNameState <| SearchState.init()
            setSearchTreeState <| SearchState.init()
            setIsSearching true
        React.useEffect((fun () -> setParent parent'), dependencies=[|box parent'|]) // careful, check console. might result in maximum dependency depth error.
        Bulma.control.div [
            if isExpanded then Bulma.control.isExpanded
            if size.IsSome then size.Value
            Bulma.control.hasIconsLeft
            Bulma.control.hasIconsRight
            prop.style [
                if fullwidth then style.flexGrow 1; 
            ]
            if loading then Bulma.control.isLoading
            prop.children [
                Bulma.input.text [
                    if size.IsSome then size.Value
                    if state.IsSome then prop.valueOrDefault state.Value.NameText
                    prop.onDoubleClick(fun e ->
                        let s : string = e.target?value
                        if s.Trim() = "" && parent.IsSome && parent.Value.TermAccessionShort <> "" then // trigger get all by parent search
                            startSearch(None)
                            allByParentSearch(parent.Value, setSearchTreeState, setLoading, stopSearch, debounceStorage, 0)
                        elif s.Trim() <> "" then
                            startSearch (Some s)
                            mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage, 0)
                        else 
                            ()
                    )
                    prop.onChange(fun (s: string) ->
                        if s.Trim() = "" then
                            startSearch(None)
                            stopSearch()
                        else
                            startSearch (Some s)
                            mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage, 1000)
                    )
                    prop.onKeyDown(key.escape, fun _ -> stopSearch())
                ]
                TermSearch.TermSelectArea (SelectAreaID, searchNameState, searchTreeState, selectTerm, isSearching, dropdownWidth, alignRight)
                Components.searchIcon
                if state.IsSome && state.Value.Name.IsSome && state.Value.TermAccessionNumber.IsSome && not isSearching then Components.verifiedIcon
                // Optional elements
                Html.div [
                    prop.classes ["is-flex"]
                    prop.children [
                        if parent.IsSome && displayParent then 
                            Bulma.help [
                                Html.span "Parent: "
                                Html.span $"{parent.Value.NameText}, {parent.Value.TermAccessionShort}"
                            ]
                        if showAdvancedSearch then
                            Components.AdvancedSearch.Main(advancedSearchActive, setAdvancedSearchActive, (fun t -> 
                                setAdvancedSearchActive false
                                Some t |> selectTerm),
                                dispatch
                            )
                            Html.a [
                                prop.onClick(fun e -> e.preventDefault(); e.stopPropagation(); setAdvancedSearchActive true)
                                prop.style [style.custom("marginLeft","auto")]
                                prop.text "Use advanced search"
                            ] 
                        ]
                ]   
            ]
        ]
