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
            OnKeyDown (fun k -> if (int k.keyCode) = 13 then ont |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
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

let createAdvancedTermSearchResultList (model:Model) (dispatch: Msg -> unit) =
    if model.AdvancedSearchState.AdvancedSearchTermResults.Length > 0 then
        model.AdvancedSearchState.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg.Name |> AdvancedSearchResultUsed |> AdvancedSearch |> dispatch)
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
        |> List.ofArray
            else
        [
            tr [] [
                td [] [str "No terms found matching your input."]
            ]
        ]


let advancedTermSearchComponent (model:Model) (dispatch: Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.keyCode) = 13 then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
        br []
        Field.div [] [
            Label.label [] [ str "Ontology"]
            Help.help [] [str "Only search terms in the selected ontology"]
            Field.div [] [
                Dropdown.dropdown [Dropdown.IsActive model.AdvancedSearchState.HasOntologyDropdownVisible] [
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
                    Dropdown.menu [Props[colorControl model.SiteStyleState.ColorMode]] [
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
            Label.label [] [ str "Starts with:"]
            Help.help [] [str "The term name must start with this string"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "Enter starts with text"
                        Input.Size IsMedium
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
            Label.label [] [ str "Must contain:"]
            Help.help [] [str "The term name must contain any of these space-separated words (at any position)"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "Enter contains text"
                        Input.Size IsMedium
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
            Label.label [] [ str "Ends with:"]
            Help.help [] [str "The term must end with this string"]
            Field.div [] [
                Control.div [] [
                    Input.input [
                        Input.Placeholder "enter ends with text"
                        Input.Size IsMedium
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
            Label.label [] [ str "Definition must contain:"]
            Help.help [] [str "The definition of the term must contain any of these space-separated words (at any position)"]
            Field.body [] [
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder "enter definition must contain text"
                            Input.Size IsMedium
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
        Field.div [] [
            Control.div [] [
                Button.button   [
                    let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                    if isValid then
                        Button.CustomClass "is-success"
                        Button.IsActive true
                    else
                        Button.CustomClass "is-danger"
                        Button.Props [Disabled (not isValid)]
                    Button.IsFullWidth
                    Button.OnClick (fun _ -> model.AdvancedSearchState.AdvancedSearchOptions |> GetNewAdvancedTermSearchResults |> Request |> Api |> dispatch)
                ] [ str "Start advanced search"]
            ]
        ]
        Field.div [Field.Props [] ] [
            Label.label [] [str "Results:"]
        ]
    ]

let advancedSearchModal (model:Model) (dispatch: Msg -> unit) (resultHandler: DbDomain.Term -> Msg) =
    Modal.modal [Modal.IsActive model.AdvancedSearchState.HasModalVisible] [
        Modal.background [] []
        Modal.Card.card [] [
            Modal.Card.head [] [
                Modal.close [Modal.Close.Size IsLarge; Modal.Close.OnClick (fun _ -> ToggleModal |> AdvancedSearch |> dispatch)] []
                Heading.h2 [] [
                    str "Advanced Search"
                ]
            ]
            Modal.Card.body [] [
                advancedTermSearchComponent model dispatch 
            ]
            Modal.Card.foot [] [
                Field.div [] [
                    Control.div [] [
                        Button.button   [   let hasText = model.AdvancedSearchState.SelectedResult.IsSome
                                            if hasText then
                                                Button.CustomClass "is-success"
                                                Button.IsActive true
                                            else
                                                Button.CustomClass "is-danger"
                                                Button.Props [Disabled true]
                                            Button.IsFullWidth
                                            Button.OnClick (fun _ -> model.AdvancedSearchState.SelectedResult.Value |> resultHandler |> dispatch)

                                        ] [
                            str "Confirm"
                            
                        ]
                    ]
                ]
            ]
        ]
    ]