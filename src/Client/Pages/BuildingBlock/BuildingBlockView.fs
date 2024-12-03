module BuildingBlock.Core

open Model
open Messages
open Messages.BuildingBlock
open Shared
open Components

open Elmish

let update (addBuildingBlockMsg:BuildingBlock.Msg) (state: BuildingBlock.Model) : BuildingBlock.Model * Cmd<Messages.Msg> =
    match addBuildingBlockMsg with
    | UpdateBodyArg next ->
        let nextState = { state with BodyArg = next }
        nextState, Cmd.none
    | UpdateHeaderArg next ->
        let nextState = { state with HeaderArg = next}
        nextState, Cmd.none
    | UpdateHeaderCellType next ->
        let nextState =
            if Helper.isSameMajorCompositeHeaderDiscriminate state.HeaderCellType next then
                { state with
                    HeaderCellType = next
                }
            else
                let nextBodyCellType = if next.IsTermColumn() then CompositeCellDiscriminate.Term else CompositeCellDiscriminate.Text
                { state with
                    HeaderCellType = next
                    BodyCellType = nextBodyCellType
                    HeaderArg = None
                    BodyArg = None
                }
        nextState, Cmd.none
    | UpdateHeaderWithIO (hct, iotype) ->
        let nextState = {
            state with
                HeaderCellType = hct
                HeaderArg = Some (Fable.Core.U2.Case2 iotype)
                BodyArg = None
                BodyCellType = CompositeCellDiscriminate.Text
        }
        nextState, Cmd.none
    | UpdateBodyCellType next ->
        let nextState = { state with BodyCellType = next }
        nextState, Cmd.none

open Feliz
open Feliz.DaisyUI

//let addUnitToExistingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
//    mainFunctionContainer [
//        Bulma.field.div [
//            Daisy.button.button [

//                let isValid = model.AddBuildingBlockState.Unit2TermSearchText <> ""
//                Bulma.color.isSuccess
//                if isValid then
//                    Daisy.button.isActive
//                else
//                    button.error
//                    prop.disabled true
//                button.block
//                prop.onClick (fun _ ->
//                    let unitTerm =
//                        if model.AddBuildingBlockState.Unit2SelectedTerm.IsSome then Some <| TermMinimal.ofTerm model.AddBuildingBlockState.Unit2SelectedTerm.Value else None
//                    match model.AddBuildingBlockState.Unit2TermSearchText with
//                    | "" ->
//                        curry GenericLog Cmd.none ("Error", "Cannot execute function with empty unit input") |> DevMsg |> dispatch
//                    | hasUnitTerm when model.AddBuildingBlockState.Unit2SelectedTerm.IsSome ->
//                        OfficeInterop.UpdateUnitForCells unitTerm.Value |> OfficeInteropMsg |> dispatch
//                    | freeText ->
//                        OfficeInterop.UpdateUnitForCells (TermMinimal.create model.AddBuildingBlockState.Unit2TermSearchText "") |> OfficeInteropMsg |> dispatch
//                )
//                prop.text "Update unit for cells"
//            ]
//        ]
//    ]

let addBuildingBlockComponent (model:Model) (dispatch:Messages.Msg -> unit) =
    SidebarComponents.SidebarLayout.Container [
        SidebarComponents.SidebarLayout.Header "Building Blocks"

        // Input forms, etc related to add building block.
        SidebarComponents.SidebarLayout.Description "Add annotation building blocks (columns) to the annotation table."
        Components.LogicContainer [
            SearchComponent.Main model dispatch
        ]
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            Html.p "Convert existing Building Block."
            Components.LogicContainer [
                CellConvertComponent.Main ()
            ]
        | _ -> Html.none
    ]