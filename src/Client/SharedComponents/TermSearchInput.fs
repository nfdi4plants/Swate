namespace Components

open Feliz
open Feliz.Bulma
open Browser.Types
open ARCtrl
open Shared
open Shared.Database
open DTO
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
        Results: Term []
    } with
        static member init() = {
            SearchIs = SearchIs.Idle
            Results = [||]
        }

    let searchByName(query: string, setResults: Term [] -> unit) =
        async {
            let query = TermQuery.create(query, 10)
            let! terms = Api.ontology.searchTerm query
            setResults terms
        }

    let searchByParent(query: string, parentTAN: string, setResults: Term [] -> unit) =
        async {
            let query = TermQuery.create(query, 50, parentTAN)
            let! terms = Api.ontology.searchTerm query
            setResults terms
        }

    let searchAllByParent(parentTAN: string, setResults: Term [] -> unit) =
        async {
            let! terms = Api.api.getAllTermsByParentTerm <| Shared.SwateObsolete.TermMinimal.create "" parentTAN
            setResults terms
        }

    let allByParentSearch (
        parent: OntologyAnnotation,
        setSearchTreeState: SearchState -> unit,
        setLoading: bool -> unit,
        stopSearch: unit -> unit,
        debounceStorage: DebounceStorage,
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
        debounceStorage: DebounceStorage,
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

    let dsetter (inp: OntologyAnnotation option, setter, debounceStorage: DebounceStorage, setLoading: bool -> unit, debounceSetter: int option) =
        if debounceSetter.IsSome then
            debouncel debounceStorage "SetterDebounce" debounceSetter.Value setLoading setter inp
        else
            setter inp

    module Components =

        let termSeachNoResults (advancedTermSearchActiveSetter: (bool -> unit) option) = [
            Html.div [
                prop.key $"TermSelectItem_NoResults"
                prop.classes ["term-select-item"]
                prop.children [
                    Html.div "No terms found matching your input."
                ]
            ]
            if advancedTermSearchActiveSetter.IsSome then
                Html.div [
                    prop.key $"TermSelectItem_Suggestion"
                    prop.classes ["term-select-item"]
                    prop.children [
                        Html.span "Can't find the term you are looking for? "
                        Html.a [
                            prop.onClick(fun e -> e.preventDefault(); e.stopPropagation(); advancedTermSearchActiveSetter.Value true)
                            prop.text "Try Advanced Search!"
                        ]
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

        let termSelectItemMain (term: Term, show, setShow, setTerm, isDirectedSearchResult: bool) =
            Html.div [
                prop.classes ["is-flex"; "is-flex-direction-row"; "term-select-item-main"]
                prop.onClick setTerm
                prop.style [style.position.relative]
                prop.children [
                    Html.i [
                        prop.style [style.width (length.px 20)]
                        if term.IsObsolete then
                            prop.classes ["fa-solid fa-link-slash"; "is-flex"; "is-align-items-center"; "has-text-danger"]
                            prop.title "Obsolete"
                        elif isDirectedSearchResult then
                            prop.classes ["fa-solid fa-diagram-project"; "is-flex"; "is-align-items-center"]
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
                                prop.href (ARCtrl.OntologyAnnotation(tan=term.Accession).TermAccessionOntobeeUrl)
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

        let termSelectItemMore (term: Term, show) =
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
                    prop.style [style.marginRight 0]
                    prop.children [
                        Bulma.button.a [
                            prop.className "h-full"
                            prop.style [style.borderWidth 0; style.borderRadius 0]
                            if not searchable then Bulma.color.hasTextGreyLight
                            Bulma.button.isInverted
                            prop.onClick(fun _ -> searchableSetter (not searchable))
                            prop.children [Bulma.icon [Html.i [prop.className "fa-solid fa-magnifying-glass"]]]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TermSelectItem (term: Term, setTerm, ?isDirectedSearchResult: bool) =
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

    static member TermSelectArea (id: string, searchNameState: SearchState, searchTreeState: SearchState, setTerm: Term option -> unit, show: bool, setAdvancedTermSearchActive) =
        let searchesAreComplete = searchNameState.SearchIs = SearchIs.Done && searchTreeState.SearchIs = SearchIs.Done
        let foundInBoth (term:Term) =
            (searchTreeState.Results |> Array.contains term)
            && (searchNameState.Results |> Array.contains term)
        let matchSearchState (ss: SearchState) (isDirectedSearch: bool) =
            match ss with
            | {SearchIs = SearchIs.Done; Results = [||]} when not isDirectedSearch ->
                Components.termSeachNoResults setAdvancedTermSearchActive
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
                    Html.div [
                        prop.className "px-3 py-2"
                        prop.text "loading.."
                    ]
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
        ?input: OntologyAnnotation, ?parent: OntologyAnnotation,
        ?searchableToggle: bool,
        ?advancedSearchDispatch: Messages.Msg -> unit,
        ?portalTermSelectArea: HTMLElement,
        ?onBlur: Event -> unit, ?onEscape: KeyboardEvent -> unit, ?onEnter: KeyboardEvent -> unit,
        ?autofocus: bool, ?fullwidth: bool, ?size: IReactProperty, ?isExpanded: bool, ?displayParent: bool, ?borderRadius: int, ?border: string, ?minWidth: Styles.ICssUnit)
        =
        let searchableToggle = defaultArg searchableToggle false
        let autofocus = defaultArg autofocus false
        let displayParent = defaultArg displayParent true
        let isExpanded = defaultArg isExpanded false
        let advancedSearchActive, setAdvancedSearchActive = React.useState(false)
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let searchable, setSearchable = React.useState(true)
        let searchNameState, setSearchNameState = React.useState(SearchState.init)
        let searchTreeState, setSearchTreeState = React.useState(SearchState.init)
        let isSearching, setIsSearching = React.useState(false)
        let debounceStorage = React.useRef(newDebounceStorage())
        let ref = React.useElementRef()
        let inputRef = React.useInputRef()
        React.useEffect(
            (fun () ->
                if inputRef.current.IsSome && input.IsSome
                    then inputRef.current.Value.value <- input.Value.NameText
            ),
            [|box input|]
        )
        React.useLayoutEffectOnce(fun _ ->
            ClickOutsideHandler.AddListener (ref, fun e ->
                debounceStorage.current.ClearAndRun()
                if onBlur.IsSome then onBlur.Value e
            )
        )
        let stopSearch() =
            debounceStorage.current.Remove("TermSearch") |> ignore
            setLoading false
            setIsSearching false
            setSearchTreeState {searchTreeState with SearchIs = SearchIs.Idle}
            setSearchNameState {searchNameState with SearchIs = SearchIs.Idle}
        let selectTerm (t:Term option) =
            let oaOpt = t |> Option.map OntologyAnnotation.fromTerm
            setter oaOpt
            if inputRef.current.IsSome then
                inputRef.current.Value.value <- oaOpt |> Option.map (fun oa -> oa.Name) |> Option.flatten |> Option.defaultValue ""
            setIsSearching false
        let startSearch() =
            setLoading true
            setSearchNameState <| SearchState.init()
            setSearchTreeState <| SearchState.init()
            setIsSearching true
        //let registerChange(queryString: string option) =
        //    let oaOpt = queryString |> Option.map (fun s ->
        //        let oa = input |> Option.defaultValue (OntologyAnnotation.empty())
        //        oa.Name <- Some s
        //        oa
        //    )
        //    dsetter(oaOpt,setter,debounceStorage.current,setLoading,debounceSetter)
        Bulma.control.div [
            if isExpanded then Bulma.control.isExpanded
            if size.IsSome then size.Value
            if not searchableToggle then Bulma.control.hasIconsLeft
            Bulma.control.hasIconsRight
            if not searchableToggle then prop.ref ref
            prop.style [
                if fullwidth then style.flexGrow 1;
                if minWidth.IsSome then style.minWidth minWidth.Value
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
                    if input.IsSome then prop.valueOrDefault input.Value.NameText
                    prop.ref inputRef
                    prop.onMouseDown(fun e -> e.stopPropagation())
                    prop.onDoubleClick(fun e ->
                        let s : string = e.target?value
                        if s.Trim() = "" && parent.IsSome && parent.Value.TermAccessionShort <> "" then // trigger get all by parent search
                            log "Double click empty + parent"
                            if searchable then
                                startSearch()
                                allByParentSearch(parent.Value, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 0)
                        elif s.Trim() <> "" then
                            log "Double click not empty"
                            if searchable then
                                startSearch ()
                                mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 0)
                        else
                            ()
                    )
                    prop.onChange(fun (s: string) ->
                        if System.String.IsNullOrWhiteSpace s then
                            //registerChange(None)
                            stopSearch() // When deleting text this should stop search from completing
                        else
                            //registerChange(Some s)
                            if searchable then
                                startSearch()
                                mainSearch(s, parent, setSearchNameState, setSearchTreeState, setLoading, stopSearch, debounceStorage.current, 1000)
                    )
                    prop.onKeyDown(fun e ->
                        e.stopPropagation()
                        match e.which with
                        | 27. -> //escape
                            stopSearch()
                            debounceStorage.current.ClearAndRun()
                            if onEscape.IsSome then onEscape.Value e
                        | 13. -> //enter
                            debounceStorage.current.ClearAndRun()
                            if onEnter.IsSome then onEnter.Value e
                        | 9. -> //tab
                            if searchableToggle then
                                e.preventDefault()
                                setSearchable (not searchable)
                        | _ -> ()
                    )
                ]
                let TermSelectArea = TermSearch.TermSelectArea (SelectAreaID, searchNameState, searchTreeState, selectTerm, isSearching, (if advancedSearchDispatch.IsSome then Some setAdvancedSearchActive else None))
                if portalTermSelectArea.IsSome then
                    ReactDOM.createPortal(TermSelectArea, portalTermSelectArea.Value)
                elif ref.current.IsSome then
                    ReactDOM.createPortal(TermSelectArea, ref.current.Value)
                else
                    TermSelectArea
                if not searchableToggle then Components.searchIcon
                if input.IsSome && input.Value.Name.IsSome && input.Value.TermAccessionNumber.IsSome && not isSearching then Components.verifiedIcon
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

