module TermSearchView

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

let createTermSuggestions (model:Model) (dispatch: Msg -> unit) =
    if model.TermSearchState.Simple.TermSuggestions.Length > 0 then
        model.TermSearchState.Simple.TermSuggestions
        |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg.Name |> TermSuggestionUsed |> Simple |> TermSearch |> dispatch)
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

let createOntologySuggestions (model:Model) (dispatch: Msg -> unit) =
    model.PersistentStorageState.SearchableOntologies
    |> Array.sortByDescending (fun (bigrams,_) ->
        Suggestion.sorensenDice (model.TermSearchState.Advanced.OntologySearchText |> Suggestion.createBigrams) bigrams 
    )
    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
    |> Array.map (fun (_,sugg) ->
        tr [OnClick (fun _ -> sugg |> OntologySuggestionUsed |> Advanced |> TermSearch |> dispatch)
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
    if model.TermSearchState.Advanced.AdvancedSearchTermResults.Length > 0 then
        model.TermSearchState.Advanced.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg.Name |> AdvancedSearchResultUsed |> Advanced |> TermSearch |> dispatch)
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
            

let simpleSearchComponent (model:Model) (dispatch: Msg -> unit) =
    Field.div [] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
        a [OnClick (fun _ -> SwitchSearchMode |> TermSearch |> dispatch)] [str "Use advanced search"]
        br []
        Control.div [] [
            Input.input [   Input.Placeholder ""
                            Input.Size Size.IsLarge
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->  e.Value |> SearchTermTextChange |> Simple |> TermSearch |> dispatch)
                            Input.Value model.TermSearchState.Simple.TermSearchText
                        ]
            AutocompleteDropdown.autocompleteDropdownComponent
                model
                dispatch
                model.TermSearchState.Simple.ShowSuggestions
                model.TermSearchState.Simple.HasSuggestionsLoading
                (createTermSuggestions model dispatch)
        ]
        Help.help [] [str "When applicable, search for an ontology term to fill into the selected field(s)"]
    ]

let advancedSearchComponent (model:Model) (dispatch: Msg -> unit) =
    [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
        a [OnClick (fun _ -> SwitchSearchMode |> TermSearch |> dispatch)] [str "Use simple search"]
        br []
        Field.div [] [
            Label.label [] [ str "Ontology"]
            Help.help [] [str "Only search terms in the selected ontology"]
            Field.div [] [
                Control.div [] [
                    Input.input [   Input.Placeholder ""
                                    Input.Size Size.IsMedium
                                    Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                    Input.OnChange (fun e -> e.Value |> SearchOntologyTextChange |> Advanced |> TermSearch |> dispatch)
                                    Input.Value model.TermSearchState.Advanced.OntologySearchText
                                ]   
                    AutocompleteDropdown.autocompleteDropdownComponent
                        model
                        dispatch
                        model.TermSearchState.Advanced.ShowOntologySuggestions
                        model.TermSearchState.Advanced.HasOntologySuggestionsLoading
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
                        Input.Placeholder ""
                        Input.Size IsMedium
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.TermSearchState.Advanced.AdvancedSearchOptions
                                with StartsWith = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> Advanced
                            |> TermSearch
                            |> dispatch)
                        Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.StartsWith
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
                        Input.Placeholder ""
                        Input.Size IsMedium
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.TermSearchState.Advanced.AdvancedSearchOptions
                                with MustContain = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> Advanced
                            |> TermSearch
                            |> dispatch)
                        Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.MustContain
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
                        Input.Placeholder ""
                        Input.Size IsMedium
                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        Input.OnChange (fun e ->
                            {model.TermSearchState.Advanced.AdvancedSearchOptions
                                with EndsWith = e.Value
                            }
                            |> AdvancedSearchOptionsChange
                            |> Advanced
                            |> TermSearch
                            |> dispatch)
                        Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.EndsWith
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
                            Input.Placeholder ""
                            Input.Size IsMedium
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.TermSearchState.Advanced.AdvancedSearchOptions
                                    with DefinitionMustContain = e.Value
                                }
                                |> AdvancedSearchOptionsChange
                                |> Advanced
                                |> TermSearch
                                |> dispatch)
                            Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.DefinitionMustContain
                        ] 
                    ]
                ]
            ]
        ]
        Field.div [] [
            Control.div [] [
                Button.button   [
                    let isValid = isValidAdancedSearchOptions model.TermSearchState.Advanced.AdvancedSearchOptions
                    if isValid then
                        Button.CustomClass "is-success"
                        Button.IsActive true
                    else
                        Button.CustomClass "is-danger"
                        Button.Props [Disabled (not isValid)]
                    Button.IsFullWidth
                    Button.OnClick (fun _ -> model.TermSearchState.Advanced.AdvancedSearchOptions |> GetNewAdvancedTermSearchResults |> Request |> Api |> dispatch)
                ] [ str "Start advanced search"]
            ]
        ]
        Field.div [Field.Props [] ] [
            Label.label [] [str "Results:"]
            AutocompleteDropdown.autocompleteDropdownComponent
                model
                dispatch
                model.TermSearchState.Advanced.ShowAdvancedSearchResults
                model.TermSearchState.Advanced.HasAdvancedSearchResultsLoading
                (createAdvancedTermSearchResultList model dispatch)
        ]
    ]


let termSearchComponent (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        // Fill selection components with two search modes
        match model.TermSearchState.SearchMode with
        | TermSearchMode.Simple     -> simpleSearchComponent model dispatch
        | TermSearchMode.Advanced   -> yield! advancedSearchComponent model dispatch

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.TermSearchState.Simple.TermSearchText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> model.TermSearchState.Simple.TermSearchText |> FillSelection |> ExcelInterop |> dispatch)

                                ] [
                    str "Fill selected cells with this term"
                    
                ]
            ]
        ]
    ]