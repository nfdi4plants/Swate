module Renderer.Components.CloseWindowController


open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

type CloseWindowController =

    [<ReactComponent>]
    static member Subscription
        (
            ?onConfirmSave: unit -> JS.Promise<Result<unit, exn>>,
            ?onConfirmClose: unit -> unit,
            ?onCancelClose: unit -> unit
        ) =

        let modalIsOpen, setModalIsOpen = React.useState false
        let errorModal = ErrorModal.Context.useErrorModalCtx ()
        let arcScopeId = useCurrentArcScopeId ()

        let enqueueCloseError (title: string) (saveError: exn) =
            errorModal.enqueue (ErrorModalRequest.create(saveError.Message, title = title, ?scopeId = arcScopeId))

        let resolveCloseRequest (decision: SaveBeforeQuitDecision) =
            Api.ipcArcVaultApi.resolveCloseRequest (unbox null) decision

        let handleCancel () =
            promise {
                match! resolveCloseRequest SaveBeforeQuitDecision.CancelClose with
                | Ok() ->
                    setModalIsOpen false
                    onCancelClose |> Option.iter (fun fn -> fn ())
                | Error resolveError -> enqueueCloseError "Could not cancel close request" resolveError
            }
            |> Promise.start

        let handleCloseWithoutSaving () =
            promise {
                match! resolveCloseRequest SaveBeforeQuitDecision.CloseWithoutSaving with
                | Ok() ->
                    setModalIsOpen false
                    onConfirmClose |> Option.iter (fun fn -> fn ())
                | Error resolveError -> enqueueCloseError "Could not close window" resolveError
            }
            |> Promise.start

        let saveBeforeClose () : JS.Promise<Result<unit, exn>> =
            if onConfirmSave.IsSome then
                onConfirmSave.Value()
            else
                promise { return Ok() }

        let handleSaveAndClose () =
            promise {
                let! saveResult = saveBeforeClose ()

                match saveResult with
                | Ok() ->
                    match! resolveCloseRequest SaveBeforeQuitDecision.SaveAndClose with
                    | Ok() -> setModalIsOpen false
                    | Error resolveError -> enqueueCloseError "Could not save and close window" resolveError
                | Error saveError -> enqueueCloseError "Save before close failed" saveError
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
