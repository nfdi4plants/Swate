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
                    Standard.tr [
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
                            Bulma.buttons [
                                Bulma.buttons.isRight
                                prop.children [
                                    Bulma.button.a [
                                        prop.title "Show Term Tree"
                                        Bulma.button.isSmall
                                        Bulma.color.isSuccess
                                        Bulma.button.isInverted
                                        prop.onClick(fun e ->
                                            e.preventDefault()
                                            e.stopPropagation()
                                            Cytoscape.Msg.GetTermTree sugg.ID |> CytoscapeMsg |> dispatch
                                        )
                                        Bulma.icon [
                                            Html.i [prop.className "fa-solid fa-tree"] 
                                        ] |> prop.children
                                    ]
                                    Bulma.button.a [
                                        Bulma.button.isSmall
                                        Bulma.color.isBlack
                                        Bulma.button.isInverted
                                        prop.onClick(fun e ->
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
                                        Bulma.icon [
                                            Html.i [prop.className "fa-solid fa-chevron-down"]
                                        ] |> prop.children
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Standard.tr [
                        OnClick (fun e -> e.stopPropagation())
                        Id id
                        Class "suggestion-details"
                    ] [
                        td [ColSpan 4] [
                            Bulma.content [
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
                Standard.tr [] [
                    td [] [str "No terms found matching your input."]
                ]
            ]

    let alternative =
        Standard.tr [
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
        Standard.tr [
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



let autocompleteDropdownComponent (dispatch:Msg -> unit) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
    div [ Style [Position PositionOptions.Relative ]] [
        div [
            Style [
                if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                ZIndex "20"
                Width "100%"
                MaxHeight "400px"
                Position PositionOptions.Absolute
                MarginTop "-0.5rem"
                OverflowY OverflowOptions.Auto
                BorderWidth "0 0.5px 0.5px 0.5px"
                BorderStyle "solid"
            ]
        ] [
            Bulma.table [
                Bulma.table.isFullWidth
                if isLoading then
                    tbody [Style [Height "75px"]] [
                        Standard.tr [] [
                            td [Style [TextAlign TextAlignOptions.Center]] [
                                Modals.Loading.loadingComponent
                                br []
                            ]
                        ]
                    ]
                    |> prop.children
                else
                    tbody [] suggestions
                    |> prop.children
            ]

            
        ]
    ]

open Fable.Core.JsInterop

let autocompleteTermSearchComponentInputComponent (dispatch: Msg -> unit) isDisabled inputPlaceholderText inputSize (autocompleteParams : AutocompleteParameters<Term>) =
    Bulma.control.p [
        Bulma.control.isExpanded
        Bulma.input.text [
            prop.style [
                if isDisabled then style.borderColor ExcelColors.Colorfull.gray40
            ]
            prop.disabled isDisabled
            prop.placeholder inputPlaceholderText
            prop.valueOrDefault autocompleteParams.StateBinding
            match inputSize with
            | Some size -> size
            | _ -> ()
            prop.onDoubleClick (fun e ->
                let v = Browser.Dom.document.getElementById autocompleteParams.InputId
                v?value |> autocompleteParams.OnInputChangeMsg |> dispatch
            ) 
            prop.onChange (
                // ignore this "None". AutocompleteParameters is pure spagetthi and needs to be removed. Absolute dumpster fire. 
                fun (e: string) -> (e, None) |> autocompleteParams.OnInputChangeMsg |> dispatch
            )
            prop.id autocompleteParams.InputId  
        ]
        |> prop.children
    ]

let autocompleteTermSearchComponentOfParentOntology
    (dispatch: Msg -> unit)
    (model:Model)
    (inputPlaceholderText   : string)
    (inputSize              : IReactProperty option)
    (autocompleteParams     : AutocompleteParameters<Term>)

    =
    let parentOntologyNotificationElement (parentTerm: TermMinimal) =
        let parenTermText = parentTerm.Name
        Bulma.control.p [
            prop.style [style.maxWidth (length.perc 40)]
            prop.title parenTermText
            Bulma.button.button [
                prop.style [style.backgroundColor ExcelColors.Colorfull.white]
                Bulma.button.isStatic
                match inputSize with
                | Some size -> size
                | _ -> ()
                prop.text parenTermText 
            ]
            |> prop.children
        ]

    let useParentTerm =
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel -> model.TermSearchState.ParentOntology.IsSome 
        | Some _ when not model.SpreadsheetModel.headerIsSelected ->
            let header = model.SpreadsheetModel.getSelectedColumnHeader
            match header with
            | Some h ->
                let termSelected = h.IsTermColumn
                termSelected
            | None -> false
        | _ -> false

    let parentTerm =
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel -> model.TermSearchState.ParentOntology
        | Some _ ->
            let header = model.SpreadsheetModel.getSelectedColumnHeader
            header
            |> Option.bind (fun header ->
                if header.IsTermColumn then
                    header.ToTerm()
                    |> TermMinimal.fromOntologyAnnotation
                    |> Some
                else
                    None
            )
        | None -> None

    // REF-Parent-Term:
    // `useParentTerm` is used to show if parent term should be used for search. `useParentTerm` can be false, altough `parentTerm`.IsSome.
    // Therefore we need to doublecheck `useParentTerm` to pass option.

    Bulma.control.div [
        AdvancedSearch.advancedSearchModal model autocompleteParams.ModalId autocompleteParams.InputId dispatch autocompleteParams.OnAdvancedSearch
        Bulma.field.div [
            Bulma.field.hasAddons
            prop.children [
                if useParentTerm && model.TermSearchState.SearchByParentOntology then parentOntologyNotificationElement parentTerm.Value
                Bulma.control.p [
                    Bulma.control.isExpanded
                    Bulma.input.text [
                        prop.id autocompleteParams.InputId
                        prop.placeholder inputPlaceholderText
                        prop.valueOrDefault autocompleteParams.StateBinding
                        match inputSize with
                        | Some size -> size
                        | _ -> ()
                        prop.onFocus (fun e ->
                            //GenericLog ("Info","FOCUSED!") |> Dev |> dispatch
                            match model.PersistentStorageState.Host with
                            | Some Swatehost.Excel ->
                                OfficeInterop.GetParentTerm |> OfficeInteropMsg |> dispatch
                                let el = Browser.Dom.document.getElementById autocompleteParams.InputId
                                el.focus()
                            | _ -> ()
                        )
                        prop.onDoubleClick (fun e ->
                            if useParentTerm && model.TermSearchState.TermSearchText = "" then
                                TermSearch.GetAllTermsByParentTermRequest parentTerm.Value |> TermSearchMsg |> dispatch
                            else
                                /// REF-Parent-Term
                                let parenTerm = if useParentTerm then parentTerm else None
                                let v = Browser.Dom.document.getElementById autocompleteParams.InputId
                                (v?value, parenTerm) |> autocompleteParams.OnInputChangeMsg |> dispatch
                        )      
                        prop.onChange (fun (e:string) ->
                            /// REF-Parent-Term
                            let parenTerm = if useParentTerm then parentTerm else None
                            (e, parenTerm) |> autocompleteParams.OnInputChangeMsg |> dispatch
                        )
                    ]
                    |> prop.children
                ]
            ]
        ]
        autocompleteDropdownComponent
            dispatch
            autocompleteParams.DropDownIsVisible
            autocompleteParams.DropDownIsLoading
            (createAutocompleteSuggestions dispatch autocompleteParams model)
    ]