namespace BuildingBlock

open Feliz
open Feliz.Bulma

open OfficeInterop.Core

module private CellConvertComponentHelpers =

    let getSelectedCellType (setState: Model.BuildingBlock.BodyCellType option -> unit) =
        promise {
            //Write function to access current state of selected excel cell excel
            let! cellType = getSelectedCellType ()

            let result =
                match cellType with
                | Some BodyCellType.Unitized -> Some Model.BuildingBlock.BodyCellType.Unitized
                | Some BodyCellType.Term -> Some Model.BuildingBlock.BodyCellType.Term
                | Some BodyCellType.Text -> Some Model.BuildingBlock.BodyCellType.Text
                | Some BodyCellType.Data -> Some Model.BuildingBlock.BodyCellType.Data
                | _ -> None
            setState result
        }

    let getTargetConversionType (cellType: Model.BuildingBlock.BodyCellType option) =
        if cellType.IsSome then
            match cellType.Value with
            | Model.BuildingBlock.BodyCellType.Unitized -> Some Model.BuildingBlock.BodyCellType.Term
            | Model.BuildingBlock.BodyCellType.Term -> Some Model.BuildingBlock.BodyCellType.Unitized
            | Model.BuildingBlock.BodyCellType.Text -> Some Model.BuildingBlock.BodyCellType.Data
            | Model.BuildingBlock.BodyCellType.Data -> Some Model.BuildingBlock.BodyCellType.Text
        else None

type CellConvertComponent =

    [<ReactComponent>]
    static member Main () =

        let (state: Model.BuildingBlock.BodyCellType option), setState = React.useState(None)

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
