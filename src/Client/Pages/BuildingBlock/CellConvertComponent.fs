namespace BuildingBlock

open Feliz
open Feliz.DaisyUI
open Swate.Components.Shared

open OfficeInterop.Core
open ARCtrl

type CellDiscriminateState = {
        SelectedCellState: CompositeCellDiscriminate option
        TargetCellState: CompositeCellDiscriminate option
    } with
        static member init = {
            SelectedCellState = None
            TargetCellState = None
        }

module private CellConvertComponentHelpers =

    let setCellTypes (state: CellDiscriminateState) (setState: CellDiscriminateState -> unit) =
        promise {
            //Write function to access current state of selected excel cell excel
            let! (selectedCellType, targetCellType) = OfficeInterop.Core.Main.tryGetValidConversionCellTypes ()

            setState {
                state with
                    SelectedCellState = selectedCellType
                    TargetCellState = targetCellType
            }
        }

type CellConvertComponent =

    [<ReactComponent>]
    static member Main () =

        let (cellDiscriminateState, setCellDiscriminateState) = React.useState(CellDiscriminateState.init)

        Html.div [

            Html.div [
                Daisy.button.button [
                    button.success
                    prop.text "Refresh"
                    prop.onClick (fun _ ->
                        CellConvertComponentHelpers.setCellTypes cellDiscriminateState setCellDiscriminateState |> Promise.start
                    )
                ]
                Html.div (string cellDiscriminateState.SelectedCellState)
            ]
            Html.div [
                Daisy.button.button [
                    if cellDiscriminateState.TargetCellState.IsSome then
                        button.success
                        prop.disabled false
                        prop.text $"Convert {cellDiscriminateState.SelectedCellState.Value} to"
                    else
                        button.error
                        prop.disabled true
                        prop.text $"Unconvertible"
                    prop.onClick (fun _ ->
                        CellConvertComponentHelpers.setCellTypes cellDiscriminateState setCellDiscriminateState |> Promise.start
                        OfficeInterop.Core.Main.convertBuildingBlock () |> Promise.start)
                ]
                Html.div (string cellDiscriminateState.TargetCellState)
            ]
        ]
