module CustomComponents.AutocompleteSearch
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared
open Fable.Core

type AutocompleteSuggestion<'SearchResult> = {
    Name            : string
    ID              : string
    TooltipText     : string
    Status          : string
    StatusIsWarning : bool
    Data            : 'SearchResult 
}
with
    static member ofTerm (term:DbDomain.Term) : AutocompleteSuggestion<DbDomain.Term> = {
        Name            = term.Name
        ID              = term.Accession
        TooltipText     = term.Definition
        Status          = if term.IsObsolete then "obsolete" else ""
        StatusIsWarning = term.IsObsolete
        Data            = term
    }

    static member ofOntology (ont:DbDomain.Ontology) : AutocompleteSuggestion<DbDomain.Ontology> = {
        Name            = ont.Name
        ID              = ont.CurrentVersion
        TooltipText     = ont.Definition
        Status          = ""
        StatusIsWarning = false
        Data            = ont
    }
        

type AutocompleteParameters<'SearchResult> = {
    Id                      : string

    StateBinding            : string
    Suggestions             : AutocompleteSuggestion<'SearchResult> []
    MaxItems                : int
    DropDownIsVisible       : bool
    DropDownIsLoading       : bool

    OnInputChangeMsg        : (string -> Msg)
    OnSuggestionSelect      : ('SearchResult -> Msg)

    HasAdvancedSearch       : bool
    AdvancedSearchLinkText  : string
    OnAdvancedSearch        : ('SearchResult -> Msg)
}
with
    static member ofTermSearchState (state:TermSearchState) : AutocompleteParameters<DbDomain.Term> = {
        Id                      = "TermSearch"

        StateBinding            = state.TermSearchText
        Suggestions             = state.TermSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowSuggestions
        DropDownIsLoading       = state.HasSuggestionsLoading

        OnInputChangeMsg        = (SearchTermTextChange >> TermSearch )
        OnSuggestionSelect      = (fun (term:DbDomain.Term) -> term |> TermSuggestionUsed |> TermSearch)

        HasAdvancedSearch       = true
        AdvancedSearchLinkText  = "Cant find the Term you are looking for?"
        OnAdvancedSearch        = (fun (term:DbDomain.Term) -> term |> TermSuggestionUsed |> TermSearch )
    }

    static member ofAddBuildingBlockUnitState (state:AddBuildingBlockState) : AutocompleteParameters<DbDomain.Term> = {
        Id                      = "UnitSearch"

        StateBinding            = state.UnitTermSearchText
        Suggestions             = state.UnitTermSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowUnitTermSuggestions
        DropDownIsLoading       = state.HasUnitTermSuggestionsLoading

        AdvancedSearchLinkText   = "Can't find the unit you are looking for?"
        OnInputChangeMsg        = (SearchUnitTermTextChange >> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> sugg.Name |> UnitTermSuggestionUsed |> AddBuildingBlock)

        HasAdvancedSearch       = true
        OnAdvancedSearch        = (fun sugg -> sugg.Name |> UnitTermSuggestionUsed |> AddBuildingBlock)
    }

    static member ofAddBuildingBlockState (state:AddBuildingBlockState) : AutocompleteParameters<DbDomain.Term> = {
        Id                      = "BlockNameSearch"

        StateBinding            = state.CurrentBuildingBlock.Name
        Suggestions             = state.BuildingBlockNameSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowBuildingBlockNameSuggestions
        DropDownIsLoading       = state.HasBuildingBlockNameSuggestionsLoading

        OnInputChangeMsg        = (BuildingBlockNameChange >> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> sugg.Name |> BuildingBlockNameSuggestionUsed |> AddBuildingBlock)

        HasAdvancedSearch       = true
        AdvancedSearchLinkText   = "Cant find the Term you are looking for?"
        OnAdvancedSearch        = (fun sugg -> sugg.Name |> BuildingBlockNameSuggestionUsed |> AddBuildingBlock)
    }

let createAutocompleteSuggestions
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (autocompleteParams: AutocompleteParameters<'SearchResult>)
    =

    let suggestions = 
        if autocompleteParams.Suggestions.Length > 0 then
            autocompleteParams.Suggestions
            |> fun s -> s |> Array.take (if s.Length < autocompleteParams.MaxItems then s.Length else autocompleteParams.MaxItems)
            |> Array.map (fun sugg ->
                tr [
                    OnClick (fun _ -> sugg.Data |> autocompleteParams.OnSuggestionSelect |> dispatch)
                    OnKeyDown (fun k -> if k.key = "Enter" then sugg.Data |> autocompleteParams.OnSuggestionSelect |> dispatch)
                    TabIndex 0
                    colorControl colorMode
                    Class "suggestion"
                ] [
                    td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.TooltipText] [
                        Fa.i [Fa.Solid.InfoCircle] []
                    ]
                    td [] [
                        b [] [str sugg.Name]
                    ]
                    td [if sugg.StatusIsWarning then Style [Color "red"]] [str sugg.Status]
                    td [Style [FontWeight "light"]] [small [] [str sugg.ID]]
                ])
            |> List.ofArray
        else
            [
                tr [] [
                    td [] [str "No terms found matching your input."]
                ]
            ]

    let alternative =
        tr [
            colorControl colorMode
            Class "suggestion"
        ][
            td [ColSpan 4] [
                str (sprintf "%s " autocompleteParams.AdvancedSearchLinkText)
                a [OnClick (fun _ -> ToggleModal autocompleteParams.Id |> AdvancedSearch |> dispatch)] [
                    str "Use Advanced Search"
                ] 
            ]
        ]

    suggestions @ [alternative]



let autocompleteDropdownComponent (dispatch:Msg -> unit) (colorMode:ColorMode) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
    Container.container[] [
        Dropdown.content [Props [
            Style [
                if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                //if model.ShowFillSuggestions then Display DisplayOptions.Block else Display DisplayOptions.None
                BackgroundColor colorMode.ControlBackground
                BorderColor     colorMode.ControlForeground
            ]]
        ] [
            Table.table [Table.IsFullWidth] [
                if isLoading then
                    tbody [] [
                        tr [] [
                            td [Style [TextAlign TextAlignOptions.Center]] [
                                Loading.loadingComponent
                                br []
                            ]
                        ]
                    ]
                else
                    tbody [] suggestions
            ]

            
        ]
    ]

let autocompleteTermSearchComponent
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (model:Model)
    (inputPlaceholderText   : string)
    (inputSize              : ISize option)
    (autocompleteParams     : AutocompleteParameters<DbDomain.Term>)
    (isDisabled:bool)
    = 
    Control.div [Control.IsExpanded] [
        AdvancedSearch.advancedSearchModal model autocompleteParams.Id dispatch autocompleteParams.OnAdvancedSearch
        Input.input [
            Input.Disabled isDisabled
            Input.Placeholder inputPlaceholderText
            match inputSize with
            | Some size -> Input.Size size
            | _ -> ()
            Input.Props [
                ExcelColors.colorControl colorMode
                //OnFocus (fun e -> alert "focusout")
                //OnBlur  (fun e -> alert "focusin")
            ]           
            Input.OnChange (fun e -> e.Value |> autocompleteParams.OnInputChangeMsg |> dispatch)
            Input.Value autocompleteParams.StateBinding
                        
        ]
        autocompleteDropdownComponent
            dispatch
            colorMode
            autocompleteParams.DropDownIsVisible
            autocompleteParams.DropDownIsLoading
            (createAutocompleteSuggestions dispatch colorMode autocompleteParams)
    ]

let autocompleteTermSearchComponentOfParentOntology
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (model:Model)
    (inputPlaceholderText   : string)
    (inputSize              : ISize option)
    (autocompleteParams     : AutocompleteParameters<DbDomain.Term>)

    =
    let parentOntologyNotificationElement show =
        Control.p [ Control.Modifiers [ Modifier.IsHidden (Screen.All, show)]][
            Button.button [
                Button.IsStatic true
                match inputSize with
                | Some size -> Button.Size size
                | _ -> ()
            ] [str (sprintf "%A" model.TermSearchState.ParentOntology.Value)]
        ]

    Control.div [Control.IsExpanded] [
        AdvancedSearch.advancedSearchModal model autocompleteParams.Id dispatch autocompleteParams.OnAdvancedSearch
        Field.div [Field.HasAddons][
            parentOntologyNotificationElement ((model.TermSearchState.ParentOntology.IsSome && model.TermSearchState.SearchByParentOntology) |> not)
            Control.p [Control.IsExpanded][
                Input.input [
                    Input.Props [Id "TermSearchInput"]
                    Input.Placeholder inputPlaceholderText
                    match inputSize with
                    | Some size -> Input.Size size
                    | _ -> ()
                    Input.Props [
                        ExcelColors.colorControl colorMode
                        //OnFocus (fun e -> alert "focusout")
                        //OnBlur  (fun e -> alert "focusin")
                        OnFocus (fun e ->
                            //GenericLog ("Info","FOCUSED!") |> Dev |> dispatch
                            GetParentTerm |> ExcelInterop |> dispatch
                            let el = Browser.Dom.document.getElementById "TermSearchInput"
                            el.focus()
                        )
                    ]           
                    Input.OnChange (fun e -> e.Value |> autocompleteParams.OnInputChangeMsg |> dispatch)
                    Input.Value autocompleteParams.StateBinding
                ]
            ]
        ]
        autocompleteDropdownComponent
            dispatch
            colorMode
            autocompleteParams.DropDownIsVisible
            autocompleteParams.DropDownIsLoading
            (createAutocompleteSuggestions dispatch colorMode autocompleteParams)
    ]