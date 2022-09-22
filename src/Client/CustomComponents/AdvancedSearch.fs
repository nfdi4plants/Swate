module AdvancedSearch

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Elmish

open Shared
open TermTypes
open ExcelColors
open Model
open Messages
open CustomComponents

open AdvancedSearchTypes
open AdvancedSearch

let update (advancedTermSearchMsg: AdvancedSearch.Msg) (currentState:AdvancedSearch.Model) : AdvancedSearch.Model * Cmd<Messages.Msg> =
    match advancedTermSearchMsg with
    | UpdateAdvancedTermSearchSubpage subpage ->
        let nextState = {
            currentState with
                AdvancedTermSearchSubpage = subpage
        }
        nextState, Cmd.none

    | ToggleModal modalId ->
        let nextState = {
            currentState with
                ModalId = modalId
                HasModalVisible = (not currentState.HasModalVisible)
        }

        nextState,Cmd.none

    | ToggleOntologyDropdown ->
        let nextState = {
            currentState with
                HasOntologyDropdownVisible = (not currentState.HasOntologyDropdownVisible)
        }

        nextState,Cmd.none

    | UpdateAdvancedTermSearchOptions opts ->

        let nextState = {
            currentState with
                AdvancedSearchOptions = opts
                HasOntologyDropdownVisible = false
        }

        nextState,Cmd.none

    | StartAdvancedSearch ->

        let nextState = {
            currentState with
                AdvancedTermSearchSubpage       = AdvancedSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = true
        }

        let nextCmd =
            currentState.AdvancedSearchOptions
            |> GetNewAdvancedTermSearchResults
            |> Request
            |> Api
            |> Cmd.ofMsg

        nextState,nextCmd

    | ResetAdvancedSearchState ->
        let nextState = AdvancedSearch.Model.init()

        nextState,Cmd.none

    | NewAdvancedSearchResults results ->
        let nextState = {
            currentState with
                AdvancedSearchTermResults       = results
                AdvancedTermSearchSubpage       = AdvancedSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = false
        }

        nextState,Cmd.none

open Messages

let createLinkOfAccession (accession:string) =
    a [
        let link = accession |> URLs.termAccessionUrlOfAccessionStr
        Href link
        Target "_Blank"
    ] [
        str accession
    ]

let private isValidAdancedSearchOptions (opt:AdvancedSearchOptions) =
    ((
        opt.TermName.Length
        + opt.TermDescription.Length
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

module ResultsTable =
    open Feliz

    open Shared.TermTypes

    type TableModel = {
        Data            : Term []
        ActiveDropdowns : string list
        ElementsPerPage : int
        PageIndex       : int
        Dispatch        : Msg -> unit
        RelatedInputId  : string 
        ResultHandler   : Term -> Msg
    }

    let createPaginationLinkFromIndex (updatePageIndex: int ->unit) (pageIndex:int) (currentPageinationIndex: int)=
        let isActve = pageIndex = currentPageinationIndex
        Pagination.Link.a [
            Pagination.Link.Current isActve
            Pagination.Link.Props [
                Style [
                    if isActve then Color "white"; BackgroundColor NFDIColors.Mint.Base; BorderColor NFDIColors.Mint.Base;
                ]
                OnClick (fun _ -> pageIndex |> updatePageIndex)
            ]
        ] [
            span [] [str (string (pageIndex+1))]
        ]

    let pageinateDynamic (updatePageIndex:int->unit) (currentPageinationIndex: int) (pageCount:int)  = 
        (*[0 .. pageCount-1].*)
        [(max 1 (currentPageinationIndex-2)) .. (min (currentPageinationIndex+2) (pageCount-1)) ]
        |> List.map (
            fun index -> createPaginationLinkFromIndex updatePageIndex index currentPageinationIndex
        ) 

    let private createAdvancedTermSearchResultRows (state:TableModel) (setState:TableModel -> unit) =
        if state.Data |> Array.isEmpty |> not then
            state.Data
            |> Array.collect (fun sugg ->
                let id = sprintf "isHidden_advanced_%s" sugg.Accession 
                [|
                    tr [
                        OnClick (fun e ->
                            // dont close modal on click
                            e.stopPropagation()
                            e.preventDefault()
                            let relInput = Browser.Dom.document.getElementById(state.RelatedInputId)
                            // select wanted term
                            sugg |> state.ResultHandler |> state.Dispatch
                            // propagate wanted term name to related input on main page
                            relInput?value <- sugg.Name
                            // reset advanced term search state
                            ResetAdvancedSearchState |> AdvancedSearchMsg |> state.Dispatch
                        )
                        TabIndex 0
                        Class "suggestion"
                    ] [
                        td [] [
                            b [] [ str sugg.Name ]
                        ]
                        if sugg.IsObsolete then
                            td [Style [Color "red"]] [str "obsolete"]
                        else
                            td [] []
                        td [
                            OnClick (
                                fun e ->
                                    e.stopPropagation()
                            )
                            Style [FontWeight "light"]
                        ] [
                            small [] [
                                createLinkOfAccession sugg.Accession
                        ] ]
                        td [] [
                            Button.list [Button.List.IsRight] [
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
                                Button.a [
                                    Button.Size IsSmall
                                    Button.Color IsBlack
                                    Button.IsInverted
                                    Button.OnClick(fun e ->
                                        e.preventDefault()
                                        e.stopPropagation()
                                        if List.contains id state.ActiveDropdowns then
                                            let nextState = { state with ActiveDropdowns = List.except [id] state.ActiveDropdowns}
                                            setState nextState
                                        else
                                            let nextState = { state with ActiveDropdowns = id::state.ActiveDropdowns}
                                            setState nextState
                                    )
                                ] [
                                    Icon.icon [] [
                                        Fa.i [Fa.Solid.ChevronDown] []
                                    ]
                                ]
                            ]
                        ]
                    ]
                    tr [
                        OnClick (fun e -> e.stopPropagation())
                        Id id
                        Class "suggestion-details"
                        if List.contains id state.ActiveDropdowns then
                            Style [Visibility "visible"]
                        else
                            Style [Visibility "collapse"]
                    ] [
                        td [ColSpan 4] [
                            Content.content [] [
                                b [] [ str "Definition: " ]
                                str sugg.Description
                            ]
                        ]
                    ]
                |]
            )
        else
            [|
                tr [] [
                    td [] [str "No terms found matching your input."]
                ]
            |]


    open Feliz


    [<ReactComponent>]
    let paginatedTableComponent (model:Model) (data: TableModel) =

        let handlerState, setState = React.useState data
        let updatePageIndex (model:TableModel) (ind:int) =
            let nextState = {model with PageIndex = ind}
            setState nextState

        if data.Data.Length > 0 then 

            let tableRows = createAdvancedTermSearchResultRows handlerState setState
            let currentPageinationIndex = handlerState.PageIndex
            let chunked = tableRows |> Array.chunkBySize data.ElementsPerPage
            let len = chunked.Length 
    
            Container.container [] [
                Table.table [
                    Table.IsFullWidth
                    Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text]]
                ] [
                    thead [] []
                    tbody [] (
                        chunked.[currentPageinationIndex] |> List.ofArray
                    )
                ]
                Pagination.pagination [Pagination.IsCentered] [
                    Pagination.previous [
                        Props [
                            Style [Cursor "pointer"]
                            OnClick (fun _ ->
                                max (currentPageinationIndex - 1) 0 |> updatePageIndex handlerState 
                            )
                            Disabled (currentPageinationIndex = 0)
                        ]
                    ] [
                        str "Prev"
                    ]
                    Pagination.list [] [
                        yield createPaginationLinkFromIndex (updatePageIndex handlerState) 0 currentPageinationIndex
                        if len > 5 && currentPageinationIndex > 3 then yield Pagination.ellipsis []
                        yield! pageinateDynamic (updatePageIndex handlerState) currentPageinationIndex (len - 1)
                        if len > 5 && currentPageinationIndex < len-4 then yield Pagination.ellipsis []
                        if len > 1 then yield createPaginationLinkFromIndex (updatePageIndex handlerState) (len-1) currentPageinationIndex
                    ]
                    Pagination.next [
                        Props [
                            Style [Cursor "pointer"]
                            OnClick (fun _ ->
                                let next = min (currentPageinationIndex + 1) (len - 1)
                                next |> updatePageIndex handlerState
                            )
                            Disabled (currentPageinationIndex = len - 1)
                        ]
                    ] [str "Next"]
                ]
            ]
        else
            div [] []

let private keepObsoleteCheckradioElement (model:Model) dispatch (keepObsolete:bool) modalId =
    let checkradioName = "keepObsolete_checkradio"
    Checkradio.checkboxInline [
        Checkradio.Name checkradioName;
        Checkradio.Id (sprintf "%s_%A_%A"checkradioName keepObsolete modalId);
        Checkradio.Checked (model.AdvancedSearchState.AdvancedSearchOptions.KeepObsolete = keepObsolete)
        Checkradio.Color (if model.SiteStyleState.IsDarkMode then IsWhite else IsBlack)
        Checkradio.OnChange (fun _ ->
            {model.AdvancedSearchState.AdvancedSearchOptions
                with KeepObsolete = keepObsolete
            }
            |> UpdateAdvancedTermSearchOptions
            |> AdvancedSearchMsg
            |> dispatch
        )
    ] [
        str (if keepObsolete then "yes" else "no")
    ]

let private inputFormPage modalId (model:Model) (dispatch: Msg -> unit) =
    div [] [
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Term name keywords:"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "... search term name"
                        Input.Size IsSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with TermName = e.Value
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearchMsg
                            |> dispatch)
                        Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.TermName
                        Input.Props [
                            OnKeyDown (fun e ->
                                match e.which with
                                | 13. ->
                                    e.preventDefault()
                                    e.stopPropagation();
                                    let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                    if isValid then
                                        StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                                | _ -> ()
                            )
                        ]
                    ] 
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Term definition keywords:"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "... search term definition"
                        Input.Size IsSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with TermDescription = e.Value
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearchMsg
                            |> dispatch)
                        Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.TermDescription
                        Input.Props [
                            OnKeyDown (fun e ->
                                match e.which with
                                | 13. ->
                                    e.preventDefault()
                                    e.stopPropagation();
                                    let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                    if isValid then
                                        StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                                | _ -> ()
                            )
                        ]
                    ] 
                ]
            ] 
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Ontology"]
            Control.div [] [
                Select.select [ ] [
                    select [
                        DefaultValue "All Ontologies";
                        if model.AdvancedSearchState.AdvancedSearchOptions.OntologyName.IsSome then Value model.AdvancedSearchState.AdvancedSearchOptions.OntologyName.Value
                        OnChange (fun e ->
                            let v = e.Value
                            let nextSearchOptions = {
                                model.AdvancedSearchState.AdvancedSearchOptions
                                    with OntologyName = if v = "All Ontologies" then None else Some v
                            }
                            nextSearchOptions |> UpdateAdvancedTermSearchOptions |> AdvancedSearchMsg |> dispatch
                        )
                    ] [
                        yield ontologyDropdownItem model dispatch None
                        yield! (
                            model.PersistentStorageState.SearchableOntologies
                            |> Array.map snd
                            |> Array.toList
                            |> List.sortBy (fun o -> o.Name)
                            |> List.map (fun ont -> ontologyDropdownItem model dispatch (Some ont))
                        )
                    ]
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Keep obsolete terms" ]
            div [] [
                keepObsoleteCheckradioElement model dispatch true modalId
                keepObsoleteCheckradioElement model dispatch false modalId
            ]
        ]
    ]

let private resultsPage relatedInputId resultHandler (model:Model) (dispatch: Msg -> unit) =
    Field.div [Field.Props [] ] [
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Results:"]
        if model.AdvancedSearchState.AdvancedTermSearchSubpage = AdvancedSearchSubpages.ResultsSubpage then
            if model.AdvancedSearchState.HasAdvancedSearchResultsLoading then
                div [
                    Style [Width "100%"; Display DisplayOptions.Flex; JustifyContent "center"]
                ] [
                    Loading.loadingComponent
                ]
            else
                let init: ResultsTable.TableModel = {
                    Data            = model.AdvancedSearchState.AdvancedSearchTermResults
                    ActiveDropdowns = []
                    ElementsPerPage = 10
                    PageIndex       = 0
                    Dispatch        = dispatch
                    ResultHandler   = resultHandler
                    RelatedInputId  = relatedInputId
                }
                ResultsTable.paginatedTableComponent
                    model
                    init
    ]

let advancedSearchModal (model:Model) (modalId: string) (relatedInputId:string) (dispatch: Msg -> unit) (resultHandler: Term -> Msg) =
    Modal.modal [
        Modal.IsActive (
            model.AdvancedSearchState.HasModalVisible
            && model.AdvancedSearchState.ModalId = modalId
        )
        Modal.Props [Id modalId]
    ] [
        // Close modal on click on background
        Modal.background [Props [OnClick (fun e -> ResetAdvancedSearchState |> AdvancedSearchMsg |> dispatch)]] []
        Modal.Card.card [Props [Style [Width "90%"; Height "80%"]]] [
            Modal.Card.head [Props [colorBackground model.SiteStyleState.ColorMode]] [
                // Close modal on click on x-button
                Modal.Card.title [Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
                    str "Advanced Search"
                ]
                Fulma.Delete.delete [Delete.OnClick(fun _ -> ResetAdvancedSearchState |> AdvancedSearchMsg |> dispatch)] []
            ]
            Modal.Card.body [Props [colorBackground model.SiteStyleState.ColorMode]] [
                Field.div [] [
                    Help.help [Help.Modifiers [Modifier.TextAlignment (Screen.All, TextAlignment.Justified)]] [str "Swate advanced search uses the Apache Lucene query parser syntax. Feel free to read the related Swate documentation [wip] for guidance on how to use it."]
                ]
                match model.AdvancedSearchState.AdvancedTermSearchSubpage with
                | AdvancedSearchSubpages.InputFormSubpage ->
                    // we need to propagate the modal id here, so we can use meaningful and UNIQUE ids to the checkradio id's
                    inputFormPage modalId model dispatch
                | AdvancedSearchSubpages.ResultsSubpage ->
                    resultsPage relatedInputId resultHandler model dispatch
            ]
            Modal.Card.foot [Props [colorBackground model.SiteStyleState.ColorMode]] [
                form [
                    OnSubmit    (fun e -> e.preventDefault())
                    OnKeyDown   (fun k -> if k.key = "Enter" then k.preventDefault())
                    Style [Width "100%"]
                ] [
                    // Show "Back" button NOT on first subpage
                    if model.AdvancedSearchState.AdvancedTermSearchSubpage <> AdvancedSearchSubpages.InputFormSubpage then
                        Level.item [] [
                            Button.button   [   
                                Button.CustomClass "is-danger"
                                Button.IsFullWidth
                                Button.OnClick (fun e -> e.stopPropagation(); e.preventDefault(); UpdateAdvancedTermSearchSubpage InputFormSubpage |> AdvancedSearchMsg |> dispatch)
                            ] [
                                str "Back"
                            ]
                        ]
                    // Show "Start advanced search" button ONLY on first subpage
                    if model.AdvancedSearchState.AdvancedTermSearchSubpage = AdvancedSearchSubpages.InputFormSubpage then
                        Level.item [] [
                            Button.button   [
                                let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                if isValid then
                                    Button.CustomClass "is-success"
                                    Button.IsActive true
                                else
                                    Button.CustomClass "is-danger"
                                    Button.Props [Disabled (not isValid)]
                                Button.IsFullWidth
                                Button.OnClick (fun e ->
                                    e.preventDefault()
                                    e.stopPropagation();
                                    StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                                )
                            ] [ str "Start advanced search"]
                    ]
                ]
            ]
        ]
    ]