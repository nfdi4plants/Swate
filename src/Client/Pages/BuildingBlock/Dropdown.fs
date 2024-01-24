module BuildingBlock.Dropdown

open Feliz
open Feliz.Bulma
open Shared
open TermTypes
open OfficeInteropTypes
open Fable.Core.JsInterop
open Elmish
open Model.BuildingBlock
open Model.TermSearch
open Model
open Messages
open ARCtrl.ISA
open BuildingBlock.Helper
open Fable.Core


[<ReactComponent>]
let FreeTextInputElement(onSubmit: string -> unit) =
    let input, setInput = React.useState ""
    Html.span [
        Html.input [
            prop.onClick (fun e -> e.stopPropagation())
            prop.onChange (fun (v:string) -> setInput v)
        ]
        Html.button [
            prop.onClick (fun e -> e.stopPropagation(); onSubmit input)
            prop.text "✅"
        ]
    ]

module private DropdownElements =

    let private itemTooltipStyle = [style.fontSize (length.rem 1.1); style.paddingRight (length.px 10); style.textAlign.center; style.color NFDIColors.Yellow.Darker20]
    let private annotationsPrinciplesUrl = Html.a [prop.href Shared.URLs.AnnotationPrinciplesUrl; prop.target "_Blank"; prop.text "info"]

    let createSubBuildingBlockDropdownLink (state:BuildingBlockUIState) setState (subpage: Model.BuildingBlock.DropdownPage) =
        Bulma.dropdownItem.a [
            prop.tabIndex 0
            prop.onClick(fun e ->
                e.preventDefault()
                e.stopPropagation()
                setState {state with DropdownPage = subpage}
            )
            prop.style [
                style.paddingRight(length.rem 1)
                style.display.inlineFlex
                style.justifyContent.spaceBetween
            ]
            prop.children [

                Html.span subpage.toString

                Html.span [
                    prop.style [style.width(length.px 20); style.alignSelf.flexEnd; style.lineHeight 1.5; style.fontSize(length.rem 1.1)]
                    prop.children (Bulma.icon (Html.i [prop.className "fa-solid fa-arrow-right"]))
                ]
            ]
        ]

    /// Navigation element back to main page
    let backToMainDropdownButton setState =
        Bulma.dropdownItem.div [
            prop.style [style.textAlign.right]
            prop.children [
                Bulma.button.button [
                    prop.style [style.float'.left; style.width(length.px 20); style.height(length.px 20); style.borderRadius(length.px 4); style.custom("border", "unset")]
                    prop.onClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation()
                        setState {DropdownPage = BuildingBlock.DropdownPage.Main; DropdownIsActive = true}
                    )
                    Bulma.button.isInverted
                    Bulma.color.isBlack
                    prop.children [
                        Bulma.icon [Html.i [
                            prop.className "fa-solid fa-arrow-left"
                        ]]
                    ]
                ]
                annotationsPrinciplesUrl
            ]
        ]

    let createBuildingBlockDropdownItem (model: Model) dispatch setUiState (headerType: BuildingBlock.HeaderCellType) =
        Bulma.dropdownItem.a [
            prop.onClick (fun e ->
                e.stopPropagation()
                Helper.selectHeaderCellType headerType setUiState dispatch
            )
            prop.onKeyDown(fun k ->
                if (int k.which) = 13 then Helper.selectHeaderCellType headerType setUiState dispatch
            )
            prop.text (headerType.ToString())
        ]

    let createIOTypeDropdownItem (model: Model) dispatch setUiState (headerType: BuildingBlock.HeaderCellType) (iotype: IOType) =
        let setIO (ioType) = 
            Helper.selectHeaderCellType headerType setUiState dispatch
            U2.Case2 ioType |> Some |> BuildingBlock.UpdateHeaderArg |> BuildingBlockMsg |> dispatch
        Bulma.dropdownItem.a [
            prop.children [
                match iotype with
                | IOType.FreeText s ->
                    let onSubmit = fun (v: string) -> 
                        let header = IOType.FreeText v
                        setIO header
                    FreeTextInputElement onSubmit
                | _ ->
                    Html.div [
                        prop.onClick (fun e -> e.stopPropagation(); setIO iotype)
                        prop.onKeyDown(fun k -> if (int k.which) = 13 then setIO iotype)
                        prop.text (iotype.ToString())
                    ]
            ]
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState (model:Model) dispatch =
        [
            DropdownPage.IOTypes BuildingBlock.HeaderCellType.Input |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            BuildingBlock.HeaderCellType.Parameter      |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.Factor         |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.Characteristic |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.Component      |> createBuildingBlockDropdownItem model dispatch setState
            Model.BuildingBlock.DropdownPage.More       |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            DropdownPage.IOTypes BuildingBlock.HeaderCellType.Output |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.right]
                prop.children annotationsPrinciplesUrl
            ]
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState (model:Model) dispatch =
        [
            BuildingBlock.HeaderCellType.Date                |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.Performer           |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.ProtocolDescription |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.ProtocolREF         |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.ProtocolType        |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.ProtocolUri         |> createBuildingBlockDropdownItem model dispatch setState
            BuildingBlock.HeaderCellType.ProtocolVersion     |> createBuildingBlockDropdownItem model dispatch setState
            // Navigation element back to main page
            backToMainDropdownButton setState
        ]

    /// Output columns subpage for dropdown
    let dropdownContentIOTypeColumns header state setState (model:Model) dispatch =
        [
            // Heading
            //Bulma.dropdownItem.div [
            //    prop.style [style.textAlign.center]
            //    prop.children [
            //        Html.h6 [
            //            prop.className "subtitle"
            //            prop.style [style.fontWeight.bold]
            //            prop.text name
            //        ]
            //    ]
            //]
            //Bulma.dropdownDivider []
            IOType.Source           |> createIOTypeDropdownItem model dispatch setState header
            IOType.Sample           |> createIOTypeDropdownItem model dispatch setState header
            IOType.Material         |> createIOTypeDropdownItem model dispatch setState header
            IOType.RawDataFile      |> createIOTypeDropdownItem model dispatch setState header
            IOType.DerivedDataFile  |> createIOTypeDropdownItem model dispatch setState header
            IOType.ImageFile        |> createIOTypeDropdownItem model dispatch setState header
            IOType.FreeText ""      |> createIOTypeDropdownItem model dispatch setState header
            // Navigation element back to main page
            backToMainDropdownButton setState
        ]

let Main state setState (model: Model) dispatch =
    Bulma.control.div [
        Bulma.dropdown [
            if state.DropdownIsActive then Bulma.dropdown.isActive //Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection
            prop.children [
                Bulma.dropdownTrigger [
                    Bulma.button.a [
                        prop.onClick (fun e -> e.stopPropagation(); setState {state with DropdownIsActive = not state.DropdownIsActive})
                        prop.children [
                            Html.span [
                                prop.style [style.marginRight 5]
                                prop.text (model.AddBuildingBlockState.HeaderCellType.ToString())
                            ]
                            Bulma.icon [ Html.i [
                                prop.className "fa-solid fa-angle-down"
                            ] ]
                        ]
                    ]
                ]
                Bulma.dropdownMenu [
                    match state.DropdownPage with
                    | Model.BuildingBlock.DropdownPage.Main ->
                        DropdownElements.dropdownContentMain state setState model dispatch
                    | Model.BuildingBlock.DropdownPage.More ->
                        DropdownElements.dropdownContentProtocolTypeColumns state setState model dispatch
                    | Model.BuildingBlock.DropdownPage.IOTypes iotype ->
                        DropdownElements.dropdownContentIOTypeColumns iotype state setState model dispatch
                    |> fun content -> Bulma.dropdownContent [ prop.children content ] 
                ]
            ]
        ] 
    ]