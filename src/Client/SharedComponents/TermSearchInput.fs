namespace Components

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
            let! terms = Api.ontology.searchTerms {|limit = 10; ontologies = []; query=query|}
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
                prop.onMouseDown setTerm
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
                        prop.onMouseDown(fun e -> 
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

    static member ToggleSearchContainer (element: ReactElement, ref: IRefValue<HTMLElement option>, searchable: bool, searchableSetter: bool -> unit) = 
        Bulma.field.div [
            prop.style [style.flexGrow 1; style.position.relative]
            prop.ref ref
            Bulma.field.hasAddons
            prop.children [
                element
                Bulma.control.p [
                    Bulma.button.a [
                        prop.style [style.borderWidth 0; style.borderRadius 0]
                        if not searchable then Bulma.color.hasTextGreyLight
                        Bulma.button.isInverted
                        prop.onClick(fun _ -> searchableSetter (not searchable))
                        prop.children [Bulma.icon [Html.i [prop.className "fa-solid fa-magnifying-glass"]]]
                    ]
                ]
            ]
        ]

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

    [<ReactComponent>]
    static member TermSelectArea (id: string, searchNameState: SearchState, searchTreeState: SearchState, setTerm: TermTypes.Term option -> unit, show: bool) =
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
            prop.style [style.width (length.perc 100); style.top (length.perc 100)]
            prop.children [
                yield! matchSearchState searchNameState false
                yield! matchSearchState searchTreeState true
            ]
        ]

    [<ReactComponent>]
    static member Input (
        setter: OntologyAnnotation option -> unit,
        ?input: OntologyAnnotation, ?parent': OntologyAnnotation, 
        ?debounceSetter: int, ?searchableToggle: bool, 
        ?advancedSearchDispatch: Messages.Msg -> unit,
        ?portalTermSelectArea: HTMLElement,
        ?onBlur: Event -> unit, ?onEscape: KeyboardEvent -> unit, ?onEnter: KeyboardEvent -> unit,
        ?autofocus: bool, ?fullwidth: bool, ?size: IReactProperty, ?isExpanded: bool, ?displayParent: bool, ?borderRadius: int, ?border: string) 
        =
        let searchableToggle = defaultArg searchableToggle false
        let autofocus = defaultArg autofocus false
        let displayParent = defaultArg displayParent true
        let isExpanded = defaultArg isExpanded false
        let advancedSearchActive, setAdvancedSearchActive = React.useState(false)
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let parent, setParent = React.useState(parent')
        let searchable, setSearchable = React.useState(not searchableToggle)
        let searchNameState, setSearchNameState = React.useState(SearchState.init)
        let searchTreeState, setSearchTreeState = React.useState(SearchState.init)
        let isSearching, setIsSearching = React.useState(false)
        let debounceStorage = React.useRef(newDebounceStorage())
        let dsetter = fun inp -> if debounceSetter.IsSome then debounce debounceStorage.current "setter_debounce" debounceSetter.Value setter inp else setter inp
        let ref = React.useElementRef()
        if onBlur.IsSome then React.useLayoutEffectOnce(fun _ -> ClickOutsideHandler.AddListener (ref, onBlur.Value))
        React.useEffect((fun () -> setState input), dependencies=[|box input|])
        React.useEffect((fun () -> setParent parent'), dependencies=[|box parent'|]) // careful, check console. might result in maximum dependency depth error.
        let stopSearch() = 
            debounceStorage.current.Remove("TermSearch") |> ignore
            setLoading false
            setIsSearching false
            setSearchTreeState {searchTreeState with SearchIs = SearchIs.Idle}
            setSearchNameState {searchNameState with SearchIs = SearchIs.Idle}
        let selectTerm (t:TermTypes.Term option) =
            let oaOpt = t |> Option.map OntologyAnnotation.fromTerm 
            setState oaOpt
            setter oaOpt
            setIsSearching false
        let startSearch(queryString: string option, isOnChange: bool) =
            let oaOpt = queryString |> Option.map (fun s -> OntologyAnnotation.fromString(s) )
            if isOnChange then
                dsetter oaOpt
                setState oaOpt
            setSearchNameState <| SearchState.init()
            setSearchTreeState <| SearchState.init()
            setIsSearching true
        Bulma.control.div [
            if isExpanded then Bulma.control.isExpanded
            if size.IsSome then size.Value
            if not searchableToggle then Bulma.control.hasIconsLeft
            Bulma.control.hasIconsRight
            if not searchableToggle then prop.ref ref
            prop.style [
                if fullwidth then style.flexGrow 1; 
                style.minWidth 400
            ]
            if loading then Bulma.control.isLoading
            prop.children [
                Bulma.input.text [
                    prop.autoFocus autofocus
                    prop.style [
                        if borderRadius.IsSome then style.borderRadius borderRadius.Value
                        if border.IsSome then style.custom("border", border.Value)
                    ]
                    if size.IsSome then size.Value
                    if state.IsSome then prop.valueOrDefault state.Value.NameText
                    prop.onMouseDown(fun e -> e.stopPropagation())
                    prop.onDoubleClick(fun e ->
                        let s : string = e.target?value
                        if s.Trim() = "" && parent.IsSome && parent.Value.TermAccessionShort <> "" then // trigger get all by parent search
                            startSearch(None, false)
                            if searchable then allByParentSearch(parent.Value, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 0) else stopSearch()
                        elif s.Trim() <> "" then
                            startSearch (Some s, false)
                            if searchable then mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 0) else stopSearch()
                        else 
                            ()
                    )
                    prop.onChange(fun (s: string) ->
                        if s.Trim() = "" then
                            startSearch(None, true)
                            stopSearch()
                        else
                            startSearch (Some s, true)
                            if searchable then mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 1000) else stopSearch()
                    )
                    prop.onKeyDown(fun e -> 
                        match e.which with
                        | 27. -> //escape
                            if onEscape.IsSome then onEscape.Value e
                            stopSearch()
                        | 13. -> //enter
                            if onEnter.IsSome then onEnter.Value e
                            setter state
                        | 9. -> //tab
                            if searchableToggle then 
                                e.preventDefault()
                                setSearchable (not searchable)
                        | _ -> ()
                            
                    )
                ]
                let TermSelectArea = 
                    TermSearch.TermSelectArea (SelectAreaID, searchNameState, searchTreeState, selectTerm, isSearching)
                if portalTermSelectArea.IsSome then
                    ReactDOM.createPortal(TermSelectArea,portalTermSelectArea.Value)
                elif ref.current.IsSome then
                    ReactDOM.createPortal(TermSelectArea,ref.current.Value)
                else
                    TermSelectArea
                if not searchableToggle then Components.searchIcon
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
                        if advancedSearchDispatch.IsSome then
                            Components.AdvancedSearch.Main(advancedSearchActive, setAdvancedSearchActive, (fun t -> 
                                setAdvancedSearchActive false
                                Some t |> selectTerm),
                                advancedSearchDispatch.Value
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
        |> fun main -> 
            if searchableToggle then
                TermSearch.ToggleSearchContainer(main, ref, searchable, setSearchable)
            else 
                main
