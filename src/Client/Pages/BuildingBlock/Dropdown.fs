module BuildingBlock.Dropdown

open Feliz
open Feliz.DaisyUI
open Model.BuildingBlock
open Model
open Messages
open ARCtrl
open Shared


[<ReactComponent>]
let FreeTextInputElement(onSubmit: string -> unit) =
    let inputS, setInput = React.useState ""
    Daisy.join [
        prop.className "w-full"
        prop.children [
            Daisy.input [
                prop.className "min-w-48 !rounded-none"
                input.sm
                prop.onClick (fun e -> e.stopPropagation())
                prop.onChange (fun (v:string) -> setInput v)
                prop.onKeyDown(key.enter, fun e -> e.stopPropagation(); onSubmit inputS)
            ]
            Daisy.button.submit [
                button.sm
                prop.onClick (fun e -> e.stopPropagation(); onSubmit inputS)
                prop.children [
                    Html.i [prop.className "fa-solid fa-check"]
                ]
            ]
        ]
    ]

module private DropdownElements =

    let private itemTooltipStyle = [style.fontSize (length.rem 1.1); style.paddingRight (length.px 10); style.textAlign.center; style.color NFDIColors.Yellow.Darker20]
    let private annotationsPrinciplesUrl = Html.a [prop.href Shared.URLs.AnnotationPrinciplesUrl; prop.target.blank; prop.text "info"]

    let createSubBuildingBlockDropdownLink (state:BuildingBlockUIState) setState (subpage: Model.BuildingBlock.DropdownPage) =
        Html.li [Html.a [
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
                    prop.children (Html.i [prop.className "fa-solid fa-arrow-right"])
                ]
            ]
        ]]

    /// Navigation element back to main page
    let backToMainDropdownButton setState =
        Html.li [
            prop.style [style.textAlign.right]
            prop.children [
                Daisy.button.button [
                    prop.style [style.float'.left; style.width(length.px 20); style.height(length.px 20); style.borderRadius(length.px 4); style.custom("border", "unset")]
                    prop.onClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation()
                        setState {DropdownPage = BuildingBlock.DropdownPage.Main; DropdownIsActive = true}
                    )
                    prop.children [
                        Html.i [
                            prop.className "fa-solid fa-arrow-left"
                        ]
                    ]
                ]
                annotationsPrinciplesUrl
            ]
        ]

    let createBuildingBlockDropdownItem (model: Model) dispatch setUiState (headerType: CompositeHeaderDiscriminate) =
        Html.li [Html.a [
            prop.onClick (fun e ->
                e.stopPropagation()
                Helper.selectCompositeHeaderDiscriminate headerType setUiState dispatch
            )
            prop.onKeyDown(fun k ->
                if (int k.which) = 13 then Helper.selectCompositeHeaderDiscriminate headerType setUiState dispatch
            )
            prop.text (headerType.ToString())
        ]]

    let createIOTypeDropdownItem (model: Model) dispatch setUiState (headerType: CompositeHeaderDiscriminate) (iotype: IOType) =
        let setIO (ioType) =
            { DropdownPage = DropdownPage.Main; DropdownIsActive = false } |> setUiState
            (headerType,ioType) |> BuildingBlock.UpdateHeaderWithIO |> BuildingBlockMsg |> dispatch
        Html.li [Daisy.button.button [
            match iotype with
            | IOType.FreeText s ->
                let onSubmit = fun (v: string) ->
                    let header = IOType.FreeText v
                    setIO header
                prop.children [FreeTextInputElement onSubmit]
            | _ ->
                prop.onClick (fun e -> e.stopPropagation(); setIO iotype)
                prop.onKeyDown(fun k -> if (int k.which) = 13 then setIO iotype)
                prop.children [
                    Html.div [prop.text (iotype.ToString())]
                ]
        ]]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState (model:Model) dispatch =
        React.fragment [
            DropdownPage.IOTypes CompositeHeaderDiscriminate.Input |> createSubBuildingBlockDropdownLink state setState
            Daisy.divider []
            CompositeHeaderDiscriminate.Parameter      |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.Factor         |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.Characteristic |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.Component      |> createBuildingBlockDropdownItem model dispatch setState
            Model.BuildingBlock.DropdownPage.More       |> createSubBuildingBlockDropdownLink state setState
            Daisy.divider []
            DropdownPage.IOTypes CompositeHeaderDiscriminate.Output |> createSubBuildingBlockDropdownLink state setState
            Html.li [
                prop.style [style.textAlign.right]
                prop.children annotationsPrinciplesUrl
            ]
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState (model:Model) dispatch =
        React.fragment [
            CompositeHeaderDiscriminate.Date                |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.Performer           |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.ProtocolDescription |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.ProtocolREF         |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.ProtocolType        |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.ProtocolUri         |> createBuildingBlockDropdownItem model dispatch setState
            CompositeHeaderDiscriminate.ProtocolVersion     |> createBuildingBlockDropdownItem model dispatch setState
            // Navigation element back to main page
            backToMainDropdownButton setState
        ]

    /// Output columns subpage for dropdown
    let dropdownContentIOTypeColumns header state setState (model:Model) dispatch =
        React.fragment [
            IOType.Source           |> createIOTypeDropdownItem model dispatch setState header
            IOType.Sample           |> createIOTypeDropdownItem model dispatch setState header
            IOType.Material         |> createIOTypeDropdownItem model dispatch setState header
            IOType.Data             |> createIOTypeDropdownItem model dispatch setState header
            IOType.FreeText ""      |> createIOTypeDropdownItem model dispatch setState header
            // Navigation element back to main page
            backToMainDropdownButton setState
        ]

let Main state setState (model: Model) dispatch =
    Daisy.dropdown [
        join.item
        // if state.DropdownIsActive then dropdown.open'
        prop.children [
            Daisy.button.div [
                prop.tabIndex 0
                prop.role "button"
                // prop.onClick (fun e -> e.stopPropagation(); setState {state with DropdownIsActive = not state.DropdownIsActive})
                prop.children [
                    Html.span [
                        prop.style [style.marginRight 5]
                        prop.text (model.AddBuildingBlockState.HeaderCellType.ToString())
                    ]
                    Html.i [
                        prop.className "fa-solid fa-angle-down"
                    ]
                ]
            ]
            Daisy.dropdownContent [
                prop.tabIndex 0
                prop.children [
                    match state.DropdownPage with
                    | Model.BuildingBlock.DropdownPage.Main ->
                        DropdownElements.dropdownContentMain state setState model dispatch
                    | Model.BuildingBlock.DropdownPage.More ->
                        DropdownElements.dropdownContentProtocolTypeColumns state setState model dispatch
                    | Model.BuildingBlock.DropdownPage.IOTypes iotype ->
                        DropdownElements.dropdownContentIOTypeColumns iotype state setState model dispatch
                ]
            ]
        ]
    ]