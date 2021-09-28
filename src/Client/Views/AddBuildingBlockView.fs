module AddBuildingBlockView

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
open Shared
open TermTypes
open CustomComponents

open OfficeInteropTypes

let isValidBuildingBlock (block : BuildingBlockNamePrePrint) =
    match block.Type with
    | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
        block.Name.Length > 0
    | BuildingBlockType.Sample | BuildingBlockType.Data | BuildingBlockType.Source ->
        true
    | _ -> false

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


let createBuildingBlockDropdownItem (model:Model) (dispatch:Msg -> unit) (block: BuildingBlockType )  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun _ -> BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)
            OnKeyDown (fun k -> if (int k.which) = 13 then BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)
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

let addBuildingBlockFooterComponent (model:Model) (dispatch:Msg -> unit) =
    Content.content [ ] [
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ 
            str (sprintf "More about %s:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type.toString ))
        ]
        Text.p [Props [Style [TextAlign TextAlignOptions.Justify]]][
            span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type |> BuildingBlockType.toLongExplanation |> str]
            span [] [str " You can find more information on our "]
            a [Href Shared.URLs.AnnotationPrinciplesUrl; Target "_blank"][str "website"]
        ]
    ]

let addBuildingBlockElements (model:Model) (dispatch:Msg -> unit) =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ] [
        Field.div [
            Field.HasAddons
        ] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection
                ] [
                    Dropdown.trigger [] [
                        Button.a [Button.OnClick (fun _ -> ToggleSelectionDropdown |> AddBuildingBlock |> dispatch)] [
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
                            ToggleBuildingBlockHasUnit |> AddBuildingBlock |> dispatch
                        )
                    ] [
                        if model.AddBuildingBlockState.BuildingBlockHasUnit then
                            Fa.i [ Fa.Size Fa.FaLarge; Fa.Solid.Check ][ ]
                        else
                            Fa.i [ Fa.Size Fa.FaLarge ; Fa.Solid.Ban ][ ]
                    ]
                ]
                Control.p [] [
                    Button.button [Button.IsStatic true] [
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
                    a [OnClick (fun _ -> ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockState model.AddBuildingBlockState).ModalId |> AdvancedSearch |> dispatch)] [
                        str "Use advanced search building block"
                    ]
                ]
                match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                | BuildingBlockType.Parameter | BuildingBlockType.Characteristics | BuildingBlockType.Factor ->
                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                        a [OnClick (fun _ -> ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).ModalId |> AdvancedSearch |> dispatch)] [
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
                        let colTerm     = if model.AddBuildingBlockState.BuildingBlockSelectedTerm.IsSome then TermMinimal.ofTerm model.AddBuildingBlockState.BuildingBlockSelectedTerm.Value |> Some else None
                        let unitTerm    = if model.AddBuildingBlockState.UnitSelectedTerm.IsSome then TermMinimal.ofTerm model.AddBuildingBlockState.UnitSelectedTerm.Value |> Some else None
                        let newBuildingBlock = BuildingBlockTypes.InsertBuildingBlock.create colName colTerm unitTerm
                        AddAnnotationBlock newBuildingBlock |> ExcelInterop |> dispatch
                    )
                ] [
                    str "Insert annotation building block"
                ]
            ]
        ]
    ]

let addUnitToExistingBlockElements (model:Model) (dispatch:Msg -> unit) =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
    ]] [
        Field.div [][
            Help.help [][
                str "Adds a unit to a the complete building block. If the building block already has a unit assigned, the new unit is only applied to selected rows of the selected column."
            ]
        ]
        Field.div [Field.HasAddons] [
            Control.p [] [
                Button.button [Button.IsStatic true] [
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
            a [OnClick (fun _ -> ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnit2State model.AddBuildingBlockState).ModalId |> AdvancedSearch |> dispatch)] [
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
                            UpdateUnitForCells unitTerm.Value |> ExcelInterop |> dispatch
                        | freeText ->
                            UpdateUnitForCells (TermMinimal.create model.AddBuildingBlockState.Unit2TermSearchText "") |> ExcelInterop |> dispatch
                    )
                ] [
                    str "Update unit for cells"
                ]
            ]
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Msg -> unit) =
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