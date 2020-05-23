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

let isValidBuildingBlock (block : AnnotationBuildingBlock) =
    match block.Type with
    | Parameter | Characteristics | Factor ->
        block.Name.Length > 0
    | Sample | Data ->
        true
    | _ -> false

let addBuildingBlockComponent (model:Model) (dispatch:Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Annotation building block selection"]

        Field.div [] [
            Label.label [] [ str "Building block"]
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
                        Dropdown.menu [] [
                            Dropdown.content [] [
                                Dropdown.Item.a [Dropdown.Item.Props [OnClick (fun _ -> AnnotationBuildingBlock.init Characteristics |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)]] [str "Characteristics"]
                                Dropdown.Item.a [Dropdown.Item.Props [OnClick (fun _ -> AnnotationBuildingBlock.init Parameter       |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)]] [str "Parameter"]
                                Dropdown.Item.a [Dropdown.Item.Props [OnClick (fun _ -> AnnotationBuildingBlock.init Factor          |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)]] [str "Factor"]
                                Dropdown.Item.a [Dropdown.Item.Props [OnClick (fun _ -> AnnotationBuildingBlock.init Sample          |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)]] [str "Sample"]
                                Dropdown.Item.a [Dropdown.Item.Props [OnClick (fun _ -> AnnotationBuildingBlock.init Data            |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)]] [str "Data"]
                            ]
                        ]
                    ]
                ]
                Control.div [Control.IsExpanded] [
                    match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                    | Parameter | Characteristics | Factor ->
                        Input.input [
                            Input.OnChange (fun ev -> ev.Value |> BuildingBlockNameChange |> AddBuildingBlock |> dispatch)
                        ]
                    | _ -> ()
                ]
            ]
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
                                    //TODO: add fill support via Excel interop here
                                    //Button.OnClick (fun _ -> model.TermSearchState.Simple.TermSearchText |> FillSelection |> ExcelInterop |> dispatch)

                                ] [
                    str "Insert this annotation building block"
                ]
            ]
        ]
    ]