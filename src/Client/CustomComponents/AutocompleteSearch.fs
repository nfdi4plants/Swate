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

type AutocompleteSuggestion = {
    Name            : string
    ID              : string
    TooltipText     : string
    Status          : string
    StatusIsWarning : bool
}
with
    static member ofTerm (term:DbDomain.Term) = {
        Name            = term.Name
        ID              = term.Accession
        TooltipText     = term.Definition
        Status          = if term.IsObsolete then "obsolete" else ""
        StatusIsWarning = term.IsObsolete
    }

    static member ofOntology (ont:DbDomain.Ontology) = {
        Name            = ont.Name
        ID              = ont.CurrentVersion
        TooltipText     = ont.Definition
        Status          = ""
        StatusIsWarning = false
    }
        

type AutocompleteParameters = {
    StateBinding            : string
    Suggestions             : AutocompleteSuggestion []
    MaxItems                : int
    DropDownIsVisible       : bool
    DropDownIsLoading       : bool
    AlternativeSearchText   : string
    OnInputChangeMsg        : (string -> Msg)
    OnSuggestionSelect      : (AutocompleteSuggestion -> Msg)
    OnAlternativeSearch     : Msg
}
with
    static member ofTermSearchState (state:TermSearchState) = {
        StateBinding            = state.TermSearchText
        Suggestions             = state.TermSuggestions |> Array.map AutocompleteSuggestion.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowSuggestions
        DropDownIsLoading       = state.HasSuggestionsLoading
        AlternativeSearchText   = "Cant find the Term you are looking for?"
        OnInputChangeMsg        = (SearchTermTextChange >> TermSearch )
        OnSuggestionSelect      = (fun sugg -> sugg.Name |> TermSuggestionUsed |> TermSearch)
        OnAlternativeSearch     = DoNothing
    }

    static member ofAddBuildingBlockState (state:AddBuildingBlockState) = {
        StateBinding            = state.CurrentBuildingBlock.Name
        Suggestions             = state.BuildingBlockNameSuggestions |> Array.map AutocompleteSuggestion.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowBuildingBlockNameSuggestions
        DropDownIsLoading       = state.HasBuildingBlockNameSuggestionsLoading
        AlternativeSearchText   = "Cant find the Term you are looking for?"
        OnInputChangeMsg        = (BuildingBlockNameChange >> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> sugg.Name |> BuildingBlockNameSuggestionUsed |> AddBuildingBlock)
        OnAlternativeSearch     = DoNothing
    }

    static member ofAddBuildingBlockUnitState (state:AddBuildingBlockState) = {
        StateBinding            = state.UnitTermSearchText
        Suggestions             = state.UnitTermSuggestions |> Array.map AutocompleteSuggestion.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowUnitTermSuggestions
        DropDownIsLoading       = state.HasUnitTermSuggestionsLoading
        AlternativeSearchText   = "Can't find the unit you are looking for?"
        OnInputChangeMsg        = (SearchUnitTermTextChange >> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> sugg.Name |> UnitTermSuggestionUsed |> AddBuildingBlock)
        OnAlternativeSearch     = DoNothing
    }

let createAutocompleteSuggestions
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (autocompleteParams: AutocompleteParameters)
    =

    let suggestions = 
        if autocompleteParams.Suggestions.Length > 0 then
            autocompleteParams.Suggestions
            |> fun s -> s |> Array.take (if s.Length < autocompleteParams.MaxItems then s.Length else autocompleteParams.MaxItems)
            |> Array.map (fun sugg ->
                tr [
                    OnClick (fun _ -> sugg |> autocompleteParams.OnSuggestionSelect |> dispatch)
                    OnKeyDown (fun k -> if (int k.keyCode) = 13 then sugg |> autocompleteParams.OnSuggestionSelect |> dispatch)
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
            td [] [
                str autocompleteParams.AlternativeSearchText
                a [OnClick (fun _ -> autocompleteParams.OnAlternativeSearch |> dispatch)] [
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

let autocompleteSearchComponent 
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (inputPlaceholderText   : string)
    (inputSize              : ISize option)
    (autocompleteParams     : AutocompleteParameters)

    = 
    Control.div [Control.IsExpanded] [
        Input.input [   Input.Placeholder inputPlaceholderText
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