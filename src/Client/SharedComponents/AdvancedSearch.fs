module Components.AdvancedSearch

open Fable.React
open Fable.React.Props
open Elmish

open Shared
open TermTypes
open ExcelColors
open Model
open Messages

open Feliz
open Feliz.Bulma

open AdvancedSearchTypes
open AdvancedSearch

open Messages

let private StartAdvancedSearch (state: AdvancedSearch.Model) setState dispatch =
    let setter (terms: Term []) = 
        setState 
            { state with 
                AdvancedSearchTermResults = terms
                Subpage = AdvancedSearch.AdvancedSearchSubpages.ResultsSubpage
            }
    AdvancedSearch.Msg.GetSearchResults {|config=state.AdvancedSearchOptions; responseSetter = setter|}  |> AdvancedSearchMsg |> dispatch

let private createLinkOfAccession (accession:string) =
    let link = accession |> URLs.termAccessionUrlOfAccessionStr
    Html.a [
        prop.href link
        prop.target.blank
        prop.text accession
    ]

let private isValidAdancedSearchOptions (opt:AdvancedSearchOptions) =
    ((
        opt.TermName.Length
        + opt.TermDefinition.Length
    ) > 0)

let private ontologyDropdownItem (model:Model) (dispatch:Msg -> unit) (ontOpt: Ontology option)  =
    let str =
        if ontOpt.IsSome then
            ontOpt.Value.Name |> str
        else
            "All Ontologies" |> str
    option [
        TabIndex 0
        Value str
    ] [
        str
    ]

open Fable.Core.JsInterop

module private ResultsTable =
    open Feliz

    open Shared.TermTypes

    type TableModel = {
        Data            : Term []
        ActiveDropdowns : string list
        ElementsPerPage : int
        PageIndex       : int
        ResultHandler   : Term -> unit
    }

    let createPaginationLinkFromIndex (updatePageIndex: int ->unit) (pageIndex:int) (currentPageinationIndex: int)=
        let isActve = pageIndex = currentPageinationIndex
        Bulma.paginationLink.a [
            if isActve then Bulma.paginationLink.isCurrent
            prop.style [
                if isActve then style.color "white";
                style.backgroundColor NFDIColors.Mint.Base
                style.borderColor NFDIColors.Mint.Base
            ]
            prop.onClick(fun _ -> pageIndex |> updatePageIndex)
            prop.text (string (pageIndex+1))
        ]

    let pageinateDynamic (updatePageIndex:int->unit) (currentPageinationIndex: int) (pageCount:int)  = 
        (*[0 .. pageCount-1].*)
        [(max 1 (currentPageinationIndex-2)) .. (min (currentPageinationIndex+2) (pageCount-1)) ]
        |> List.map (
            fun index -> createPaginationLinkFromIndex updatePageIndex index currentPageinationIndex
        ) 

    let private createAdvancedTermSearchResultRows (state:TableModel) (setState:TableModel -> unit) (resetAdvancedSearchState: unit -> unit) =
        if state.Data |> Array.isEmpty |> not then
            state.Data
            |> Array.collect (fun sugg ->
                let id = sprintf "isHidden_advanced_%s" sugg.Accession 
                [|
                    Html.tr [
                        prop.onClick (fun e ->
                            // dont close modal on click
                            e.stopPropagation()
                            e.preventDefault()
                            // select wanted term
                            state.ResultHandler sugg
                            // reset advanced term search state
                            resetAdvancedSearchState()
                        )
                        prop.tabIndex 0
                        prop.className "suggestion"
                        prop.children [
                            Html.td [Html.b sugg.Name ]
                            if sugg.IsObsolete then
                                Html.td [prop.style [style.color "red"]; prop.text "obsolete"]
                            else
                                Html.td []
                            Html.td [
                                prop.onClick (
                                    fun e ->
                                        e.stopPropagation()
                                )
                                prop.style [style.fontWeight.lighter]
                                prop.children [
                                    Html.small [
                                        createLinkOfAccession sugg.Accession
                                    ]
                                ]
                            ]
                            Html.td [
                                Bulma.buttons [
                                    Bulma.buttons.isRight
                                    prop.children [
                                        //Button.a [
                                        //    Button.Props [Title "Show Term Tree"]
                                        //    Button.Size IsSmall
                                        //    Button.Color IsSuccess
                                        //    Button.IsInverted
                                        //    Button.OnClick(fun e ->
                                        //        e.preventDefault()
                                        //        e.stopPropagation()
                                        //        Cytoscape.Msg.GetTermTree sugg.Accession |> CytoscapeMsg |> dispatch
                                        //    )
                                        //] [
                                        //    Icon.icon [] [
                                        //        Fa.i [Fa.Solid.Tree] []
                                        //    ]
                                        //]
                                        Bulma.button.a [
                                            Bulma.button.isSmall
                                            Bulma.color.isBlack
                                            Bulma.button.isInverted
                                            prop.onClick(fun e ->
                                                e.preventDefault()
                                                e.stopPropagation()
                                                if List.contains id state.ActiveDropdowns then
                                                    let nextState = { state with ActiveDropdowns = List.except [id] state.ActiveDropdowns}
                                                    setState nextState
                                                else
                                                    let nextState = { state with ActiveDropdowns = id::state.ActiveDropdowns}
                                                    setState nextState
                                            )
                                            prop.children [
                                                Bulma.icon [
                                                    Html.i [prop.className "fa-solid fa-chevron-down"]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.tr [
                        prop.onClick(fun e -> e.stopPropagation())
                        prop.id id
                        prop.className "suggestion-details"
                        prop.style [if List.contains id state.ActiveDropdowns then style.visibility.visible else style.visibility.collapse]
                        prop.children [
                            Html.td [
                                prop.colSpan 4
                                prop.children [
                                    Bulma.content [
                                        Html.b "Definition: "
                                        Html.text sugg.Description
                                    ]
                                ]
                            ]
                        ]
                    ]
                |]
            )
        else
            [|
                Html.tr [
                    Html.td "No terms found matching your input."
                ]
            |]


    open Feliz


    [<ReactComponent>]
    let paginatedTableComponent (state:AdvancedSearch.Model) setState (data: TableModel) =
        
        let resetAdvancedSearchState = fun _ -> AdvancedSearch.Model.init() |> setState
        let handlerState, setState = React.useState data
        let updatePageIndex (model:TableModel) (ind:int) =
            let nextState = {model with PageIndex = ind}
            setState nextState

        if data.Data.Length > 0 then 

            let tableRows = createAdvancedTermSearchResultRows handlerState setState resetAdvancedSearchState
            let currentPageinationIndex = handlerState.PageIndex
            let chunked = tableRows |> Array.chunkBySize data.ElementsPerPage
            let len = chunked.Length 
    
            Bulma.container [
                Bulma.table [
                    Bulma.table.isFullWidth
                    prop.children [
                        Html.thead []
                        Html.tbody (
                            chunked.[currentPageinationIndex] |> List.ofArray
                        )
                    ]
                ]
                Bulma.pagination [
                    Bulma.pagination.isCentered
                    prop.children [
                        Bulma.paginationPrevious.button [
                            prop.style [style.cursor.pointer]
                            prop.onClick (fun _ ->
                                max (currentPageinationIndex - 1) 0 |> updatePageIndex handlerState 
                            )
                            prop.disabled <| (currentPageinationIndex = 0)
                            prop.text "Prev"
                        ]
                        Bulma.paginationList [
                            yield createPaginationLinkFromIndex (updatePageIndex handlerState) 0 currentPageinationIndex
                            if len > 5 && currentPageinationIndex > 3 then yield Bulma.paginationEllipsis []
                            yield! pageinateDynamic (updatePageIndex handlerState) currentPageinationIndex (len - 1)
                            if len > 5 && currentPageinationIndex < len-4 then yield Bulma.paginationEllipsis []
                            if len > 1 then yield createPaginationLinkFromIndex (updatePageIndex handlerState) (len-1) currentPageinationIndex
                        ]
                        Bulma.paginationNext.button [
                            prop.style [style.cursor.pointer]
                            prop.onClick (fun _ ->
                                let next = min (currentPageinationIndex + 1) (len - 1)
                                next |> updatePageIndex handlerState
                            )
                            prop.disabled <| (currentPageinationIndex = len - 1)
                            prop.text "Next"
                        ]
                    ]
                ]
            ]
        else
            Html.div []

let private keepObsoleteCheckradioElement (state:AdvancedSearch.Model) setState =
    let currentKeepObsolete = state.AdvancedSearchOptions.KeepObsolete
    Bulma.field.div [
        Bulma.control.div [
            Html.label [
                prop.className "checkbox"
                prop.children [
                    Html.input [
                        prop.type'.checkbox
                        prop.isChecked (state.AdvancedSearchOptions.KeepObsolete)
                        prop.onChange (fun (e:bool) ->
                            {state with AdvancedSearch.Model.AdvancedSearchOptions.KeepObsolete = e}
                            |> setState
                        )
                    ]
                    Html.span [
                        prop.className "is-unselectable"
                        prop.text (if currentKeepObsolete then " yes" else " no")
                    ]
                ]
            ]
        ]
    ]

let private inputFormPage (state:AdvancedSearch.Model) (setState:AdvancedSearch.Model -> unit) dispatch =
    Html.div [
        Bulma.field.div [
            Bulma.label  "Term name keywords:"
            Bulma.field.div [
                Bulma.control.div [
                    Bulma.input.text [
                        prop.placeholder "... search term name"
                        Bulma.input.isSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        prop.onChange (fun (e:string) ->
                            {state with AdvancedSearch.Model.AdvancedSearchOptions.TermName = e }|> setState
                        )
                        prop.valueOrDefault state.AdvancedSearchOptions.TermName
                        prop.onKeyDown (fun e ->
                            match e.which with
                            | 13. ->
                                e.preventDefault()
                                e.stopPropagation();
                                let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                                if isValid then
                                    setState 
                                        { state with
                                            Subpage                         = AdvancedSearchSubpages.ResultsSubpage
                                            HasAdvancedSearchResultsLoading = true
                                        }
                                    StartAdvancedSearch state setState dispatch
                            | _ -> ()
                        )
                    ]
                ] 
            ]
        ]
        Bulma.field.div [
            Bulma.label "Term definition keywords:"
            Bulma.field.div [
                Bulma.control.div [
                    Bulma.input.text [
                        prop.placeholder "... search term definition"
                        Bulma.input.isSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        prop.onChange (fun (e: string) -> {state with AdvancedSearch.Model.AdvancedSearchOptions.TermDefinition = e} |> setState)
                        prop.onKeyDown (fun e ->
                            match e.which with
                            | 13. ->
                                e.preventDefault()
                                e.stopPropagation();
                                let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                                if isValid then
                                    StartAdvancedSearch state setState dispatch
                            | _ -> ()
                        )
                        prop.valueOrDefault state.AdvancedSearchOptions.TermDefinition
                    ]
                ] 
            ]
        ] 
        //Bulma.field.div [
        //    Bulma.label "Ontology"
        //    Bulma.control.div [
        //        Bulma.select [
        //            Html.select [
        //                prop.placeholder "All Ontologies";
        //                if state.AdvancedSearchOptions.OntologyName.IsSome then prop.value state.AdvancedSearchOptions.OntologyName.Value
        //                prop.onChange (fun (e:string) ->
        //                    { state with AdvancedSearch.Model.AdvancedSearchOptions.OntologyName = if e = "All Ontologies" then None else Some e}
        //                    |> setState
        //                )
        //                prop.children [
        //                    ontologyDropdownItem model dispatch None
        //                    yield! (
        //                        model.PersistentStorageState.SearchableOntologies
        //                        |> Array.map snd
        //                        |> Array.toList
        //                        |> List.sortBy (fun o -> o.Name)
        //                        |> List.map (fun ont -> ontologyDropdownItem model dispatch (Some ont))
        //                    )
        //                ]
        //            ]
        //        ]
        //    ]
        //]
        Bulma.field.div [
            Bulma.label "Keep obsolete terms"
            keepObsoleteCheckradioElement state setState
        ]
    ]

let private resultsPage (resultHandler: Term -> unit) (state: AdvancedSearch.Model) setState =
    Bulma.field.div [
        Bulma.label "Results:"
        if state.Subpage = AdvancedSearchSubpages.ResultsSubpage then
            if state.HasAdvancedSearchResultsLoading then
                Html.div [
                    prop.style [style.width(length.perc 100); style.display.flex; style.justifyContent.center]
                    prop.children Modals.Loading.loadingComponent
                ]
            else
                let init: ResultsTable.TableModel = {
                    Data            = state.AdvancedSearchTermResults
                    ActiveDropdowns = []
                    ElementsPerPage = 10
                    PageIndex       = 0
                    ResultHandler   = resultHandler
                }
                ResultsTable.paginatedTableComponent
                    state
                    setState
                    init
    ]

[<ReactComponent>]
let Main (isActive: bool, setIsActive: bool -> unit, resultHandler: Term -> unit, dispatch) =
    let state, setState = React.useState(AdvancedSearch.Model.init)
    React.useEffect(
        (fun _ -> AdvancedSearch.Model.init() |> setState), 
        [|box isActive|]
    )
    Bulma.modal [
        //if (model.AdvancedSearchState.HasModalVisible
        //    && model.AdvancedSearchState.ModalId = modalId) then
        if isActive then Bulma.modal.isActive
        //prop.id modalId
        prop.children [
            // Close modal on click on background
            Bulma.modalBackground [ prop.onClick (fun e -> setIsActive false)]
            Bulma.modalCard [
                prop.style [style.width(length.perc 90); style.maxWidth(length.px 600); style.height(length.perc 80); style.maxHeight(length.px 600)]
                prop.children [
                    Bulma.modalCardHead [
                        prop.children [
                            Bulma.modalCardTitle "Advanced Search"
                            Bulma.delete [prop.onClick(fun _ -> setIsActive false)]
                        ]
                    ]
                    Bulma.modalCardBody [
                        prop.children [
                            Bulma.field.div [ Bulma.help [
                                prop.style [style.textAlign.justify]
                                prop.text "Swate advanced search uses the Apache Lucene query parser syntax. Feel free to read the related Swate documentation [wip] for guidance on how to use it."
                            ]]
                            match state.Subpage with
                            | AdvancedSearchSubpages.InputFormSubpage ->
                                // we need to propagate the modal id here, so we can use meaningful and UNIQUE ids to the checkradio id's
                                inputFormPage state setState dispatch
                            | AdvancedSearchSubpages.ResultsSubpage ->
                                resultsPage resultHandler state setState
                        ]
                    ]
                    Bulma.modalCardFoot [
                        Html.form [
                            prop.onSubmit (fun e -> e.preventDefault())
                            prop.onKeyDown(key.enter, fun k -> k.preventDefault())
                            prop.style [style.width(length.perc 100)]
                            prop.children [
                                if state.Subpage <> AdvancedSearchSubpages.InputFormSubpage then
                                    Bulma.levelItem [
                                        Bulma.button.button [   
                                            Bulma.color.isDanger
                                            Bulma.button.isFullWidth
                                            prop.onClick (fun e -> e.stopPropagation(); e.preventDefault(); setState {state with Subpage = InputFormSubpage })
                                            prop.text "Back"
                                        ]
                                    ]
                                // Show "Start advanced search" button ONLY on first subpage
                                if state.Subpage = AdvancedSearchSubpages.InputFormSubpage then
                                    Bulma.levelItem [
                                        Bulma.button.button [
                                            let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                                            if isValid then
                                                Bulma.color.isSuccess
                                                Bulma.button.isActive
                                            else
                                                Bulma.color.isDanger
                                                prop.disabled true
                                            Bulma.button.isFullWidth
                                            prop.onClick (fun e ->
                                                e.preventDefault()
                                                e.stopPropagation();
                                                StartAdvancedSearch state setState dispatch
                                            )
                                            prop.text "Start advanced search"
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]