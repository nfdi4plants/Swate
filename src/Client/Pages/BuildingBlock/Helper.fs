module BuildingBlock.Helper

open Shared
open Messages
open TermTypes
open Fulma
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open ExcelColors
open Model
open Messages.BuildingBlock
open OfficeInteropTypes



let isValidBuildingBlock (block : BuildingBlockNamePrePrint) =
    if block.Type.isFeaturedColumn then true
    elif block.Type.isTermColumn then block.Name.Length > 0
    elif block.Type.isSingleColumn then true
    else false

let createBuildingBlockDropdownItem (model:Model) (dispatch:Messages.Msg -> unit) (block: BuildingBlockType )  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            OnClick (fun e ->
                e.stopPropagation()
                BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> BuildingBlockMsg |> dispatch
            )
            OnKeyDown (fun k -> if (int k.which) = 13 then BuildingBlockNamePrePrint.init block |> NewBuildingBlockSelected |> BuildingBlockMsg |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ] [
        span [
            Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
            Class "has-tooltip-multiline"
            Props.Custom ("data-tooltip", block.toShortExplanation)
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]

        span [] [str block.toString]
    ]

let createSubBuildingBlockDropdownLink (model:Model) (dispatch:Messages.Msg -> unit) (subpage: Model.BuildingBlock.DropdownPage) =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun e ->
                e.preventDefault()
                e.stopPropagation()
                UpdateDropdownPage subpage |> BuildingBlockMsg |> dispatch
            )
            Style [
                yield! colorControlInArray model.SiteStyleState.ColorMode;
                PaddingRight "0.5rem"
            ]
        ]

    ] [
        span [
            Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
            Class "has-tooltip-multiline"
            Props.Custom ("data-tooltip", subpage.toTooltip)
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]

        span [] [
            str <| subpage.toString
        ]

        span [
            Style [ Width "20px"; Float FloatOptions.Right; LineHeight "1.5"; FontSize "1.1rem"]
        ] [
            Fa.i [Fa.Solid.ArrowRight] [] 
        ]
    ]

module DropdownElements =

    /// Navigation element back to main page
    let backToMainDropdownButton (model:Model) (dispatch:Messages.Msg -> unit) =
        Dropdown.Item.div [Dropdown.Item.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Right)] ] [
            Button.button [
                Button.Modifiers [Modifier.IsPulledLeft]
                Button.OnClick (fun e ->
                    e.preventDefault()
                    e.stopPropagation()
                    UpdateDropdownPage Model.BuildingBlock.DropdownPage.Main |> BuildingBlockMsg |> dispatch
                )
                Button.IsInverted
                if model.SiteStyleState.IsDarkMode then Button.IsOutlined
                Button.Color IsBlack
                Button.Props [Style [ Width "20px"; Height "20px"; BorderRadius "4px"; Border "unset"]]
            ] [
                Fa.i [Fa.Solid.ArrowLeft] [] 
            ]
            a [ Href Shared.URLs.AnnotationPrinciplesUrl; Target "_Blank"] [ str "info" ]
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain (model:Model) (dispatch:Messages.Msg -> unit) =
        [
            BuildingBlockType.Source            |> createBuildingBlockDropdownItem model dispatch
            Dropdown.divider []
            BuildingBlockType.Parameter         |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.Factor            |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.Characteristic    |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.Component         |> createBuildingBlockDropdownItem model dispatch
            Model.BuildingBlock.DropdownPage.ProtocolTypes |>  createSubBuildingBlockDropdownLink model dispatch
            Dropdown.divider []
            Model.BuildingBlock.DropdownPage.Output |>  createSubBuildingBlockDropdownLink model dispatch
            Dropdown.Item.div [Dropdown.Item.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Right)] ] [
                a [ Href Shared.URLs.AnnotationPrinciplesUrl; Target "_Blank"] [ str "info" ]
            ]
        ]
    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns (model:Model) (dispatch: Messages.Msg -> unit) =
        [
            // Heading
            Dropdown.Item.div [Dropdown.Item.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Centered)] ] [
                Heading.h6 [Heading.IsSubtitle; Heading.Modifiers [Modifier.TextWeight TextWeight.Option.Bold]] [str BuildingBlock.DropdownPage.ProtocolTypes.toString]
            ]
            Dropdown.divider []
            BuildingBlockType.ProtocolType      |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.ProtocolREF       |> createBuildingBlockDropdownItem model dispatch
            // Navigation element back to main page
            backToMainDropdownButton model dispatch
        ]

    /// Output columns subpage for dropdown
    let dropdownContentOutputColumns (model:Model) (dispatch: Messages.Msg -> unit) =
        [
            // Heading
            Dropdown.Item.div [Dropdown.Item.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Centered)] ] [
                Heading.h6 [Heading.IsSubtitle; Heading.Modifiers [Modifier.TextWeight TextWeight.Option.Bold]] [str BuildingBlock.DropdownPage.Output.toString]
            ]
            Dropdown.divider []
            BuildingBlockType.Sample            |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.RawDataFile       |> createBuildingBlockDropdownItem model dispatch
            BuildingBlockType.DerivedDataFile   |> createBuildingBlockDropdownItem model dispatch
            // Navigation element back to main page
            backToMainDropdownButton model dispatch
        ]