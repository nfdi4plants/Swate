module BuildingBlock.Core

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop

open ExcelColors
open Model
open Messages
open Messages.BuildingBlock
open Shared
open TermTypes
open CustomComponents

open OfficeInteropTypes
open Elmish

let update (addBuildingBlockMsg:BuildingBlock.Msg) (state: BuildingBlock.Model) : BuildingBlock.Model * Cmd<Messages.Msg> =
    match addBuildingBlockMsg with
    | UpdateHeaderSearchText str ->
        let nextState = {state with HeaderSearchText = str}
        nextState, Cmd.none
    | GetHeaderSuggestions (queryString,uiSetter) ->
        let cmd = 
            Cmd.OfAsync.either
                Api.api.getTermSuggestions
                {|n= 5; query = queryString; ontology = None|}
                (fun t -> GetHeaderSuggestionsResponse (t,uiSetter) |> BuildingBlockMsg)
                (curry GenericError Cmd.none >> DevMsg)
        state, cmd
    | GetHeaderSuggestionsResponse (termSuggestions, uiSetter) ->
        let nextState = { state with HeaderSearchResults = termSuggestions }
        let state, setState = uiSetter.state, uiSetter.setState
        setState {state with SearchIsLoading = false; SearchIsActive = true}
        nextState, Cmd.none
    | SelectHeaderTerm term ->
        let nextState = { state with HeaderSelectedTerm = term; HeaderSearchText = if term.IsSome then term.Value.Name else state.HeaderSearchText }
        nextState, Cmd.none
    | UpdateBodySearchText str ->
        let nextState = {state with BodySearchText = str}
        nextState, Cmd.none
    | GetBodySuggestions (queryString,uiSetter) ->
        let cmd = 
            Cmd.OfAsync.either
                Api.api.getTermSuggestions
                {|n= 5; query = queryString; ontology = None|}
                (fun t -> GetBodySuggestionsResponse (t,uiSetter) |> BuildingBlockMsg)
                (curry GenericError Cmd.none >> DevMsg)
        state, cmd
    | GetBodySuggestionsByParent (queryString,parentTerm,uiSetter) ->
        let cmd = 
            Cmd.OfAsync.either
                Api.api.getTermSuggestionsByParentTerm
                {|n= 5; query = queryString; parent_term = parentTerm|}
                (fun t -> GetBodySuggestionsResponse (t,uiSetter) |> BuildingBlockMsg)
                (curry GenericError Cmd.none >> DevMsg)
        state, cmd
    | GetBodyTermsByParent (parentTerm,uiSetter) ->
        let cmd = 
            Cmd.OfAsync.either
                Api.api.getAllTermsByParentTerm
                parentTerm
                (fun t -> GetBodySuggestionsResponse (t,uiSetter) |> BuildingBlockMsg)
                (curry GenericError Cmd.none >> DevMsg)
        state, cmd
    | GetBodySuggestionsResponse (termSuggestions, uiSetter) ->
        let nextState = { state with BodySearchResults = termSuggestions }
        let state, setState = uiSetter.state, uiSetter.setState
        setState {state with SearchIsLoading = false; SearchIsActive = true}
        nextState, Cmd.none
    | SelectBodyTerm term ->
        let nextState = { state with BodySelectedTerm = term}
        nextState, Cmd.none

    | SearchUnitTermTextChange (newTerm) ->

        let triggerNewSearch =
            newTerm.Length > 2
       
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewUnitTermSuggestions",
            (
                if triggerNewSearch then
                    (newTerm) |> (GetNewUnitTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState = {
            state with
                Unit2TermSearchText                  = newTerm
                Unit2SelectedTerm                    = None
                ShowUnit2TermSuggestions             = triggerNewSearch
                HasUnit2TermSuggestionsLoading       = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewUnitTermSuggestions suggestions ->

        let nextState = {
            state with
                Unit2TermSuggestions             = suggestions
                ShowUnit2TermSuggestions         = true
                HasUnit2TermSuggestionsLoading   = false
        }

        nextState,Cmd.none

    | UnitTermSuggestionUsed suggestion ->
        let nextState ={
            state with
                Unit2TermSearchText             = suggestion.Name
                Unit2SelectedTerm               = Some suggestion
                ShowUnit2TermSuggestions        = false
                HasUnit2TermSuggestionsLoading  = false
        }
        nextState, Cmd.none

//let addBuildingBlockFooterComponent (model:Model) (dispatch:Messages.Msg -> unit) =
//    Content.content [ ] [
//        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ 
//            str (sprintf "More about %s:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString ))
//        ]
//        Text.p [Props [Style [TextAlign TextAlignOptions.Justify]]] [
//            span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type.toLongExplanation |> str]
//            span [] [str " You can find more information on our "]
//            a [Href Shared.URLs.AnnotationPrinciplesUrl; Target "_blank"] [str "website"]
//            span [] [str "."]
//        ]
//    ]

open SidebarComponents

//let addBuildingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
//    let autocompleteParamsTerm = AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockState model.AddBuildingBlockState
//    let autocompleteParamsUnit = AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState

//    mainFunctionContainer [
//        AdvancedSearch.advancedSearchModal model autocompleteParamsTerm.ModalId autocompleteParamsTerm.InputId dispatch autocompleteParamsTerm.OnAdvancedSearch
//        AdvancedSearch.advancedSearchModal model autocompleteParamsUnit.ModalId autocompleteParamsUnit.InputId dispatch autocompleteParamsUnit.OnAdvancedSearch
//        Field.div [] [
//            let autocompleteParams = (AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockState model.AddBuildingBlockState)
//            Field.div [Field.HasAddons] [
//                 Choose building block type dropdown element
//                Control.p [] [
//                    Dropdown.dropdown [
//                        Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection
//                    ] [
//                        Dropdown.trigger [] [
//                            Button.a [Button.OnClick (fun e -> e.stopPropagation(); ToggleSelectionDropdown |> BuildingBlockMsg |> dispatch)] [
//                                span [Style [MarginRight "5px"]] [str model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString]
//                                Fa.i [Fa.Solid.AngleDown] []
//                            ]
//                        ]
//                        Dropdown.menu [ ] [
//                            match model.AddBuildingBlockState.DropdownPage with
//                            | Model.BuildingBlock.DropdownPage.Main ->
//                                Helper.DropdownElements.dropdownContentMain model dispatch
//                            | Model.BuildingBlock.DropdownPage.ProtocolTypes ->
//                                Helper.DropdownElements.dropdownContentProtocolTypeColumns model dispatch
//                            | Model.BuildingBlock.DropdownPage.Output ->
//                                Helper.DropdownElements.dropdownContentOutputColumns model dispatch
//                            |> fun content -> Dropdown.content [Props [Style [yield! colorControlInArray model.SiteStyleState.ColorMode]] ] content
//                        ]
//                    ]
//                ]
//                 Ontology Term search field
//                if model.AddBuildingBlockState.CurrentBuildingBlock.Type.isTermColumn && model.AddBuildingBlockState.CurrentBuildingBlock.Type.isFeaturedColumn |> not then
//                    AutocompleteSearch.autocompleteTermSearchComponentInputComponent
//                        dispatch
//                        false // isDisabled
//                        "Start typing to search"
//                        None // No input size specified
//                        autocompleteParams

//            ]
//             Ontology Term search preview
//            AutocompleteSearch.autocompleteDropdownComponent
//                dispatch
//                model.SiteStyleState.ColorMode
//                autocompleteParams.DropDownIsVisible
//                autocompleteParams.DropDownIsLoading
//                (AutocompleteSearch.createAutocompleteSuggestions dispatch autocompleteParams model)
//        ]
//         Ontology Unit Term search field
//        if model.AddBuildingBlockState.CurrentBuildingBlock.Type.isTermColumn && model.AddBuildingBlockState.CurrentBuildingBlock.Type.isFeaturedColumn |> not then
//            let unitAutoCompleteParams = AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState
//            Field.div [] [
//                Field.div [Field.HasAddons] [
//                    Control.p [] [
//                        Button.a [
//                            Button.Props [Style [
//                                if model.AddBuildingBlockState.BuildingBlockHasUnit then Color NFDIColors.Mint.Base else Color NFDIColors.Red.Base
//                            ]]
//                            Button.OnClick (fun _ ->
//                                let inputId = (AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).InputId
//                                if model.AddBuildingBlockState.BuildingBlockHasUnit = true then
//                                    let e = Browser.Dom.document.getElementById inputId
//                                    e?value <- null
//                                ToggleBuildingBlockHasUnit |> BuildingBlockMsg |> dispatch
//                            )
//                        ] [
//                            Fa.i [
//                                Fa.Size Fa.FaLarge;
//                                Fa.Props [Style [AlignSelf AlignSelfOptions.Center; Transform "translateY(1px)"]]
//                                if model.AddBuildingBlockState.BuildingBlockHasUnit then
//                                    Fa.Solid.Check
//                                else
//                                    Fa.Solid.Ban
//                            ] [ ]
//                        ]
//                    ]
//                    Control.p [] [
//                        Button.button [Button.IsStatic true; Button.Props [Style [BackgroundColor ExcelColors.Colorfull.white]]] [
//                            str (sprintf "This %s has a unit:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString))
//                        ]
//                    ]
//                    AutocompleteSearch.autocompleteTermSearchComponentInputComponent
//                        dispatch
//                         if BuildingBlockHasUnit = false then disabled = true
//                        (model.AddBuildingBlockState.BuildingBlockHasUnit |> not) 
//                        "Start typing to search"
//                        None // No input size specified
//                        unitAutoCompleteParams
//                ]
//                 Ontology Unit Term search preview
//                AutocompleteSearch.autocompleteDropdownComponent
//                    dispatch
//                    model.SiteStyleState.ColorMode
//                    unitAutoCompleteParams.DropDownIsVisible
//                    unitAutoCompleteParams.DropDownIsLoading
//                    (AutocompleteSearch.createAutocompleteSuggestions dispatch unitAutoCompleteParams model)
//            ]

//            div [] [
//                Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
//                    a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockState model.AddBuildingBlockState).ModalId |> AdvancedSearchMsg |> dispatch)] [
//                        str "Use advanced search building block"
//                    ]
//                ]
//                if model.AddBuildingBlockState.CurrentBuildingBlock.Type.isTermColumn then
//                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
//                        a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).ModalId |> AdvancedSearchMsg |> dispatch)] [
//                            str "Use advanced search unit"
//                        ]
//                    ]
//            ]

//        Field.div [] [
//            Button.button   [
//                let isValid = model.AddBuildingBlockState.CurrentBuildingBlock |> Helper.isValidBuildingBlock
//                if isValid then
//                    Button.Color Color.IsSuccess
//                    Button.IsActive true
//                else
//                    Button.Color Color.IsDanger
//                    Button.Props [Disabled true]
//                Button.IsFullWidth
//                Button.OnClick (fun e ->
//                    let colName     = model.AddBuildingBlockState.CurrentBuildingBlock
//                    let colTerm     =
//                        if colName.isFeaturedColumn then
//                            TermMinimal.create colName.Type.toString colName.Type.getFeaturedColumnAccession |> Some
//                        elif model.AddBuildingBlockState.BuildingBlockSelectedTerm.IsSome && not colName.isSingleColumn then
//                            TermMinimal.ofTerm model.AddBuildingBlockState.BuildingBlockSelectedTerm.Value |> Some
//                        else
//                            None
//                    let unitTerm    = if model.AddBuildingBlockState.UnitSelectedTerm.IsSome && colName.isTermColumn && not colName.isFeaturedColumn then TermMinimal.ofTerm model.AddBuildingBlockState.UnitSelectedTerm.Value |> Some else None
//                    let newBuildingBlock = InsertBuildingBlock.create colName colTerm unitTerm Array.empty
//                    SpreadsheetInterface.AddAnnotationBlock newBuildingBlock |> InterfaceMsg |> dispatch
//                )
//            ] [
//                str "Add building block"
//            ]
//        ]
//    ]

open Feliz
open Feliz.Bulma

let addUnitToExistingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
    /// advanced unit term search 2
    let autocompleteParamsUnit2 = AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState
    mainFunctionContainer [
        // advanced unit term search 2
        AdvancedSearch.advancedSearchModal model autocompleteParamsUnit2.ModalId autocompleteParamsUnit2.InputId dispatch autocompleteParamsUnit2.OnAdvancedSearch
        Bulma.field.div [
            Bulma.help [
                b [] [str "Adds a unit to a complete building block." ]
                str " If the building block already has a unit assigned, the new unit is only applied to selected rows of the selected column."
            ]
        ]
        Bulma.field.div [
            let changeUnitAutoCompleteParams = AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState
            Bulma.field.div [
                Bulma.field.hasAddons
                prop.children [
                    Bulma.control.p [
                        Bulma.button.button [
                            Bulma.button.isStatic
                            Bulma.color.hasBackgroundWhite
                            prop.text "Add unit"
                        ]
                    ]
                    // Add/Update unit ontology term search field
                    AutocompleteSearch.autocompleteTermSearchComponentInputComponent
                        dispatch
                        false // isDisabled
                        "Start typing to search"
                        None // No input size specified
                        changeUnitAutoCompleteParams
                ]
            ]
            // Add/Update Ontology Unit Term search preview
            AutocompleteSearch.autocompleteDropdownComponent
                dispatch
                changeUnitAutoCompleteParams.DropDownIsVisible
                changeUnitAutoCompleteParams.DropDownIsLoading
                (AutocompleteSearch.createAutocompleteSuggestions dispatch changeUnitAutoCompleteParams model)

        ]
        Bulma.help [
            prop.style [style.display.inlineElement]
            prop.children [
                Html.a [
                    prop.onClick(fun e ->
                        e.preventDefault()
                        AdvancedSearch.ToggleModal (
                            AutocompleteSearch.AutocompleteParameters<Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState).ModalId
                            |> AdvancedSearchMsg
                            |> dispatch
                    )
                    prop.text "Use advanced search"
                ]
            ]
        ]

        Bulma.field.div [
            Bulma.button.button [
                
                let isValid = model.AddBuildingBlockState.Unit2TermSearchText <> ""
                Bulma.color.isSuccess
                if isValid then
                    Bulma.button.isActive
                else
                    Bulma.color.isDanger
                    prop.disabled true
                Bulma.button.isFullWidth
                prop.onClick (fun _ ->
                    let unitTerm =
                        if model.AddBuildingBlockState.Unit2SelectedTerm.IsSome then Some <| TermMinimal.ofTerm model.AddBuildingBlockState.Unit2SelectedTerm.Value else None
                    match model.AddBuildingBlockState.Unit2TermSearchText with
                    | "" ->
                        curry GenericLog Cmd.none ("Error", "Cannot execute function with empty unit input") |> DevMsg |> dispatch
                    | hasUnitTerm when model.AddBuildingBlockState.Unit2SelectedTerm.IsSome ->
                        OfficeInterop.UpdateUnitForCells unitTerm.Value |> OfficeInteropMsg |> dispatch
                    | freeText ->
                        OfficeInterop.UpdateUnitForCells (TermMinimal.create model.AddBuildingBlockState.Unit2TermSearchText "") |> OfficeInteropMsg |> dispatch
                )
                prop.text "Update unit for cells"
            ]
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Messages.Msg -> unit) =
    div [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Bulma.label "Building Blocks"

        // Input forms, etc related to add building block.
        Bulma.label "Add annotation building blocks (columns) to the annotation table."
        //match model.PersistentStorageState.Host with
        //| Swatehost.Excel _ ->
        //    addBuildingBlockElements model dispatch
        //| _ ->
        //    ()
        SearchComponent.Main model dispatch

        match model.PersistentStorageState.Host with
        | Swatehost.Excel _ ->
            Bulma.label "Add/Update unit reference to existing building block."
            // Input forms, etc related to add unit to existing building block.
            addUnitToExistingBlockElements model dispatch
        | _ -> ()
    ]