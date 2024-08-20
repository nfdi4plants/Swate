module BuildingBlock.Core

open Model
open Messages
open Messages.BuildingBlock

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
            if Helper.isSameMajorHeaderCellType state.HeaderCellType next then
                { state with 
                    HeaderCellType = next 
                }
            else
                let nextBodyCellType = if next.IsTermColumn() then BuildingBlock.BodyCellType.Term else BuildingBlock.BodyCellType.Text
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
                BodyCellType = BuildingBlock.BodyCellType.Text
        }
        nextState, Cmd.none
    | UpdateBodyCellType next ->
        let nextState = { state with BodyCellType = next }
        nextState, Cmd.none

open Feliz
open Feliz.Bulma

//let addUnitToExistingBlockElements (model:Model) (dispatch:Messages.Msg -> unit) =
//    mainFunctionContainer [
//        Bulma.field.div [
//            Bulma.button.button [
                
//                let isValid = model.AddBuildingBlockState.Unit2TermSearchText <> ""
//                Bulma.color.isSuccess
//                if isValid then
//                    Bulma.button.isActive
//                else
//                    Bulma.color.isDanger
//                    prop.disabled true
//                Bulma.button.isFullWidth
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
    Html.div [
        prop.onSubmit (fun e -> e.preventDefault())
        prop.onKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
        prop.children [
            pageHeader "Building Blocks"

            // Input forms, etc related to add building block.
            Bulma.label "Add annotation building blocks (columns) to the annotation table."
            mainFunctionContainer [
                SearchComponent.Main model dispatch
            ]
            if model.PersistentStorageState.Host.IsSome && model.PersistentStorageState.Host.Value = Swatehost.Excel then
                // Validate selected building block and those next to it.
                mainFunctionContainer [
                    ValidateBuildingBlocksComponent.Main dispatch
                ]
                // Input forms, etc related to add building block.
                Bulma.label "Convert existing Building Block."
                mainFunctionContainer [
                    CellConvertComponent.Main ()
                ]
        ]
    ]