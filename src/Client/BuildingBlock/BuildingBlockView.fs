module BuildingBlock

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core
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

let update (addBuildingBlockMsg:BuildingBlock.Msg) (currentState: BuildingBlock.Model) : BuildingBlock.Model * Cmd<Messages.Msg> =
    match addBuildingBlockMsg with
    | NewBuildingBlockSelected nextBB ->
        let nextState = {
            currentState with
                CurrentBuildingBlock = if not nextBB.isSingleColumn && currentState.BuildingBlockSelectedTerm.IsSome then {nextBB with Name = currentState.BuildingBlockSelectedTerm.Value.Name} else nextBB
                ShowBuildingBlockSelection = false
        }
        nextState,Cmd.none

    | ToggleSelectionDropdown ->
        let nextState = {
            currentState with
                ShowBuildingBlockSelection = not currentState.ShowBuildingBlockSelection
        }

        nextState,Cmd.none

    | SearchUnitTermTextChange (newTerm,relUnit) ->

        let triggerNewSearch =
            newTerm.Length > 2
       
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewUnitTermSuggestions",
            (
                if triggerNewSearch then
                    (newTerm,relUnit) |> (GetNewUnitTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                    UnitTermSearchText                  = newTerm
                    UnitSelectedTerm                    = None
                    ShowUnitTermSuggestions             = triggerNewSearch
                    HasUnitTermSuggestionsLoading       = true
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSearchText                  = newTerm
                    Unit2SelectedTerm                    = None
                    ShowUnit2TermSuggestions             = triggerNewSearch
                    HasUnit2TermSuggestionsLoading       = true
                }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewUnitTermSuggestions (suggestions,relUnit) ->

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                        UnitTermSuggestions             = suggestions
                        ShowUnitTermSuggestions         = true
                        HasUnitTermSuggestionsLoading   = false
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSuggestions             = suggestions
                    ShowUnit2TermSuggestions         = true
                    HasUnit2TermSuggestionsLoading   = false
                }

        nextState,Cmd.none

    | UnitTermSuggestionUsed (suggestion, relUnit) ->

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                    UnitTermSearchText              = suggestion.Name
                    UnitSelectedTerm                = Some suggestion
                    ShowUnitTermSuggestions         = false
                    HasUnitTermSuggestionsLoading   = false
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSearchText             = suggestion.Name
                    Unit2SelectedTerm               = Some suggestion
                    ShowUnit2TermSuggestions        = false
                    HasUnit2TermSuggestionsLoading  = false
                }
        nextState, Cmd.none

    | BuildingBlockNameChange newName ->

        let triggerNewSearch =
            newName.Length > 2
   
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewBuildingBlockNameTermSuggestions",
            (
                if triggerNewSearch then
                    newName  |> (GetNewBuildingBlockNameSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextBB = {
            currentState.CurrentBuildingBlock with
                Name = newName
        }

        let nextState = {
            currentState with
                CurrentBuildingBlock                    = nextBB
                BuildingBlockSelectedTerm               = None
                ShowBuildingBlockTermSuggestions        = triggerNewSearch
                HasBuildingBlockTermSuggestionsLoading  = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewBuildingBlockNameSuggestions suggestions ->

        let nextState = {
            currentState with
                BuildingBlockNameSuggestions            = suggestions
                ShowBuildingBlockTermSuggestions        = true
                HasBuildingBlockTermSuggestionsLoading  = false
        }

        nextState,Cmd.none

    | BuildingBlockNameSuggestionUsed suggestion ->
        
        let nextBB = {
            currentState.CurrentBuildingBlock with
                Name = suggestion.Name
        }

        let nextState = {
            currentState with
                CurrentBuildingBlock                    = nextBB

                BuildingBlockSelectedTerm               = Some suggestion
                ShowBuildingBlockTermSuggestions        = false
                HasBuildingBlockTermSuggestionsLoading  = false
        }
        nextState, Cmd.none

    | ToggleBuildingBlockHasUnit ->

        let hasUnit = not currentState.BuildingBlockHasUnit

        let nextState =
            if hasUnit then
                {
                    currentState with
                        BuildingBlockHasUnit = hasUnit
                }
            else
                {
                currentState with
                    BuildingBlockHasUnit = hasUnit
                    UnitSelectedTerm = None
                    UnitTermSearchText = ""
                    UnitTermSuggestions = [||]
                    ShowUnitTermSuggestions = false
                    HasUnitTermSuggestionsLoading = false
                }
        nextState, Cmd.none

let isValidBuildingBlock (block : BuildingBlockNamePrePrint) =
    match block.Type with
    | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
        block.Name.Length > 0
    | BuildingBlockType.Sample | BuildingBlockType.Data | BuildingBlockType.Source ->
        true

//let createUnitTermSuggestions (model:Model) (dispatch: Msg -> unit) =
//    if model.AddBuildingBlockState.UnitTermSuggestions.Length > 0 then
//        model.AddBuildingBlockState.UnitTermSuggestions
//        |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
//        |> Array.map (fun sugg ->
//            tr [OnClick (fun _ -> sugg |> UnitTermSuggestionUsed |> AddBuildingBlock |> dispatch)
//                colorControl model.SiteStyleState.ColorMode
//                Class "suggestion"
//            ] [
//                td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
//                    Fa.i [Fa.Solid.InfoCircle] []
//                ]
//                td [] [
//                    b [] [str sugg.Name]
//                ]
//                td [Style [Color ""]] [if sugg.IsObsolete then str "obsolete"]
//                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
//            ])
//        |> List.ofArray
//    else
//        [
//            tr [] [
//                td [] [str "No terms found matching your input."]
//            ]
//        ]


let createBuildingBlockDropdownItem (model:Model) (dispatch:Messages.Msg -> unit) (block: BuildingBlockType )  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun e ->
                e.stopPropagation()
                BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> BuildingBlockMsg |> dispatch
            )
            OnKeyDown (fun k -> if (int k.which) = 13 then BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> BuildingBlockMsg |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip (block |> BuildingBlockType.toShortExplanation)
                Style [PaddingRight "10px"]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]
        
        Text.span [] [str block.toString]
    ]

let addBuildingBlockFooterComponent (model:Model) (dispatch:Messages.Msg -> unit) =
    Content.content [ ] [
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ 
            str (sprintf "More about %s:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString ))
        ]
        Text.p [Props [Style [TextAlign TextAlignOptions.Justify]]][
            span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type |> BuildingBlockType.toLongExplanation |> str]
            span [] [str " You can find more information on our "]
            a [Href Shared.URLs.AnnotationPrinciplesUrl; Target "_blank"][str "website"]
            span [][str "."]
        ]
    ]

let addBuildingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [
            Field.HasAddons
        ] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection
                ] [
                    Dropdown.trigger [] [
                        Button.a [Button.OnClick (fun e -> e.stopPropagation(); ToggleSelectionDropdown |> BuildingBlockMsg |> dispatch)] [
                            span [Style [MarginRight "5px"]] [str model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [ ] [
                        Dropdown.content [Props [colorControl model.SiteStyleState.ColorMode]] [
                            BuildingBlockType.Source            |> createBuildingBlockDropdownItem model dispatch
                            Dropdown.divider []
                            BuildingBlockType.Parameter         |> createBuildingBlockDropdownItem model dispatch
                            BuildingBlockType.Factor            |> createBuildingBlockDropdownItem model dispatch
                            BuildingBlockType.Characteristics   |> createBuildingBlockDropdownItem model dispatch
                            Dropdown.divider []
                            BuildingBlockType.Sample            |> createBuildingBlockDropdownItem model dispatch
                            BuildingBlockType.Data              |> createBuildingBlockDropdownItem model dispatch
                            Dropdown.Item.div [
                                Dropdown.Item.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Right)]
                            ][
                                a [ Href Shared.URLs.AnnotationPrinciplesUrl; Target "_Blank" ] [ str "more" ]
                            ]
                        ] 
                    ]
                ]
            ]
            Control.div [Control.IsExpanded] [
                match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
                    AutocompleteSearch.autocompleteTermSearchComponent
                        dispatch
                        model.SiteStyleState.ColorMode
                        model
                        "Start typing to search"
                        None
                        (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockState model.AddBuildingBlockState)
                        false
                | _ -> ()
            ]
        ]
        match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
        | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
            Field.div [Field.HasAddons] [
                Control.div [] [
                    Button.a [
                        Button.Props [Style [
                            if model.AddBuildingBlockState.BuildingBlockHasUnit then Color NFDIColors.Mint.Base else Color NFDIColors.Red.Base
                        ]]
                        Button.OnClick (fun _ ->
                            let inputId = (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).InputId
                            if model.AddBuildingBlockState.BuildingBlockHasUnit = true then
                                let e = Browser.Dom.document.getElementById inputId
                                e?value <- null
                            ToggleBuildingBlockHasUnit |> BuildingBlockMsg |> dispatch
                        )
                    ] [
                        Fa.i [
                            Fa.Size Fa.FaLarge;
                            Fa.Props [Style [AlignSelf AlignSelfOptions.Center; Transform "translateY(1px)"]]
                            if model.AddBuildingBlockState.BuildingBlockHasUnit then
                                Fa.Solid.Check
                            else
                                Fa.Solid.Ban
                        ][ ]
                    ]
                ]
                Control.p [] [
                    Button.button [Button.IsStatic true; Button.Props [Style [BackgroundColor ExcelColors.Colorfull.white]]] [
                        str (sprintf "This %s has a unit:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString))
                    ]
                ]
                AutocompleteSearch.autocompleteTermSearchComponent
                    dispatch
                    model.SiteStyleState.ColorMode
                    model
                    "Start typing to search"
                    None
                    (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState)
                    // if BuildingBlockHasUnit = false then disabled = true
                    (model.AddBuildingBlockState.BuildingBlockHasUnit |> not)
            ]

            div [][
                Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                    a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockState model.AddBuildingBlockState).ModalId |> AdvancedSearchMsg |> dispatch)] [
                        str "Use advanced search building block"
                    ]
                ]
                match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                        a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).ModalId |> AdvancedSearchMsg |> dispatch)] [
                            str "Use advanced search unit"
                        ]
                    ]
                | _ -> ()
            ]

        | _ -> ()

        Field.div [] [
            Control.div [] [
                Button.button   [
                    let isValid = model.AddBuildingBlockState.CurrentBuildingBlock |> isValidBuildingBlock
                    Button.Color Color.IsSuccess
                    if isValid then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.OnClick (fun e ->
                        let colName     = model.AddBuildingBlockState.CurrentBuildingBlock
                        let colTerm     = if model.AddBuildingBlockState.BuildingBlockSelectedTerm.IsSome && not colName.isSingleColumn then TermMinimal.ofTerm model.AddBuildingBlockState.BuildingBlockSelectedTerm.Value |> Some else None
                        let unitTerm    = if model.AddBuildingBlockState.UnitSelectedTerm.IsSome && not colName.isSingleColumn then TermMinimal.ofTerm model.AddBuildingBlockState.UnitSelectedTerm.Value |> Some else None
                        let newBuildingBlock = BuildingBlockTypes.InsertBuildingBlock.create colName colTerm unitTerm
                        OfficeInterop.AddAnnotationBlock newBuildingBlock |> OfficeInteropMsg |> dispatch
                    )
                ] [
                    str "Add building block"
                ]
            ]
        ]
    ]

let addUnitToExistingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [][
            Help.help [][
                str "Adds a unit to a the complete building block. If the building block already has a unit assigned, the new unit is only applied to selected rows of the selected column."
            ]
        ]
        Field.div [Field.HasAddons] [
            Control.p [] [
                Button.button [Button.IsStatic true; Button.Props [Style [BackgroundColor ExcelColors.Colorfull.white]]] [
                    str "Add unit"
                ]
            ]
            AutocompleteSearch.autocompleteTermSearchComponent
                dispatch
                model.SiteStyleState.ColorMode
                model
                "Start typing to search"
                None
                (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState)
                // if BuildingBlockHasUnit = false then disabled = true
                false
        ]
        Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
            a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState).ModalId |> AdvancedSearchMsg |> dispatch)] [
                str "Use advanced search"
            ]
        ]

        Field.div [] [
            Control.div [] [
                Button.button   [
                    let isValid = model.AddBuildingBlockState.Unit2TermSearchText <> ""
                    Button.Color Color.IsSuccess
                    if isValid then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.OnClick (fun e ->
                        let unitTerm =
                            if model.AddBuildingBlockState.Unit2SelectedTerm.IsSome then Some <| TermMinimal.ofTerm model.AddBuildingBlockState.Unit2SelectedTerm.Value else None
                        match model.AddBuildingBlockState.Unit2TermSearchText with
                        | "" ->
                            GenericLog ("Error", "Cannot execute function with empty unit input") |> Dev |> dispatch
                        | hasUnitTerm when model.AddBuildingBlockState.Unit2SelectedTerm.IsSome ->
                            OfficeInterop.UpdateUnitForCells unitTerm.Value |> OfficeInteropMsg |> dispatch
                        | freeText ->
                            OfficeInterop.UpdateUnitForCells (TermMinimal.create model.AddBuildingBlockState.Unit2TermSearchText "") |> OfficeInteropMsg |> dispatch
                    )
                ] [
                    str "Update unit for cells"
                ]
            ]
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Messages.Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())

    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Annotation building block selection"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add annotation building blocks (columns) to the annotation table."]
        // Input forms, etc related to add building block.
        addBuildingBlockElements model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add/Update unit reference to existing building block."]
        // Input forms, etc related to add unit to existing building block.
        addUnitToExistingBlockElements model dispatch


    ]