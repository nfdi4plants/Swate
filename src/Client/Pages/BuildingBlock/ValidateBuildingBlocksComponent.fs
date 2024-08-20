namespace BuildingBlock

open Feliz
open Feliz.Bulma

open Messages

type ValidateBuildingBlocksComponent =

    [<ReactComponent>]
    static member Main (dispatch:Msg -> unit) =

        Html.div [
            
            Bulma.buttons [
                Bulma.button.button [
                    Bulma.color.isSuccess
                    prop.text "Validate"
                    prop.onClick (fun _ -> SpreadsheetInterface.ValidateBuildingBlock |> InterfaceMsg |> dispatch)                    
                ]
                Html.div ("Validate selected Building Block and Building Blocks next to it")
            ]
        ]
