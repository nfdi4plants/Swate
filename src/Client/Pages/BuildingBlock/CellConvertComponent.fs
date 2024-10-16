namespace BuildingBlock

open Feliz
open Feliz.Bulma

open OfficeInterop.Core

module private CellConvertComponentHelpers =

    let getSelectedCellType (setState: Model.BuildingBlock.BodyCellType option -> unit) =

        promise {
            //Write function to access current header state of excel

            let! mainColumn = getArcMainColumn ()

            let result =
                match mainColumn with
                | x when x.Header.isInput -> None
                | x when x.Header.isOutput -> None
                | x when x.Cells.[0].isUnitized -> Model.BuildingBlock.BodyCellType.Unitized |> Some
                | x when x.Cells.[0].isTerm -> Model.BuildingBlock.BodyCellType.Term |> Some
                | x when x.Cells.[0].isFreeText -> Model.BuildingBlock.BodyCellType.Text |> Some
                | x when x.Cells.[0].isData -> Model.BuildingBlock.BodyCellType.Data |> Some
                | _ -> None

            setState result
        }

    let getTargetConversionType (cellType: Model.BuildingBlock.BodyCellType option) =
        if cellType.IsSome then
            match cellType.Value with
            | Model.BuildingBlock.BodyCellType.Unitized -> Model.BuildingBlock.BodyCellType.Term |> Some
            | Model.BuildingBlock.BodyCellType.Term -> Model.BuildingBlock.BodyCellType.Unitized |> Some
            | Model.BuildingBlock.BodyCellType.Text -> Model.BuildingBlock.BodyCellType.Data |> Some
            | Model.BuildingBlock.BodyCellType.Data -> Model.BuildingBlock.BodyCellType.Text |> Some
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
