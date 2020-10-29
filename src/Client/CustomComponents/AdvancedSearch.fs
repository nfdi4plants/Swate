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


let isValidAdancedSearchOptions (opt:AdvancedTermSearchOptions) =
    ((
        opt.DefinitionMustContain.Length
        + opt.EndsWith.Length
        + opt.MustContain.Length
        + opt.DefinitionMustContain.Length
            ) > 0)
            || opt.Ontology.IsSome

let createOntologyDropdownItem (model:Model) (dispatch:Msg -> unit) (ont: DbDomain.Ontology)  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun _ -> ont |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
            OnKeyDown (fun k -> if k.key = "Enter" then ont |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip (ont.Definition)
                Style [PaddingRight "10px"]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]
        
        Text.span [] [ont.Name |> str]
    ]

let createAdvancedTermSearchResultRows (model:Model) (dispatch: Msg -> unit) (suggestionUsedHandler: DbDomain.Term -> Msg) =
    if model.AdvancedSearchState.AdvancedSearchTermResults.Length > 0 then
        model.AdvancedSearchState.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg |> suggestionUsedHandler |> dispatch)
                colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
            ] [
                td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                    Fa.i [Fa.Solid.InfoCircle] []
                ]
                td [] [
                    b [] [str sugg.Name]
                ]
                td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
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
            Field.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.AdvancedSearchState.HasOntologyDropdownVisible
                ] [
                    Dropdown.trigger [] [
                        Button.button [Button.OnClick (fun _ -> ToggleOntologyDropdown |> AdvancedSearch |> dispatch)] [
                            span [] [
                                match model.AdvancedSearchState.AdvancedSearchOptions.Ontology with
                                | None -> "select ontology" |> str
                                | Some ont -> ont.Name |> str
                            ]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [Props[colorControl model.SiteStyleState.ColorMode];] [
                        Dropdown.content [] (
                            model.PersistentStorageState.SearchableOntologies
                            |> Array.map snd
                            |> Array.toList
                            |> List.map (createOntologyDropdownItem model dispatch))
                    ]
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Starts with:"]
            Help.help [] [str "The term name must start with this string"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "Enter starts with text"
                        Input.Size IsSmall
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with StartsWith = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> AdvancedSearch
                            |> dispatch)
                        Input.Value model.AdvancedSearchState.AdvancedSearchOptions.StartsWith
                    ] 
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Must contain:"]
            Help.help [] [str "The term name must contain any of these space-separated words (at any position)"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "Enter contains text"
                        Input.Size IsSmall
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with MustContain = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> AdvancedSearch
                            |> dispatch)
                        Input.Value model.AdvancedSearchState.AdvancedSearchOptions.MustContain
                    ] 
                ]
            ]
        ]
        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Ends with:"]
            Help.help [] [str "The term must end with this string"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "enter ends with text"
                        Input.Size IsSmall
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with EndsWith = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> AdvancedSearch
                            |> dispatch)
                        Input.Value model.AdvancedSearchState.AdvancedSearchOptions.EndsWith
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
                            Input.Placeholder "enter definition must contain text"
                            Input.Size IsSmall
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.AdvancedSearchState.AdvancedSearchOptions
                                    with DefinitionMustContain = e.Value
                                }
                                |> AdvancedSearchOptionsChange
                                |> AdvancedSearch
                                |> dispatch)
                            Input.Value model.AdvancedSearchState.AdvancedSearchOptions.DefinitionMustContain
                        ] 
                    ]
                ]
            ]
        ]
    ]

let advancedSearchResultTable (model:Model) (dispatch: Msg -> unit) =
    Field.div [Field.Props [] ] [
        Label.label [] [str "Results:"]
        if model.AdvancedSearchState.ShowAdvancedSearchResults then
            if model.AdvancedSearchState.HasAdvancedSearchResultsLoading then
                Loading.loadingComponent
            else
                Button.buttonComponent model.SiteStyleState.ColorMode true "Change search options" (fun _ -> ResetAdvancedSearchOptions |> AdvancedSearch |> dispatch)
                PaginatedTable.paginatedTableComponent
                    model
                    dispatch
                    (createAdvancedTermSearchResultRows
                        model
                        dispatch
                        (AdvancedSearchResultSelected >> AdvancedSearch)
                    )
    ]

let advancedSearchSelectedResultDisplay (model:Model) (result:DbDomain.Term) =
    Container.container [] [
        Heading.h4 [] [str "Selected Result:"]
        Table.table [Table.IsFullWidth] [
            thead [] []
            tbody [] [
                tr [
                colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
                ] [
                    td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip result.Definition] [
                        Fa.i [Fa.Solid.InfoCircle] []
                    ]
                    td [] [
                        b [] [str result.Name]
                    ]
                    td [Style [Color "red"]] [if result.IsObsolete then str "obsolete"]
                    td [Style [FontWeight "light"]] [small [] [str result.Accession]]
                ]
            ]
        ]
    ]

let advancedSearchModal (model:Model) (id:string) (dispatch: Msg -> unit) (resultHandler: DbDomain.Term -> Msg) =
    Modal.modal [
        Modal.IsActive (
            model.AdvancedSearchState.HasModalVisible
            && model.AdvancedSearchState.ModalId = id
        )
        Modal.Props [
            colorControl model.SiteStyleState.ColorMode
            Id id
        ]
    ] [
        Modal.background [] []
        Modal.Card.card [Props [colorControl model.SiteStyleState.ColorMode]] [
            Modal.Card.head [Props [colorControl model.SiteStyleState.ColorMode]] [
                Modal.close [Modal.Close.Size IsLarge; Modal.Close.OnClick (fun _ -> ToggleModal id |> AdvancedSearch |> dispatch)] []
                Heading.h4 [Heading.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
                    str "Advanced Search"
                ]
            ]
            Modal.Card.body [Props [colorControl model.SiteStyleState.ColorMode]] [
                if (not model.AdvancedSearchState.ShowAdvancedSearchResults) then
                    advancedTermSearchComponent model dispatch
                else
                    match model.AdvancedSearchState.SelectedResult with
                    |None   -> advancedSearchResultTable model dispatch 
                    |Some r -> advancedSearchSelectedResultDisplay model r

            ]
            Modal.Card.foot [] [
                form [
                    OnSubmit    (fun e -> e.preventDefault())
                    OnKeyDown   (fun k -> if k.key = "Enter" then k.preventDefault())
                ] [
                    Field.div [Field.HasAddons;Field.IsExpanded] [
                        Control.div [Control.IsExpanded] [
                            if (not model.AdvancedSearchState.ShowAdvancedSearchResults) then
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
                            else
                                Button.button   [   let hasText = model.AdvancedSearchState.SelectedResult.IsSome
                                                    if hasText then
                                                        Button.CustomClass "is-success"
                                                        Button.IsActive true
                                                    else
                                                        Button.CustomClass "is-danger"
                                                        Button.Props [Disabled true]
                                                    Button.IsFullWidth
                                                    Button.OnClick (fun _ ->
                                                        ResetAdvancedSearchState |> AdvancedSearch |> dispatch;
                                                        model.AdvancedSearchState.SelectedResult.Value |> resultHandler |> dispatch)
                                                ] [
                                    str "Confirm"
                            
                                ]
                        ]
                        Control.div [Control.IsExpanded] [
                            Button.button   [   
                                                Button.CustomClass "is-danger"
                                                Button.IsFullWidth
                                                Button.OnClick (fun _ -> ResetAdvancedSearchOptions |> AdvancedSearch |> dispatch)

                                            ] [
                                str "Reset"
                            ]
                        ]
                        Control.div [Control.IsExpanded] [
                            Button.button   [   
                                                Button.CustomClass "is-danger"
                                                Button.IsFullWidth
                                                Button.OnClick (fun _ -> ResetAdvancedSearchState |> AdvancedSearch |> dispatch)

                                            ] [
                                str "Cancel"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]