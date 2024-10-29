namespace BuildingBlock

open Feliz
open Feliz.Bulma

open OfficeInterop.Core
open Shared
open ARCtrl.Helper
open ExcelJS.Fable.GlobalBindings

module private CellConvertComponentHelpers =

    let getSelectedCellType (setState: CompositeCellDiscriminate option -> unit) =
        promise {
            //Write function to access current header state of excel

            let! mainColumn = tryGetArcMainColumnFromFrontEnd ()

            let result =
                match mainColumn with
                | Some column when column.Header.isInput -> None
                | Some column when column.Header.isOutput -> None
                | Some column when column.Cells.[0].isUnitized -> CompositeCellDiscriminate.Unitized |> Some
                | Some column when column.Cells.[0].isTerm -> CompositeCellDiscriminate.Term |> Some
                | Some column when column.Cells.[0].isFreeText -> CompositeCellDiscriminate.Text |> Some
                | Some column when column.Cells.[0].isData -> CompositeCellDiscriminate.Data |> Some
                | _ -> None

            setState result
        }

    let getTargetConversionType (cellType: CompositeCellDiscriminate option) =
        if cellType.IsSome then
            match cellType.Value with
            | CompositeCellDiscriminate.Unitized -> CompositeCellDiscriminate.Term |> Some
            | CompositeCellDiscriminate.Term -> CompositeCellDiscriminate.Unitized |> Some
            | CompositeCellDiscriminate.Text -> CompositeCellDiscriminate.Data |> Some
            | CompositeCellDiscriminate.Data -> CompositeCellDiscriminate.Text |> Some
        else None

type CellConvertComponent =

    [<ReactComponent>]
    static member Main () =

        let (state: CompositeCellDiscriminate option), setState = React.useState(None)

        React.useLayoutEffectOnce(fun () ->
            CellConvertComponentHelpers.getSelectedCellType setState
            |> Promise.start
        )

        Html.div [
            
            Bulma.buttons [
                Bulma.button.button [
                    Bulma.color.isSuccess
                    prop.text "Refresh"
                    prop.onClick (fun _ -> CellConvertComponentHelpers.getSelectedCellType setState |> Promise.start)                    
                ]
                Html.div (string state)
            ]
            Bulma.buttons [
                Bulma.button.button [                    
                    if state.IsSome then
                        Bulma.color.isSuccess
                        prop.disabled false
                        prop.text $"Convert {state.Value} to"



                    else
                        Bulma.color.isDanger
                        prop.disabled true
                        prop.text $"Unconvertible"
                    prop.onClick (fun _ ->
                        CellConvertComponentHelpers.getSelectedCellType setState |> ignore
                        convertBuildingBlock () |> Promise.start)                    
                ]
                Html.div (string (CellConvertComponentHelpers.getTargetConversionType state))
            ]
        ]
