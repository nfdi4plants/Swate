module Renderer.components.InitState

open Feliz
open Swate.Components

module private InitStateHelper =
    let openARC =
        fun () -> promise {
            let! r = Api.arcVaultApi.openARC Fable.Core.JS.undefined

            match r with
            | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
            | Ok _ -> ()
        }

open InitStateHelper

[<ReactComponent>]
let InitState () =

    CardGrid.CardGrid(
        React.Fragment [
            CardGrid.CardGridButton(
                Html.i [
                    prop.className "swt:iconify swt:fluent--folder-open-24-filled"
                ],
                "Open ARC",
                "Open a locally existing ARC!",
                (openARC >> Promise.start)
            )
            CardGrid.CardGridButton(
                Html.i [
                    prop.className "swt:iconify swt:fluent--folder-open-24-filled"
                ],
                "New ARC",
                "Create a new ARC!",
                (fun _ -> console.log "Click")
            )
        ],
        gridClassName = "swt:grid swt:grid-cols-2"
    )