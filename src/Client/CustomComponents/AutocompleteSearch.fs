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

open Fable.Core.JsInterop

type AutocompleteParameters<'SearchResult> = {
    ModalId                 : string
    InputId                 : string

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
        ModalId                 = "TermSearch_ID"
        InputId                 = "TermSearchInput_ID"

        StateBinding            = state.TermSearchText
        Suggestions             = state.TermSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowSuggestions
        DropDownIsLoading       = state.HasSuggestionsLoading

        OnInputChangeMsg        = (SearchTermTextChange >> TermSearch )
        OnSuggestionSelect      = ( fun (term:DbDomain.Term) -> term |> TermSuggestionUsed |> TermSearch)

        HasAdvancedSearch       = true
        AdvancedSearchLinkText  = "Cant find the Term you are looking for?"
        OnAdvancedSearch        = (fun (term:DbDomain.Term) -> term |> TermSuggestionUsed |> TermSearch )
    }

    static member ofAddBuildingBlockUnitState (state:AddBuildingBlockState) : AutocompleteParameters<DbDomain.Term> = {
        ModalId                 = "UnitSearch_ID"
        InputId                 = "UnitSearchInput_ID"

        StateBinding            = state.UnitTermSearchText
        Suggestions             = state.UnitTermSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowUnitTermSuggestions
        DropDownIsLoading       = state.HasUnitTermSuggestionsLoading

        AdvancedSearchLinkText   = "Can't find the unit you are looking for?"
        OnInputChangeMsg        = (fun str -> SearchUnitTermTextChange (str, Unit1) |> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> (sugg, Unit1) |> UnitTermSuggestionUsed |> AddBuildingBlock)

        HasAdvancedSearch       = true
        OnAdvancedSearch        = (fun sugg -> (sugg, Unit1) |> UnitTermSuggestionUsed |> AddBuildingBlock)
    }

    static member ofAddBuildingBlockUnit2State (state:AddBuildingBlockState) : AutocompleteParameters<DbDomain.Term> = {
        ModalId                 = "Unit2Search_ID"
        InputId                 = "Unit2SearchInput_ID"

        StateBinding            = state.Unit2TermSearchText
        Suggestions             = state.Unit2TermSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowUnit2TermSuggestions
        DropDownIsLoading       = state.HasUnit2TermSuggestionsLoading

        AdvancedSearchLinkText   = "Can't find the unit you are looking for?"
        OnInputChangeMsg        = (fun str -> SearchUnitTermTextChange (str,Unit2) |> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> (sugg, Unit2) |> UnitTermSuggestionUsed |> AddBuildingBlock)

        HasAdvancedSearch       = true
        OnAdvancedSearch        = (fun sugg -> (sugg, Unit2) |> UnitTermSuggestionUsed |> AddBuildingBlock)
    }

    static member ofAddBuildingBlockState (state:AddBuildingBlockState) : AutocompleteParameters<DbDomain.Term> = {
        ModalId                 = "BlockNameSearch_ID"
        InputId                 = "BlockNameSearchInput_ID"

        StateBinding            = state.CurrentBuildingBlock.Name
        Suggestions             = state.BuildingBlockNameSuggestions |> Array.map AutocompleteSuggestion<DbDomain.Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowBuildingBlockTermSuggestions
        DropDownIsLoading       = state.HasBuildingBlockTermSuggestionsLoading

        OnInputChangeMsg        = (BuildingBlockNameChange >> AddBuildingBlock)
        OnSuggestionSelect      = (fun sugg -> sugg |> BuildingBlockNameSuggestionUsed |> AddBuildingBlock)

        HasAdvancedSearch       = true
        AdvancedSearchLinkText   = "Cant find the Term you are looking for?"
        OnAdvancedSearch        = (fun sugg -> sugg |> BuildingBlockNameSuggestionUsed |> AddBuildingBlock)
    }



let createAutocompleteSuggestions
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (autocompleteParams: AutocompleteParameters<'SearchResult>)
    =

    let suggestions = 
        if autocompleteParams.Suggestions.Length > 0 then
            autocompleteParams.Suggestions
            //|> fun s -> s |> Array.take (if s.Length < autocompleteParams.MaxItems then s.Length else autocompleteParams.MaxItems)
            |> Array.map (fun sugg ->
                tr [
                    OnClick (fun _ ->
                        let e = Browser.Dom.document.getElementById(autocompleteParams.InputId)
                        e?value <- sugg.Name
                        sugg.Data |> autocompleteParams.OnSuggestionSelect |> dispatch)
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
                    td [
                        OnClick (
                            fun e ->
                                e.stopPropagation()
                        )
                        Style [FontWeight "light"]
                    ] [
                        small [] [
                            AdvancedSearch.createLinkOfAccession sugg.ID
                    ] ]
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
                a [OnClick (fun _ -> ToggleModal autocompleteParams.ModalId |> AdvancedSearch |> dispatch)] [
                    str "Use Advanced Search"
                ] 
            ]
        ]

    let alternative2 =
        tr [
            colorControl colorMode
            Class "suggestion"
        ][
            td [ColSpan 4] [
                str ("You can also request a term by opening an ")
                a [Href Shared.URLs.Nfdi4psoOntologyUrl; Target "_Blank"] [
                    str "Issue"
                ]
                str "."
            ]
        ]

    suggestions @ [alternative] @ [alternative2]



let autocompleteDropdownComponent (dispatch:Msg -> unit) (colorMode:ColorMode) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
    Container.container[ ] [
        Dropdown.content [Props [
            Style [
                if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                //if model.ShowFillSuggestions then Display DisplayOptions.Block else Display DisplayOptions.None
                ZIndex "20"
                Width "100%"
                MaxHeight "400px"
                Position PositionOptions.Absolute
                BackgroundColor colorMode.ControlBackground
                BorderColor     colorMode.ControlForeground
                MarginTop "-0.5rem"
                OverflowY OverflowOptions.Scroll
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

open Fable.Core.JsInterop

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
        AdvancedSearch.advancedSearchModal model autocompleteParams.ModalId autocompleteParams.InputId dispatch autocompleteParams.OnAdvancedSearch
        Input.input [
            Input.Props [Style [BorderColor ExcelColors.Colorfull.gray40]]
            Input.Disabled isDisabled
            Input.Placeholder inputPlaceholderText
            Input.ValueOrDefault autocompleteParams.StateBinding
            match inputSize with
            | Some size -> Input.Size size
            | _ -> ()
            Input.OnChange (
                fun e -> e.Value |> autocompleteParams.OnInputChangeMsg |> dispatch
            )
            Input.Id autocompleteParams.InputId  
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
            ] [str (
                sprintf "%A" (if model.TermSearchState.ParentOntology.IsSome then model.TermSearchState.ParentOntology.Value.Name else "") 
            )]
        ]

    Control.div [Control.IsExpanded] [
        AdvancedSearch.advancedSearchModal model autocompleteParams.ModalId autocompleteParams.InputId dispatch autocompleteParams.OnAdvancedSearch
        Field.div [Field.HasAddons][
            parentOntologyNotificationElement ((model.TermSearchState.ParentOntology.IsSome && model.TermSearchState.SearchByParentOntology) |> not)
            Control.p [Control.IsExpanded][
                Input.input [
                    Input.Props [Id autocompleteParams.InputId]
                    Input.Placeholder inputPlaceholderText
                    Input.ValueOrDefault autocompleteParams.StateBinding
                    match inputSize with
                    | Some size -> Input.Size size
                    | _ -> ()
                    Input.Props [
                        OnFocus (fun e ->
                            //GenericLog ("Info","FOCUSED!") |> Dev |> dispatch
                            GetParentTerm |> ExcelInterop |> dispatch
                            let el = Browser.Dom.document.getElementById autocompleteParams.InputId
                            el.focus()
                        )
                        OnDoubleClick (fun e ->
                            if model.TermSearchState.ParentOntology.IsSome && model.TermSearchState.TermSearchText = "" then
                                let parentOnt = model.TermSearchState.ParentOntology.Value
                                let (parentOntInfo:OntologyInfo) = { Name = parentOnt.Name; TermAccession = parentOnt.TermAccession }
                                GetAllTermsByParentTermRequest parentOntInfo |> TermSearch |> dispatch
                            else
                                let v = Browser.Dom.document.getElementById autocompleteParams.InputId
                                v?value |> autocompleteParams.OnInputChangeMsg |> dispatch
                        )
                    ]           
                    Input.OnChange (fun e -> e.Value |> autocompleteParams.OnInputChangeMsg |> dispatch)
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