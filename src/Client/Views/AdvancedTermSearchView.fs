module AdvancedTermSearchView

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

let createOntologySuggestions (model:Model) (dispatch: Msg -> unit) =
    model.PersistentStorageState.SearchableOntologies
    |> Array.sortByDescending (fun (bigrams,_) ->
        Suggestion.sorensenDice (model.AdvancedSearchState.OntologySearchText |> Suggestion.createBigrams) bigrams 
    )
    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
    |> Array.map (fun (_,sugg) ->
        tr [OnClick (fun _ -> sugg |> OntologySuggestionUsed |> AdvancedSearch |> dispatch)
            colorControl model.SiteStyleState.ColorMode
            Class "suggestion"
        ] [
            td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
            td [] [
                b [] [str sugg.Name]
            ]
            td [Style [FontWeight "light"]] [small [] [str sugg.CurrentVersion]]
        ])
    |> List.ofArray



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

let advancedSearchComponent (model:Model) (dispatch: Msg -> unit) =
    [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
        br []
        Field.div [] [
            Label.label [] [ str "Ontology"]
            Help.help [] [str "Only search terms in the selected ontology"]
            Field.div [] [
                Control.div [] [
                    Input.input [   Input.Placeholder "Start typing to start search"
                                    Input.Size Size.IsMedium
                                    Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                    Input.OnChange (fun e -> e.Value |> SearchOntologyTextChange |> AdvancedSearch |> dispatch)
                                    Input.Value model.AdvancedSearchState.OntologySearchText
                                ]   
                    AutocompleteSearch.autocompleteDropdownComponent
                        dispatch
                        model.SiteStyleState.ColorMode
                        model.AdvancedSearchState.ShowOntologySuggestions
                        model.AdvancedSearchState.HasOntologySuggestionsLoading
                        (createOntologySuggestions model dispatch)
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
            AutocompleteSearch.autocompleteDropdownComponent
                dispatch
                model.SiteStyleState.ColorMode
                model.AdvancedSearchState.ShowAdvancedSearchResults
                model.AdvancedSearchState.HasAdvancedSearchResultsLoading
                (createAdvancedTermSearchResultList model dispatch)
        ]
    ]