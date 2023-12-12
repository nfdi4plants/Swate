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
                //Html.span [
                //    prop.style itemTooltipStyle
                //    prop.className "has-tooltip-multiline"
                //    prop.custom("data-tooltip", subpage.toTooltip)
                //    prop.children (Bulma.icon [
                //        Html.i [prop.className "fa-solid fa-circle-info"]
                //    ])
                //]

                Html.span subpage.toString

                Html.span [
                    prop.style [style.width(length.px 20); style.alignSelf.flexEnd; style.lineHeight 1.5; style.fontSize(length.rem 1.1)]
                    prop.children (Bulma.icon (Html.i [prop.className "fa-solid fa-arrow-right"]))
                ]
            ]
        ]

    /// Navigation element back to main page
    let backToMainDropdownButton (state: BuildingBlockUIState) setState =
        Bulma.dropdownItem.div [
            prop.style [style.textAlign.right]
            prop.children [
                Bulma.button.button [
                    prop.style [style.float'.left; style.width(length.px 20); style.height(length.px 20); style.borderRadius(length.px 4); style.custom("border", "unset")]
                    prop.onClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation()
                        setState {state with DropdownPage = BuildingBlock.DropdownPage.Main}
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

    let createBuildingBlockDropdownItem (model: Model) dispatch uiState setUiState (header: CompositeHeader) =
        let isDeepFreeText = 
            match header with
            | CompositeHeader.FreeText _ 
            | CompositeHeader.Input (IOType.FreeText _) 
            | CompositeHeader.Output (IOType.FreeText _) -> 
                true
            | _ ->
                false
        Bulma.dropdownItem.a [
            if not isDeepFreeText then //disable clicking on freetext elements
                let nextHeader = 
                    if header.IsTermColumn && not header.IsFeaturedColumn then
                        header.UpdateDeepWith model.AddBuildingBlockState.Header
                    else 
                        header
                prop.onClick (fun e ->
                    e.stopPropagation()
                    selectHeader uiState setUiState nextHeader |> dispatch
                )
                prop.onKeyDown(fun k ->
                    if (int k.which) = 13 then selectHeader uiState setUiState nextHeader |> dispatch
                )
            prop.children [
                //Html.span [
                //    prop.style itemTooltipStyle
                //    prop.className "has-tooltip-multiline"
                //    prop.custom("data-tooltip", header.GetUITooltip())
                //    prop.children (Bulma.icon [
                //        Html.i [prop.className "fa-solid fa-circle-info"]
                //    ])
                //]
                match header with
                | CompositeHeader.Output io | CompositeHeader.Input io -> 
                    match io with
                    | IOType.FreeText str ->
                        let ch = if header.isOutput then CompositeHeader.Output else CompositeHeader.Input
                        let onSubmit = fun (v: string) -> 
                            let header = IOType.FreeText v |> ch
                            selectHeader uiState setUiState header |> dispatch
                        FreeTextInputElement onSubmit
                    | otherIO -> 
                        Html.span (string otherIO)
                | CompositeHeader.Component _ | CompositeHeader.Parameter _ | CompositeHeader.Factor _ | CompositeHeader.Characteristic _ ->
                        Html.span header.AsButtonName
                | _ -> Html.span (string header)
            ]
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState (model:Model) dispatch =
        [
            DropdownPage.IOTypes (CompositeHeader.Input, CompositeHeader.InputEmpty.AsButtonName) |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            CompositeHeader.ParameterEmpty      |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.FactorEmpty         |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.CharacteristicEmpty |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ComponentEmpty      |> createBuildingBlockDropdownItem model dispatch state setState
            Model.BuildingBlock.DropdownPage.More  |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            DropdownPage.IOTypes (CompositeHeader.Output, CompositeHeader.OutputEmpty.AsButtonName) |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.right]
                prop.children annotationsPrinciplesUrl
            ]
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState state_search setState_search (model:Model) dispatch =
        [
            // Heading
            //Bulma.dropdownItem.div [
            //    prop.style [style.textAlign.center]
            //    prop.children [
            //        Html.h6 [
            //            prop.className "subtitle"
            //            prop.style [style.fontWeight.bold]
            //            prop.text BuildingBlock.DropdownPage.More.toString
            //        ]
            //    ]
            //]
            //Bulma.dropdownDivider []
            CompositeHeader.Date                |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.Performer           |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ProtocolDescription |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ProtocolREF         |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ProtocolType        |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ProtocolUri         |> createBuildingBlockDropdownItem model dispatch state setState
            CompositeHeader.ProtocolVersion     |> createBuildingBlockDropdownItem model dispatch state setState
            // Navigation element back to main page
            backToMainDropdownButton state setState
        ]

    /// Output columns subpage for dropdown
    let dropdownContentIOTypeColumns (createHeaderFunc: IOType -> CompositeHeader) state setState (model:Model) dispatch =
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
            createHeaderFunc IOType.Source            |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc IOType.Sample            |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc IOType.Material          |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc IOType.RawDataFile       |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc IOType.DerivedDataFile   |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc IOType.ImageFile         |> createBuildingBlockDropdownItem model dispatch state setState
            createHeaderFunc (IOType.FreeText "")     |> createBuildingBlockDropdownItem model dispatch state setState
            // Navigation element back to main page
            backToMainDropdownButton state setState
        ]

let Main state setState state_search setState_search (model: Model) dispatch =
    let state_bb = model.AddBuildingBlockState
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
                                prop.text (state_bb.Header.AsButtonName)
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
                        DropdownElements.dropdownContentProtocolTypeColumns state setState state_search setState_search model dispatch
                    | Model.BuildingBlock.DropdownPage.IOTypes (createHeader,_) ->
                        DropdownElements.dropdownContentIOTypeColumns createHeader state setState model dispatch
                    |> fun content -> Bulma.dropdownContent [ prop.children content ] 
                ]
            ]
        ] 
    ]