module BuildingBlock.SearchComponent

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

let private termOrUnitizedSwitch (model:Messages.Model) dispatch =
        
    let state = model.AddBuildingBlockState
    Bulma.buttons [
        Bulma.buttons.hasAddons
        prop.style [style.flexWrap.nowrap; style.marginBottom 0; style.marginRight (length.rem 1)]
        prop.children [
            Bulma.button.a [
                let isActive = state.BodyCellType = BodyCellType.Term
                if isActive then Bulma.color.isSuccess
                prop.onClick (fun _ -> BuildingBlock.UpdateBodyCellType BodyCellType.Term |> BuildingBlockMsg |> dispatch)
                prop.classes ["pr-2 pl-2 mb-0"; if isActive then "is-selected"]
                prop.text "Term"
            ]
            Bulma.button.a [
                let isActive = state.BodyCellType = BodyCellType.Unitized
                if isActive then Bulma.color.isSuccess
                prop.onClick (fun _ -> BuildingBlock.UpdateBodyCellType BodyCellType.Unitized |> BuildingBlockMsg |> dispatch)
                prop.classes ["pr-2 pl-2 mb-0"; if isActive then "is-selected"]
                prop.text "Unit"
            ]
        ]
    ]


open Fable.Core

let private SearchBuildingBlockBodyElement (model: Messages.Model) dispatch =
    let id = "SearchBuildingBlockBodyElementID"
    let element = React.useElementRef()
    React.useEffectOnce(fun _ -> element.current <- Some <| Browser.Dom.document.getElementById(id))
    let width = element.current |> Option.map (fun ele -> length.px ele.clientWidth)
    Bulma.field.div [
        prop.id id
        prop.style [ style.display.flex; style.justifyContent.spaceBetween ]
        prop.children [
            termOrUnitizedSwitch model dispatch
            let setter (oaOpt: OntologyAnnotation option) =
                let case = oaOpt |> Option.map (fun oa -> !^oa)
                BuildingBlock.UpdateBodyArg case |> BuildingBlockMsg |> dispatch
            let parent = model.AddBuildingBlockState.TryHeaderOA()
            let input = model.AddBuildingBlockState.TryBodyOA()
            Components.TermSearch.Input(setter, dispatch, fullwidth=true, ?input=input, ?parent'=parent, displayParent=false, ?dropdownWidth=width, alignRight=true)
        ]
    ]

let private SearchBuildingBlockHeaderElement (ui: BuildingBlockUIState) setUi (model: Model) dispatch =
    let state = model.AddBuildingBlockState
    let id = "SearchBuildingBlockHeaderElementID"
    let element = React.useElementRef()
    React.useEffectOnce(fun _ -> element.current <- Some <| Browser.Dom.document.getElementById(id))
    let width = element.current |> Option.map (fun ele -> length.px ele.clientWidth)
    Bulma.field.div [
        prop.id id
        Bulma.field.hasAddons
        prop.style [style.position.relative]
        // Choose building block type dropdown element
        prop.children [
            // Dropdown building block type choice
            Dropdown.Main ui setUi model dispatch
            // Term search field
            if state.HeaderCellType.HasOA() then
                let setter (oaOpt: OntologyAnnotation option) =
                    let case = oaOpt |> Option.map (fun oa -> !^oa)
                    BuildingBlock.UpdateHeaderArg case |> BuildingBlockMsg |> dispatch
                    //selectHeader ui setUi h |> dispatch 
                let input = model.AddBuildingBlockState.TryHeaderOA()
                Components.TermSearch.Input(setter, dispatch, ?input=input, isExpanded=true, fullwidth=true, ?dropdownWidth=width, alignRight=true)
            elif state.HeaderCellType.HasIOType() then
                Bulma.control.div [
                    Bulma.control.isExpanded
                    prop.children [
                        Bulma.control.p [
                            Bulma.input.text [
                                Bulma.color.hasBackgroundGreyLighter
                                prop.readOnly true
                                prop.valueOrDefault (
                                    state.TryHeaderIO()
                                    |> Option.get
                                    |> _.ToString()
                                )
                            ]
                        ]
                    ]
                ]
        ]
    ]

let private addBuildingBlockButton (model: Model) dispatch =
    let state = model.AddBuildingBlockState
    Bulma.field.div [
        Bulma.button.button  [
            let header = Helper.createCompositeHeaderFromState state
            let body = Helper.tryCreateCompositeCellFromState state
            let isValid = Helper.isValidColumn header
            if isValid then
                Bulma.color.isSuccess
                Bulma.button.isActive
            else
                Bulma.color.isDanger
                prop.disabled true
            Bulma.button.isFullWidth
            prop.onClick (fun _ ->
                let column = CompositeColumn.create(header, [|if body.IsSome then body.Value|])
                SpreadsheetInterface.AddAnnotationBlock column |> InterfaceMsg |> dispatch
            )
            prop.text "Add Column"
        ]
    ]

[<ReactComponent>]
let Main (model: Model) dispatch =
    let state_bb, setState_bb = React.useState(BuildingBlockUIState.init)
    //let state_searchHeader, setState_searchHeader = React.useState(TermSearchUIState.init)
    //let state_searchBody, setState_searchBody = React.useState(TermSearchUIState.init)
    mainFunctionContainer [
        SearchBuildingBlockHeaderElement state_bb setState_bb model dispatch
        if model.AddBuildingBlockState.HeaderCellType.IsTermColumn() then
            SearchBuildingBlockBodyElement model dispatch
            //AdvancedSearch.modal_container state_bb setState_bb model dispatch
            //AdvancedSearch.links_container model.AddBuildingBlockState.Header dispatch
        addBuildingBlockButton model dispatch
    ]
