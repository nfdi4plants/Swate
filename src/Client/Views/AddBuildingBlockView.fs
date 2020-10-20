module AddBuildingBlockView

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
open Fable.Core

let isValidBuildingBlock (block : AnnotationBuildingBlock) =
    match block.Type with
    | Parameter | Characteristics | Factor ->
        block.Name.Length > 0
    | Sample | Data | Source ->
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
//                td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
//                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
//            ])
//        |> List.ofArray
//    else
//        [
//            tr [] [
//                td [] [str "No terms found matching your input."]
//            ]
//        ]


let createBuildingBlockDropdownItem (model:Model) (dispatch:Msg -> unit) (block: AnnotationBuildingBlockType )  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun _ -> AnnotationBuildingBlock.init block |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)
            OnKeyDown (fun k -> if (int k.which) = 13 then AnnotationBuildingBlock.init block |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip (block |> AnnotationBuildingBlockType.toShortExplanation)
                Style [PaddingRight "10px"]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]
        
        Text.span [] [block |> AnnotationBuildingBlockType.toString |> str]
    ]

let addBuildingBlockFooterComponent (model:Model) (dispatch:Msg -> unit) =
    Content.content [] [
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ 
            str (sprintf "More about %s:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString))
        ]
        Text.p [] [
            model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toLongExplanation |> str
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Annotation building block selection"]

        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Building block"]
            Help.help [] [str "Select the type of annotation building block (column) to add to the annotation table"]
            Field.div [Field.HasAddons] [
                Control.div [] [
                    Dropdown.dropdown [Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection] [
                        Dropdown.trigger [] [
                            Button.button [Button.OnClick (fun _ -> ToggleSelectionDropdown |> AddBuildingBlock |> dispatch)] [
                                span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString |> str]
                                Fa.i [Fa.Solid.AngleDown] []
                            ]
                        ]
                        Dropdown.menu [Props[colorControl model.SiteStyleState.ColorMode]] [
                            Dropdown.content [] ([
                                Parameter       
                                Factor          
                                Characteristics 
                                Sample          
                                Data            
                                Source          
                            ]  |> List.map (createBuildingBlockDropdownItem model dispatch))
                        ]
                    ]
                ]
                Control.div [Control.IsExpanded] [
                    match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                    | Parameter | Characteristics | Factor ->
                        AutocompleteSearch.autocompleteTermSearchComponent
                            dispatch
                            model.SiteStyleState.ColorMode
                            model
                            "Start typing to search"
                            None
                            (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockState model.AddBuildingBlockState)
                        
                    | _ -> ()
                ]
            ]
            match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
            | Parameter | Characteristics | Factor ->
                Field.div [Field.HasAddons] [
                    Control.div [] [
                        Button.button [ Button.OnClick (fun _ -> BuildingBlockHasUnitSwitch |> AddBuildingBlock |> dispatch)] [ 
                            Checkbox.checkbox [] [
                                Checkbox.input [
                                    Props [
                                        Checked model.AddBuildingBlockState.BuildingBlockHasUnit
                                        
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Control.p [] [
                        Button.button [Button.IsStatic true] [
                            str (sprintf "This %s has a unit:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString ))
                        ]
                    ]
                    AutocompleteSearch.autocompleteTermSearchComponent
                        dispatch
                        model.SiteStyleState.ColorMode
                        model
                        "Start typing to search"
                        None
                        (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState)


                ]
            | _ -> ()
        ]

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let isValid = model.AddBuildingBlockState.CurrentBuildingBlock |> isValidBuildingBlock
                                    if isValid then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (
                                        let format =
                                            match model.AddBuildingBlockState.UnitTermSearchText with
                                            | "" -> "0.00"
                                            | str ->
                                                sprintf "0.00 \"%s\"" str
                                        let colName = model.AddBuildingBlockState.CurrentBuildingBlock |> AnnotationBuildingBlock.toAnnotationTableHeader
                                        fun _ -> (colName,format) |> AddColumn |> ExcelInterop |> dispatch
                                    )
                                ] [
                    str "Insert this annotation building block"
                ]
            ]
        ]
    ]