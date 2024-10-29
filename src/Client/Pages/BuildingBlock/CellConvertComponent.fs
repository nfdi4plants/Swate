namespace BuildingBlock

open Feliz
open Feliz.Bulma

open OfficeInterop.Core
open Shared

module private CellConvertComponentHelpers =

    let getSelectedCellType (setState: CompositeCellDiscriminate option -> unit) =
        promise {
            //Write function to access current state of selected excel cell excel
            let! cellType = getSelectedCellType ()

            setState cellType
        }

    let getTargetConversionType (cellType: CompositeCellDiscriminate option) =
        if cellType.IsSome then
            match cellType.Value with
            | CompositeCellDiscriminate.Unitized -> Some CompositeCellDiscriminate.Term
            | CompositeCellDiscriminate.Term -> Some CompositeCellDiscriminate.Unitized
            | CompositeCellDiscriminate.Text -> Some CompositeCellDiscriminate.Data
            | CompositeCellDiscriminate.Data -> Some CompositeCellDiscriminate.Text
        else None

type CellConvertComponent =

    [<ReactComponent>]
    static member Main () =

        let (state: CompositeCellDiscriminate option), setState = React.useState(None)

        React.useEffectOnce(fun () ->
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
