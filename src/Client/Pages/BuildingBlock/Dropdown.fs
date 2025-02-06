module BuildingBlock.Dropdown

open Feliz
open Feliz.DaisyUI
open Model.BuildingBlock
open Model
open Messages
open ARCtrl
open Swate.Components.Shared


[<ReactComponent>]
let FreeTextInputElement(onSubmit: string -> unit) =
    let inputS, setInput = React.useState ""
    Html.div [
        prop.className "flex flex-row gap-0 p-0"
        prop.children [
            Daisy.input [
                join.item
                input.sm
                prop.placeholder "..."
                prop.className "grow truncate"
                prop.onClick (fun e -> e.stopPropagation())
                prop.onChange (fun (v:string) -> setInput v)
                prop.onKeyDown(key.enter, fun e -> e.stopPropagation(); onSubmit inputS)
            ]
            Daisy.button.button [
                join.item
                button.accent
                button.sm
                prop.onClick (fun e -> e.stopPropagation(); onSubmit inputS)
                prop.children [
                    Html.i [prop.className "fa-solid fa-check"]
                ]
            ]
        ]
    ]

module private DropdownElements =

    let divider = Daisy.divider [prop.className "mx-2 my-0"]
    let private annotationsPrinciplesLink = Html.a [prop.href Swate.Components.Shared.URLs.AnnotationPrinciplesUrl; prop.target.blank; prop.className "ml-auto link-info"; prop.text "info"]

    let createSubBuildingBlockDropdownLink (state:BuildingBlockUIState) setState (subpage: Model.BuildingBlock.DropdownPage) =
        Html.li [
            prop.onClick(fun e ->
                e.preventDefault()
                e.stopPropagation()
                setState {state with DropdownPage = subpage}
            )
            prop.children [
                Html.div [
                    prop.className "flex flex-row justify-between"
                    prop.children [
                        Html.span subpage.toString
                        Html.i [prop.className "fa-solid fa-arrow-right"]
                    ]
                ]
            ]
        ]

    /// Navigation element back to main page
    let DropdownContentInfoFooter setState (hasBack: bool) =
        Html.li [
            prop.className "flex flex-row justify-between pt-1"
            prop.onClick(fun e ->
                e.preventDefault()
                e.stopPropagation()
                setState {DropdownPage = BuildingBlock.DropdownPage.Main; DropdownIsActive = true}
            )
            prop.children [
                if hasBack then
                    Html.a [
                        prop.className "content-center"
                        prop.children [
                            Html.i [ prop.className "fa-solid fa-arrow-left" ]
                        ]
                    ]
                annotationsPrinciplesLink
            ]
        ]

    let createBuildingBlockDropdownItem (model: Model) (dispatch: Msg -> unit) setUiState close (headerType: CompositeHeaderDiscriminate) =
        Html.li [Html.a [
            prop.onClick (fun e ->
                e.stopPropagation()
                Helper.selectCompositeHeaderDiscriminate headerType setUiState close dispatch
            )
            prop.onKeyDown(fun k ->
                if (int k.which) = 13 then Helper.selectCompositeHeaderDiscriminate headerType setUiState close dispatch
            )
            prop.text (headerType.ToString())
        ]]

    let createIOTypeDropdownItem (model: Model) dispatch setUiState close (headerType: CompositeHeaderDiscriminate) (iotype: IOType) =
        let setIO (ioType) =
            { DropdownPage = DropdownPage.Main; DropdownIsActive = false } |> setUiState
            close()
            (headerType,ioType) |> BuildingBlock.UpdateHeaderWithIO |> BuildingBlockMsg |> dispatch
        Html.li [
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
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState close (model:Model) (dispatch: Msg -> unit) =
        React.fragment [
            DropdownPage.IOTypes CompositeHeaderDiscriminate.Input |> createSubBuildingBlockDropdownLink state setState
            divider
            CompositeHeaderDiscriminate.Parameter      |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.Factor         |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.Characteristic |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.Component      |> createBuildingBlockDropdownItem model dispatch setState close
            Model.BuildingBlock.DropdownPage.More       |> createSubBuildingBlockDropdownLink state setState
            divider
            DropdownPage.IOTypes CompositeHeaderDiscriminate.Output |> createSubBuildingBlockDropdownLink state setState
            DropdownContentInfoFooter setState false
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState close (model:Model) dispatch =
        React.fragment [
            CompositeHeaderDiscriminate.Date                |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.Performer           |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.ProtocolDescription |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.ProtocolREF         |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.ProtocolType        |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.ProtocolUri         |> createBuildingBlockDropdownItem model dispatch setState close
            CompositeHeaderDiscriminate.ProtocolVersion     |> createBuildingBlockDropdownItem model dispatch setState close
            // Navigation element back to main page
            DropdownContentInfoFooter setState true
        ]

    /// Output columns subpage for dropdown
    let dropdownContentIOTypeColumns header state setState close (model:Model) dispatch =
        React.fragment [
            IOType.Source           |> createIOTypeDropdownItem model dispatch setState close header
            IOType.Sample           |> createIOTypeDropdownItem model dispatch setState close header
            IOType.Material         |> createIOTypeDropdownItem model dispatch setState close header
            IOType.Data             |> createIOTypeDropdownItem model dispatch setState close header
            IOType.FreeText ""      |> createIOTypeDropdownItem model dispatch setState close header
            // Navigation element back to main page
            DropdownContentInfoFooter setState true
        ]

[<ReactComponent>]
let Main(state, setState, model: Model, dispatch: Msg -> unit) =
    let isOpen, setOpen = React.useState false
    let close = fun _ -> setOpen false
    Components.BaseDropdown.Main(
        isOpen,
        setOpen,
        Daisy.button.div [
                button.primary
                prop.onClick (fun _ -> setOpen (not isOpen))
                prop.role "button"
                join.item
                prop.className "flex-nowrap"
                prop.children [
                    Html.span (model.AddBuildingBlockState.HeaderCellType.ToString())
                    Html.i [
                        prop.className "fa-solid fa-angle-down"
                    ]
                ]
            ],
        [
            match state.DropdownPage with
            | Model.BuildingBlock.DropdownPage.Main ->
                DropdownElements.dropdownContentMain state setState close model dispatch
            | Model.BuildingBlock.DropdownPage.More ->
                DropdownElements.dropdownContentProtocolTypeColumns state setState close model dispatch
            | Model.BuildingBlock.DropdownPage.IOTypes iotype ->
                DropdownElements.dropdownContentIOTypeColumns iotype state setState close model dispatch
        ],
        style=Style.init("join-item dropdown", Map [
            "content", Style.init("!min-w-64")
        ])
    )
    // Daisy.dropdown [
    //     join.item
    //     if isOpen then dropdown.open'
    //     prop.children [

    //         Daisy.dropdownContent [
    //             prop.className "bg-base-300 w-64 menu rounded-box z-[1] p-2 shadow"
    //             prop.children
    //         ]
    //     ]
    // ]