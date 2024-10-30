namespace BuildingBlock

open Feliz
open Feliz.Bulma

open OfficeInterop.Core
open Shared

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
            let! (selectedCellType, targetCellType) = getValidConversionCellTypes ()

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
            
            Bulma.buttons [
                Bulma.button.button [
                    Bulma.color.isSuccess
                    prop.text "Refresh"
                    prop.onClick (fun _ ->
                        CellConvertComponentHelpers.setCellTypes cellDiscriminateState setCellDiscriminateState |> Promise.start
                    )                    
                ]
                Html.div (string cellDiscriminateState.SelectedCellState)
            ]
            Bulma.buttons [
                Bulma.button.button [                    
                    if cellDiscriminateState.TargetCellState.IsSome then
                        Bulma.color.isSuccess
                        prop.disabled false
                        prop.text $"Convert {cellDiscriminateState.SelectedCellState.Value} to"
                    else
                        Bulma.color.isDanger
                        prop.disabled true
                        prop.text $"Unconvertible"
                    prop.onClick (fun _ ->
                        CellConvertComponentHelpers.setCellTypes cellDiscriminateState setCellDiscriminateState |> Promise.start
                        convertBuildingBlock () |> Promise.start)                    
                ]
                Html.div (string cellDiscriminateState.TargetCellState)
            ]
        ]
