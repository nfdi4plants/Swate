module AdvancedSearch

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open CustomComponents

let createLinkOfAccession (accession:string) =
    a [
        let link = accession |> URLs.termAccessionUrlOfAccessionStr
        Href link
        Target "_Blank"
    ] [
        str accession
    ]

let isValidAdancedSearchOptions (opt:AdvancedTermSearchOptions) =
    ((
        opt.SearchTermName.Length
        + opt.SearchTermDefinition.Length
        + opt.MustContainName.Length
        + opt.MustContainDefinition.Length
            ) > 0)
            || opt.Ontology.IsSome

let createOntologyDropdownItem (model:Model) (dispatch:Msg -> unit) (ontOpt: DbDomain.Ontology option)  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun _ -> ontOpt |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
            OnKeyDown (fun k -> if k.key = "Enter" then ontOpt |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
        ]
    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                if ontOpt.IsSome then Tooltip.dataTooltip (ontOpt.Value.Definition)
                Style [PaddingRight "10px"]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]
        
        Text.span [] [
            if ontOpt.IsSome then
                ontOpt.Value.Name |> str
            else
                "No Ontology" |> str
        ]
    ]

let createAdvancedTermSearchResultRows (model:Model) (dispatch: Msg -> unit) (suggestionUsedHandler: DbDomain.Term -> Msg) =
    if model.AdvancedSearchState.AdvancedSearchTermResults |> Array.isEmpty |> not then
        model.AdvancedSearchState.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [
                OnClick (fun _ -> sugg |> suggestionUsedHandler |> dispatch)
                Class "suggestion"
                colorControl model.SiteStyleState.ColorMode
            ] [
                td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
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

let advancedTermSearchComponent (model:Model) (dispatch: Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Ontology"]
            Help.help [] [str "Only search terms in the selected ontology"]
            Field.div [Field.Props [Style [BorderRadius "unset"]]] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.AdvancedSearchState.HasOntologyDropdownVisible
                ] [
                    Dropdown.trigger [] [
                        Button.button [
                            Button.OnClick (fun _ -> ToggleOntologyDropdown |> AdvancedSearch |> dispatch);
                            Button.Size Size.IsSmall;
                            Button.Props [Style [MarginTop "0.5rem"]]
                        ] [
                            span [] [
                                match model.AdvancedSearchState.AdvancedSearchOptions.Ontology with
                                | None -> "select ontology" |> str
                                | Some ont -> ont.Name |> str
                            ]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [Props[colorControl model.SiteStyleState.ColorMode];] [
                        Dropdown.content [
                            Props [Style [MaxHeight "180px"; OverflowY OverflowOptions.Scroll; MarginRight "-100px"; PaddingRight "100px"; Border "1px solid darkgrey"]]
                        ] [
                            // all ontologies found in database
                            yield createOntologyDropdownItem model dispatch None
                            yield! (
                                model.PersistentStorageState.SearchableOntologies
                                |> Array.map snd
                                |> Array.toList
                                |> List.sortBy (fun o -> o.Name)
                                |> List.map (fun ont -> createOntologyDropdownItem model dispatch (Some ont))
                            )
                        ]
                    ]
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Term name keywords:"]
            Help.help [] [str "Search the term name for the following words."]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "... search key words"
                        Input.Size IsSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with SearchTermName = e.Value
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearch
                            |> dispatch)
                        Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.SearchTermName
                    ] 
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Name must contain:"]
            Help.help [] [str "Only suggest Terms, which name includes the following text part."]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "... must exist in name"
                        Input.Size IsSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with MustContainName = e.Value
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearch
                            |> dispatch)
                        Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.MustContainName
                    ] 
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Term definition keywords:"]
            Help.help [] [str "Search the term definition for the following words."]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "... search key words"
                        Input.Size IsSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with SearchTermDefinition = e.Value
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearch
                            |> dispatch)
                        Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.SearchTermDefinition
                    ] 
                ]
            ] 
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Definition must contain:"]
            Help.help [] [str "The definition of the term must contain any of these space-separated words (at any position)"]
            Field.body [] [
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder "... must exist in definition"
                            Input.Size IsSmall
                            //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.AdvancedSearchState.AdvancedSearchOptions
                                    with MustContainDefinition = e.Value
                                }
                                |> UpdateAdvancedTermSearchOptions
                                |> AdvancedSearch
                                |> dispatch)
                            Input.ValueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.MustContainDefinition
                        ] 
                    ]
                ]
            ]
        ]
    ]

let advancedSearchResultTable (model:Model) (dispatch: Msg -> unit) =
    Field.div [Field.Props [] ] [
        div [
            Style [Margin "1rem"]
        ][
            Button.buttonComponent model.SiteStyleState.ColorMode true "Change search options" (fun _ -> UpdateAdvancedTermSearchSubpage AdvancedTermSearchSubpages.InputFormSubpage |> AdvancedSearch |> dispatch)
        ]
        Label.label [Label.Props [colorControl model.SiteStyleState.ColorMode]] [str "Results:"]
        if model.AdvancedSearchState.AdvancedTermSearchSubpage = AdvancedTermSearchSubpages.ResultsSubpage then
            if model.AdvancedSearchState.HasAdvancedSearchResultsLoading then
                div [
                    Style [Width "100%"; Display DisplayOptions.Flex; JustifyContent "center"]
                ][
                    Loading.loadingComponent
                ]
            else
                PaginatedTable.paginatedTableComponent
                    model
                    dispatch
                    (createAdvancedTermSearchResultRows
                        model
                        dispatch
                        /// The following line defines which message is executed onClick on one of the terms in the result table.
                        ((fun t ->
                            UpdateAdvancedTermSearchSubpage <| AdvancedTermSearchSubpages.SelectedResultSubpage t) >> AdvancedSearch
                        )
                    )
    ]

let advancedSearchSelectedResultDisplay (model:Model) (result:DbDomain.Term) =
    Container.container [] [
        Heading.h4 [Heading.Props [colorControl model.SiteStyleState.ColorMode]] [str "Selected Result:"]
        Table.table [Table.IsFullWidth] [
            tbody [colorControl model.SiteStyleState.ColorMode] [
                tr [
                //colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
                ] [
                    td [] [
                        b [] [str result.Name]
                    ]
                    td [Style [Color "red"]] [if result.IsObsolete then str "obsolete"]
                    td [Style [FontWeight "light"]] [small [] [str result.Accession]]
                ]
            ]
        ]
    ]

open Fable.Core.JsInterop

let advancedSearchModal (model:Model) (modalId: string) (relatedInputId:string) (dispatch: Msg -> unit) (resultHandler: DbDomain.Term -> Msg) =
    Modal.modal [
        Modal.IsActive (
            model.AdvancedSearchState.HasModalVisible
            && model.AdvancedSearchState.ModalId = modalId
        )
        Modal.Props [
            //colorControl model.SiteStyleState.ColorMode
            Id modalId
        ]
    ] [
        Modal.background [] []
        Modal.Card.card [] [
            Modal.Card.head [Props [colorControl model.SiteStyleState.ColorMode]] [
                Modal.close [Modal.Close.Size IsLarge; Modal.Close.OnClick (fun _ -> ToggleModal modalId |> AdvancedSearch |> dispatch)] []
                Heading.h4 [Heading.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
                    str "Advanced Search"
                ]
            ]
            Modal.Card.body [Props [colorControl model.SiteStyleState.ColorMode]] [
                match model.AdvancedSearchState.AdvancedTermSearchSubpage with
                | AdvancedTermSearchSubpages.InputFormSubpage ->
                    advancedTermSearchComponent model dispatch
                | AdvancedTermSearchSubpages.ResultsSubpage ->
                    advancedSearchResultTable model dispatch
                | AdvancedTermSearchSubpages.SelectedResultSubpage r ->
                    advancedSearchSelectedResultDisplay model r
                //else
                //    match model.AdvancedSearchState.SelectedResult with
                //    |None   -> advancedSearchResultTable model dispatch 
                //    |Some r -> advancedSearchSelectedResultDisplay model r
            ]
            Modal.Card.foot [Props [colorControl model.SiteStyleState.ColorMode]] [
                form [
                    OnSubmit    (fun e -> e.preventDefault())
                    OnKeyDown   (fun k -> if k.key = "Enter" then k.preventDefault())
                    Style [Width "100%"]
                ] [
                    Level.level [][
                        Level.item [][
                            Level.level [Level.Level.Props [Style [Width "100%"]]][
                                Level.item [][
                                    Button.button   [   
                                        Button.CustomClass "is-danger"
                                        Button.IsFullWidth
                                        Button.OnClick (fun _ -> ResetAdvancedSearchOptions |> AdvancedSearch |> dispatch)
                                    ] [
                                        str "Reset"
                                    ]
                                ]
                                Level.item [][
                                    Button.button [   
                                        Button.CustomClass "is-danger"
                                        Button.IsFullWidth
                                        Button.OnClick (fun _ -> ResetAdvancedSearchState |> AdvancedSearch |> dispatch)

                                    ] [
                                        str "Cancel"
                                    ]
                                ]
                            ]
                        ]
                        Level.item [][
                            match model.AdvancedSearchState.AdvancedTermSearchSubpage with
                            | AdvancedTermSearchSubpages.SelectedResultSubpage sth ->
                                Button.button   [
                                    let hasText = model.AdvancedSearchState.SelectedResult.IsSome
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ ->
                                        let e = Browser.Dom.document.getElementById(relatedInputId)
                                        sth |> resultHandler |> dispatch

                                        e?value <- sth.Name
                                        ResetAdvancedSearchState |> AdvancedSearch |> dispatch)
                                ] [
                                    str "Confirm"
                            
                                ]
                            | _ ->
                                Button.button   [
                                    let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                    if isValid then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled (not isValid)]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> StartAdvancedSearch |> AdvancedSearch |> dispatch)
                                ] [ str "Start advanced search"]
                        ]
                    ]
                ]
            ]
        ]
    ]