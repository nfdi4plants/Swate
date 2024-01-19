namespace Components

open Feliz
open Feliz.Bulma
open Browser.Types
open ARCtrl.ISA
open Shared

module TermSearchAux =

    type SearchState = {
        Done: bool
        Results: TermTypes.Term []
    } with
        static member init() = {
            Done = false
            Results = [||]
        }

    let searchByName(name: string, setResults: TermTypes.Term [] -> unit) =
        async {
            let! searchNameTerms = Api.ontology.searchTerms {|limit = 5; ontologies = []; query=name|}
            setResults searchNameTerms
        }

    let startSearch (
        inputString: string, 
        state: OntologyAnnotation, 
        setState: OntologyAnnotation -> unit,
        setIsSearching: bool -> unit, 
        debounceStorage: System.Collections.Generic.Dictionary<string,int>,
        setSearchNameState: SearchState -> unit,
        setLoading: bool -> unit,
        debounceTimer: int
    ) =
        let oa = OntologyAnnotation.fromString(inputString)
        setState oa
        if inputString = "" then 
            setIsSearching false
            debounceStorage.Clear()
        else
            let queryString = inputString.Trim()
            let startSearch() =
                [
                    searchByName(queryString, fun terms -> setSearchNameState {Results = terms; Done = true})
                ]
                |> Async.Parallel
                |> Async.Ignore
                |> Async.StartImmediate
            setIsSearching true
            debouncel debounceStorage "TermSearch" debounceTimer setLoading startSearch ()

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

        let termSelectItemMain (term: TermTypes.Term, show, setShow, setTerm) = 
            Html.div [
                prop.classes ["is-flex"; "is-flex-direction-row"; "term-select-item-main"]
                prop.onClick setTerm
                prop.children [
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
    static member TermSelectItem (term: TermTypes.Term, setTerm) =
        let show, setShow = React.useState(false)
        Html.div [
            prop.key $"TermSelectItem_{term.Accession}"
            prop.classes ["term-select-item"]
            prop.children [
                Components.termSelectItemMain(term, show, setShow, setTerm)
                Components.termSelectItemMore(term, show)
            ]
        ]

    static member TermSelectArea (searchNameState: SearchState, setTerm: TermTypes.Term option -> unit, ?show: bool) =
        let show = defaultArg show true
        Html.div [
            prop.classes [
                if not show then "is-hidden";
                "term-select-area"; 
            ]
            prop.children [
                match searchNameState with
                | {Done = true; Results = [||]} ->  
                    yield! Components.termSeachNoResults
                | _ ->
                    for term in searchNameState.Results do
                        let setTerm = fun (e: MouseEvent) -> setTerm (Some term)
                        TermSearch.TermSelectItem (term, setTerm)
            ]
        ]

    [<ReactComponent>]
    static member Input (setter: OntologyAnnotation -> unit, ?input: OntologyAnnotation, ?label: string, ?fullwidth: bool, ?size: IReactProperty) =
        let fullwidth = defaultArg fullwidth false
        let input : OntologyAnnotation = defaultArg input OntologyAnnotation.empty
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let searchNameState, setSearchNameState = React.useState(SearchState.init)
        let isSearching, setIsSearching = React.useState(false)
        let selectTerm (t:TermTypes.Term option) =
            let oa = t |> Option.map OntologyAnnotation.fromTerm |> Option.defaultValue OntologyAnnotation.empty
            setState oa
            setIsSearching false
        let debounceStorage, setdebounceStorage = React.useState(newDebounceStorage)
        //React.useEffect((fun () -> setState input), dependencies=[|box input|]) // careful, check console. might result in maximum dependency depth error.
        Bulma.field.div [
            prop.style [if fullwidth then style.flexGrow 1]
            prop.children [
                if label.IsSome then Bulma.label label.Value
                Bulma.field.div [
                    Bulma.field.hasAddons
                    prop.children [
                        Bulma.control.div [
                            if size.IsSome then size.Value
                            Bulma.control.hasIconsLeft
                            Bulma.control.hasIconsRight
                            prop.style [if fullwidth then style.flexGrow 1; style.position.relative]
                            if loading then Bulma.control.isLoading
                            prop.children [
                                Bulma.input.text [
                                    if size.IsSome then size.Value
                                    prop.valueOrDefault state.NameText
                                    prop.onDoubleClick(fun e ->
                                        let s : string = e.target?value
                                        startSearch(s, state, setState, setIsSearching,debounceStorage,setSearchNameState,setLoading,0)
                                    )
                                    prop.onChange(fun (s: string) ->
                                        startSearch(s, state, setState, setIsSearching,debounceStorage,setSearchNameState,setLoading,1000)
                                    )
                                ]
                                TermSearch.TermSelectArea (searchNameState, selectTerm, isSearching)
                                //TermSearch.TermSelectArea([|
                                //    TermTypes.createTerm "MS:000001" "instrument model" "Lorem ipsum dolor sit amet, consetetur sadipscing elitr" false "ms" 
                                //    TermTypes.createTerm "MS:000002" "Instrument Model" "Lorem ipsum dolor sit amet, consetetur sadipscing elitr" false "ms"
                                //    TermTypes.createTerm "MS:000003" "IonSpec instrument model" "" false "ms"
                                //    TermTypes.createTerm "MS:000004" "Hitachi instrument model Hitachi instrument model Hitachi instrument model Hitachi instrument model Hitachi instrument model" "" false "ms"
                                //    TermTypes.createTerm "MS:000005" "LECO instrument model" "" true "ms"
                                //|], setSelectedTerm, show=true)
                                Components.searchIcon
                                if state.Name.IsSome && state.TermAccessionNumber.IsSome && not isSearching then Components.verifiedIcon
                            ]
                        ]
                    ]
                ]
            ]
        ]
