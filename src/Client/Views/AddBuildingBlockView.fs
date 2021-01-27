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
open CustomComponents


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
        Text.p [Props [Style [TextAlign TextAlignOptions.Justify]]][
            span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toLongExplanation |> str]
            span [] [str " You can find more information on our "]
            a [Href Shared.URLs.AnnotationPrinciplesUrl; Target "_blank"][str "website"]
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Annotation building block selection"]
        br []

        Field.div [] [
            Label.label [Label.Size Size.IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Select the type of annotation building block (column) to add to the annotation table."]
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
                            false
                    | _ -> ()
                ]
            ]
            match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
            | Parameter | Characteristics | Factor ->
                Field.div [Field.HasAddons] [
                    Control.div [] [
                        Button.a [
                            Button.OnClick (fun _ ->
                                let inputId = (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockUnitState model.AddBuildingBlockState).InputId
                                if model.AddBuildingBlockState.BuildingBlockHasUnit = true then
                                    let e = Browser.Dom.document.getElementById inputId
                                    e?value <- null
                                ToggleBuildingBlockHasUnit |> AddBuildingBlock |> dispatch
                            )
                        ] [ 
                            Fa.stack [Fa.Stack.Size Fa.FaSmall; Fa.Stack.Props [Style [ Color "#666666"]]][
                                Fa.i [Fa.Regular.Square; Fa.Stack2x][]
                                if model.AddBuildingBlockState.BuildingBlockHasUnit then
                                    Fa.i [Fa.Solid.Check; Fa.Stack1x][]
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
                        // if BuildingBlockHasUnit = false then disabled = true
                        (model.AddBuildingBlockState.BuildingBlockHasUnit |> not)
                ]

                a [OnClick (fun _ -> ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofAddBuildingBlockState model.AddBuildingBlockState).ModalId |> AdvancedSearch |> dispatch)] [
                    str "Use Advanced Search"
                ]

            | _ -> ()

            
        ]


        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [
                    let isValid = model.AddBuildingBlockState.CurrentBuildingBlock |> isValidBuildingBlock
                    if isValid then
                        Button.CustomClass "is-success"
                        Button.IsActive true
                    else
                        Button.CustomClass "is-danger"
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.OnClick (
                        let format =
                            match model.AddBuildingBlockState.BuildingBlockHasUnit, model.AddBuildingBlockState.UnitTermSearchText, model.AddBuildingBlockState.UnitTermSearchTextHasTermAccession with
                            | _,"", _                   -> None //"0.00"
                            | false, _, _               -> None//"0.00"
                            | true, str, Some accession -> Some (str,Some accession)
                            | true, str, None           -> Some (str,None)
                                //sprintf "0.00 \"%s\"" str
                        let colName = model.AddBuildingBlockState.CurrentBuildingBlock |> AnnotationBuildingBlock.toAnnotationTableHeader
                        fun _ -> (colName,format) |> pipeNameTuple2 AddColumn |> ExcelInterop |> dispatch
                    )
                ] [
                    str "Insert this annotation building block"
                ]
            ]
        ]
    ]