module Renderer.components.CloseWindowController

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Swate.Components
open Swate.Electron.Shared.IPCTypes

type CloseWindowController =

    [<ReactComponent>]
    static member Subscription
        (onConfirmSave: unit -> JS.Promise<Result<unit, string>>, ?onConfirmClose: unit -> unit, ?onCancelClose: unit -> unit)
        =

        let modalIsOpen, setModalIsOpen = React.useState false

        let resolveCloseRequest (decision: SaveBeforeQuitDecision) =
            Api.resolveCloseRequest decision
            |> Promise.map (
                function
                | Ok _ -> ()
                | Microsoft.FSharp.Core.Error exn -> console.error ($"Failed to resolve close request: {exn.Message}")
            )

        let handleCancel () =
            setModalIsOpen false
            onCancelClose |> Option.iter (fun fn -> fn ())
            resolveCloseRequest SaveBeforeQuitDecision.CancelClose |> Promise.start

        let handleCloseWithoutSaving () =
            setModalIsOpen false
            onConfirmClose |> Option.iter (fun fn -> fn ())
            resolveCloseRequest SaveBeforeQuitDecision.CloseWithoutSaving |> Promise.start

        let handleSaveAndClose () =
            promise {
                let! saveResult = onConfirmSave ()

                match saveResult with
                | Ok() ->
                    setModalIsOpen false
                    do! resolveCloseRequest SaveBeforeQuitDecision.SaveAndClose
                | Microsoft.FSharp.Core.Error msg ->
                    console.error ($"Save before close failed: {msg}")
            }
            |> Promise.start

        let saveBeforeQuitHandler: IMainSaveBeforeQuitApi = {
            // This IPC call is triggered by the ArcVaults window close event. It should open the modal to ask user what they want to do.
            requestSaveBeforeQuit = fun () -> setModalIsOpen true
        }

        React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler saveBeforeQuitHandler)

        BaseModal.Modal(
            isOpen = modalIsOpen,
            setIsOpen =
                (fun isOpen ->
                    if not isOpen then
                        handleCancel ()
                ),
            header = Html.text "Close Window",
            description = Html.text "Do you want to save your changes before closing this window?",
            children = Html.none,
            footer =
                Html.div [
                    prop.className "swt:flex swt:gap-2 swt:w-full"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-neutral"
                            prop.text "Cancel"
                            prop.onClick (fun _ -> handleCancel ())
                        ]
                        Html.button [
                            prop.className "swt:btn swt:ml-auto"
                            prop.text "Close"
                            prop.onClick (fun _ -> handleCloseWithoutSaving ())
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-primary"
                            prop.text "Save and Close"
                            prop.onClick (fun _ -> handleSaveAndClose ())
                        ]
                    ]
                ]
        )
