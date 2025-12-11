module Renderer.components.InitState

open Feliz
open Swate.Components

module private InitStateHelper =
    let openARC setAppState =
        fun () -> promise {
            let! r = Api.startUpApi.openARC ()

            match r with
            | Error e -> failwith e.Message
            | Ok v -> Swate.Electron.Shared.AppState.ARC v |> setAppState
        }

open InitStateHelper

[<ReactComponent>]
let InitState () =
    let appStateCtx = React.useContext (Renderer.context.AppStateCtx.AppStateCtx)

    CardGrid.CardGrid(
        React.Fragment [
            CardGrid.CardGridButton(
                Html.i [
                    prop.className "swt:iconify swt:fluent--folder-open-24-filled"
                ],
                "Open ARC",
                "Open a locally existing ARC!",
                (openARC appStateCtx.setState >> Promise.start)
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