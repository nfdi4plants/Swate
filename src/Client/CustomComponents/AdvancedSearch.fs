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

    | ChangePageinationIndex index ->
        let nextState = {
            currentState with
                AdvancedSearchResultPageinationIndex = index
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

let private createAdvancedTermSearchResultRows relatedInputId resultHandler  (model:Model) (dispatch: Msg -> unit) =
    if model.AdvancedSearchState.AdvancedSearchTermResults |> Array.isEmpty |> not then
        model.AdvancedSearchState.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [
                OnClick (fun e ->
                    // dont close modal on click
                    e.stopPropagation()
                    let relInput = Browser.Dom.document.getElementById(relatedInputId)
                    // select wanted term
                    sugg |> resultHandler |> dispatch
                    // propagate wanted term name to related input on main page
                    relInput?value <- sugg.Name
                    // reset advanced term search state
                    ResetAdvancedSearchState |> AdvancedSearchMsg |> dispatch

                )
                Class "suggestion hoverTableEle"
                //colorControl model.SiteStyleState.ColorMode
            ] [
                td [Class "has-tooltip-right has-tooltip-multiline"; Props.Custom ("data-tooltip", sugg.Description) ] [
                    Fa.i [Fa.Solid.InfoCircle] []
                ]
                td [] [
                    b [] [str sugg.Name]
                ]
                td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
                td [
                    OnClick (fun e -> e.stopPropagation())
                    Style [FontWeight "light"]
                ] [
                    small [] [
                        createLinkOfAccession sugg.Accession
                    ]
                ]
            ])
    else
        [|
            tr [] [
                td [] [str "No terms found matching your input."]
            ]
        |]

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
                PaginatedTable.paginatedTableComponent
                    model
                    dispatch
                    10
                    (createAdvancedTermSearchResultRows relatedInputId resultHandler model dispatch)
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
        Modal.Card.card [] [
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
                    Level.level [] [
                        Level.left [] [
                            // Show "Back" button NOT on first subpage
                            if model.AdvancedSearchState.AdvancedTermSearchSubpage <> AdvancedSearchSubpages.InputFormSubpage then
                                Level.item [] [
                                    Button.button   [   
                                        Button.CustomClass "is-danger"
                                        Button.IsFullWidth
                                        Button.OnClick (fun e -> e.stopPropagation(); UpdateAdvancedTermSearchSubpage InputFormSubpage |> AdvancedSearchMsg |> dispatch)
                                    ] [
                                        str "Back"
                                    ]
                                ]
                        ]
                        Level.right [] [
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
        ]
    ]