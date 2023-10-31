module SidebarComponents.AutocompleteSearch

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.Bulma
open ExcelColors
open Api
open Model
open Messages
open Shared
open Shared.TermTypes


type AutocompleteSuggestion<'SearchResult> = {
    Name            : string
    ID              : string
    TooltipText     : string
    Status          : string
    StatusIsWarning : bool
    Data            : 'SearchResult 
}
with
    static member ofTerm (term:Term) : AutocompleteSuggestion<Term> = {
        Name            = term.Name
        ID              = term.Accession
        TooltipText     = term.Description
        Status          = if term.IsObsolete then "obsolete" else ""
        StatusIsWarning = term.IsObsolete
        Data            = term
    }

    static member ofOntology (ont:Ontology) : AutocompleteSuggestion<Ontology> = {
        Name            = ont.Name
        ID              = ont.Version
        TooltipText     = ""
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

    OnInputChangeMsg        : ((string*TermMinimal option) -> Msg)
    OnSuggestionSelect      : ('SearchResult -> Msg)

    HasAdvancedSearch       : bool
    AdvancedSearchLinkText  : string
    OnAdvancedSearch        : ('SearchResult -> Msg)
}
with
    static member ofTermSearchState (state:TermSearch.Model) : AutocompleteParameters<Term> = {
        ModalId                 = "TermSearch_ID"
        InputId                 = "TermSearchInput_ID"

        StateBinding            = state.TermSearchText
        Suggestions             = state.TermSuggestions |> Array.map AutocompleteSuggestion<Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowSuggestions
        DropDownIsLoading       = state.HasSuggestionsLoading

        OnInputChangeMsg        = (TermSearch.SearchTermTextChange >> TermSearchMsg )
        OnSuggestionSelect      = ( fun (term:Term) -> term |> TermSearch.TermSuggestionUsed |> TermSearchMsg)

        HasAdvancedSearch       = true
        AdvancedSearchLinkText  = "Cant find the Term you are looking for?"
        OnAdvancedSearch        = (fun (term:Term) -> term |> TermSearch.TermSuggestionUsed |> TermSearchMsg )
    }

    //static member ofAddBuildingBlockUnitState (state:BuildingBlock.Model) : AutocompleteParameters<Term> = {
    //    ModalId                 = "UnitSearch_ID"
    //    InputId                 = "UnitSearchInput_ID"

    //    StateBinding            = state.UnitTermSearchText
    //    Suggestions             = state.UnitTermSuggestions |> Array.map AutocompleteSuggestion<Term>.ofTerm
    //    MaxItems                = 5
    //    DropDownIsVisible       = state.ShowUnitTermSuggestions
    //    DropDownIsLoading       = state.HasUnitTermSuggestionsLoading

    //    AdvancedSearchLinkText   = "Can't find the unit you are looking for?"
    //    OnInputChangeMsg        = (fun (str,_) -> BuildingBlock.Msg.SearchUnitTermTextChange (str, Unit1) |> BuildingBlockMsg)
    //    OnSuggestionSelect      = (fun sugg -> (sugg, Unit1) |> BuildingBlock.Msg.UnitTermSuggestionUsed |> BuildingBlockMsg)

    //    HasAdvancedSearch       = true
    //    OnAdvancedSearch        = (fun sugg -> (sugg, Unit1) |> BuildingBlock.Msg.UnitTermSuggestionUsed |> BuildingBlockMsg)
    //}

    static member ofAddBuildingBlockUnit2State (state:BuildingBlock.Model) : AutocompleteParameters<Term> = {
        ModalId                 = "Unit2Search_ID"
        InputId                 = "Unit2SearchInput_ID"

        StateBinding            = state.Unit2TermSearchText
        Suggestions             = state.Unit2TermSuggestions |> Array.map AutocompleteSuggestion<Term>.ofTerm
        MaxItems                = 5
        DropDownIsVisible       = state.ShowUnit2TermSuggestions
        DropDownIsLoading       = state.HasUnit2TermSuggestionsLoading

        AdvancedSearchLinkText   = "Can't find the unit you are looking for?"
        OnInputChangeMsg        = (fun (str,_) -> BuildingBlock.Msg.SearchUnitTermTextChange (str) |> BuildingBlockMsg)
        OnSuggestionSelect      = (fun sugg -> sugg |> BuildingBlock.Msg.UnitTermSuggestionUsed |> BuildingBlockMsg)

        HasAdvancedSearch       = true
        OnAdvancedSearch        = (fun sugg -> sugg |> BuildingBlock.Msg.UnitTermSuggestionUsed |> BuildingBlockMsg)
    }

    //static member ofAddBuildingBlockState (state:BuildingBlock.Model) : AutocompleteParameters<Term> = {
    //    ModalId                 = "BlockNameSearch_ID"
    //    InputId                 = "BlockNameSearchInput_ID"

    //    StateBinding            = state.CurrentBuildingBlock.Name
    //    Suggestions             = state.BuildingBlockNameSuggestions |> Array.map AutocompleteSuggestion<Term>.ofTerm
    //    MaxItems                = 5
    //    DropDownIsVisible       = state.ShowBuildingBlockTermSuggestions
    //    DropDownIsLoading       = state.HasBuildingBlockTermSuggestionsLoading

    //    OnInputChangeMsg        = (fst >> BuildingBlock.Msg.BuildingBlockNameChange >> BuildingBlockMsg)
    //    OnSuggestionSelect      = (fun sugg -> sugg |> BuildingBlock.Msg.BuildingBlockNameSuggestionUsed |> BuildingBlockMsg)

    //    HasAdvancedSearch       = true
    //    AdvancedSearchLinkText   = "Cant find the Term you are looking for?"
    //    OnAdvancedSearch        = (fun sugg -> sugg |> BuildingBlock.Msg.BuildingBlockNameSuggestionUsed |> BuildingBlockMsg)
    //}



let createAutocompleteSuggestions
    (dispatch: Msg -> unit)
    (autocompleteParams: AutocompleteParameters<'SearchResult>)
    (model:Model)
    =

    let suggestions = 
        if autocompleteParams.Suggestions.Length > 0 then
            autocompleteParams.Suggestions
            |> Array.collect (fun sugg ->
                let id = sprintf "isHidden_%s" sugg.ID 
                [|
                    tr [
                        OnClick (fun _ ->
                            let e = Browser.Dom.document.getElementById(autocompleteParams.InputId)
                            e?value <- sugg.Name
                            sugg.Data |> autocompleteParams.OnSuggestionSelect |> dispatch)
                        OnKeyDown (fun k -> if k.key = "Enter" then sugg.Data |> autocompleteParams.OnSuggestionSelect |> dispatch)
                        TabIndex 0
                        Class "suggestion"
                    ] [
                        //td [
                        //    Class "has-tooltip-right has-tooltip-multiline"; Props.Custom ("data-tooltip", if sugg.TooltipText.Trim() <> "" then sugg.TooltipText else "No definition found")
                        //    Style [FontSize "1.1rem"; Padding "0 0 0 .4rem"; TextAlign TextAlignOptions.Center; VerticalAlign "middle"; Color NFDIColors.Yellow.Darker20]
                        //] [
                        //    Fa.i [Fa.Solid.InfoCircle] []
                        //]
                        td [] [
                            b [] [ str sugg.Name ]
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
                        td [] [
                            Button.list [Button.List.IsRight] [
                                Button.a [
                                    Button.Props [Title "Show Term Tree"]
                                    Button.Size IsSmall
                                    Button.Color IsSuccess
                                    Button.IsInverted
                                    Button.OnClick(fun e ->
                                        e.preventDefault()
                                        e.stopPropagation()
                                        Cytoscape.Msg.GetTermTree sugg.ID |> CytoscapeMsg |> dispatch
                                    )
                                ] [
                                    Icon.icon [] [
                                        Fa.i [Fa.Solid.Tree] []
                                    ]
                                ]
                                Button.a [
                                    Button.Size IsSmall
                                    Button.Color IsBlack
                                    Button.IsInverted
                                    Button.OnClick(fun e ->
                                        e.preventDefault()
                                        e.stopPropagation()
                                        let ele = Browser.Dom.document.getElementById(id)
                                        let isCollapsed =
                                            let vis = string ele?style?visibility
                                            vis = "collapse" || vis = ""
                                        if isCollapsed then 
                                            ele?style?visibility <- "visible"
                                        else
                                            ele?style?visibility <- "collapse"
                                        ()
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
                    ] [
                        td [ColSpan 4] [
                            Content.content [] [
                                b [] [ str "Definition: " ]
                                str (if sugg.TooltipText = "" then "No definition found" else sugg.TooltipText)
                            ]
                        ]
                    ]
                |]
            )
            |> List.ofArray
        else
            [
                tr [] [
                    td [] [str "No terms found matching your input."]
                ]
            ]

    let alternative =
        tr [
            Class "suggestion"
        ] [
            td [ColSpan 4] [
                str (sprintf "%s " autocompleteParams.AdvancedSearchLinkText)
                str "Try "
                a [OnClick (fun _ -> AdvancedSearch.ToggleModal autocompleteParams.ModalId |> AdvancedSearchMsg |> dispatch)] [
                    str "Advanced Search"
                ]
                str "!"
            ]
        ]

    let alternative2 =
        tr [
            Class "suggestion"
        ] [
            td [ColSpan 4] [
                str "Still can't find what you need? Get in "
                a [Href Shared.URLs.Helpdesk.UrlOntologyTopic; Target "_Blank"] [
                    str "contact"
                ]
                str " with us!"
            ]
        ]

    suggestions @ [alternative] @ [alternative2]



let autocompleteDropdownComponent (dispatch:Msg -> unit) (colorMode:ColorMode) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
    div [ Style [Position PositionOptions.Relative ]] [
        div [
            Style [
                if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                ZIndex "20"
                Width "100%"
                MaxHeight "400px"
                Position PositionOptions.Absolute
                BackgroundColor colorMode.ControlBackground
                BorderColor     colorMode.ControlForeground
                MarginTop "-0.5rem"
                OverflowY OverflowOptions.Auto
                BorderWidth "0 0.5px 0.5px 0.5px"
                BorderStyle "solid"
            ]
        ] [
            Table.table [Table.IsFullWidth; Table.Props [ colorControl colorMode ]] [
                if isLoading then
                    tbody [Style [Height "75px"]] [
                        tr [] [
                            td [Style [TextAlign TextAlignOptions.Center]] [
                                Modals.Loading.loadingComponent
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

let autocompleteTermSearchComponentInputComponent (dispatch: Msg -> unit) isDisabled inputPlaceholderText inputSize (autocompleteParams : AutocompleteParameters<Term>) =
    Control.p [Control.IsExpanded] [
        Input.input [
            Input.Props [Style [
                if isDisabled then BorderColor ExcelColors.Colorfull.gray40
            ]]
            Input.Disabled isDisabled
            Input.Placeholder inputPlaceholderText
            Input.ValueOrDefault autocompleteParams.StateBinding
            match inputSize with
            | Some size -> Input.Size size
            | _ -> ()
            Input.Props [
                OnDoubleClick (fun e ->
                    let v = Browser.Dom.document.getElementById autocompleteParams.InputId
                    v?value |> autocompleteParams.OnInputChangeMsg |> dispatch
                )
            ]      
            Input.OnChange (
                // ignore this "None". AutocompleteParameters is pure spagetthi and needs to be removed. Absolute dumpster fire. 
                fun e -> (e.Value, None) |> autocompleteParams.OnInputChangeMsg |> dispatch
            )
            Input.Id autocompleteParams.InputId  
        ]
    ]

let autocompleteTermSearchComponentOfParentOntology
    (dispatch: Msg -> unit)
    (colorMode:ColorMode)
    (model:Model)
    (inputPlaceholderText   : string)
    (inputSize              : IReactProperty option)
    (autocompleteParams     : AutocompleteParameters<Term>)

    =
    let parentOntologyNotificationElement (parentTerm: TermMinimal) =
        let parenTermText = parentTerm.Name
        Control.p [ Control.Props [Title parenTermText; Style [MaxWidth "40%"]]] [
            Button.button [
                Button.Props [Style [BackgroundColor ExcelColors.Colorfull.white]]
                Button.IsStatic true
                match inputSize with
                | Some size -> Button.Size size
                | _ -> ()
            ] [str parenTermText ]
        ]

    let useParentTerm =
        match model.PersistentStorageState.Host with
        | Swatehost.Excel _ -> model.TermSearchState.ParentOntology.IsSome 
        | Swatehost.Browser when not model.SpreadsheetModel.headerIsSelected ->
            let header = model.SpreadsheetModel.getSelectedColumnHeader
            match header with
            | Some h ->
                let termSelected = h.isTermColumn && h.Term.IsSome
                termSelected
            | None -> false
        | _ -> false

    let parentTerm =
        match model.PersistentStorageState.Host with
        | Swatehost.Excel _ -> model.TermSearchState.ParentOntology
        | Swatehost.Browser ->
            let header = model.SpreadsheetModel.getSelectedColumnHeader
            header
            |> Option.bind (fun header ->
                if header.isTermColumn then
                    header.Term
                elif header.isFeaturedColumn then
                    Some header.getFeaturedTerm
                else
                    None
            )
        | _ -> None

    // REF-Parent-Term:
    // `useParentTerm` is used to show if parent term should be used for search. `useParentTerm` can be false, altough `parentTerm`.IsSome.
    // Therefore we need to doublecheck `useParentTerm` to pass option.

    Control.div [] [
        AdvancedSearch.advancedSearchModal model autocompleteParams.ModalId autocompleteParams.InputId dispatch autocompleteParams.OnAdvancedSearch
        Field.div [Field.HasAddons] [
            if useParentTerm && model.TermSearchState.SearchByParentOntology then parentOntologyNotificationElement parentTerm.Value
            Control.p [Control.IsExpanded] [
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
                            match model.PersistentStorageState.Host with
                            | Swatehost.Excel _ ->
                                OfficeInterop.GetParentTerm |> OfficeInteropMsg |> dispatch
                                let el = Browser.Dom.document.getElementById autocompleteParams.InputId
                                el.focus()
                            | _ -> ()
                        )
                        OnDoubleClick (fun e ->
                            if useParentTerm && model.TermSearchState.TermSearchText = "" then
                                TermSearch.GetAllTermsByParentTermRequest parentTerm.Value |> TermSearchMsg |> dispatch
                            else
                                /// REF-Parent-Term
                                let parenTerm = if useParentTerm then parentTerm else None
                                let v = Browser.Dom.document.getElementById autocompleteParams.InputId
                                (v?value, parenTerm) |> autocompleteParams.OnInputChangeMsg |> dispatch
                        )
                    ]           
                    Input.OnChange (fun e ->
                        let x = "01101110 01101001 01100011 01100101 01011111 01110010 01100111 01100010".Split(" ") |> Array.map (fun x -> System.Convert.ToInt32(x, 2) |> char |> string) |> String.concat ""
                        if e.Value = x then
                            let c = { model.SiteStyleState.ColorMode with Name = model.SiteStyleState.ColorMode.Name + "_rgb"}
                            UpdateColorMode c |> Messages.StyleChange |> dispatch
                        /// REF-Parent-Term
                        let parenTerm = if useParentTerm then parentTerm else None
                        (e.Value, parenTerm) |> autocompleteParams.OnInputChangeMsg |> dispatch
                    )
                ]
            ]
        ]
        autocompleteDropdownComponent
            dispatch
            colorMode
            autocompleteParams.DropDownIsVisible
            autocompleteParams.DropDownIsLoading
            (createAutocompleteSuggestions dispatch autocompleteParams model)
    ]